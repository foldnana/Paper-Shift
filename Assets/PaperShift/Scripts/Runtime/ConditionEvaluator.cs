using System;
using PaperShift.Data;
using PaperShift.Domain;

namespace PaperShift.Runtime
{
    public sealed class ConditionEvaluator
    {
        public bool AreMet(ConditionDefinition[] conditions, PaperShiftRunState state, CompanyDefinition company, JobDefinition job, Random random)
        {
            if (conditions == null || conditions.Length == 0)
            {
                return true;
            }

            for (var i = 0; i < conditions.Length; i++)
            {
                if (!IsMet(conditions[i], state, company, job, random))
                {
                    return false;
                }
            }

            return true;
        }

        public bool IsMet(ConditionDefinition condition, PaperShiftRunState state, CompanyDefinition company, JobDefinition job, Random random)
        {
            if (condition == null)
            {
                return true;
            }

            var result = Evaluate(condition, state, company, job, random);
            return condition.Invert ? !result : result;
        }

        private static bool Evaluate(ConditionDefinition condition, PaperShiftRunState state, CompanyDefinition company, JobDefinition job, Random random)
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
                default:
                    return false;
            }
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
