using System;
using System.Collections.Generic;
using PaperShift.Data;
using PaperShift.Domain;

namespace PaperShift.Runtime
{
    public sealed class PaperShiftGameService
    {
        private readonly PaperShiftDatabase database;
        private readonly ConditionEvaluator conditions;
        private readonly EffectResolver effects;
        private Random random;

        public PaperShiftGameService(PaperShiftDatabase database = null, int? seed = null)
        {
            this.database = database == null ? PaperShiftSeedData.CreateDefaultDatabase() : database;
            conditions = new ConditionEvaluator();
            effects = new EffectResolver(this.database);
            random = new Random(seed.HasValue ? seed.Value : Environment.TickCount);
        }

        public PaperShiftDatabase Database
        {
            get { return database; }
        }

        public PaperShiftRunState StartNewRun(string eraId = "modern", int? seed = null)
        {
            if (seed.HasValue)
            {
                random = new Random(seed.Value);
            }

            var era = database.FindEra(eraId);
            if (era == null && database.Eras.Length > 0)
            {
                era = database.Eras[0];
            }

            var state = new PaperShiftRunState();
            state.Seed = seed.HasValue ? seed.Value : Environment.TickCount;
            state.Generation = 1;
            state.CurrentYear = era == null ? 2000 : random.Next(era.StartYear, era.EndYear + 1);
            state.Worker = CreateRandomWorker(era, state.CurrentYear, 1, 0);
            state.Phase = PaperShiftPhase.CreateWorker;
            state.AddLog("新的打工人生开始了。");
            return state;
        }

        public void RandomizeWorker(PaperShiftRunState state, string eraId)
        {
            var era = database.FindEra(eraId);
            state.Worker = CreateRandomWorker(era, state.CurrentYear, state.Generation, 0);
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
            if (enabled)
            {
                if (!state.Resume.IntentTagIds.Contains(intentTagId))
                {
                    state.Resume.IntentTagIds.Add(intentTagId);
                }
            }
            else
            {
                state.Resume.IntentTagIds.Remove(intentTagId);
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
                    var weight = 20 + MatchIntentWeight(state, job) + Math.Max(0, 70 - job.Difficulty);
                    picker.Add(new CompanyJobPair(company, job), weight);
                }
            }

            if (picker.Count == 0)
            {
                return false;
            }

            var pair = picker.Pick(random);
            var salary = RollSalary(state, pair.Job);
            state.Interview = new InterviewState
            {
                CompanyId = pair.Company.Id,
                CompanyName = pair.Company.DisplayName,
                JobId = pair.Job.Id,
                JobTitle = pair.Job.DisplayName,
                MaxRounds = Math.Max(1, pair.Job.InterviewRounds),
                Round = 0,
                OfferThreshold = pair.Job.OfferThreshold,
                Salary = salary,
                Satisfaction = StartingInterviewSuccessRate(state, pair.Job)
            };
            state.Phase = PaperShiftPhase.Interview;
            state.AddLog("投递到了 " + pair.Company.DisplayName + " · " + pair.Job.DisplayName + "。");
            return true;
        }

        public InterviewStepResult PrepareInterviewStep(PaperShiftRunState state)
        {
            var company = database.FindCompany(state.Interview.CompanyId);
            var job = database.FindJob(state.Interview.CompanyId, state.Interview.JobId);
            if (company == null || job == null)
            {
                return InterviewStepResult.Failed("还没有可以准备的面试。");
            }

            var satisfactionBefore = state.Interview.Satisfaction;
            var score = ComputeInterviewPreparationDelta(state, company, job);
            state.Interview.Satisfaction = Clamp(state.Interview.Satisfaction + score, 0, 100);
            var satisfactionAfter = state.Interview.Satisfaction;
            state.AddLog("准备面试，成功率变为 " + state.Interview.Satisfaction + "%。");

            var deltaText = score >= 0 ? "+" + score : score.ToString();
            return InterviewStepResult.Continue("准备面试，成功率 " + deltaText + "%，当前 " + state.Interview.Satisfaction + "%。", satisfactionBefore, satisfactionAfter);
        }

        public InterviewStepResult ApplyInterview(PaperShiftRunState state)
        {
            var company = database.FindCompany(state.Interview.CompanyId);
            var job = database.FindJob(state.Interview.CompanyId, state.Interview.JobId);
            if (company == null || job == null)
            {
                return InterviewStepResult.Failed("还没有可以参加的面试。");
            }

            var stage = Math.Max(0, state.Interview.Round) + 1;
            state.Interview.Round = stage;

            var satisfactionBefore = state.Interview.Satisfaction;
            var score = ComputeInterviewMeetingDelta(state, company, job, stage);
            state.Interview.Satisfaction = Clamp(state.Interview.Satisfaction + score, 0, 100);
            var satisfactionAfter = state.Interview.Satisfaction;

            if (stage < 4)
            {
                var continueMessage = InterviewStageMessage(stage, score, satisfactionAfter);
                state.AddLog(continueMessage);
                return InterviewStepResult.Continue(continueMessage, satisfactionBefore, satisfactionAfter);
            }

            if (satisfactionAfter >= 100 || random.Next(0, 100) < satisfactionAfter)
            {
                var message = "面试通过，" + company.DisplayName + " 发来了 Offer，进入 " + job.DisplayName + " 试用期。";
                state.Interview.Satisfaction = Math.Max(satisfactionAfter, state.Interview.OfferThreshold);
                StartProbation(state);
                return InterviewStepResult.Passed(message, satisfactionBefore, state.Interview.Satisfaction);
            }

            if (stage < 6 && random.Next(0, 100) < 35)
            {
                var holdMessage = InterviewStageMessage(stage, score, satisfactionAfter);
                state.AddLog(holdMessage);
                return InterviewStepResult.Continue(holdMessage, satisfactionBefore, satisfactionAfter);
            }

            var failedMessage = "面试失败。对方认为匹配度不够，你还在求职状态。";
            state.Worker.Stress = Clamp(state.Worker.Stress + 6, 0, 100);
            state.AddLog(failedMessage, EventNoticeType.Banner);
            return InterviewStepResult.Failed(failedMessage, satisfactionBefore, satisfactionAfter);
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
                Salary = state.Interview.Salary,
                Intensity = job.WorkIntensity,
                PromotionProgress = StartingRegularizationChance(state, job),
                QuitRisk = 0,
                WorkYears = 0
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
            if (state == null || !state.HasActiveJob)
            {
                return ProbationStepResult.Failed("当前没有正在进行的试用期。", 0, 0, 0, 0);
            }

            var job = database.FindJob(state.CurrentJob.CompanyId, state.CurrentJob.JobId);
            if (job == null)
            {
                return ProbationStepResult.Failed("试用期岗位数据丢失，已回到求职状态。", 0, 0, 0, 0);
            }

            state.Phase = PaperShiftPhase.Probation;
            state.CurrentJob.WorkYears++;
            state.Worker.Stress = Clamp(state.Worker.Stress + Math.Max(1, state.CurrentJob.Intensity / 12), 0, 100);

            var chanceBefore = state.CurrentJob.PromotionProgress;
            var aptitude = WorkerAptitude(state, job);
            var chanceDelta = Clamp(8 + job.PromotionBase / 3 + aptitude / 12 + random.Next(-18, 19) - state.Worker.Stress / 20, -30, 32);

            state.CurrentJob.PromotionProgress = Clamp(state.CurrentJob.PromotionProgress + chanceDelta, 0, 100);
            state.CurrentJob.QuitRisk = 0;

            var deltaText = chanceDelta >= 0 ? "+" + chanceDelta : chanceDelta.ToString();
            var continueMessage = "试用期推进，转正概率 " + deltaText + "%，当前 " + state.CurrentJob.PromotionProgress + "%。";
            state.AddLog(continueMessage);
            return ProbationStepResult.Continue(continueMessage, chanceBefore, state.CurrentJob.PromotionProgress, 0, 0);
        }

        public ProbationStepResult ApplyRegularization(PaperShiftRunState state)
        {
            if (state == null || !state.HasActiveJob)
            {
                return ProbationStepResult.Failed("当前没有可以申请入职的试用期。", 0, 0, 0, 0);
            }

            var chanceBefore = state.CurrentJob.PromotionProgress;
            if (chanceBefore < 100 && random.Next(0, 100) < 45)
            {
                var chanceDelta = ComputeRegularizationReviewDelta(state);
                state.CurrentJob.PromotionProgress = Clamp(state.CurrentJob.PromotionProgress + chanceDelta, 0, 100);
                var reviewMessage = RegularizationReviewMessage(chanceDelta, state.CurrentJob.PromotionProgress);
                state.AddLog(reviewMessage);
                return ProbationStepResult.Continue(reviewMessage, chanceBefore, state.CurrentJob.PromotionProgress, 0, 0);
            }

            if (chanceBefore >= 100 || random.Next(0, 100) < chanceBefore)
            {
                CompleteGenerationByHire(state);
                return ProbationStepResult.Passed("申请入职成功，正式转正。这一代结算。", chanceBefore, chanceBefore, 0, 0);
            }

            if (random.Next(0, 100) < 35)
            {
                var chanceDelta = ComputeRegularizationReviewDelta(state);
                state.CurrentJob.PromotionProgress = Clamp(state.CurrentJob.PromotionProgress + chanceDelta, 0, 100);
                var reviewMessage = RegularizationReviewMessage(chanceDelta, state.CurrentJob.PromotionProgress);
                state.AddLog(reviewMessage);
                return ProbationStepResult.Continue(reviewMessage, chanceBefore, state.CurrentJob.PromotionProgress, 0, 0);
            }

            var message = "申请入职没有通过，已自动继续寻找下一家公司。";
            state.AddLog(message, EventNoticeType.Banner);
            state.CurrentJob = new CurrentJobState();
            state.Interview = new InterviewState();
            state.Phase = PaperShiftPhase.Interview;
            return ProbationStepResult.Failed(message, chanceBefore, chanceBefore, 0, 0);
        }

        public void CompleteGenerationByHire(PaperShiftRunState state)
        {
            state.Phase = PaperShiftPhase.Retirement;
            state.Retirement.Reason = RunEndReason.Custom;
            state.Retirement.ReasonText = "通过试用期，正式入职，这一代结算。";
            state.Retirement.FinalSavings = state.Worker.Money + Math.Max(0, state.CurrentJob.Salary);
            state.Retirement.WorkYears = state.CurrentJob.WorkYears;
            state.Retirement.FinalJobTitle = state.CurrentJob.JobTitle;
            EnsureHeirs(state);
            state.AddLog(state.Retirement.ReasonText, EventNoticeType.Banner);
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

            TickYear(state);

            var stressDelta = state.CurrentJob.Intensity / 8;
            stressDelta += effects.SumPassive(state, EffectKind.PassiveStressPerYear, "remote");
            stressDelta += effects.SumPassive(state, EffectKind.PassiveStressPerYear, "physical");
            state.Worker.Stress = Clamp(state.Worker.Stress + stressDelta, 0, 100);
            state.CurrentJob.PromotionProgress = Clamp(state.CurrentJob.PromotionProgress + job.PromotionBase + WorkerAptitude(state, job) / 8, 0, 100);
            state.CurrentJob.QuitRisk = Clamp(state.CurrentJob.QuitRisk + Math.Max(0, state.Worker.Stress - 70) / 5, 0, 100);
            state.CurrentJob.WorkYears++;

            ApplyAnnualBudget(state);
            state.AddLog(state.CurrentYear + " 年结算：获得收入并完成预算分配。");

            if (ShouldRetire(state))
            {
                Retire(state, RetirementReason(state));
                return null;
            }

            return TryTriggerEvent(state, GameEventPhase.WorkYear, company, job);
        }

        public TriggeredEvent SaveBudget(PaperShiftRunState state, BudgetPlan budget)
        {
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

            state.AddLog("你主动离开了 " + state.CurrentJob.CompanyName + "。");
            state.CurrentJob = new CurrentJobState();
            state.Phase = PaperShiftPhase.EditResume;
        }

        public void Retire(PaperShiftRunState state, RunEndReason reason)
        {
            state.Phase = PaperShiftPhase.Retirement;
            state.Retirement.Reason = reason;
            state.Retirement.ReasonText = ReasonText(reason);
            state.Retirement.FinalSavings = state.Worker.Money;
            state.Retirement.WorkYears = state.CurrentJob.WorkYears;
            state.Retirement.FinalJobTitle = state.CurrentJob.JobTitle;
            EnsureHeirs(state);
            state.AddLog(state.Retirement.ReasonText, EventNoticeType.Banner);
        }

        public bool StartNextGeneration(PaperShiftRunState state, int heirIndex)
        {
            if (state.Worker.Heirs.Count == 0 || heirIndex < 0 || heirIndex >= state.Worker.Heirs.Count)
            {
                return false;
            }

            var heir = state.Worker.Heirs[heirIndex];
            var era = NextEra(state.Worker.EraId);
            var inheritedMoney = state.Worker.Money * Math.Max(0, heir.InheritancePercent) / 100;
            var next = CreateRandomWorker(era, state.CurrentYear, state.Generation + 1, inheritedMoney);
            next.FirstName = heir.Name.Length > state.Worker.LastName.Length ? heir.Name.Substring(state.Worker.LastName.Length) : heir.Name;
            next.Gender = heir.Gender;
            next.Tags.AddRange(heir.Tags);
            for (var i = 0; i < heir.Stats.Count; i++)
            {
                next.SetStat(heir.Stats[i].Id, heir.Stats[i].Value);
            }

            state.Generation++;
            state.Worker = next;
            state.Resume = new ResumeProfile();
            state.Interview = new InterviewState();
            state.CurrentJob = new CurrentJobState();
            state.Retirement = new RetirementState();
            state.Phase = PaperShiftPhase.CreateWorker;
            state.AddLog("第 " + state.Generation + " 代开始了。", EventNoticeType.Banner);
            return true;
        }

        public TriggeredEvent TryTriggerEvent(PaperShiftRunState state, GameEventPhase phase, CompanyDefinition company, JobDefinition job)
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

                if (state.GetEventCooldown(gameEvent.Id) > 0)
                {
                    continue;
                }

                if (!conditions.AreMet(gameEvent.Conditions, state, company, job, random))
                {
                    continue;
                }

                var weight = gameEvent.BaseWeight + effects.SumPassive(state, EffectKind.PassiveEventWeight, gameEvent.Id);
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

            state.AddLog(picked.DisplayName + "：" + picked.Body, picked.NoticeType);
            return new TriggeredEvent(picked, AvailableOptions(picked, state, company, job));
        }

        public bool ChooseEventOption(PaperShiftRunState state, TriggeredEvent triggeredEvent, string optionId)
        {
            if (triggeredEvent == null || triggeredEvent.Options == null)
            {
                return false;
            }

            for (var i = 0; i < triggeredEvent.Options.Length; i++)
            {
                var option = triggeredEvent.Options[i];
                if (option.Id != optionId)
                {
                    continue;
                }

                effects.Apply(option.Effects, state);
                return true;
            }

            return false;
        }

        private WorkerProfile CreateRandomWorker(EraDefinition era, int currentYear, int generation, int inheritedMoney)
        {
            var worker = new WorkerProfile
            {
                Id = Guid.NewGuid().ToString("N"),
                LastName = Pick(database.LastNames, "李"),
                Gender = random.NextDouble() < 0.5 ? "女" : "男",
                Personality = Pick(new[] { "沉稳", "谨慎", "开朗", "灵活", "较真", "随和" }, "沉稳"),
                EraId = era == null ? "modern" : era.Id,
                EraName = era == null ? "现代城市" : era.DisplayName,
                Generation = generation,
                Age = random.Next(18, 31),
                Stress = random.Next(5, 26),
                Health = random.Next(65, 96),
                Money = inheritedMoney
            };
            worker.FirstName = worker.Gender == "女" ? Pick(database.FemaleFirstNames, "小满") : Pick(database.MaleFirstNames, "知行");
            worker.BirthYear = currentYear - worker.Age;

            for (var i = 0; i < database.Stats.Length; i++)
            {
                var stat = database.Stats[i];
                worker.SetStat(stat.Id, random.Next(stat.StartMin, stat.StartMax + 1));
            }

            worker.SetStat(PaperShiftWorkerAttributes.Height, RollHeight(worker.Gender));
            worker.Money += 1000 + worker.GetStat(PaperShiftWorkerAttributes.Family) * 80 + worker.GetStat(PaperShiftWorkerAttributes.Ability) * 35;
            return worker;
        }

        private int RollHeight(string gender)
        {
            if (gender == "女")
            {
                return random.Next(155, 179);
            }

            return random.Next(165, 189);
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

        private int ComputeInterviewPreparationDelta(PaperShiftRunState state, CompanyDefinition company, JobDefinition job)
        {
            var aptitude = WorkerAptitude(state, job);
            var score = random.Next(-12, 13);
            score += MatchIntentWeight(state, job) / 16;
            score += aptitude / 28;
            score -= job.Difficulty / 18;
            score -= state.Resume.DeceptionRisk / 18;
            score -= state.Worker.Stress / 25;

            for (var i = 0; i < job.TagIds.Length; i++)
            {
                score += effects.SumPassive(state, EffectKind.PassiveInterviewScore, job.TagIds[i]) / 4;
            }

            for (var i = 0; i < company.TagIds.Length; i++)
            {
                score += effects.SumPassive(state, EffectKind.PassiveInterviewScore, company.TagIds[i]) / 4;
            }

            score += ResumePresentationBonus(state.Resume) / 3;
            return Clamp(score, -18, 18);
        }

        private int ComputeInterviewMeetingDelta(PaperShiftRunState state, CompanyDefinition company, JobDefinition job, int stage)
        {
            var aptitude = WorkerAptitude(state, job);
            var score = random.Next(-10, 11);
            score += MatchIntentWeight(state, job) / 22;
            score += aptitude / 36;
            score -= job.Difficulty / 24;
            score -= state.Resume.DeceptionRisk / 22;
            score -= state.Worker.Stress / 30;
            score += Math.Min(stage, 4) * 2;

            for (var i = 0; i < job.TagIds.Length; i++)
            {
                score += effects.SumPassive(state, EffectKind.PassiveInterviewScore, job.TagIds[i]) / 6;
            }

            for (var i = 0; i < company.TagIds.Length; i++)
            {
                score += effects.SumPassive(state, EffectKind.PassiveInterviewScore, company.TagIds[i]) / 6;
            }

            return Clamp(score, -14, 16);
        }

        private static string InterviewStageMessage(int stage, int delta, int satisfaction)
        {
            var deltaText = delta >= 0 ? "+" + delta : delta.ToString();
            var stageText = "进入下一轮沟通";
            switch (stage)
            {
                case 1:
                    stageText = "初面结束，对方约你进入二面";
                    break;
                case 2:
                    stageText = "二面聊完，对方安排你进入三面";
                    break;
                case 3:
                    stageText = "三面通过，对方把你推进到终面";
                    break;
                default:
                    stageText = "终面暂时没有拍板，对方要求补充沟通";
                    break;
            }

            return stageText + "。成功率 " + deltaText + "%，当前 " + satisfaction + "%。";
        }

        private int ComputeRegularizationReviewDelta(PaperShiftRunState state)
        {
            var score = random.Next(-14, 15);
            score += state.CurrentJob.Intensity <= 55 ? 4 : -3;
            score -= state.Worker.Stress / 28;
            score += state.CurrentJob.WorkYears / 2;
            return Clamp(score, -18, 18);
        }

        private static string RegularizationReviewMessage(int delta, int chance)
        {
            var deltaText = delta >= 0 ? "+" + delta : delta.ToString();
            if (delta > 0)
            {
                return "主管追加了一轮试用反馈，转正概率 " + deltaText + "%，当前 " + chance + "%。";
            }

            if (delta < 0)
            {
                return "转正审批被要求补材料，转正概率 " + deltaText + "%，当前 " + chance + "%。";
            }

            return "转正审批还在排队，概率暂时不变，当前 " + chance + "%。";
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
            var salary = random.Next(job.SalaryMin, job.SalaryMax + 1);
            var percent = 0;
            for (var i = 0; i < job.TagIds.Length; i++)
            {
                percent += effects.SumPassive(state, EffectKind.PassiveSalaryPercent, job.TagIds[i]);
            }

            return salary + salary * percent / 100;
        }

        private int StartingInterviewSuccessRate(PaperShiftRunState state, JobDefinition job)
        {
            var chance = 22 + MatchIntentWeight(state, job) / 4 + WorkerAptitude(state, job) / 12;
            chance -= job.Difficulty / 5;
            chance -= state.Resume.DeceptionRisk / 4;
            chance -= state.Worker.Stress / 16;
            return Clamp(chance, 5, 65);
        }

        private int StartingRegularizationChance(PaperShiftRunState state, JobDefinition job)
        {
            var chance = 24 + job.PromotionBase / 2 + WorkerAptitude(state, job) / 14;
            chance -= job.WorkIntensity / 8;
            chance -= state.Worker.Stress / 18;
            return Clamp(chance, 8, 70);
        }

        private void ApplyAnnualBudget(PaperShiftRunState state)
        {
            var yearlyIncome = state.CurrentJob.Salary * 12;
            var savings = yearlyIncome * state.Budget.Savings / 100;
            state.Worker.Money += savings;
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

        private void TickYear(PaperShiftRunState state)
        {
            state.CurrentYear++;
            state.Worker.Age++;
            state.Worker.TickTemporaryTags();
            state.TickEventCooldowns();
        }

        private bool ShouldRetire(PaperShiftRunState state)
        {
            return state.Worker.Age >= 60 || state.Worker.Health <= 0 || state.Worker.Stress >= 100 || state.CurrentJob.QuitRisk >= 100;
        }

        private RunEndReason RetirementReason(PaperShiftRunState state)
        {
            if (state.Worker.Age >= 60)
            {
                return RunEndReason.Retired;
            }

            if (state.Worker.Health <= 0)
            {
                return RunEndReason.HealthCollapse;
            }

            if (state.Worker.Stress >= 100)
            {
                return RunEndReason.StressCollapse;
            }

            return RunEndReason.Fired;
        }

        private void EnsureHeirs(PaperShiftRunState state)
        {
            if (state.Worker.Heirs.Count > 0)
            {
                return;
            }

            var count = random.Next(2, 4);
            for (var i = 0; i < count; i++)
            {
                var gender = random.NextDouble() < 0.5 ? "女" : "男";
                var first = gender == "女" ? Pick(database.FemaleFirstNames, "君语") : Pick(database.MaleFirstNames, "知行");
                var heir = new HeirProfile
                {
                    Id = "heir_" + (i + 1),
                    Name = state.Worker.LastName + first,
                    Gender = gender,
                    Age = random.Next(0, 22),
                    InheritancePercent = i == 0 ? 50 : 25,
                    TraitSummary = i == 0 ? "学习好，适合接班" : "属性随机，路线未定"
                };
                heir.Stats.Add(new StatValue { Id = PaperShiftWorkerAttributes.Family, Value = Clamp(state.Worker.GetStat(PaperShiftWorkerAttributes.Family) + state.Budget.Savings / 5, 0, 100) });
                heir.Stats.Add(new StatValue { Id = PaperShiftWorkerAttributes.Education, Value = Clamp(state.Worker.GetStat(PaperShiftWorkerAttributes.Education) + state.Budget.Education / 4, 0, 100) });
                heir.Stats.Add(new StatValue { Id = PaperShiftWorkerAttributes.Ability, Value = Clamp(state.Worker.GetStat(PaperShiftWorkerAttributes.Ability), 0, 100) });
                InheritTag(state, heir, "family_craft");
                InheritTag(state, heir, "good_accounting");
                state.Worker.Heirs.Add(heir);
            }
        }

        private void InheritTag(PaperShiftRunState state, HeirProfile heir, string tagId)
        {
            if (!state.Worker.HasTag(tagId) || random.NextDouble() > 0.45)
            {
                return;
            }

            var tag = database.FindTag(tagId);
            if (tag != null)
            {
                heir.Tags.Add(CreateTagInstance(tag, state.CurrentYear));
            }
        }

        private EraDefinition NextEra(string currentEraId)
        {
            for (var i = 0; i < database.Eras.Length; i++)
            {
                if (database.Eras[i].Id == currentEraId)
                {
                    return database.Eras[Math.Min(i + 1, database.Eras.Length - 1)];
                }
            }

            return database.Eras.Length == 0 ? null : database.Eras[0];
        }

        private EventOptionDefinition[] AvailableOptions(GameEventDefinition gameEvent, PaperShiftRunState state, CompanyDefinition company, JobDefinition job)
        {
            var result = new List<EventOptionDefinition>();
            for (var i = 0; i < gameEvent.Options.Length; i++)
            {
                var option = gameEvent.Options[i];
                if (conditions.AreMet(option.Conditions, state, company, job, random))
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

        private static int MatchIntentWeight(PaperShiftRunState state, JobDefinition job)
        {
            var weight = 0;
            for (var i = 0; i < state.Resume.IntentTagIds.Count; i++)
            {
                for (var j = 0; j < job.IntentTagIds.Length; j++)
                {
                    if (state.Resume.IntentTagIds[i] == job.IntentTagIds[j])
                    {
                        weight += 24;
                    }
                }
            }

            return weight;
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

        private string Pick(string[] values, string fallback)
        {
            if (values == null || values.Length == 0)
            {
                return fallback;
            }

            return values[random.Next(0, values.Length)];
        }

        private static string ReasonText(RunEndReason reason)
        {
            switch (reason)
            {
                case RunEndReason.Retired:
                    return "到龄退休，正常进入下一代。";
                case RunEndReason.Fired:
                    return "工作关系破裂，被迫离开岗位。";
                case RunEndReason.Quit:
                    return "主动停止工作，准备开启下一代。";
                case RunEndReason.HealthCollapse:
                    return "健康崩溃，无法继续工作。";
                case RunEndReason.StressCollapse:
                    return "压力过大，无法继续工作。";
                case RunEndReason.Accident:
                    return "意外事故结束了这一代工作。";
                default:
                    return "这一代打工人生结束了。";
            }
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

    public enum InterviewStepOutcome
    {
        Continue,
        Passed,
        Failed,
        Event
    }

    public sealed class InterviewStepResult
    {
        public readonly InterviewStepOutcome Outcome;
        public readonly string Message;
        public readonly TriggeredEvent TriggeredEvent;
        public readonly int SatisfactionBefore;
        public readonly int SatisfactionAfter;

        public int SatisfactionDelta
        {
            get { return SatisfactionAfter - SatisfactionBefore; }
        }

        private InterviewStepResult(InterviewStepOutcome outcome, string message, TriggeredEvent triggeredEvent, int satisfactionBefore, int satisfactionAfter)
        {
            Outcome = outcome;
            Message = message;
            TriggeredEvent = triggeredEvent;
            SatisfactionBefore = satisfactionBefore;
            SatisfactionAfter = satisfactionAfter;
        }

        public static InterviewStepResult Continue(string message, int satisfactionBefore, int satisfactionAfter)
        {
            return new InterviewStepResult(InterviewStepOutcome.Continue, message, null, satisfactionBefore, satisfactionAfter);
        }

        public static InterviewStepResult Passed(string message, int satisfactionBefore, int satisfactionAfter)
        {
            return new InterviewStepResult(InterviewStepOutcome.Passed, message, null, satisfactionBefore, satisfactionAfter);
        }

        public static InterviewStepResult Failed(string message)
        {
            return new InterviewStepResult(InterviewStepOutcome.Failed, message, null, 0, 0);
        }

        public static InterviewStepResult Failed(string message, int satisfactionBefore, int satisfactionAfter)
        {
            return new InterviewStepResult(InterviewStepOutcome.Failed, message, null, satisfactionBefore, satisfactionAfter);
        }

        public static InterviewStepResult Event(TriggeredEvent triggeredEvent, int satisfactionBefore, int satisfactionAfter)
        {
            return new InterviewStepResult(InterviewStepOutcome.Event, string.Empty, triggeredEvent, satisfactionBefore, satisfactionAfter);
        }
    }

    public enum ProbationStepOutcome
    {
        Continue,
        Passed,
        Failed,
        Event
    }

    public sealed class ProbationStepResult
    {
        public readonly ProbationStepOutcome Outcome;
        public readonly string Message;
        public readonly TriggeredEvent TriggeredEvent;
        public readonly int ProgressBefore;
        public readonly int ProgressAfter;
        public readonly int RiskBefore;
        public readonly int RiskAfter;

        public int ProgressDelta
        {
            get { return ProgressAfter - ProgressBefore; }
        }

        public int RiskDelta
        {
            get { return RiskAfter - RiskBefore; }
        }

        private ProbationStepResult(ProbationStepOutcome outcome, string message, TriggeredEvent triggeredEvent, int progressBefore, int progressAfter, int riskBefore, int riskAfter)
        {
            Outcome = outcome;
            Message = message;
            TriggeredEvent = triggeredEvent;
            ProgressBefore = progressBefore;
            ProgressAfter = progressAfter;
            RiskBefore = riskBefore;
            RiskAfter = riskAfter;
        }

        public static ProbationStepResult Continue(string message, int progressBefore, int progressAfter, int riskBefore, int riskAfter)
        {
            return new ProbationStepResult(ProbationStepOutcome.Continue, message, null, progressBefore, progressAfter, riskBefore, riskAfter);
        }

        public static ProbationStepResult Passed(string message, int progressBefore, int progressAfter, int riskBefore, int riskAfter)
        {
            return new ProbationStepResult(ProbationStepOutcome.Passed, message, null, progressBefore, progressAfter, riskBefore, riskAfter);
        }

        public static ProbationStepResult Failed(string message, int progressBefore, int progressAfter, int riskBefore, int riskAfter)
        {
            return new ProbationStepResult(ProbationStepOutcome.Failed, message, null, progressBefore, progressAfter, riskBefore, riskAfter);
        }

        public static ProbationStepResult Event(TriggeredEvent triggeredEvent, int progressBefore, int progressAfter, int riskBefore, int riskAfter)
        {
            return new ProbationStepResult(ProbationStepOutcome.Event, string.Empty, triggeredEvent, progressBefore, progressAfter, riskBefore, riskAfter);
        }
    }

    public sealed class TriggeredEvent
    {
        public readonly GameEventDefinition Event;
        public readonly EventOptionDefinition[] Options;

        public TriggeredEvent(GameEventDefinition gameEvent, EventOptionDefinition[] options)
        {
            Event = gameEvent;
            Options = options;
        }
    }
}
