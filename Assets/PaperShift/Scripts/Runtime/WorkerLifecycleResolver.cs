using System;
using PaperShift.Data;
using PaperShift.Domain;

namespace PaperShift.Runtime
{
    internal sealed class WorkerLifecycleResolver
    {
        private readonly PaperShiftDatabase database;
        private readonly Func<Random> randomProvider;

        public WorkerLifecycleResolver(PaperShiftDatabase database, Func<Random> randomProvider)
        {
            this.database = database;
            this.randomProvider = randomProvider;
        }

        private Random Random
        {
            get { return randomProvider == null ? null : randomProvider(); }
        }

        public WorkerProfile CreateRandomWorker(int currentYear, int generation)
        {
            var random = Random;
            var worker = new WorkerProfile
            {
                Id = Guid.NewGuid().ToString("N"),
                LastName = Pick(database.LastNames, "李"),
                Gender = random.NextDouble() < 0.5 ? "女" : "男",
                Personality = Pick(new[] { "沉稳", "谨慎", "开朗", "灵活", "较真", "随和" }, "沉稳"),
                EraId = string.Empty,
                EraName = currentYear + "年",
                Generation = generation,
                Age = random.Next(18, 31),
                Stress = random.Next(5, 26),
                Health = random.Next(65, 96),
                Money = 0
            };
            worker.FirstName = worker.Gender == "女" ? Pick(database.FemaleFirstNames, "小满") : Pick(database.MaleFirstNames, "知行");
            worker.BirthYear = currentYear - worker.Age;

            for (var i = 0; i < database.Stats.Length; i++)
            {
                var stat = database.Stats[i];
                worker.SetStat(stat.Id, random.Next(stat.StartMin, stat.StartMax + 1));
            }

            worker.SetStat(PaperShiftWorkerAttributes.Height, RollHeight(worker.Gender));
            return worker;
        }

        public void CompleteGenerationByHire(PaperShiftRunState state)
        {
            if (state == null)
            {
                return;
            }

            state.Phase = PaperShiftPhase.Retirement;
            state.Retirement.Reason = RunEndReason.Custom;
            state.Retirement.ReasonText = "通过试用期，正式入职，这一代结算。";
            state.Retirement.FinalSavings = state.Worker.Money;
            state.Retirement.WorkYears = state.CurrentJob.WorkYears;
            state.Retirement.FinalJobTitle = state.CurrentJob.JobTitle;
            EnsureHeirs(state);
            state.AddLog(state.Retirement.ReasonText, EventNoticeType.Banner);
        }

        public void Retire(PaperShiftRunState state, RunEndReason reason)
        {
            if (state == null)
            {
                return;
            }

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
            if (state == null || state.Worker == null || state.Worker.Heirs.Count == 0 || heirIndex < 0 || heirIndex >= state.Worker.Heirs.Count)
            {
                return false;
            }

            var heir = state.Worker.Heirs[heirIndex];
            var yearAdvance = Math.Max(1, 18 - heir.Age);
            var nextYear = state.CurrentYear + yearAdvance;
            var next = CreateRandomWorker(nextYear, state.Generation + 1);
            next.FirstName = heir.Name.Length > state.Worker.LastName.Length ? heir.Name.Substring(state.Worker.LastName.Length) : heir.Name;
            next.Gender = heir.Gender;
            next.Age = Math.Max(18, heir.Age + yearAdvance);
            next.BirthYear = nextYear - next.Age;
            next.Tags.AddRange(heir.Tags);
            for (var i = 0; i < heir.Stats.Count; i++)
            {
                next.SetStat(heir.Stats[i].Id, heir.Stats[i].Value);
            }

            state.Generation++;
            state.CurrentYear = nextYear;
            state.Worker = next;
            state.Resume = new ResumeProfile();
            state.Interview = new InterviewState();
            state.CurrentJob = new CurrentJobState();
            state.Retirement = new RetirementState();
            state.Logs.Clear();
            state.Banners.Clear();
            state.Phase = PaperShiftPhase.CreateWorker;
            state.AddLog("第 " + state.Generation + " 代打工人生开始了。", EventNoticeType.Banner);
            return true;
        }

        public bool ShouldRetire(PaperShiftRunState state)
        {
            return state != null && state.Worker != null && (state.Worker.Age >= 60 || state.Worker.Health <= 0 || state.Worker.Stress >= 100);
        }

        public RunEndReason RetirementReason(PaperShiftRunState state)
        {
            if (state == null || state.Worker == null)
            {
                return RunEndReason.Custom;
            }

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

        public void EnsureHeirs(PaperShiftRunState state)
        {
            if (state == null || state.Worker == null || state.Worker.Heirs.Count > 0)
            {
                return;
            }

            var random = Random;
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

        public static string ReasonText(RunEndReason reason)
        {
            switch (reason)
            {
                case RunEndReason.Retired:
                    return "年龄到了退休线，人生进入下一段。";
                case RunEndReason.Fired:
                    return "岗位发生变化，你离开了公司，准备重新规划。";
                case RunEndReason.Quit:
                    return "你主动结束了这份工作，人生转向新的路线。";
                case RunEndReason.HealthCollapse:
                    return "身体已经扛不住了，这份工作只能停下。";
                case RunEndReason.StressCollapse:
                    return "压力长期积累，这份工作只能停下。";
                case RunEndReason.Accident:
                    return "突发事件改变了节奏，这份工作只能停下。";
                default:
                    return "这一代打工人的故事暂时告一段落。";
            }
        }

        private int RollHeight(string gender)
        {
            var random = Random;
            if (gender == "女")
            {
                return random.Next(155, 179);
            }

            return random.Next(165, 189);
        }

        private void InheritTag(PaperShiftRunState state, HeirProfile heir, string tagId)
        {
            if (!state.Worker.HasTag(tagId) || Random.NextDouble() > 0.45)
            {
                return;
            }

            var tag = database.FindTag(tagId);
            if (tag != null)
            {
                heir.Tags.Add(CreateTagInstance(tag, state.CurrentYear));
            }
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

            return values[Random.Next(0, values.Length)];
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
