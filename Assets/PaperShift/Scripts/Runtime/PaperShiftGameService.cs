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
                Satisfaction = 0
            };
            state.Phase = PaperShiftPhase.Interview;
            state.AddLog("投递到了 " + pair.Company.DisplayName + " · " + pair.Job.DisplayName + "。");
            return true;
        }

        public InterviewStepResult AdvanceInterviewStep(PaperShiftRunState state)
        {
            var company = database.FindCompany(state.Interview.CompanyId);
            var job = database.FindJob(state.Interview.CompanyId, state.Interview.JobId);
            if (company == null || job == null)
            {
                return InterviewStepResult.Failed("还没有可推进的面试。");
            }

            state.Interview.Round++;
            var satisfactionBefore = state.Interview.Satisfaction;
            var score = ComputeInterviewScore(state, company, job);
            state.Interview.Satisfaction = Clamp(state.Interview.Satisfaction + score, 0, 100);
            var satisfactionAfter = state.Interview.Satisfaction;
            state.AddLog("第" + state.Interview.Round + "轮面试结束，满意度变为 " + state.Interview.Satisfaction + "。");

            var triggered = TryTriggerEvent(state, GameEventPhase.Interview, company, job);
            if (triggered != null)
            {
                return InterviewStepResult.Event(triggered, satisfactionBefore, satisfactionAfter);
            }

            if (state.Interview.HasOffer)
            {
                var message = "面试通过，" + company.DisplayName + " 发来了 Offer，你已入职 " + job.DisplayName + "。";
                state.Interview.Satisfaction = Math.Max(state.Interview.Satisfaction, state.Interview.OfferThreshold);
                AcceptOffer(state);
                return InterviewStepResult.Passed(message, satisfactionBefore, satisfactionAfter);
            }

            if (state.Interview.Round < state.Interview.MaxRounds && random.Next(0, 100) < EarlyInterviewFailureChance(state, job))
            {
                var message = "面试提前结束。面试官认为匹配度不够，你还在求职状态，可以再投一家。";
                state.Worker.Stress = Clamp(state.Worker.Stress + 6, 0, 100);
                state.AddLog(message, EventNoticeType.Banner);
                return InterviewStepResult.Failed(message, satisfactionBefore, satisfactionAfter);
            }

            if (state.Interview.Round >= state.Interview.MaxRounds)
            {
                var message = "面试失败。对方认为匹配度不够，你还在求职状态，可以再投一家。";
                state.Worker.Stress = Clamp(state.Worker.Stress + 8, 0, 100);
                state.AddLog(message, EventNoticeType.Banner);
                return InterviewStepResult.Failed(message, satisfactionBefore, satisfactionAfter);
            }

            return InterviewStepResult.Continue("第" + state.Interview.Round + "轮面试结束，满意度变为 " + state.Interview.Satisfaction + "，等待下一轮面试。", satisfactionBefore, satisfactionAfter);
        }

        public TriggeredEvent AdvanceInterview(PaperShiftRunState state)
        {
            var result = AdvanceInterviewStep(state);
            return result == null ? null : result.TriggeredEvent;
        }

        public bool AcceptOffer(PaperShiftRunState state)
        {
            if (!state.Interview.HasOffer)
            {
                var chance = state.Interview.Satisfaction / 100f;
                if (random.NextDouble() > chance)
                {
                    state.AddLog("你申请入职，但对方没有同意。");
                    return false;
                }
            }

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
                PromotionProgress = 0,
                QuitRisk = job.QuitRiskBase,
                WorkYears = 0
            };
            state.Interview = new InterviewState();
            state.Phase = PaperShiftPhase.Working;
            state.AddLog("正式入职 " + company.DisplayName + " · " + job.DisplayName + "。", EventNoticeType.Banner);
            return true;
        }

        public bool ResolveInterviewResult(PaperShiftRunState state, out string message)
        {
            message = string.Empty;
            var company = database.FindCompany(state.Interview.CompanyId);
            var job = database.FindJob(state.Interview.CompanyId, state.Interview.JobId);
            if (company == null || job == null)
            {
                message = "还没有可询问的面试结果。";
                return false;
            }

            if (state.Interview.Round <= 0)
            {
                state.Interview.Round = 1;
                state.Interview.Satisfaction = Clamp(state.Interview.Satisfaction + ComputeInterviewScore(state, company, job), 0, 100);
            }

            var aptitude = WorkerAptitude(state, job) / 5;
            var chance = state.Interview.Satisfaction + 25 - job.Difficulty / 2 + aptitude - state.Resume.DeceptionRisk / 2;
            chance = Clamp(chance, 8, 92);
            var success = random.Next(0, 100) < chance;
            if (success)
            {
                state.Interview.Satisfaction = Math.Max(state.Interview.Satisfaction, state.Interview.OfferThreshold);
                AcceptOffer(state);
                message = "面试通过！" + company.DisplayName + " 发来了 Offer，你已入职 " + job.DisplayName + "。";
                return true;
            }

            state.Worker.Stress = Clamp(state.Worker.Stress + 6, 0, 100);
            state.Interview.Satisfaction = Clamp(state.Interview.Satisfaction - random.Next(8, 20), 0, 100);
            state.Phase = PaperShiftPhase.Interview;
            message = "面试失败。对方认为匹配度不够，你还在求职状态，可以再投一家。";
            state.AddLog(message, EventNoticeType.Banner);
            return false;
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

            worker.Money += 1000 + worker.GetStat("family") * 120;
            return worker;
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

        private int ComputeInterviewScore(PaperShiftRunState state, CompanyDefinition company, JobDefinition job)
        {
            var score = 12 - job.Difficulty / 10 + WorkerAptitude(state, job) / 7;
            score += MatchIntentWeight(state, job) / 4;
            for (var i = 0; i < job.TagIds.Length; i++)
            {
                score += effects.SumPassive(state, EffectKind.PassiveInterviewScore, job.TagIds[i]);
            }

            for (var i = 0; i < company.TagIds.Length; i++)
            {
                score += effects.SumPassive(state, EffectKind.PassiveInterviewScore, company.TagIds[i]);
            }

            score += ResumePresentationBonus(state.Resume);
            score -= state.Worker.Stress / 12;
            return Clamp(score, -30, 45);
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

        private static int EarlyInterviewFailureChance(PaperShiftRunState state, JobDefinition job)
        {
            if (state.Interview.Satisfaction >= 35)
            {
                return 0;
            }

            var chance = 8 + (35 - state.Interview.Satisfaction) / 2 + job.Difficulty / 20 + state.Resume.DeceptionRisk / 8;
            return Clamp(chance, 0, 35);
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
                state.Worker.AddStat("literacy", 1);
                state.Worker.AddStat("logic", 1);
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
                heir.Stats.Add(new StatValue { Id = "family", Value = Clamp(state.Worker.GetStat("family") + state.Budget.Savings / 5, 0, 100) });
                heir.Stats.Add(new StatValue { Id = "literacy", Value = Clamp(state.Worker.GetStat("literacy") + state.Budget.Education / 4, 0, 100) });
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
