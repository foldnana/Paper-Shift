using System;
using PaperShift.Data;
using PaperShift.Domain;

namespace PaperShift.Runtime
{
    public sealed class ConditionEvaluator
    {
        public bool AreMet(ConditionDefinition[] conditions, PaperShiftRunState state, CompanyDefinition company, JobDefinition job, Random random, FlowCheckpointAction? action = null)
        {
            if (conditions == null || conditions.Length == 0)
            {
                return true;
            }

            for (var i = 0; i < conditions.Length; i++)
            {
                if (!IsMet(conditions[i], state, company, job, random, action))
                {
                    return false;
                }
            }

            return true;
        }

        public bool IsMet(ConditionDefinition condition, PaperShiftRunState state, CompanyDefinition company, JobDefinition job, Random random, FlowCheckpointAction? action = null)
        {
            if (condition == null)
            {
                return true;
            }

            var result = Evaluate(condition, state, company, job, random, action);
            return condition.Invert ? !result : result;
        }

        private static bool Evaluate(ConditionDefinition condition, PaperShiftRunState state, CompanyDefinition company, JobDefinition job, Random random, FlowCheckpointAction? action)
        {
            switch (condition.Kind)
            {
                case ConditionKind.Always:
                    return true;
                case ConditionKind.Stat:
                    return Compare(state.Worker.GetStat(condition.Key), condition.IntValue, condition.Operator);
                case ConditionKind.HasTag:
                    return state.Worker.HasTag(condition.Key);
                case ConditionKind.MissingTag:
                    return !state.Worker.HasTag(condition.Key);
                case ConditionKind.BudgetAtLeast:
                    return state.Budget.GetCategory(condition.Key) >= condition.IntValue;
                case ConditionKind.BudgetAtMost:
                    return state.Budget.GetCategory(condition.Key) <= condition.IntValue;
                case ConditionKind.Phase:
                    return state.Phase.ToString() == condition.TextValue;
                case ConditionKind.AgeAtLeast:
                    return state.Worker.Age >= condition.IntValue;
                case ConditionKind.AgeAtMost:
                    return state.Worker.Age <= condition.IntValue;
                case ConditionKind.MoneyAtLeast:
                    return state.Worker.Money >= condition.IntValue;
                case ConditionKind.CompanyHasTag:
                    return company != null && company.HasTag(condition.Key);
                case ConditionKind.JobHasTag:
                    return job != null && job.HasTag(condition.Key);
                case ConditionKind.RandomChance:
                    return random.NextDouble() < condition.FloatValue;
                case ConditionKind.EventSeen:
                    return state.HasSeenEvent(condition.Key);
                case ConditionKind.WorkYearsAtLeast:
                    return state.CurrentJob.WorkYears >= condition.IntValue;
                case ConditionKind.ResumeRiskAtLeast:
                    return state.Resume.DeceptionRisk >= condition.IntValue;
                case ConditionKind.StressAtLeast:
                    return state.Worker.Stress >= condition.IntValue;
                case ConditionKind.StressAtMost:
                    return state.Worker.Stress <= condition.IntValue;
                case ConditionKind.RecognitionAtLeast:
                    return Recognition(state) >= condition.IntValue;
                case ConditionKind.RecognitionAtMost:
                    return Recognition(state) <= condition.IntValue;
                case ConditionKind.ResumeFieldMode:
                    return ResumeFieldModeMatches(state, condition.Key, condition.TextValue, condition.IntValue);
                case ConditionKind.ResumeFieldHidden:
                    return ResumeFieldMode(state, condition.Key) == ResumePackagingMode.Hide;
                case ConditionKind.ResumeFieldExaggerated:
                    return IsResumeFieldExaggerated(ResumeFieldMode(state, condition.Key));
                case ConditionKind.AnyResumeFieldHidden:
                    return HasAnyResumeFieldMode(state, ResumePackagingMode.Hide);
                case ConditionKind.AnyResumeFieldExaggerated:
                    return HasAnyExaggeratedResumeField(state);
                case ConditionKind.ResumeTagHidden:
                    return state.Resume != null && state.Resume.HiddenTagIds != null && state.Resume.HiddenTagIds.Contains(condition.Key);
                case ConditionKind.AnyResumeTagHidden:
                    return state.Resume != null && state.Resume.HiddenTagIds != null && state.Resume.HiddenTagIds.Count > 0;
                case ConditionKind.ActionIs:
                    return ActionMatches(action, condition);
                case ConditionKind.CurrentJobMonthsAtLeast:
                    return CurrentJobMonths(state) >= condition.IntValue;
                case ConditionKind.CurrentJobMonthsAtMost:
                    return CurrentJobMonths(state) <= condition.IntValue;
                default:
                    return false;
            }
        }

        private static bool ActionMatches(FlowCheckpointAction? action, ConditionDefinition condition)
        {
            if (!action.HasValue || condition == null)
            {
                return false;
            }

            var expected = string.IsNullOrEmpty(condition.TextValue) ? condition.Key : condition.TextValue;
            if (!string.IsNullOrEmpty(expected))
            {
                if (Enum.TryParse(expected.Trim(), true, out FlowCheckpointAction parsed))
                {
                    return action.Value == parsed;
                }

                switch (expected.Trim().ToLowerInvariant())
                {
                    case "prepare":
                    case "prepare_interview":
                    case "interview_prepare":
                        return action.Value == FlowCheckpointAction.PrepareInterview;
                    case "attend":
                    case "interview":
                    case "attend_interview":
                    case "interview_attend":
                        return action.Value == FlowCheckpointAction.AttendInterview;
                    case "work":
                    case "probation":
                    case "work_probation":
                    case "probation_work":
                        return action.Value == FlowCheckpointAction.WorkProbation;
                    case "apply":
                    case "regularization":
                    case "apply_regularization":
                    case "regularize":
                        return action.Value == FlowCheckpointAction.ApplyRegularization;
                    case "event":
                    case "event_choice":
                    case "choice":
                        return action.Value == FlowCheckpointAction.EventChoice;
                }

                return false;
            }

            return condition.IntValue > 0 && condition.IntValue <= (int)FlowCheckpointAction.EventChoice && action.Value == (FlowCheckpointAction)condition.IntValue;
        }

        private static int CurrentJobMonths(PaperShiftRunState state)
        {
            if (state == null || state.CurrentJob == null || string.IsNullOrEmpty(state.CurrentJob.JobId))
            {
                return 0;
            }

            var startYear = state.CurrentJob.StartYear > 0 ? state.CurrentJob.StartYear : state.CurrentYear;
            var startMonth = NormalizeMonth(state.CurrentJob.StartMonth);
            var currentMonth = NormalizeMonth(state.CurrentMonth);
            var months = (state.CurrentYear - startYear) * 12 + currentMonth - startMonth;
            return months < 0 ? 0 : months;
        }

        private static bool ResumeFieldModeMatches(PaperShiftRunState state, string fieldId, string textValue, int intValue)
        {
            var expected = ParseResumePackagingMode(textValue, intValue);
            return ResumeFieldMode(state, fieldId) == expected;
        }

        private static bool HasAnyResumeFieldMode(PaperShiftRunState state, ResumePackagingMode mode)
        {
            if (state == null || state.Resume == null || state.Resume.Packaging == null)
            {
                return false;
            }

            for (var i = 0; i < state.Resume.Packaging.Count; i++)
            {
                var choice = state.Resume.Packaging[i];
                if (choice != null && choice.OptionIndex >= 0 && choice.Mode == mode)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasAnyExaggeratedResumeField(PaperShiftRunState state)
        {
            if (state == null || state.Resume == null || state.Resume.Packaging == null)
            {
                return false;
            }

            for (var i = 0; i < state.Resume.Packaging.Count; i++)
            {
                var choice = state.Resume.Packaging[i];
                if (choice != null && choice.OptionIndex >= 0 && IsResumeFieldExaggerated(choice.Mode))
                {
                    return true;
                }
            }

            return false;
        }

        private static ResumePackagingMode ResumeFieldMode(PaperShiftRunState state, string fieldId)
        {
            if (state == null || state.Resume == null || state.Resume.Packaging == null || string.IsNullOrEmpty(fieldId))
            {
                return ResumePackagingMode.Normal;
            }

            fieldId = PaperShiftWorkerAttributes.Canonicalize(fieldId);
            for (var i = 0; i < state.Resume.Packaging.Count; i++)
            {
                var choice = state.Resume.Packaging[i];
                if (choice == null || choice.OptionIndex < 0)
                {
                    continue;
                }

                if (PaperShiftWorkerAttributes.Canonicalize(choice.FieldId) == fieldId)
                {
                    return choice.Mode;
                }
            }

            return ResumePackagingMode.Normal;
        }

        private static ResumePackagingMode ParseResumePackagingMode(string textValue, int intValue)
        {
            switch ((textValue ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "hide":
                case "hidden":
                case "隐藏":
                case "隐瞒":
                    return ResumePackagingMode.Hide;
                case "exaggerate":
                case "exaggerated":
                case "夸大":
                    return ResumePackagingMode.Exaggerate;
                case "fake":
                case "伪造":
                    return ResumePackagingMode.Fake;
                case "normal":
                case "正常":
                    return ResumePackagingMode.Normal;
            }

            return intValue >= 0 && intValue <= 3 ? (ResumePackagingMode)intValue : ResumePackagingMode.Normal;
        }

        private static bool IsResumeFieldExaggerated(ResumePackagingMode mode)
        {
            return mode == ResumePackagingMode.Exaggerate || mode == ResumePackagingMode.Fake;
        }

        private static int NormalizeMonth(int month)
        {
            if (month < 1)
            {
                return 1;
            }

            return month > 12 ? 12 : month;
        }

        private static int Recognition(PaperShiftRunState state)
        {
            if (state.Phase == PaperShiftPhase.Probation || state.HasActiveJob)
            {
                return state.CurrentJob.Recognition;
            }

            return state.Interview.Recognition;
        }

        private static bool Compare(int current, int expected, CompareOperator op)
        {
            switch (op)
            {
                case CompareOperator.Equal:
                    return current == expected;
                case CompareOperator.NotEqual:
                    return current != expected;
                case CompareOperator.GreaterOrEqual:
                    return current >= expected;
                case CompareOperator.Greater:
                    return current > expected;
                case CompareOperator.LessOrEqual:
                    return current <= expected;
                case CompareOperator.Less:
                    return current < expected;
                default:
                    return false;
            }
        }
    }
}
