using PaperShift.Data;
using PaperShift.Domain;

namespace PaperShift.Runtime
{
    public sealed class FlowRuleResolver
    {
        private readonly PaperShiftDatabase database;
        private readonly ConditionEvaluator conditions;

        public FlowRuleResolver(PaperShiftDatabase database, ConditionEvaluator conditions)
        {
            this.database = database;
            this.conditions = conditions;
        }

        public FlowRuleResult Evaluate(PaperShiftRunState state, CompanyDefinition company, JobDefinition job, FitProfile profile, GameEventPhase phase, System.Random random)
        {
            var result = new FlowRuleResult();
            if (state == null || job == null || profile == null)
            {
                return result;
            }

            ApplyWorkTags(result, state, company, job, profile, phase, random);
            ApplyWorkerBaseEffects(result, state);
            ApplyWorkerConditionalEffects(result, state, company, job, phase, random);
            return result;
        }

        public int JobRollWeight(PaperShiftRunState state, CompanyDefinition company, JobDefinition job, FitProfile profile, System.Random random)
        {
            var weight = 100;
            var result = new FlowRuleResult();
            ApplyWorkerJobRollEffects(result, state, company, job, random);
            weight += result.JobWeightDelta;
            return weight < 1 ? 1 : weight;
        }

        private void ApplyWorkTags(FlowRuleResult result, PaperShiftRunState state, CompanyDefinition company, JobDefinition job, FitProfile profile, GameEventPhase phase, System.Random random)
        {
            ApplyWorkTagList(result, state, company, job, profile, phase, random, company == null ? null : company.TagIds);
            ApplyWorkTagList(result, state, company, job, profile, phase, random, job.TagIds);
        }

        private void ApplyWorkTagList(FlowRuleResult result, PaperShiftRunState state, CompanyDefinition company, JobDefinition job, FitProfile profile, GameEventPhase phase, System.Random random, string[] tagIds)
        {
            if (tagIds == null)
            {
                return;
            }

            for (var i = 0; i < tagIds.Length; i++)
            {
                var workTag = database.FindWorkTag(tagIds[i]);
                if (workTag == null)
                {
                    continue;
                }

                ApplyRequirements(result, state, profile, workTag.Requirements);
                ApplyEffects(result, workTag.Effects);
                ApplyEventWeights(result, state, company, job, random, workTag.EventWeights);
            }
        }

        private void ApplyRequirements(FlowRuleResult result, PaperShiftRunState state, FitProfile profile, WorkRequirementDefinition[] requirements)
        {
            if (requirements == null)
            {
                return;
            }

            for (var i = 0; i < requirements.Length; i++)
            {
                var requirement = requirements[i];
                if (requirement == null)
                {
                    continue;
                }

                var passed = IsRequirementMet(state, profile, requirement);
                if (passed)
                {
                    result.RecognitionDelta += requirement.RecognitionOnPass;
                    result.StressDelta += requirement.StressOnPass;
                    continue;
                }

                result.RecognitionDelta += requirement.RecognitionOnFail;
                result.StressDelta += requirement.StressOnFail;
                if (!string.IsNullOrEmpty(requirement.FailEventId))
                {
                    result.AddEventWeight(requirement.FailEventId, 20);
                }

                if (requirement.HardFail)
                {
                    result.SetDirective(FlowDirective.DirectFail);
                }
            }
        }

        private static bool IsRequirementMet(PaperShiftRunState state, FitProfile profile, WorkRequirementDefinition requirement)
        {
            var current = 0;
            switch (requirement.Target)
            {
                case RequirementTarget.FitDimension:
                    current = profile.Get(requirement.Dimension);
                    break;
                case RequirementTarget.RawAttribute:
                    current = RawAttributeValue(state, requirement.Key);
                    break;
                case RequirementTarget.WorkerTag:
                    return state.Worker.HasTag(requirement.Key);
                default:
                    return true;
            }

            return Compare(current, requirement.IntValue, requirement.Operator);
        }

        private static int RawAttributeValue(PaperShiftRunState state, string key)
        {
            key = PaperShiftWorkerAttributes.Canonicalize(key);
            switch (key)
            {
                case PaperShiftWorkerAttributes.Age:
                    return state.Worker.Age;
                case PaperShiftWorkerAttributes.Height:
                    return state.Worker.GetStat(PaperShiftWorkerAttributes.Height);
                default:
                    return state.Worker.GetStat(key);
            }
        }

        private void ApplyWorkerConditionalEffects(FlowRuleResult result, PaperShiftRunState state, CompanyDefinition company, JobDefinition job, GameEventPhase phase, System.Random random)
        {
            for (var i = 0; i < state.Worker.Tags.Count; i++)
            {
                var tag = database.FindTag(state.Worker.Tags[i].TagId);
                if (tag == null || tag.ConditionalEffects == null)
                {
                    continue;
                }

                for (var effectIndex = 0; effectIndex < tag.ConditionalEffects.Length; effectIndex++)
                {
                    var conditional = tag.ConditionalEffects[effectIndex];
                    if (conditional == null || !PhaseMatches(conditional.Phase, phase))
                    {
                        continue;
                    }

                    if (!WorkTagsMatch(conditional.WorkTagIds, company, job))
                    {
                        continue;
                    }

                    if (!conditions.AreMet(conditional.Conditions, state, company, job, random))
                    {
                        continue;
                    }

                    ApplyEffects(result, conditional.Effects);
                }
            }
        }

        private void ApplyWorkerBaseEffects(FlowRuleResult result, PaperShiftRunState state)
        {
            for (var i = 0; i < state.Worker.Tags.Count; i++)
            {
                var tag = database.FindTag(state.Worker.Tags[i].TagId);
                if (tag == null || tag.Effects == null)
                {
                    continue;
                }

                for (var effectIndex = 0; effectIndex < tag.Effects.Length; effectIndex++)
                {
                    var effect = tag.Effects[effectIndex];
                    if (effect == null || effect.Timing != EffectTiming.Passive || effect.Kind == EffectKind.AddFitScore)
                    {
                        continue;
                    }

                    ApplyEffect(result, effect);
                }
            }
        }

        private void ApplyWorkerJobRollEffects(FlowRuleResult result, PaperShiftRunState state, CompanyDefinition company, JobDefinition job, System.Random random)
        {
            if (state == null || state.Worker == null || state.Worker.Tags == null)
            {
                return;
            }

            for (var i = 0; i < state.Worker.Tags.Count; i++)
            {
                var tag = database.FindTag(state.Worker.Tags[i].TagId);
                if (tag == null)
                {
                    continue;
                }

                ApplyJobWeightEffects(result, tag.Effects);
                if (tag.ConditionalEffects == null)
                {
                    continue;
                }

                for (var effectIndex = 0; effectIndex < tag.ConditionalEffects.Length; effectIndex++)
                {
                    var conditional = tag.ConditionalEffects[effectIndex];
                    if (conditional == null || !PhaseMatches(conditional.Phase, GameEventPhase.Any))
                    {
                        continue;
                    }

                    if (!WorkTagsMatch(conditional.WorkTagIds, company, job))
                    {
                        continue;
                    }

                    if (!conditions.AreMet(conditional.Conditions, state, company, job, random))
                    {
                        continue;
                    }

                    ApplyJobWeightEffects(result, conditional.Effects);
                }
            }
        }

        private static void ApplyJobWeightEffects(FlowRuleResult result, EffectDefinition[] effects)
        {
            if (effects == null)
            {
                return;
            }

            for (var i = 0; i < effects.Length; i++)
            {
                var effect = effects[i];
                if (effect != null && effect.Kind == EffectKind.AddJobWeight)
                {
                    result.JobWeightDelta += effect.IntValue;
                }
            }
        }

        private static bool PhaseMatches(GameEventPhase expected, GameEventPhase actual)
        {
            return expected == GameEventPhase.Any || expected == actual || actual == GameEventPhase.Any;
        }

        private static bool WorkTagsMatch(string[] requiredTags, CompanyDefinition company, JobDefinition job)
        {
            if (requiredTags == null || requiredTags.Length == 0)
            {
                return true;
            }

            for (var i = 0; i < requiredTags.Length; i++)
            {
                var tagId = requiredTags[i];
                if ((company != null && company.HasTag(tagId)) || (job != null && job.HasTag(tagId)))
                {
                    return true;
                }
            }

            return false;
        }

        private void ApplyEventWeights(FlowRuleResult result, PaperShiftRunState state, CompanyDefinition company, JobDefinition job, System.Random random, EventWeightDefinition[] eventWeights)
        {
            if (eventWeights == null)
            {
                return;
            }

            for (var i = 0; i < eventWeights.Length; i++)
            {
                var modifier = eventWeights[i];
                if (modifier == null || !conditions.AreMet(modifier.Conditions, state, company, job, random))
                {
                    continue;
                }

                result.AddEventWeight(modifier.EventId, modifier.WeightDelta);
            }
        }

        public static void ApplyEffects(FlowRuleResult result, EffectDefinition[] effects)
        {
            if (effects == null)
            {
                return;
            }

            for (var i = 0; i < effects.Length; i++)
            {
                ApplyEffect(result, effects[i]);
            }
        }

        private static void ApplyEffect(FlowRuleResult result, EffectDefinition effect)
        {
            if (effect == null)
            {
                return;
            }

            switch (effect.Kind)
            {
                case EffectKind.AddRecognition:
                    result.RecognitionDelta += effect.IntValue;
                    break;
                case EffectKind.SetRecognition:
                    result.RecognitionOverride = effect.IntValue;
                    break;
                case EffectKind.AddJobWeight:
                    result.JobWeightDelta += effect.IntValue;
                    break;
                case EffectKind.AddStress:
                    result.StressDelta += effect.IntValue;
                    break;
                case EffectKind.AddResumeRisk:
                    result.ResumeRiskDelta += effect.IntValue;
                    break;
                case EffectKind.AddEventWeight:
                    result.AddEventWeight(effect.Key, effect.IntValue);
                    break;
                case EffectKind.TriggerEvent:
                    result.TriggerEventId = effect.Key;
                    break;
                case EffectKind.DirectPass:
                    result.SetDirective(FlowDirective.DirectPass);
                    break;
                case EffectKind.DirectFail:
                    result.SetDirective(FlowDirective.DirectFail);
                    break;
                case EffectKind.ReturnToJobSearch:
                    result.SetDirective(FlowDirective.ReturnToJobSearch);
                    break;
                case EffectKind.EndRun:
                    result.SetDirective(FlowDirective.EndRun);
                    break;
                case EffectKind.AddLog:
                    if (!string.IsNullOrEmpty(effect.TextValue))
                    {
                        result.Logs.Add(effect.TextValue);
                    }
                    break;
            }
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
