using PaperShift.Data;
using PaperShift.Domain;

namespace PaperShift.Runtime
{
    public sealed class EffectResolver
    {
        private readonly PaperShiftDatabase database;

        public EffectResolver(PaperShiftDatabase database)
        {
            this.database = database;
        }

        public void Apply(EffectDefinition[] effects, PaperShiftRunState state)
        {
            if (effects == null)
            {
                return;
            }

            for (var i = 0; i < effects.Length; i++)
            {
                Apply(effects[i], state);
            }
        }

        public void Apply(EffectDefinition effect, PaperShiftRunState state)
        {
            if (effect == null || effect.Timing == EffectTiming.Passive)
            {
                return;
            }

            switch (effect.Kind)
            {
                case EffectKind.AddStat:
                    state.Worker.AddStat(effect.Key, effect.IntValue);
                    break;
                case EffectKind.SetStat:
                    state.Worker.SetStat(effect.Key, effect.IntValue);
                    break;
                case EffectKind.AddMoney:
                    state.Worker.Money += effect.IntValue;
                    break;
                case EffectKind.AddStress:
                    state.Worker.Stress = Clamp(state.Worker.Stress + effect.IntValue, 0, 100);
                    break;
                case EffectKind.AddHealth:
                    state.Worker.Health = Clamp(state.Worker.Health + effect.IntValue, 0, 100);
                    break;
                case EffectKind.AddTag:
                    AddTag(effect, state);
                    break;
                case EffectKind.RemoveTag:
                    state.Worker.RemoveTag(effect.Key);
                    break;
                case EffectKind.AddHeir:
                    AddHeir(effect, state);
                    break;
                case EffectKind.AddLog:
                    state.AddLog(effect.TextValue, EventNoticeType.Log);
                    break;
                case EffectKind.AddBanner:
                    state.AddLog(effect.TextValue, EventNoticeType.Banner);
                    break;
                case EffectKind.AddResumeRisk:
                    state.Resume.DeceptionRisk = Clamp(state.Resume.DeceptionRisk + effect.IntValue, 0, 100);
                    break;
                case EffectKind.AddRecognition:
                    AddRecognition(state, effect.IntValue);
                    break;
                case EffectKind.SetRecognition:
                    SetRecognition(state, effect.IntValue);
                    break;
                case EffectKind.EndRun:
                    EndRun(effect, state);
                    break;
                case EffectKind.DirectFail:
                    state.AddLog(string.IsNullOrEmpty(effect.TextValue) ? "当前机会失败。" : effect.TextValue, EventNoticeType.Banner);
                    state.CurrentJob = new CurrentJobState();
                    state.Interview = new InterviewState();
                    state.Phase = PaperShiftPhase.Interview;
                    break;
                case EffectKind.ReturnToJobSearch:
                    state.CurrentJob = new CurrentJobState();
                    state.Interview = new InterviewState();
                    state.Phase = PaperShiftPhase.Interview;
                    break;
            }
        }

        public int SumPassive(PaperShiftRunState state, EffectKind kind, string key)
        {
            var total = 0;
            for (var i = 0; i < state.Worker.Tags.Count; i++)
            {
                var definition = database.FindTag(state.Worker.Tags[i].TagId);
                if (definition == null || definition.Effects == null)
                {
                    continue;
                }

                for (var effectIndex = 0; effectIndex < definition.Effects.Length; effectIndex++)
                {
                    var effect = definition.Effects[effectIndex];
                    if (effect.Timing != EffectTiming.Passive || effect.Kind != kind)
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(key) || effect.Key == key)
                    {
                        total += effect.IntValue;
                    }
                }
            }

            return total;
        }

        private void AddTag(EffectDefinition effect, PaperShiftRunState state)
        {
            var definition = database.FindTag(effect.Key);
            var tag = new TagInstance
            {
                TagId = effect.Key,
                DisplayName = definition == null ? effect.Key : definition.DisplayName,
                Scope = definition == null ? effect.TagScope : definition.Scope,
                RarityId = definition == null ? "normal" : definition.RarityId,
                Temporary = effect.Temporary,
                RemainingYears = effect.DurationYears,
                AcquiredYear = state.CurrentYear
            };

            state.Worker.AddTag(tag, definition == null || definition.Unique);
        }

        private static void AddHeir(EffectDefinition effect, PaperShiftRunState state)
        {
            var heir = new HeirProfile
            {
                Id = "heir_" + (state.Worker.Heirs.Count + 1),
                Name = string.IsNullOrEmpty(effect.TextValue) ? state.Worker.LastName + "新生儿" : effect.TextValue,
                Gender = string.IsNullOrEmpty(effect.SecondaryText) ? "未知" : effect.SecondaryText,
                Age = 0,
                InheritancePercent = effect.IntValue <= 0 ? 25 : effect.IntValue,
                TraitSummary = "由事件产生"
            };

            state.Worker.Heirs.Add(heir);
        }

        private static void EndRun(EffectDefinition effect, PaperShiftRunState state)
        {
            state.Phase = PaperShiftPhase.Retirement;
            state.Retirement.Reason = effect.EndReason == RunEndReason.None ? RunEndReason.Custom : effect.EndReason;
            state.Retirement.ReasonText = string.IsNullOrEmpty(effect.TextValue) ? "这一代打工人生结束了" : effect.TextValue;
            state.Retirement.FinalSavings = 0;
            state.Retirement.WorkYears = state.CurrentJob.WorkYears;
            state.Retirement.FinalJobTitle = state.CurrentJob.JobTitle;
        }

        private static void AddRecognition(PaperShiftRunState state, int delta)
        {
            if (state.Phase == PaperShiftPhase.Probation || state.HasActiveJob)
            {
                state.CurrentJob.Recognition = Clamp(state.CurrentJob.Recognition + delta, 0, 100);
                return;
            }

            state.Interview.Recognition = Clamp(state.Interview.Recognition + delta, 0, 100);
        }

        private static void SetRecognition(PaperShiftRunState state, int value)
        {
            if (state.Phase == PaperShiftPhase.Probation || state.HasActiveJob)
            {
                state.CurrentJob.Recognition = Clamp(value, 0, 100);
                return;
            }

            state.Interview.Recognition = Clamp(value, 0, 100);
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
