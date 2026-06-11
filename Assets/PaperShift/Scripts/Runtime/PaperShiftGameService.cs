using System;
using System.Collections.Generic;
using PaperShift.Data;
using PaperShift.Domain;

namespace PaperShift.Runtime
{
    public sealed class PaperShiftGameService
    {
        public const int FirstGenerationStartYear = 2026;
        public const int FirstGenerationStartMonth = 1;

        private const int JobSearchMonths = 1;
        private const int InterviewPreparationMonths = 1;
        private const int InterviewAttendanceMonths = 1;
        private const int ProbationWorkMonths = 1;
        private const int RegularizationMonths = 1;
        private const int EventChoiceMonths = 1;
        private const int BudgetPlanMonths = 1;
        private const int QuitJobMonths = 1;
        private const int WorkYearMonths = 12;

        private readonly PaperShiftDatabase database;
        private readonly ConditionEvaluator conditions;
        private readonly EffectResolver effects;
        private readonly FitProfileResolver fitProfiles;
        private readonly FlowRuleResolver flowRules;
        private readonly WorkerLifecycleResolver workerLifecycle;
        private readonly FlowCheckpointResolver checkpoints;
        private Random random;

        public PaperShiftGameService(PaperShiftDatabase database = null, int? seed = null)
        {
            this.database = database == null ? PaperShiftSeedData.CreateDefaultDatabase() : database;
            PaperShiftSeedData.ApplyRuntimeDefaults(this.database);
            conditions = new ConditionEvaluator();
            effects = new EffectResolver(this.database);
            fitProfiles = new FitProfileResolver(this.database);
            flowRules = new FlowRuleResolver(this.database, conditions);
            workerLifecycle = new WorkerLifecycleResolver(this.database, () => random);
            checkpoints = new FlowCheckpointResolver(this, this.database, conditions, fitProfiles, flowRules, () => random);
            random = new Random(seed.HasValue ? seed.Value : Environment.TickCount);
        }

        public PaperShiftDatabase Database
        {
            get { return database; }
        }

        public PaperShiftRunState StartNewRun(string eraId = null, int? seed = null)
        {
            var actualSeed = seed.HasValue ? seed.Value : Environment.TickCount;
            random = new Random(actualSeed);

            var state = new PaperShiftRunState();
            state.Seed = actualSeed;
            state.Generation = 1;
            state.CurrentYear = FirstGenerationStartYear;
            state.CurrentMonth = FirstGenerationStartMonth;
            state.GenerationStartYear = FirstGenerationStartYear;
            state.GenerationStartMonth = FirstGenerationStartMonth;
            state.Worker = workerLifecycle.CreateRandomWorker(state.CurrentYear, state.CurrentMonth, 1);
            state.Phase = PaperShiftPhase.CreateWorker;
            state.AddLog("新的打工人生开始了。");
            return state;
        }

        public void RandomizeWorker(PaperShiftRunState state, string eraId)
        {
            EnsureDate(state);
            state.Worker = workerLifecycle.CreateRandomWorker(state.CurrentYear, state.CurrentMonth, state.Generation);
            state.LaterLife = new LaterLifeState();
            state.Phase = PaperShiftPhase.CreateWorker;
            state.AddLog("已随机生成新的劳动者。");
        }

        public List<TagDefinition> RollStartingTags(PaperShiftRunState state, int count)
        {
            var result = new List<TagDefinition>();
            var guard = 0;
            while (result.Count < count && guard < count * 20)
            {
                guard++;
                var tag = PickStartingTag(state);
                if (tag == null || ContainsTag(result, tag.Id))
                {
                    continue;
                }

                result.Add(tag);
            }

            return result;
        }

        public bool SelectStartingTag(PaperShiftRunState state, string tagId, int maxTags = 3)
        {
            if (state.Worker.Tags.Count >= maxTags || state.Worker.HasTag(tagId))
            {
                return false;
            }

            var definition = database.FindTag(tagId);
            if (definition == null || definition.Scope != TagScope.Worker)
            {
                return false;
            }

            state.Worker.AddTag(CreateTagInstance(definition, state.CurrentYear), definition.Unique);
            state.AddLog("获得标签：" + definition.DisplayName);
            return true;
        }

        public bool ToggleStartingTag(PaperShiftRunState state, string tagId, int maxTags = 3)
        {
            if (state.Worker.HasTag(tagId))
            {
                var definition = database.FindTag(tagId);
                state.Worker.RemoveTag(tagId);
                state.AddLog("移除标签：" + (definition == null ? tagId : definition.DisplayName));
                return true;
            }

            return SelectStartingTag(state, tagId, maxTags);
        }

        public void SetResumeIntent(PaperShiftRunState state, string intentTagId, bool enabled)
        {
            if (state != null && state.Resume != null && state.Resume.IntentTagIds != null)
            {
                state.Resume.IntentTagIds.Clear();
            }
        }

        public void SetResumePackaging(PaperShiftRunState state, string fieldId, ResumePackagingMode mode)
        {
            SetResumePackaging(state, fieldId, mode, -1);
        }

        public void SetResumePackaging(PaperShiftRunState state, string fieldId, ResumePackagingMode mode, int optionIndex)
        {
            var choice = state.Resume.GetOrCreateChoice(fieldId);
            choice.Mode = mode;
            choice.OptionIndex = optionIndex;
            state.Resume.DeceptionRisk = CalculateResumeRisk(state.Resume);
        }

        public bool ToggleResumeHiddenTag(PaperShiftRunState state, string tagId, int maxHiddenTags = 3)
        {
            if (state == null || state.Worker == null || state.Resume == null || string.IsNullOrEmpty(tagId))
            {
                return false;
            }

            if (state.Resume.HiddenTagIds == null)
            {
                state.Resume.HiddenTagIds = new List<string>();
            }

            if (state.Resume.HiddenTagIds.Contains(tagId))
            {
                state.Resume.HiddenTagIds.Remove(tagId);
                state.Resume.DeceptionRisk = CalculateResumeRisk(state.Resume);
                return true;
            }

            if (!state.Worker.HasTag(tagId) || state.Resume.HiddenTagIds.Count >= maxHiddenTags)
            {
                return false;
            }

            state.Resume.HiddenTagIds.Add(tagId);
            state.Resume.DeceptionRisk = CalculateResumeRisk(state.Resume);
            return true;
        }

        public bool FindInterviewOffer(PaperShiftRunState state)
        {
            AdvanceTime(state, JobSearchMonths);
            var picker = new WeightedPicker<CompanyJobPair>();
            for (var companyIndex = 0; companyIndex < database.Companies.Length; companyIndex++)
            {
                var company = database.Companies[companyIndex];
                if (!MatchesEra(company.EraIds, state.Worker.EraId))
                {
                    continue;
                }

                for (var jobIndex = 0; jobIndex < company.Jobs.Length; jobIndex++)
                {
                    var job = company.Jobs[jobIndex];
                    var profile = fitProfiles.Build(state);
                    var weight = flowRules.JobRollWeight(state, company, job, profile, random);
                    picker.Add(new CompanyJobPair(company, job), weight);
                }
            }

            if (picker.Count == 0)
            {
                return false;
            }

            var pair = picker.Pick(random);
            var initialRecognition = InitialRecognition(state, pair.Company, pair.Job);
            state.Interview = new InterviewState
            {
                CompanyId = pair.Company.Id,
                CompanyName = pair.Company.DisplayName,
                JobId = pair.Job.Id,
                JobTitle = pair.Job.DisplayName,
                OfferThreshold = pair.Job.OfferThreshold,
                Salary = 0,
                Recognition = initialRecognition
            };
            state.Phase = PaperShiftPhase.Interview;
            state.AddLog("投递到了 " + pair.Company.DisplayName + " · " + pair.Job.DisplayName + "。");
            return true;
        }

        public InterviewStepResult PrepareInterviewStep(PaperShiftRunState state)
        {
            AdvanceTime(state, InterviewPreparationMonths);
            return checkpoints.ResolveInterviewStep(state, FlowCheckpointAction.PrepareInterview);
        }

        public InterviewStepResult ApplyInterview(PaperShiftRunState state)
        {
            AdvanceTime(state, InterviewAttendanceMonths);
            return checkpoints.ResolveInterviewStep(state, FlowCheckpointAction.AttendInterview);
        }

        public TriggeredEvent AdvanceInterview(PaperShiftRunState state)
        {
            var result = ApplyInterview(state);
            return result == null ? null : result.TriggeredEvent;
        }

        public bool StartProbation(PaperShiftRunState state)
        {
            var company = database.FindCompany(state.Interview.CompanyId);
            var job = database.FindJob(state.Interview.CompanyId, state.Interview.JobId);
            if (company == null || job == null)
            {
                return false;
            }

            state.CurrentJob = new CurrentJobState
            {
                CompanyId = company.Id,
                CompanyName = company.DisplayName,
                JobId = job.Id,
                JobTitle = job.DisplayName,
                Salary = RollSalary(state, job),
                Intensity = job.WorkIntensity,
                Recognition = InitialProbationRecognition(state, company, job),
                WorkYears = 0,
                StartYear = state.CurrentYear,
                StartMonth = state.CurrentMonth
            };
            state.Interview = new InterviewState();
            state.Phase = PaperShiftPhase.Probation;
            state.AddLog("进入试用期 " + company.DisplayName + " · " + job.DisplayName + "。", EventNoticeType.Banner);
            return true;
        }

        public bool AcceptOffer(PaperShiftRunState state)
        {
            return StartProbation(state);
        }

        public void RestartJobSearch(PaperShiftRunState state)
        {
            if (state.HasActiveJob)
            {
                state.AddLog("已离开 " + state.CurrentJob.CompanyName + "，重新寻找机会。", EventNoticeType.Banner);
            }

            state.Interview = new InterviewState();
            state.CurrentJob = new CurrentJobState();
            state.Phase = PaperShiftPhase.Interview;
        }

        public ProbationStepResult AdvanceProbationStep(PaperShiftRunState state)
        {
            AdvanceTime(state, ProbationWorkMonths);
            return checkpoints.ResolveProbationStep(state, FlowCheckpointAction.WorkProbation);
        }

        public ProbationStepResult ApplyRegularization(PaperShiftRunState state)
        {
            AdvanceTime(state, RegularizationMonths);
            return checkpoints.ResolveProbationStep(state, FlowCheckpointAction.ApplyRegularization);
        }

        public void CompleteGenerationByHire(PaperShiftRunState state)
        {
            workerLifecycle.CompleteGenerationByHire(state);
        }

        public bool ResolveInterviewResult(PaperShiftRunState state, out string message)
        {
            var result = ApplyInterview(state);
            message = result == null ? "还没有可推进的面试。" : result.Message;
            return result != null && result.Outcome == InterviewStepOutcome.Passed;
        }

        public TriggeredEvent CompleteWorkYear(PaperShiftRunState state)
        {
            if (!state.HasActiveJob)
            {
                return null;
            }

            var company = database.FindCompany(state.CurrentJob.CompanyId);
            var job = database.FindJob(state.CurrentJob.CompanyId, state.CurrentJob.JobId);
            if (company == null || job == null)
            {
                return null;
            }

            AdvanceTime(state, WorkYearMonths);

            var stressDelta = state.CurrentJob.Intensity / 8;
            stressDelta += effects.SumPassive(state, EffectKind.PassiveStressPerYear, "remote");
            stressDelta += effects.SumPassive(state, EffectKind.PassiveStressPerYear, "physical");
            state.Worker.Stress = Clamp(state.Worker.Stress + stressDelta, 0, 100);
            state.CurrentJob.Recognition = Clamp(state.CurrentJob.Recognition + job.PromotionBase + WorkerAptitude(state, job) / 8, 0, 100);
            state.CurrentJob.WorkYears++;

            ApplyAnnualBudget(state);
            state.AddLog(state.CurrentYear + " 年结算：获得收入并完成预算分配。");

            if (workerLifecycle.ShouldRetire(state))
            {
                Retire(state, workerLifecycle.RetirementReason(state));
                return null;
            }

            return TryTriggerEvent(state, GameEventPhase.WorkYear, company, job);
        }

        public TriggeredEvent SaveBudget(PaperShiftRunState state, BudgetPlan budget)
        {
            AdvanceTime(state, BudgetPlanMonths);
            state.Budget = budget;
            state.Budget.NormalizeTo100();
            state.Phase = PaperShiftPhase.Working;
            ApplyBudgetTags(state);
            return TryTriggerEvent(state, GameEventPhase.Budget, null, null);
        }

        public void QuitJob(PaperShiftRunState state)
        {
            if (!state.HasActiveJob)
            {
                return;
            }

            AdvanceTime(state, QuitJobMonths);
            state.AddLog("你主动离开了 " + state.CurrentJob.CompanyName + "。");
            state.CurrentJob = new CurrentJobState();
            state.Phase = PaperShiftPhase.EditResume;
        }

        public void Retire(PaperShiftRunState state, RunEndReason reason)
        {
            workerLifecycle.Retire(state, reason);
        }

        public bool StartNextGeneration(PaperShiftRunState state, int heirIndex)
        {
            return workerLifecycle.StartNextGeneration(state, heirIndex);
        }

        public TriggeredEvent TryTriggerEvent(PaperShiftRunState state, GameEventPhase phase, CompanyDefinition company, JobDefinition job)
        {
            return TryTriggerEvent(state, phase, company, job, null, null);
        }

        public TriggeredEvent TryTriggerEvent(PaperShiftRunState state, GameEventPhase phase, CompanyDefinition company, JobDefinition job, FlowRuleResult flow)
        {
            return TryTriggerEvent(state, phase, company, job, flow, null);
        }

        private TriggeredEvent TryTriggerEvent(PaperShiftRunState state, GameEventPhase phase, CompanyDefinition company, JobDefinition job, FlowRuleResult flow, FlowCheckpointAction? action)
        {
            if (phase == GameEventPhase.WorkYear && random.NextDouble() > 0.35f)
            {
                return null;
            }

            if (phase == GameEventPhase.Budget && random.Next(0, 100) >= BudgetEventChance(state))
            {
                return null;
            }

            var picker = new WeightedPicker<GameEventDefinition>();
            for (var i = 0; i < database.Events.Length; i++)
            {
                var gameEvent = database.Events[i];
                if (gameEvent.Phase != GameEventPhase.Any && gameEvent.Phase != phase)
                {
                    continue;
                }

                if (!CanTriggerEvent(state, gameEvent))
                {
                    continue;
                }

                if (!AllowsCheckpointAction(gameEvent, action))
                {
                    continue;
                }

                if (!conditions.AreMet(gameEvent.Conditions, state, company, job, random, action))
                {
                    continue;
                }

                var weight = gameEvent.BaseWeight + effects.SumPassive(state, EffectKind.PassiveEventWeight, gameEvent.Id);
                if (flow != null)
                {
                    weight += flow.EventWeight(gameEvent.Id);
                }

                picker.Add(gameEvent, weight);
            }

            if (picker.Count == 0)
            {
                return null;
            }

            var picked = picker.Pick(random);
            if (picked == null)
            {
                return null;
            }

            state.MarkEventSeen(picked.Id);
            if (picked.CooldownYears > 0)
            {
                state.SetEventCooldown(picked.Id, picked.CooldownYears);
            }

            return new TriggeredEvent(picked, AvailableOptions(picked, state, company, job, action));
        }

        private TriggeredEvent ResolveTriggeredEvent(PaperShiftRunState state, GameEventPhase phase, CompanyDefinition company, JobDefinition job, FlowRuleResult flow, FlowCheckpointAction? action)
        {
            if (flow != null && !string.IsNullOrEmpty(flow.TriggerEventId))
            {
                var triggered = TriggerEventById(state, flow.TriggerEventId, company, job, action);
                if (triggered != null)
                {
                    return triggered;
                }
            }

            if (action == FlowCheckpointAction.EventChoice)
            {
                return null;
            }

            if (state != null && state.Worker.Stress >= 100)
            {
                var stressEvent = TriggerEventById(state, "stress_breakdown", company, job, action);
                if (stressEvent != null)
                {
                    return stressEvent;
                }
            }

            if (phase == GameEventPhase.Interview && state != null && state.Resume.DeceptionRisk >= 75)
            {
                var auditEvent = TriggerEventById(state, "resume_audit", company, job, action);
                if (auditEvent != null)
                {
                    return auditEvent;
                }
            }

            if (flow != null && state != null && state.Worker != null && state.Worker.Stress >= 90)
            {
                flow.AddEventWeight("stress_breakdown", state.Worker.Stress >= 100 ? 100 : 45);
            }

            if (flow != null && phase == GameEventPhase.Interview && state != null && state.Resume != null && state.Resume.DeceptionRisk >= 20)
            {
                flow.AddEventWeight("resume_audit", state.Resume.DeceptionRisk);
            }

            return TryTriggerEvent(state, phase, company, job, flow, action);
        }

        internal TriggeredEvent ResolveTriggeredEventForCheckpoint(PaperShiftRunState state, GameEventPhase phase, CompanyDefinition company, JobDefinition job, FlowRuleResult flow, FlowCheckpointAction action)
        {
            return ResolveTriggeredEvent(state, phase, company, job, flow, action);
        }

        private TriggeredEvent TriggerEventById(PaperShiftRunState state, string eventId, CompanyDefinition company, JobDefinition job, FlowCheckpointAction? action)
        {
            var gameEvent = FindEvent(eventId);
            if (gameEvent == null || !CanTriggerEvent(state, gameEvent) || !AllowsCheckpointAction(gameEvent, action) || !conditions.AreMet(gameEvent.Conditions, state, company, job, random, action))
            {
                return null;
            }

            state.MarkEventSeen(gameEvent.Id);
            if (gameEvent.CooldownYears > 0)
            {
                state.SetEventCooldown(gameEvent.Id, gameEvent.CooldownYears);
            }

            return new TriggeredEvent(gameEvent, AvailableOptions(gameEvent, state, company, job, action));
        }

        private static bool AllowsCheckpointAction(GameEventDefinition gameEvent, FlowCheckpointAction? action)
        {
            if (!action.HasValue || gameEvent == null)
            {
                return true;
            }

            if (action.Value != FlowCheckpointAction.PrepareInterview || gameEvent.Phase != GameEventPhase.Interview)
            {
                return true;
            }

            return HasActionCondition(gameEvent.Conditions, FlowCheckpointAction.PrepareInterview);
        }

        private static bool HasActionCondition(ConditionDefinition[] conditions, FlowCheckpointAction action)
        {
            if (conditions == null)
            {
                return false;
            }

            for (var i = 0; i < conditions.Length; i++)
            {
                var condition = conditions[i];
                if (condition != null && condition.Kind == ConditionKind.ActionIs && ActionConditionMatches(condition, action))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ActionConditionMatches(ConditionDefinition condition, FlowCheckpointAction action)
        {
            var expected = string.IsNullOrEmpty(condition.TextValue) ? condition.Key : condition.TextValue;
            if (!string.IsNullOrEmpty(expected))
            {
                if (Enum.TryParse(expected.Trim(), true, out FlowCheckpointAction parsed))
                {
                    return parsed == action;
                }

                var normalized = expected.Trim().ToLowerInvariant();
                return action == FlowCheckpointAction.PrepareInterview
                    && (normalized == "prepare" || normalized == "prepare_interview" || normalized == "interview_prepare");
            }

            return condition.IntValue >= 0 && condition.IntValue <= (int)FlowCheckpointAction.EventChoice && (FlowCheckpointAction)condition.IntValue == action;
        }

        private GameEventDefinition FindEvent(string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
            {
                return null;
            }

            for (var i = 0; i < database.Events.Length; i++)
            {
                if (database.Events[i].Id == eventId)
                {
                    return database.Events[i];
                }
            }

            return null;
        }

        private static bool CanTriggerEvent(PaperShiftRunState state, GameEventDefinition gameEvent)
        {
            if (state == null || gameEvent == null || string.IsNullOrEmpty(gameEvent.Id))
            {
                return false;
            }

            if (state.GetEventCooldown(gameEvent.Id) > 0)
            {
                return false;
            }

            return gameEvent.CooldownYears < 0 || gameEvent.CooldownYears > 0 || !state.HasSeenEvent(gameEvent.Id);
        }

        public EventOptionChoiceResult ChooseEventOption(PaperShiftRunState state, TriggeredEvent triggeredEvent, string optionId)
        {
            if (triggeredEvent == null || triggeredEvent.Options == null)
            {
                return EventOptionChoiceResult.Ignored();
            }

            for (var i = 0; i < triggeredEvent.Options.Length; i++)
            {
                var option = triggeredEvent.Options[i];
                if (option.Id != optionId)
                {
                    continue;
                }

                AdvanceTime(state, EventChoiceMonths);
                if (!option.RunCheckpointAfterChoice)
                {
                    effects.Apply(option.Effects, state);
                    return EventOptionChoiceResult.Applied(option, null);
                }

                var phase = ResolveEventChoicePhase(state, triggeredEvent);
                if (phase != GameEventPhase.Interview && phase != GameEventPhase.Probation)
                {
                    effects.Apply(option.Effects, state);
                    return EventOptionChoiceResult.Applied(option, null);
                }

                var optionFlow = BuildEventOptionFlow(option);
                ApplyNonCheckpointEffects(option.Effects, state);
                var checkpoint = checkpoints.Resolve(state, FlowCheckpointAction.EventChoice, phase, optionFlow);
                if (ShouldResolveSourceCheckpointAfterEvent(triggeredEvent, checkpoint, phase))
                {
                    checkpoint = checkpoints.ResolveNaturalOnly(state, triggeredEvent.SourceAction, phase);
                }

                return EventOptionChoiceResult.Applied(option, checkpoint);
            }

            return EventOptionChoiceResult.Ignored();
        }

        public EventOptionChoiceResult ContinueEvent(PaperShiftRunState state, TriggeredEvent triggeredEvent)
        {
            if (triggeredEvent == null || triggeredEvent.Event == null)
            {
                return EventOptionChoiceResult.Ignored();
            }

            var phase = ResolveEventChoicePhase(state, triggeredEvent);
            if (phase != GameEventPhase.Interview && phase != GameEventPhase.Probation)
            {
                effects.Apply(triggeredEvent.Event.Effects, state);
                return EventOptionChoiceResult.Applied(null, null);
            }

            var eventFlow = BuildEventFlow(triggeredEvent.Event.Effects);
            ApplyNonCheckpointEffects(triggeredEvent.Event.Effects, state);
            var checkpoint = checkpoints.Resolve(state, FlowCheckpointAction.EventChoice, phase, eventFlow);
            if (ShouldResolveSourceCheckpointAfterEvent(triggeredEvent, checkpoint, phase))
            {
                checkpoint = checkpoints.ResolveNaturalOnly(state, triggeredEvent.SourceAction, phase);
            }

            return EventOptionChoiceResult.Applied(null, checkpoint);
        }

        private static bool ShouldResolveSourceCheckpointAfterEvent(TriggeredEvent triggeredEvent, FlowCheckpointResult checkpoint, GameEventPhase phase)
        {
            return triggeredEvent != null &&
                triggeredEvent.HasSourceAction &&
                triggeredEvent.SourceAction == FlowCheckpointAction.ApplyRegularization &&
                phase == GameEventPhase.Probation &&
                checkpoint != null &&
                checkpoint.Outcome == FlowCheckpointOutcome.Continue;
        }

        private static FlowRuleResult BuildEventOptionFlow(EventOptionDefinition option)
        {
            var result = new FlowRuleResult();
            if (option != null)
            {
                FlowRuleResolver.ApplyEffects(result, option.Effects);
            }

            return result;
        }

        private static FlowRuleResult BuildEventFlow(EffectDefinition[] eventEffects)
        {
            var result = new FlowRuleResult();
            FlowRuleResolver.ApplyEffects(result, eventEffects);
            return result;
        }

        internal void ApplyNonCheckpointEffects(EffectDefinition[] optionEffects, PaperShiftRunState state)
        {
            if (optionEffects == null)
            {
                return;
            }

            for (var i = 0; i < optionEffects.Length; i++)
            {
                var effect = optionEffects[i];
                if (effect == null || IsCheckpointManagedEffect(effect.Kind))
                {
                    continue;
                }

                effects.Apply(effect, state);
            }
        }

        private static bool IsCheckpointManagedEffect(EffectKind kind)
        {
            switch (kind)
            {
                case EffectKind.AddRecognition:
                case EffectKind.SetRecognition:
                case EffectKind.AddStress:
                case EffectKind.AddResumeRisk:
                case EffectKind.AddLog:
                case EffectKind.AddJobWeight:
                case EffectKind.AddEventWeight:
                case EffectKind.TriggerEvent:
                case EffectKind.DirectPass:
                case EffectKind.DirectFail:
                case EffectKind.ReturnToJobSearch:
                case EffectKind.EndRun:
                    return true;
                default:
                    return false;
            }
        }

        private static GameEventPhase ResolveEventChoicePhase(PaperShiftRunState state, TriggeredEvent triggeredEvent)
        {
            if (triggeredEvent != null && triggeredEvent.Event != null)
            {
                if (triggeredEvent.Event.Phase == GameEventPhase.Interview || triggeredEvent.Event.Phase == GameEventPhase.Probation)
                {
                    return triggeredEvent.Event.Phase;
                }
            }

            if (state != null && state.HasActiveJob)
            {
                return GameEventPhase.Probation;
            }

            if (state != null && state.Interview != null && !string.IsNullOrEmpty(state.Interview.JobId))
            {
                return GameEventPhase.Interview;
            }

            return GameEventPhase.Any;
        }

        private TagDefinition PickStartingTag(PaperShiftRunState state)
        {
            var picker = new WeightedPicker<TagDefinition>();
            for (var i = 0; i < database.Tags.Length; i++)
            {
                var tag = database.Tags[i];
                if (tag.Scope != TagScope.Worker || !MatchesEra(tag.EraIds, state.Worker.EraId))
                {
                    continue;
                }

                if (!conditions.AreMet(tag.Conditions, state, null, null, random))
                {
                    continue;
                }

                picker.Add(tag, RarityWeight(tag.RarityId));
            }

            return picker.Pick(random);
        }

        private int InitialRecognition(PaperShiftRunState state, CompanyDefinition company, JobDefinition job)
        {
            var profile = fitProfiles.Build(state);
            var flow = flowRules.Evaluate(state, company, job, profile, GameEventPhase.Interview, random);
            if (flow.Directive == FlowDirective.DirectFail)
            {
                return 0;
            }

            var score = 36;
            score += flow.RecognitionDelta;
            score += (profile.Get(FitDimension.Professionalism) - 50) / 5;
            score += (profile.Get(FitDimension.Communication) - 50) / 7;
            score += (profile.Get(FitDimension.Presence) - 50) / 10;
            score += ResumePresentationBonus(state.Resume);
            score -= job.Difficulty / 6;
            score -= state.Resume.DeceptionRisk / 5;
            score -= state.Worker.Stress / 18;
            return Clamp(score, 0, 85);
        }

        private int InitialProbationRecognition(PaperShiftRunState state, CompanyDefinition company, JobDefinition job)
        {
            var profile = fitProfiles.Build(state);
            var flow = flowRules.Evaluate(state, company, job, profile, GameEventPhase.Probation, random);
            if (flow.Directive == FlowDirective.DirectFail)
            {
                return 0;
            }

            var score = 24 + state.Interview.Recognition / 2;
            score += flow.RecognitionDelta;
            score += (profile.Get(FitDimension.Execution) - 50) / 5;
            score += (profile.Get(FitDimension.Resilience) - 50) / 8;
            score -= job.WorkIntensity / 8;
            score -= state.Worker.Stress / 18;
            return Clamp(score, 0, 90);
        }

        private int WorkerAptitude(PaperShiftRunState state, JobDefinition job)
        {
            var score = 0;
            for (var i = 0; i < job.Requirements.Length; i++)
            {
                var requirement = job.Requirements[i];
                var value = state.Worker.GetStat(requirement.StatId);
                var diff = value - requirement.MinValue;
                score += diff * Math.Max(1, requirement.Weight);
            }

            return score;
        }

        private int RollSalary(PaperShiftRunState state, JobDefinition job)
        {
            var min = Math.Max(0, job.SalaryMin);
            var max = Math.Max(0, job.SalaryMax);
            if (min > max)
            {
                var temp = min;
                min = max;
                max = temp;
            }

            var salary = min == max ? min : random.Next(min, max + 1);
            var percent = 0;
            for (var i = 0; i < job.TagIds.Length; i++)
            {
                percent += effects.SumPassive(state, EffectKind.PassiveSalaryPercent, job.TagIds[i]);
            }

            return salary + salary * percent / 100;
        }

        private void ApplyAnnualBudget(PaperShiftRunState state)
        {
            state.Worker.Health = Clamp(state.Worker.Health + (state.Budget.Food - 20) / 5, 0, 100);
            state.Worker.Stress = Clamp(state.Worker.Stress - (state.Budget.Housing - 20) / 8, 0, 100);
            if (state.Budget.Education >= 18)
            {
                state.Worker.AddStat(PaperShiftWorkerAttributes.Education, 1);
                state.Worker.AddStat(PaperShiftWorkerAttributes.Ability, 1);
            }
        }

        private void ApplyBudgetTags(PaperShiftRunState state)
        {
            if (state.Budget.Food >= 30)
            {
                state.AddLog("饮食投入较高，健康恢复更快。");
            }

            if (state.Budget.Romance >= 25)
            {
                state.AddLog("恋爱预算提高，相关事件概率上升。");
            }

            if (state.Budget.Education >= 25)
            {
                state.AddLog("教育投入提高，下一代成长会更好。");
            }
        }

        internal void EnsureHeirsForCheckpoint(PaperShiftRunState state)
        {
            workerLifecycle.EnsureHeirs(state);
        }

        private void AdvanceTime(PaperShiftRunState state, int months)
        {
            if (state == null || months <= 0)
            {
                return;
            }

            EnsureDate(state);
            for (var i = 0; i < months; i++)
            {
                state.CurrentMonth++;
                if (state.CurrentMonth > 12)
                {
                    state.CurrentMonth = 1;
                    state.CurrentYear++;
                    if (state.Worker != null)
                    {
                        state.Worker.TickTemporaryTags();
                    }

                    state.TickEventCooldowns();
                }

                WorkerLifecycleResolver.RefreshAgeAt(state.Worker, state.CurrentYear, state.CurrentMonth);
            }
        }

        private static void EnsureDate(PaperShiftRunState state)
        {
            if (state == null)
            {
                return;
            }

            if (state.CurrentYear <= 0)
            {
                state.CurrentYear = FirstGenerationStartYear;
            }

            if (state.CurrentMonth < 1 || state.CurrentMonth > 12)
            {
                state.CurrentMonth = FirstGenerationStartMonth;
            }

            if (state.GenerationStartYear <= 0)
            {
                state.GenerationStartYear = state.CurrentYear;
            }

            if (state.GenerationStartMonth < 1 || state.GenerationStartMonth > 12)
            {
                state.GenerationStartMonth = state.CurrentMonth;
            }

            WorkerLifecycleResolver.RefreshAgeAt(state.Worker, state.CurrentYear, state.CurrentMonth);
        }

        private EventOptionDefinition[] AvailableOptions(GameEventDefinition gameEvent, PaperShiftRunState state, CompanyDefinition company, JobDefinition job, FlowCheckpointAction? action = null)
        {
            var result = new List<EventOptionDefinition>();
            for (var i = 0; i < gameEvent.Options.Length; i++)
            {
                var option = gameEvent.Options[i];
                if (conditions.AreMet(option.Conditions, state, company, job, random, action))
                {
                    result.Add(option);
                }
            }

            return result.ToArray();
        }

        private static int BudgetEventChance(PaperShiftRunState state)
        {
            var chance = 10;
            chance += state.Budget.Romance / 2;
            chance += state.Budget.Education / 4;
            chance -= Math.Max(0, state.Budget.Savings - 35) / 3;
            return Clamp(chance, 5, 70);
        }

        private static int CalculateResumeRisk(ResumeProfile resume)
        {
            var risk = resume.HiddenTagIds == null ? 0 : resume.HiddenTagIds.Count * 3;
            for (var i = 0; i < resume.Packaging.Count; i++)
            {
                if (resume.Packaging[i].OptionIndex < 0)
                {
                    continue;
                }

                switch (resume.Packaging[i].Mode)
                {
                    case ResumePackagingMode.Exaggerate:
                        risk += 12;
                        break;
                    case ResumePackagingMode.Fake:
                        risk += 28;
                        break;
                    case ResumePackagingMode.Hide:
                        risk += 4;
                        break;
                }
            }

            return Clamp(risk, 0, 100);
        }

        private static int ResumePresentationBonus(ResumeProfile resume)
        {
            var bonus = 0;
            for (var i = 0; i < resume.Packaging.Count; i++)
            {
                if (resume.Packaging[i].OptionIndex < 0)
                {
                    continue;
                }

                if (resume.Packaging[i].Mode == ResumePackagingMode.Exaggerate)
                {
                    bonus += 3;
                }
                else if (resume.Packaging[i].Mode == ResumePackagingMode.Fake)
                {
                    bonus += 7;
                }
            }

            return bonus;
        }

        private int RarityWeight(string rarityId)
        {
            for (var i = 0; i < database.Rarities.Length; i++)
            {
                if (database.Rarities[i].Id == rarityId)
                {
                    return Math.Max(1, database.Rarities[i].Weight);
                }
            }

            return 50;
        }

        private static bool MatchesEra(string[] eraIds, string eraId)
        {
            if (string.IsNullOrEmpty(eraId))
            {
                return true;
            }

            if (eraIds == null || eraIds.Length == 0)
            {
                return true;
            }

            for (var i = 0; i < eraIds.Length; i++)
            {
                if (eraIds[i] == eraId)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsTag(List<TagDefinition> tags, string tagId)
        {
            for (var i = 0; i < tags.Count; i++)
            {
                if (tags[i].Id == tagId)
                {
                    return true;
                }
            }

            return false;
        }

        private static TagInstance CreateTagInstance(TagDefinition definition, int currentYear)
        {
            return new TagInstance
            {
                TagId = definition.Id,
                DisplayName = definition.DisplayName,
                Scope = definition.Scope,
                RarityId = definition.RarityId,
                AcquiredYear = currentYear,
                Stacks = 1
            };
        }

        internal static string ReasonTextForCheckpoint(RunEndReason reason)
        {
            return WorkerLifecycleResolver.ReasonText(reason);
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }

        private sealed class CompanyJobPair
        {
            public readonly CompanyDefinition Company;
            public readonly JobDefinition Job;

            public CompanyJobPair(CompanyDefinition company, JobDefinition job)
            {
                Company = company;
                Job = job;
            }
        }
    }

}
