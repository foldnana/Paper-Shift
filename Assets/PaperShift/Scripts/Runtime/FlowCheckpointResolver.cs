using System;
using PaperShift.Data;
using PaperShift.Domain;

namespace PaperShift.Runtime
{
    public sealed class FlowCheckpointResolver
    {
        private readonly PaperShiftGameService service;
        private readonly PaperShiftDatabase database;
        private readonly ConditionEvaluator conditions;
        private readonly FitProfileResolver fitProfiles;
        private readonly FlowRuleResolver flowRules;
        private readonly Func<Random> randomProvider;

        public FlowCheckpointResolver(
            PaperShiftGameService service,
            PaperShiftDatabase database,
            ConditionEvaluator conditions,
            FitProfileResolver fitProfiles,
            FlowRuleResolver flowRules,
            Func<Random> randomProvider)
        {
            this.service = service;
            this.database = database;
            this.conditions = conditions;
            this.fitProfiles = fitProfiles;
            this.flowRules = flowRules;
            this.randomProvider = randomProvider;
        }

        private Random Random
        {
            get { return randomProvider == null ? null : randomProvider(); }
        }

        public InterviewStepResult ResolveInterviewStep(PaperShiftRunState state, FlowCheckpointAction action)
        {
            return ToInterviewStepResult(Resolve(state, action));
        }

        public ProbationStepResult ResolveProbationStep(PaperShiftRunState state, FlowCheckpointAction action)
        {
            return ToProbationStepResult(Resolve(state, action));
        }

        public FlowCheckpointResult Resolve(PaperShiftRunState state, FlowCheckpointAction action)
        {
            return Resolve(state, action, null, null);
        }

        public FlowCheckpointResult Resolve(PaperShiftRunState state, FlowCheckpointAction action, GameEventPhase? phaseOverride, FlowRuleResult extraFlow)
        {
            var phase = phaseOverride.HasValue ? phaseOverride.Value : PhaseFor(action);
            var snapshot = FlowCheckpointSnapshot.Capture(state, phase);
            if (!TryResolveCheckpointTarget(state, action, phase, out var company, out var job, out var invalidMessage))
            {
                return FlowCheckpointResult.Failed(action, phase, invalidMessage, snapshot);
            }

            if (phase == GameEventPhase.Probation)
            {
                state.Phase = PaperShiftPhase.Probation;
            }

            var profile = fitProfiles.Build(state);
            var flow = flowRules.Evaluate(state, company, job, profile, phase, Random, action);
            MergeFlowResult(flow, extraFlow, preferSourceDirective: true);
            ApplyActionEffect(action, state, job, profile, flow);
            ApplyFlowResult(state, flow, phase);
            snapshot.CaptureAfter(state, phase);

            var directed = ResolveDirectedOutcome(state, action, phase, company, job, flow, snapshot);
            if (directed != null)
            {
                return directed;
            }

            if (ShouldTriggerCheckpointEvent(action))
            {
                var triggered = service.ResolveTriggeredEventForCheckpoint(state, phase, company, job, flow, action, CheckpointEventChance(action, state));
                if (triggered != null)
                {
                    return FlowCheckpointResult.Event(action, phase, WithCheckpointSource(triggered, action, phase), snapshot);
                }
            }

            var momentText = ApplyFlowMoment(state, action, company, job, phase, flow);
            if (momentText != null)
            {
                snapshot.CaptureAfter(state, phase);
                directed = ResolveDirectedOutcome(state, action, phase, company, job, flow, snapshot);
                if (directed != null)
                {
                    return directed;
                }
            }

            return ResolveNaturalOutcome(state, action, phase, company, job, snapshot, momentText);
        }

        internal FlowCheckpointResult ResolveNaturalOnly(PaperShiftRunState state, FlowCheckpointAction action, GameEventPhase phase)
        {
            var snapshot = FlowCheckpointSnapshot.Capture(state, phase);
            if (!TryResolveCheckpointTarget(state, action, phase, out var company, out var job, out var invalidMessage))
            {
                return FlowCheckpointResult.Failed(action, phase, invalidMessage, snapshot);
            }

            snapshot.CaptureAfter(state, phase);
            return ResolveNaturalOutcome(state, action, phase, company, job, snapshot, null);
        }

        private bool TryResolveCheckpointTarget(
            PaperShiftRunState state,
            FlowCheckpointAction action,
            GameEventPhase phase,
            out CompanyDefinition company,
            out JobDefinition job,
            out string invalidMessage)
        {
            company = null;
            job = null;
            invalidMessage = string.Empty;
            if (state == null)
            {
                invalidMessage = "当前流程数据丢失。";
                return false;
            }

            if (phase == GameEventPhase.Interview)
            {
                company = database.FindCompany(state.Interview.CompanyId);
                job = database.FindJob(state.Interview.CompanyId, state.Interview.JobId);
                if (company != null && job != null)
                {
                    return true;
                }

                invalidMessage = action == FlowCheckpointAction.PrepareInterview ? "还没有可以准备的面试。" : "还没有可以参加的面试。";
                return false;
            }

            if (phase != GameEventPhase.Probation)
            {
                invalidMessage = "当前事件没有可结算的面试或试用期目标。";
                return false;
            }

            if (!state.HasActiveJob)
            {
                invalidMessage = action == FlowCheckpointAction.ApplyRegularization ? "当前没有可以申请入职的试用期。" : "当前没有正在进行的试用期。";
                return false;
            }

            company = database.FindCompany(state.CurrentJob.CompanyId);
            job = database.FindJob(state.CurrentJob.CompanyId, state.CurrentJob.JobId);
            if (company != null && job != null)
            {
                return true;
            }

            invalidMessage = "试用期岗位数据丢失，已回到求职状态。";
            ReturnToJobSearch(state, invalidMessage);
            return false;
        }

        private void ApplyActionEffect(
            FlowCheckpointAction action,
            PaperShiftRunState state,
            JobDefinition job,
            FitProfile profile,
            FlowRuleResult flow)
        {
            switch (action)
            {
                case FlowCheckpointAction.PrepareInterview:
                    flow.RecognitionDelta += InterviewPreparationDelta(state, job, profile);
                    flow.StressDelta += 1;
                    break;
                case FlowCheckpointAction.AttendInterview:
                    flow.RecognitionDelta += InterviewActionDelta(state, job, profile);
                    flow.StressDelta += Math.Max(1, job.Difficulty / 18);
                    break;
                case FlowCheckpointAction.WorkProbation:
                    flow.RecognitionDelta += ProbationActionDelta(state, job, profile);
                    flow.StressDelta += Math.Max(2, state.CurrentJob.Intensity / 14);
                    break;
                case FlowCheckpointAction.ApplyRegularization:
                    flow.RecognitionDelta += ProbationApplyDelta(state, job, profile);
                    flow.StressDelta += 3;
                    break;
                case FlowCheckpointAction.EventChoice:
                    break;
            }
        }

        private string ApplyFlowMoment(
            PaperShiftRunState state,
            FlowCheckpointAction action,
            CompanyDefinition company,
            JobDefinition job,
            GameEventPhase phase,
            FlowRuleResult flow)
        {
            var moment = PickFlowMoment(state, action, company, job);
            if (moment == null)
            {
                return null;
            }

            var momentFlow = new FlowRuleResult();
            FlowRuleResolver.ApplyEffects(momentFlow, moment.Effects);
            var momentText = Fallback(moment.Text, moment.DisplayName);
            if (momentFlow.Directive != FlowDirective.None && string.IsNullOrEmpty(momentFlow.DirectiveMessage))
            {
                momentFlow.DirectiveMessage = momentText;
            }

            MergeFlowResult(flow, momentFlow, preferSourceDirective: false);
            ApplyFlowResult(state, momentFlow, phase);
            return momentText;
        }

        private FlowMomentDefinition PickFlowMoment(PaperShiftRunState state, FlowCheckpointAction action, CompanyDefinition company, JobDefinition job)
        {
            if (database.FlowMoments == null || database.FlowMoments.Length == 0)
            {
                return null;
            }

            var picker = new WeightedPicker<FlowMomentDefinition>();
            for (var i = 0; i < database.FlowMoments.Length; i++)
            {
                var moment = database.FlowMoments[i];
                if (moment == null || !ActionMatches(moment.Action, action))
                {
                    continue;
                }

                if (!conditions.AreMet(moment.Conditions, state, company, job, Random, action))
                {
                    continue;
                }

                picker.Add(moment, moment.BaseWeight);
            }

            return picker.Pick(Random);
        }

        private FlowCheckpointResult ResolveDirectedOutcome(
            PaperShiftRunState state,
            FlowCheckpointAction action,
            GameEventPhase phase,
            CompanyDefinition company,
            JobDefinition job,
            FlowRuleResult flow,
            FlowCheckpointSnapshot snapshot)
        {
            if (flow == null || flow.Directive == FlowDirective.None)
            {
                return null;
            }

            switch (flow.Directive)
            {
                case FlowDirective.DirectPass:
                    return PassCheckpoint(state, action, phase, company, job, snapshot, Fallback(flow.DirectiveMessage, PassMessage(action, company, job)));
                case FlowDirective.DirectFail:
                    return FailCheckpoint(state, action, phase, snapshot, Fallback(flow.DirectiveMessage, FailMessage(action)));
                case FlowDirective.ReturnToJobSearch:
                    return FailCheckpoint(state, action, phase, snapshot, Fallback(flow.DirectiveMessage, "这份机会到此结束，已回到求职状态。"));
                case FlowDirective.EndRun:
                    EndRunFromCheckpoint(state, flow);
                    return FlowCheckpointResult.Failed(action, phase, state.Retirement.ReasonText, snapshot);
                default:
                    return null;
            }
        }

        private FlowCheckpointResult ResolveNaturalOutcome(
            PaperShiftRunState state,
            FlowCheckpointAction action,
            GameEventPhase phase,
            CompanyDefinition company,
            JobDefinition job,
            FlowCheckpointSnapshot snapshot,
            string momentText)
        {
            switch (action)
            {
                case FlowCheckpointAction.PrepareInterview:
                    return ContinueCheckpoint(state, action, phase, snapshot, momentText);
                case FlowCheckpointAction.AttendInterview:
                    if (state.Interview.Recognition >= state.Interview.OfferThreshold)
                    {
                        return PassCheckpoint(state, action, phase, company, job, snapshot, PassMessage(action, company, job));
                    }

                    if (state.Interview.Recognition <= 8)
                    {
                        return FailCheckpoint(state, action, phase, snapshot, "面试失败。对方认为匹配度不够，你还在求职状态。");
                    }

                    return ContinueCheckpoint(state, action, phase, snapshot, momentText);
                case FlowCheckpointAction.WorkProbation:
                    if (state.CurrentJob.Recognition <= 0)
                    {
                        return FailCheckpoint(state, action, phase, snapshot, "试用表现没有得到认可，已自动继续寻找下一家公司。");
                    }

                    return ContinueCheckpoint(state, action, phase, snapshot, momentText);
                case FlowCheckpointAction.ApplyRegularization:
                    if (state.CurrentJob.Recognition <= 0)
                    {
                        return FailCheckpoint(state, action, phase, snapshot, "申请入职没有通过，已自动继续寻找下一家公司。");
                    }

                    if (RollPercent(state.CurrentJob.Recognition))
                    {
                        return PassCheckpoint(state, action, phase, company, job, snapshot, "申请入职成功，正式入职。这一代结算。");
                    }

                    return FailCheckpoint(state, action, phase, snapshot, "申请入职没有通过，已自动继续寻找下一家公司。");
                case FlowCheckpointAction.EventChoice:
                    return ResolveEventChoiceNaturalOutcome(state, phase, company, job, snapshot, momentText);
                default:
                    return ContinueCheckpoint(state, action, phase, snapshot, momentText);
            }
        }

        private FlowCheckpointResult ResolveEventChoiceNaturalOutcome(
            PaperShiftRunState state,
            GameEventPhase phase,
            CompanyDefinition company,
            JobDefinition job,
            FlowCheckpointSnapshot snapshot,
            string momentText)
        {
            if (phase == GameEventPhase.Interview)
            {
                if (state.Interview.Recognition >= state.Interview.OfferThreshold)
                {
                    return PassCheckpoint(state, FlowCheckpointAction.EventChoice, phase, company, job, snapshot, PassMessage(FlowCheckpointAction.AttendInterview, company, job));
                }

                if (state.Interview.Recognition <= 8)
                {
                    return FailCheckpoint(state, FlowCheckpointAction.EventChoice, phase, snapshot, "事件后，面试机会已经失去，已自动继续寻找下一家公司。");
                }
            }
            else if (phase == GameEventPhase.Probation && state.CurrentJob.Recognition <= 0)
            {
                return FailCheckpoint(state, FlowCheckpointAction.EventChoice, phase, snapshot, "事件后，这份试用机会已经失败，已自动继续寻找下一家公司。");
            }

            return ContinueCheckpoint(state, FlowCheckpointAction.EventChoice, phase, snapshot, momentText);
        }

        private FlowCheckpointResult ContinueCheckpoint(PaperShiftRunState state, FlowCheckpointAction action, GameEventPhase phase, FlowCheckpointSnapshot snapshot, string momentText)
        {
            var message = Fallback(momentText, ContinueMessage(action));
            state.AddLog(message);
            return FlowCheckpointResult.Continue(action, phase, message, snapshot);
        }

        private FlowCheckpointResult PassCheckpoint(
            PaperShiftRunState state,
            FlowCheckpointAction action,
            GameEventPhase phase,
            CompanyDefinition company,
            JobDefinition job,
            FlowCheckpointSnapshot snapshot,
            string message)
        {
            state.AddLog(message, EventNoticeType.Banner);
            if (phase == GameEventPhase.Interview)
            {
                state.Interview.Recognition = Math.Max(state.Interview.Recognition, state.Interview.OfferThreshold);
                snapshot.CaptureAfter(state, phase);
                service.StartProbation(state);
                return FlowCheckpointResult.Passed(action, phase, message, snapshot);
            }

            service.CompleteGenerationByHire(state);
            return FlowCheckpointResult.Passed(action, phase, message, snapshot);
        }

        private FlowCheckpointResult FailCheckpoint(PaperShiftRunState state, FlowCheckpointAction action, GameEventPhase phase, FlowCheckpointSnapshot snapshot, string message)
        {
            state.AddLog(message, EventNoticeType.Banner);
            ReturnToJobSearch(state, null);
            return FlowCheckpointResult.Failed(action, phase, message, snapshot);
        }

        private static void ReturnToJobSearch(PaperShiftRunState state, string message)
        {
            if (state == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(message))
            {
                state.AddLog(message, EventNoticeType.Banner);
            }

            state.CurrentJob = new CurrentJobState();
            state.Interview = new InterviewState();
            state.Phase = PaperShiftPhase.Interview;
        }

        private void EndRunFromCheckpoint(PaperShiftRunState state, FlowRuleResult flow)
        {
            if (state == null)
            {
                return;
            }

            state.Phase = PaperShiftPhase.Retirement;
            state.Retirement.Reason = flow.EndReason == RunEndReason.None ? RunEndReason.Custom : flow.EndReason;
            state.Retirement.ReasonText = Fallback(flow.DirectiveMessage, PaperShiftGameService.ReasonTextForCheckpoint(state.Retirement.Reason));
            state.Retirement.FinalSavings = 0;
            state.Retirement.WorkYears = state.CurrentJob.WorkYears;
            state.Retirement.FinalJobTitle = state.CurrentJob.JobTitle;
            service.EnsureHeirsForCheckpoint(state);
            state.AddLog(state.Retirement.ReasonText, EventNoticeType.Banner);
        }

        private static GameEventPhase PhaseFor(FlowCheckpointAction action)
        {
            return action == FlowCheckpointAction.PrepareInterview || action == FlowCheckpointAction.AttendInterview
                ? GameEventPhase.Interview
                : GameEventPhase.Probation;
        }

        private static bool ActionMatches(string actionText, FlowCheckpointAction action)
        {
            if (string.IsNullOrEmpty(actionText))
            {
                return true;
            }

            var parts = actionText.Split(',');
            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i].Trim();
                if (string.IsNullOrEmpty(part))
                {
                    continue;
                }

                if (Enum.TryParse(part, true, out FlowCheckpointAction parsed) && parsed == action)
                {
                    return true;
                }

                switch (part.ToLowerInvariant())
                {
                    case "prepare":
                    case "prepare_interview":
                    case "interview_prepare":
                        if (action == FlowCheckpointAction.PrepareInterview)
                        {
                            return true;
                        }

                        break;
                    case "attend":
                    case "interview":
                    case "attend_interview":
                    case "interview_attend":
                        if (action == FlowCheckpointAction.AttendInterview)
                        {
                            return true;
                        }

                        break;
                    case "work":
                    case "probation":
                    case "work_probation":
                    case "probation_work":
                        if (action == FlowCheckpointAction.WorkProbation)
                        {
                            return true;
                        }

                        break;
                    case "apply":
                    case "regularization":
                    case "apply_regularization":
                    case "regularize":
                        if (action == FlowCheckpointAction.ApplyRegularization)
                        {
                            return true;
                        }

                        break;
                    case "event":
                    case "event_choice":
                    case "choice":
                        if (action == FlowCheckpointAction.EventChoice)
                        {
                            return true;
                        }

                        break;
                }
            }

            return false;
        }

        private static TriggeredEvent WithCheckpointSource(TriggeredEvent triggered, FlowCheckpointAction action, GameEventPhase phase)
        {
            return triggered == null ? null : new TriggeredEvent(triggered.Event, triggered.Options, action, phase);
        }

        private static bool ShouldTriggerCheckpointEvent(FlowCheckpointAction action)
        {
            return action != FlowCheckpointAction.PrepareInterview;
        }

        private static int CheckpointEventChance(FlowCheckpointAction action, PaperShiftRunState state)
        {
            if (state == null)
            {
                return 0;
            }

            var stress = state.Worker == null ? 0 : state.Worker.Stress;
            var resumeRisk = state.Resume == null ? 0 : state.Resume.DeceptionRisk;
            var recognition = state.CurrentJob == null ? 0 : state.CurrentJob.Recognition;
            switch (action)
            {
                case FlowCheckpointAction.AttendInterview:
                    return Clamp(35 + resumeRisk / 3 + stress / 8, 15, 85);
                case FlowCheckpointAction.WorkProbation:
                    return Clamp(35 + stress / 5 + Math.Max(0, 55 - recognition) / 3, 15, 80);
                case FlowCheckpointAction.ApplyRegularization:
                    return RegularizationEventChance(state);
                case FlowCheckpointAction.EventChoice:
                    return 25;
                default:
                    return 0;
            }
        }

        private static int RegularizationEventChance(PaperShiftRunState state)
        {
            var recognition = state == null || state.CurrentJob == null ? 0 : state.CurrentJob.Recognition;
            var stress = state == null || state.Worker == null ? 0 : state.Worker.Stress;
            var resumeRisk = state == null || state.Resume == null ? 0 : state.Resume.DeceptionRisk;
            var chance = 65 - recognition * 3 / 5;
            chance += stress / 8;
            chance += resumeRisk / 8;

            if (stress >= 90)
            {
                chance += 20;
            }

            return Clamp(chance, 3, 80);
        }

        private static string ContinueMessage(FlowCheckpointAction action)
        {
            switch (action)
            {
                case FlowCheckpointAction.PrepareInterview:
                    return "他又把这次机会往前推了一点。";
                case FlowCheckpointAction.AttendInterview:
                    return "这次沟通没有立刻定结果，但对方留下了新的印象。";
                case FlowCheckpointAction.WorkProbation:
                    return "这一天的表现被看在眼里，试用期还在继续。";
                case FlowCheckpointAction.ApplyRegularization:
                    return "申请递上去后，对方还没有立刻拍板。";
                case FlowCheckpointAction.EventChoice:
                    return "这个选择的影响开始慢慢显现。";
                default:
                    return "流程继续推进。";
            }
        }

        private static string PassMessage(FlowCheckpointAction action, CompanyDefinition company, JobDefinition job)
        {
            switch (action)
            {
                case FlowCheckpointAction.PrepareInterview:
                case FlowCheckpointAction.AttendInterview:
                    var companyName = company == null ? "公司" : company.DisplayName;
                    var jobName = job == null ? "岗位" : job.DisplayName;
                    return "面试通过，" + companyName + " 发来了 Offer，进入 " + jobName + " 试用期。";
                default:
                    return "申请入职成功，正式入职。这一代结算。";
            }
        }

        private static string FailMessage(FlowCheckpointAction action)
        {
            switch (action)
            {
                case FlowCheckpointAction.PrepareInterview:
                    return "准备不足，这次机会没有继续推进。";
                case FlowCheckpointAction.AttendInterview:
                    return "面试失败。对方认为匹配度不够，你还在求职状态。";
                case FlowCheckpointAction.WorkProbation:
                    return "试用表现没有得到认可，已自动继续寻找下一家公司。";
                case FlowCheckpointAction.ApplyRegularization:
                    return "申请入职没有通过，已自动继续寻找下一家公司。";
                default:
                    return "这次机会失败，已回到求职状态。";
            }
        }

        private void ApplyFlowResult(PaperShiftRunState state, FlowRuleResult flow, GameEventPhase phase)
        {
            if (flow == null)
            {
                return;
            }

            if (phase == GameEventPhase.Probation || state.Phase == PaperShiftPhase.Probation)
            {
                state.CurrentJob.Recognition = Clamp(flow.RecognitionOverride.HasValue ? flow.RecognitionOverride.Value : state.CurrentJob.Recognition + flow.RecognitionDelta, 0, 100);
            }
            else
            {
                state.Interview.Recognition = Clamp(flow.RecognitionOverride.HasValue ? flow.RecognitionOverride.Value : state.Interview.Recognition + flow.RecognitionDelta, 0, 100);
            }

            state.Worker.Stress = Clamp(state.Worker.Stress + flow.StressDelta, 0, 100);
            state.Resume.DeceptionRisk = Clamp(state.Resume.DeceptionRisk + flow.ResumeRiskDelta, 0, 100);
            for (var i = 0; i < flow.Logs.Count; i++)
            {
                state.AddLog(flow.Logs[i]);
            }
        }

        private int InterviewPreparationDelta(PaperShiftRunState state, JobDefinition job, FitProfile profile)
        {
            var score = Random.Next(-4, 9);
            score += (profile.Get(FitDimension.Execution) - 50) / 12;
            score += (profile.Get(FitDimension.Professionalism) - 50) / 15;
            score -= job.Difficulty / 24;
            score -= state.Resume.DeceptionRisk / 26;
            score -= state.Worker.Stress / 32;
            return Clamp(score, -10, 14);
        }

        private int InterviewActionDelta(PaperShiftRunState state, JobDefinition job, FitProfile profile)
        {
            var score = Random.Next(-10, 16);
            score += (profile.Get(FitDimension.Communication) - 50) / 10;
            score += (profile.Get(FitDimension.Professionalism) - 50) / 12;
            score += (profile.Get(FitDimension.Maturity) - 50) / 18;
            score -= job.Difficulty / 18;
            score -= state.Resume.DeceptionRisk / 20;
            score -= state.Worker.Stress / 28;
            return Clamp(score, -22, 24);
        }

        private int ProbationActionDelta(PaperShiftRunState state, JobDefinition job, FitProfile profile)
        {
            var score = Random.Next(-10, 15);
            score += job.PromotionBase / 3;
            score += (profile.Get(FitDimension.Execution) - 50) / 9;
            score += (profile.Get(FitDimension.Professionalism) - 50) / 11;
            score += (profile.Get(FitDimension.Resilience) - 50) / 14;
            score -= job.WorkIntensity / 18;
            score -= state.Worker.Stress / 24;
            return Clamp(score, -24, 26);
        }

        private int ProbationApplyDelta(PaperShiftRunState state, JobDefinition job, FitProfile profile)
        {
            var score = Random.Next(-14, 18);
            score += (profile.Get(FitDimension.Execution) - 50) / 8;
            score += (profile.Get(FitDimension.Resilience) - 50) / 12;
            score -= job.WorkIntensity / 20;
            score -= state.Worker.Stress / 22;
            return Clamp(score, -28, 28);
        }

        private static void MergeFlowResult(FlowRuleResult target, FlowRuleResult source, bool preferSourceDirective)
        {
            if (target == null || source == null)
            {
                return;
            }

            target.RecognitionDelta += source.RecognitionDelta;
            if (source.RecognitionOverride.HasValue)
            {
                target.RecognitionOverride = source.RecognitionOverride;
            }

            target.JobWeightDelta += source.JobWeightDelta;
            target.StressDelta += source.StressDelta;
            target.ResumeRiskDelta += source.ResumeRiskDelta;
            if (!string.IsNullOrEmpty(source.TriggerEventId))
            {
                target.TriggerEventId = source.TriggerEventId;
            }

            foreach (var weight in source.EventWeights)
            {
                target.AddEventWeight(weight.Key, weight.Value);
            }

            for (var i = 0; i < source.Logs.Count; i++)
            {
                target.Logs.Add(source.Logs[i]);
            }

            if (source.Directive == FlowDirective.None)
            {
                return;
            }

            if (preferSourceDirective || target.Directive == FlowDirective.None)
            {
                target.Directive = source.Directive;
                target.DirectiveMessage = source.DirectiveMessage;
                target.EndReason = source.EndReason;
            }
        }

        private bool RollPercent(int chance)
        {
            return Random.Next(0, 100) < Clamp(chance, 0, 100);
        }

        private static InterviewStepResult ToInterviewStepResult(FlowCheckpointResult result)
        {
            if (result == null)
            {
                return InterviewStepResult.Failed("还没有可推进的面试。");
            }

            switch (result.Outcome)
            {
                case FlowCheckpointOutcome.Event:
                    return InterviewStepResult.Event(result.TriggeredEvent, result.RecognitionBefore, result.RecognitionAfter);
                case FlowCheckpointOutcome.Passed:
                    return InterviewStepResult.Passed(result.Message, result.RecognitionBefore, result.RecognitionAfter);
                case FlowCheckpointOutcome.Failed:
                    return InterviewStepResult.Failed(result.Message, result.RecognitionBefore, result.RecognitionAfter);
                default:
                    return InterviewStepResult.Continue(result.Message, result.RecognitionBefore, result.RecognitionAfter);
            }
        }

        private static ProbationStepResult ToProbationStepResult(FlowCheckpointResult result)
        {
            if (result == null)
            {
                return ProbationStepResult.Failed("当前没有正在进行的试用期。", 0, 0, 0, 0);
            }

            switch (result.Outcome)
            {
                case FlowCheckpointOutcome.Event:
                    return ProbationStepResult.Event(result.TriggeredEvent, result.RecognitionBefore, result.RecognitionAfter, result.StressBefore, result.StressAfter);
                case FlowCheckpointOutcome.Passed:
                    return ProbationStepResult.Passed(result.Message, result.RecognitionBefore, result.RecognitionAfter, result.StressBefore, result.StressAfter);
                case FlowCheckpointOutcome.Failed:
                    return ProbationStepResult.Failed(result.Message, result.RecognitionBefore, result.RecognitionAfter, result.StressBefore, result.StressAfter);
                default:
                    return ProbationStepResult.Continue(result.Message, result.RecognitionBefore, result.RecognitionAfter, result.StressBefore, result.StressAfter);
            }
        }

        private static string Fallback(string value, string fallback)
        {
            return string.IsNullOrEmpty(value) ? fallback : value;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }
    }
}
