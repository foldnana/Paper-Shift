using System;
using System.Collections.Generic;
using PaperShift.Data;
using PaperShift.Domain;

namespace PaperShift.Runtime
{
    internal sealed class LaterLifeSimulator
    {
        private readonly PaperShiftDatabase database;
        private readonly Func<Random> randomProvider;

        public LaterLifeSimulator(PaperShiftDatabase database, Func<Random> randomProvider)
        {
            this.database = database;
            this.randomProvider = randomProvider;
        }

        private Random Random
        {
            get { return randomProvider == null ? null : randomProvider(); }
        }

        public void EnsureSimulated(PaperShiftRunState state)
        {
            if (state == null || state.Worker == null)
            {
                return;
            }

            if (state.LaterLife != null && state.LaterLife.Simulated)
            {
                return;
            }

            Simulate(state);
        }

        private void Simulate(PaperShiftRunState state)
        {
            var context = BuildContext(state);
            ApplyRules(context);
            FinalizeContext(context);
            BuildHeirs(context);
            BuildSummary(context);
        }

        private SimulationContext BuildContext(PaperShiftRunState state)
        {
            var score = HireSettlementScoreCalculator.Calculate(state, database);
            var company = FindCompany(state);
            var job = FindJob(state, company);
            var worker = state.Worker;
            var random = Random;

            var context = new SimulationContext
            {
                State = state,
                Database = database,
                Random = random,
                Company = company,
                Job = job,
                Score = score,
                JobScore = ScoreValue(score, HireSettlementScoreCalculator.Job),
                IncomeScore = ScoreValue(score, HireSettlementScoreCalculator.Income),
                ProspectScore = ScoreValue(score, HireSettlementScoreCalculator.Prospect),
                EfficiencyScore = ScoreValue(score, HireSettlementScoreCalculator.Efficiency),
                FitScore = ScoreValue(score, HireSettlementScoreCalculator.Fit),
                TotalScore = score == null ? 0 : score.TotalPoints
            };

            var stress = worker.Stress;
            var risk = state.Resume == null ? 0 : state.Resume.DeceptionRisk;
            var family = worker.GetStat(PaperShiftWorkerAttributes.Family);
            var education = worker.GetStat(PaperShiftWorkerAttributes.Education);
            var ability = worker.GetStat(PaperShiftWorkerAttributes.Ability);
            var intensity = state.CurrentJob == null ? 40 : state.CurrentJob.Intensity;

            context.FamilyStability = Clamp(24 + context.IncomeScore / 2 + context.FitScore / 5 + family / 5 - stress / 4 - risk / 5, 0, 100);
            context.EducationResource = Clamp(24 + context.IncomeScore / 4 + education / 3 + context.FamilyStability / 5, 0, 100);
            context.IndustryInsight = Clamp(18 + context.ProspectScore / 3 + context.JobScore / 5 + ability / 5, 0, 100);
            context.LifePressure = Clamp(18 + stress / 2 + intensity / 4 + (100 - context.FitScore) / 5 + risk / 5, 0, 100);
            context.ParentCare = Clamp(70 + context.EfficiencyScore / 5 + context.FitScore / 5 - context.LifePressure / 2, 0, 100);
            context.FamilyReputation = Clamp(16 + context.JobScore / 3 + context.IncomeScore / 4 + context.FitScore / 6 - risk / 4, 0, 100);
            context.LifeRisk = Clamp((100 - context.EfficiencyScore) / 4 + context.LifePressure / 3 + risk / 3 + intensity / 5, 0, 100);
            context.SpecialOpportunity = Clamp(context.ProspectScore / 6 + context.IndustryInsight / 6, 0, 100);
            context.ChildChance = Clamp(58 + (34 - worker.Age) * 3 + context.FamilyStability / 4 + context.ParentCare / 5 - context.LifePressure / 4, 0, 95);

            return context;
        }

        private void ApplyRules(SimulationContext context)
        {
            var rules = database == null ? null : database.LaterLifeRules;
            if (rules == null || rules.Length == 0)
            {
                return;
            }

            var sorted = new List<LaterLifeRuleDefinition>(rules);
            sorted.Sort((left, right) => left.Priority.CompareTo(right.Priority));
            for (var i = 0; i < sorted.Count; i++)
            {
                var rule = sorted[i];
                if (rule == null || !ConditionsMet(context, rule.Conditions))
                {
                    continue;
                }

                ApplyEffects(context, rule.Effects);
            }
        }

        private bool ConditionsMet(SimulationContext context, LaterLifeConditionDefinition[] conditions)
        {
            if (conditions == null || conditions.Length == 0)
            {
                return true;
            }

            for (var i = 0; i < conditions.Length; i++)
            {
                if (!ConditionMet(context, conditions[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private bool ConditionMet(SimulationContext context, LaterLifeConditionDefinition condition)
        {
            if (condition == null)
            {
                return true;
            }

            var result = EvaluateCondition(context, condition);
            return condition.Invert ? !result : result;
        }

        private bool EvaluateCondition(SimulationContext context, LaterLifeConditionDefinition condition)
        {
            var state = context.State;
            switch (condition.Kind)
            {
                case LaterLifeConditionKind.Always:
                    return true;
                case LaterLifeConditionKind.ScoreAtLeast:
                    return ScoreValue(context, condition.Key) >= condition.IntValue;
                case LaterLifeConditionKind.ScoreAtMost:
                    return ScoreValue(context, condition.Key) <= condition.IntValue;
                case LaterLifeConditionKind.WorkerAgeAtLeast:
                    return state.Worker.Age >= condition.IntValue;
                case LaterLifeConditionKind.WorkerAgeAtMost:
                    return state.Worker.Age <= condition.IntValue;
                case LaterLifeConditionKind.StressAtLeast:
                    return state.Worker.Stress >= condition.IntValue;
                case LaterLifeConditionKind.StressAtMost:
                    return state.Worker.Stress <= condition.IntValue;
                case LaterLifeConditionKind.ResumeRiskAtLeast:
                    return state.Resume != null && state.Resume.DeceptionRisk >= condition.IntValue;
                case LaterLifeConditionKind.WorkerStatAtLeast:
                    return state.Worker.GetStat(condition.Key) >= condition.IntValue;
                case LaterLifeConditionKind.WorkerStatAtMost:
                    return state.Worker.GetStat(condition.Key) <= condition.IntValue;
                case LaterLifeConditionKind.HasWorkerTag:
                    return state.Worker.HasTag(condition.Key);
                case LaterLifeConditionKind.CompanyHasTag:
                    return context.Company != null && context.Company.HasTag(condition.Key);
                case LaterLifeConditionKind.JobHasTag:
                    return context.Job != null && context.Job.HasTag(condition.Key);
                case LaterLifeConditionKind.EventSeen:
                    return state.HasSeenEvent(condition.Key);
                case LaterLifeConditionKind.RandomChance:
                    return context.Random != null && context.Random.NextDouble() < condition.FloatValue;
                case LaterLifeConditionKind.LaterLifeValueAtLeast:
                    return LaterLifeValue(context, condition.Key) >= condition.IntValue;
                case LaterLifeConditionKind.LaterLifeValueAtMost:
                    return LaterLifeValue(context, condition.Key) <= condition.IntValue;
                default:
                    return false;
            }
        }

        private void ApplyEffects(SimulationContext context, LaterLifeEffectDefinition[] effects)
        {
            if (effects == null)
            {
                return;
            }

            for (var i = 0; i < effects.Length; i++)
            {
                ApplyEffect(context, effects[i]);
            }
        }

        private void ApplyEffect(SimulationContext context, LaterLifeEffectDefinition effect)
        {
            if (effect == null)
            {
                return;
            }

            switch (effect.Kind)
            {
                case LaterLifeEffectKind.AddFamilyStability:
                    context.FamilyStability += effect.IntValue;
                    break;
                case LaterLifeEffectKind.AddEducationResource:
                    context.EducationResource += effect.IntValue;
                    break;
                case LaterLifeEffectKind.AddIndustryInsight:
                    context.IndustryInsight += effect.IntValue;
                    break;
                case LaterLifeEffectKind.AddLifePressure:
                    context.LifePressure += effect.IntValue;
                    break;
                case LaterLifeEffectKind.AddParentCare:
                    context.ParentCare += effect.IntValue;
                    break;
                case LaterLifeEffectKind.AddFamilyReputation:
                    context.FamilyReputation += effect.IntValue;
                    break;
                case LaterLifeEffectKind.AddLifeRisk:
                    context.LifeRisk += effect.IntValue;
                    break;
                case LaterLifeEffectKind.AddSpecialOpportunity:
                    context.SpecialOpportunity += effect.IntValue;
                    break;
                case LaterLifeEffectKind.AddChildChance:
                    context.ChildChance += effect.IntValue;
                    break;
                case LaterLifeEffectKind.AddChildCount:
                    context.ChildCountBonus += effect.IntValue;
                    break;
                case LaterLifeEffectKind.AddHeirStat:
                    AddStatDelta(context.HeirStatDeltas, effect.Key, effect.IntValue);
                    break;
                case LaterLifeEffectKind.AddHeirStress:
                    context.HeirStressDelta += effect.IntValue;
                    break;
                case LaterLifeEffectKind.AddHeirTag:
                    AddHeirTagGrant(context, effect.Key, effect.TextValue);
                    break;
                case LaterLifeEffectKind.AddMilestone:
                    AddMilestone(context, effect.IntValue, effect.TextValue, effect.SecondaryText);
                    break;
                case LaterLifeEffectKind.AddStoryFragment:
                    AddUnique(context.StoryFragments, effect.TextValue);
                    break;
                case LaterLifeEffectKind.AddWorkYears:
                    context.WorkYearsDelta += effect.IntValue;
                    break;
                case LaterLifeEffectKind.AddProspectScore:
                    context.ProspectScore += effect.IntValue;
                    break;
                case LaterLifeEffectKind.AddPressureScore:
                    context.LifePressure += effect.IntValue;
                    break;
            }
        }

        private void FinalizeContext(SimulationContext context)
        {
            context.FamilyStability = Clamp(context.FamilyStability, 0, 100);
            context.EducationResource = Clamp(context.EducationResource, 0, 100);
            context.IndustryInsight = Clamp(context.IndustryInsight, 0, 100);
            context.LifePressure = Clamp(context.LifePressure, 0, 100);
            context.ParentCare = Clamp(context.ParentCare, 0, 100);
            context.FamilyReputation = Clamp(context.FamilyReputation, 0, 100);
            context.LifeRisk = Clamp(context.LifeRisk, 0, 100);
            context.SpecialOpportunity = Clamp(context.SpecialOpportunity, 0, 100);
            context.ProspectScore = Clamp(context.ProspectScore, 0, 100);

            var age = context.State.Worker.Age;
            var maxYears = Clamp(63 - age, 3, 42);
            var workYears = 8 + context.ProspectScore / 8 + context.FamilyStability / 9 + context.ParentCare / 14;
            workYears -= context.LifePressure / 10;
            workYears -= context.LifeRisk / 12;
            workYears += Roll(-3, 6, context.Random);
            workYears += context.WorkYearsDelta;
            context.WorkYears = Clamp(workYears, 3, maxYears);

            if (context.WorkYears >= 18)
            {
                context.ChildChance += 8;
            }

            if (age >= 35)
            {
                context.ChildChance -= (age - 34) * 6;
            }

            if (context.LifePressure >= 75)
            {
                context.ChildChance -= 12;
            }

            if (context.SpecialOpportunity >= 35)
            {
                context.ChildChance += 12;
            }

            context.ChildChance = Clamp(context.ChildChance, 0, 95);
            context.FinalCareer = BuildFinalCareer(context);
        }

        private void BuildHeirs(SimulationContext context)
        {
            var state = context.State;
            state.Worker.Heirs.Clear();

            var count = RollChildCount(context);
            if (count <= 0)
            {
                context.LineageEnded = true;
                AddMilestone(context, state.Worker.Age + Math.Max(1, context.WorkYears / 2), "无后", "没有接班人");
                return;
            }

            if (context.WorkYears < 20 && state.Worker.Age <= 34)
            {
                context.WorkYears = Math.Min(42, Math.Max(context.WorkYears, 20));
            }

            var finalAge = state.Worker.Age + context.WorkYears;
            for (var i = 0; i < count; i++)
            {
                var heir = BuildHeir(context, i, finalAge);
                state.Worker.Heirs.Add(heir);
            }

            var firstChild = state.Worker.Heirs[0];
            AddMilestone(context, finalAge - firstChild.Age, "生子", "下一代登场");
        }

        private int RollChildCount(SimulationContext context)
        {
            var random = context.Random;
            if (random == null)
            {
                return 0;
            }

            var chance = context.ChildChance;
            var count = random.Next(0, 100) < chance ? 1 : 0;
            if (count > 0 && random.Next(0, 100) < Math.Max(0, chance - 45))
            {
                count++;
            }

            if (count > 0 && random.Next(0, 100) < Math.Max(0, chance - 70))
            {
                count++;
            }

            count += context.ChildCountBonus;
            if (count <= 0 && context.ChildCountBonus > 0 && context.SpecialOpportunity >= 30)
            {
                count = 1;
            }

            return Clamp(count, 0, 4);
        }

        private HeirProfile BuildHeir(SimulationContext context, int index, int finalAge)
        {
            var random = context.Random;
            var state = context.State;
            var gender = random.NextDouble() < 0.5 ? "女" : "男";
            var first = gender == "女" ? Pick(database.FemaleFirstNames, "小满", random) : Pick(database.MaleFirstNames, "知行", random);
            var birthAge = Clamp(state.Worker.Age + 5 + index * 3 + Roll(-2, 3, random) + context.LifePressure / 28 - context.FamilyStability / 34, state.Worker.Age + 1, 48);
            var heirAge = Clamp(finalAge - birthAge, 18, 30);
            var heir = new HeirProfile
            {
                Id = "heir_" + (index + 1),
                Name = state.Worker.LastName + first,
                Gender = gender,
                Age = heirAge,
                Personality = RollPersonality(context, index),
                Stress = Clamp(18 + context.LifePressure / 3 - context.FamilyStability / 9 - context.ParentCare / 12 + context.HeirStressDelta + Roll(-6, 8, random), 0, 90),
                InheritancePercent = 0
            };

            var family = Clamp(context.FamilyStability / 2 + state.Worker.GetStat(PaperShiftWorkerAttributes.Family) / 3 + context.FamilyReputation / 6 + Roll(-8, 9, random), 0, 100);
            var education = Clamp(context.EducationResource / 2 + state.Worker.GetStat(PaperShiftWorkerAttributes.Education) / 4 + context.ParentCare / 7 + Roll(-7, 10, random), 0, 100);
            var major = Clamp(context.IndustryInsight / 2 + state.Worker.GetStat(PaperShiftWorkerAttributes.Major) / 4 + education / 6 + Roll(-8, 9, random), 0, 100);
            var ability = Clamp(state.Worker.GetStat(PaperShiftWorkerAttributes.Ability) / 3 + context.IndustryInsight / 4 + context.SpecialOpportunity / 3 + context.ParentCare / 10 + Roll(-8, 10, random), 0, 100);
            var appearance = Clamp(state.Worker.GetStat(PaperShiftWorkerAttributes.Appearance) / 3 + family / 4 + Roll(20, 45, random), 0, 100);
            var baseHeight = gender == "女" ? Roll(156, 176, random) : Roll(166, 188, random);
            var height = Clamp(baseHeight + (state.Worker.GetStat(PaperShiftWorkerAttributes.Height) - 170) / 8, 145, 205);

            family += StatDelta(context, PaperShiftWorkerAttributes.Family);
            education += StatDelta(context, PaperShiftWorkerAttributes.Education);
            major += StatDelta(context, PaperShiftWorkerAttributes.Major);
            ability += StatDelta(context, PaperShiftWorkerAttributes.Ability);
            appearance += StatDelta(context, PaperShiftWorkerAttributes.Appearance);
            height += StatDelta(context, PaperShiftWorkerAttributes.Height);

            heir.Stats.Add(new StatValue { Id = PaperShiftWorkerAttributes.Family, Value = Clamp(family, 0, 100) });
            heir.Stats.Add(new StatValue { Id = PaperShiftWorkerAttributes.Education, Value = Clamp(education, 0, 100) });
            heir.Stats.Add(new StatValue { Id = PaperShiftWorkerAttributes.Major, Value = Clamp(major, 0, 100) });
            heir.Stats.Add(new StatValue { Id = PaperShiftWorkerAttributes.Ability, Value = Clamp(ability, 0, 100) });
            heir.Stats.Add(new StatValue { Id = PaperShiftWorkerAttributes.Appearance, Value = Clamp(appearance, 0, 100) });
            heir.Stats.Add(new StatValue { Id = PaperShiftWorkerAttributes.Height, Value = Clamp(height, 145, 205) });

            ApplyHeirTags(context, heir);
            heir.TraitSummary = BuildTraitSummary(heir, context);
            return heir;
        }

        private void ApplyHeirTags(SimulationContext context, HeirProfile heir)
        {
            for (var i = 0; i < context.HeirTags.Count; i++)
            {
                AddHeirTag(heir, context.HeirTags[i].TagId, context.HeirTags[i].DisplayName, context.State.CurrentYear);
            }

            if (heir.GetStat(PaperShiftWorkerAttributes.Family) >= 60)
            {
                AddHeirTag(heir, "stable_family", null, context.State.CurrentYear);
            }

            if (context.Job != null && context.Job.HasTag("ai") && context.IndustryInsight >= 55)
            {
                AddHeirTag(heir, "ai_influence", null, context.State.CurrentYear);
            }

            if (heir.Stress >= 45 || context.LifePressure >= 65)
            {
                AddHeirTag(heir, context.LifePressure >= 75 ? "parent_expectation" : "early_mature", null, context.State.CurrentYear);
            }

            if (context.SpecialOpportunity >= 45 && heir.GetStat(PaperShiftWorkerAttributes.Ability) >= 70)
            {
                AddHeirTag(heir, "upward_seed", null, context.State.CurrentYear);
            }
        }

        private void BuildSummary(SimulationContext context)
        {
            var state = context.State;
            var later = new LaterLifeState
            {
                Simulated = true,
                LineageEnded = context.LineageEnded,
                FinalCareer = context.FinalCareer,
                WorkYears = context.WorkYears,
                PressureScore = Clamp(context.LifePressure, 0, 100),
                PressureLabel = PressureLabel(context.LifePressure),
                ProspectScore = Clamp(context.ProspectScore, 0, 100),
                ProspectLabel = ProspectLabel(context.ProspectScore),
                TotalScore = context.TotalScore,
                FamilyStability = context.FamilyStability,
                EducationResource = context.EducationResource,
                IndustryInsight = context.IndustryInsight,
                LifePressure = context.LifePressure,
                ParentCare = context.ParentCare,
                FamilyReputation = context.FamilyReputation,
                LifeRisk = context.LifeRisk,
                SpecialOpportunity = context.SpecialOpportunity,
                ChildChance = context.ChildChance,
                NextGenerationYear = state.CurrentYear + Math.Max(1, context.WorkYears),
                NextGenerationMonth = NormalizeMonth(state.CurrentMonth)
            };

            FillDefaultMilestones(context);
            NormalizeMilestones(context);
            for (var i = 0; i < context.Milestones.Count && i < 6; i++)
            {
                later.Milestones.Add(context.Milestones[i]);
            }

            for (var i = 0; i < context.StoryFragments.Count; i++)
            {
                later.StoryFragments.Add(context.StoryFragments[i]);
            }

            later.StoryText = BuildStory(context);
            state.LaterLife = later;
        }

        private void FillDefaultMilestones(SimulationContext context)
        {
            var worker = context.State.Worker;
            AddMilestone(context, worker.Age + 1, "转正", "长期饭碗");
            if (context.FamilyStability >= 45 && context.ChildChance >= 35)
            {
                AddMilestone(context, worker.Age + 4 + context.LifePressure / 45, "结婚", "合并账本");
            }

            if (context.FamilyStability >= 55 || context.IncomeScore >= 70)
            {
                AddMilestone(context, worker.Age + 8 + Math.Max(0, 55 - context.FamilyStability) / 12, "买房", "家境稳定");
            }

            if (context.LifeRisk >= 55)
            {
                AddMilestone(context, worker.Age + Math.Max(3, context.WorkYears * 2 / 3), "波折", "一次转弯");
            }

            FillMilestoneSlots(context);
        }

        private void FillMilestoneSlots(SimulationContext context)
        {
            var worker = context.State.Worker;
            var years = Math.Max(1, context.WorkYears);
            AddMilestoneIfNeeded(context, worker.Age + Math.Max(1, years / 5), "适应", "摸清节奏");

            if (context.FamilyStability >= 58 || context.IncomeScore >= 65)
            {
                AddMilestoneIfNeeded(context, worker.Age + Math.Max(2, years / 3), "存钱", "日子变稳");
            }
            else
            {
                AddMilestoneIfNeeded(context, worker.Age + Math.Max(2, years / 3), "精算", "每月盘账");
            }

            if (context.IndustryInsight >= 58 || context.ProspectScore >= 65)
            {
                AddMilestoneIfNeeded(context, worker.Age + Math.Max(3, years / 2), "带路", "经验反哺");
            }
            else
            {
                AddMilestoneIfNeeded(context, worker.Age + Math.Max(3, years / 2), "站稳", "熟悉岗位");
            }

            if (context.LifePressure >= 66)
            {
                AddMilestoneIfNeeded(context, worker.Age + Math.Max(4, years * 2 / 3), "喘息", "开始减压");
            }
            else
            {
                AddMilestoneIfNeeded(context, worker.Age + Math.Max(4, years * 2 / 3), "沉淀", "经验留下");
            }

            if (context.LineageEnded)
            {
                AddMilestoneIfNeeded(context, worker.Age + Math.Max(5, years - 2), "收尾", "独自收场");
            }
            else if (context.ParentCare >= 55)
            {
                AddMilestoneIfNeeded(context, worker.Age + Math.Max(5, years - 2), "陪伴", "照看家里");
            }
            else
            {
                AddMilestoneIfNeeded(context, worker.Age + Math.Max(5, years - 2), "托举", "给孩子铺路");
            }

            AddMilestoneIfNeeded(context, worker.Age + Math.Max(2, years / 4), "账本", "收支平衡");
            AddMilestoneIfNeeded(context, worker.Age + Math.Max(3, years * 3 / 4), "取舍", "做出选择");
        }

        private string BuildStory(SimulationContext context)
        {
            var jobTitle = context.FinalCareer;
            var prospectText = context.ProspectScore >= 70 ? "行业还在往上走" : context.ProspectScore >= 45 ? "行业没有太差，也不算轻松" : "行业前景开始变窄";
            var pressureText = context.LifePressure >= 70 ? "压力很高" : context.LifePressure >= 45 ? "压力不小" : "节奏还算平稳";
            var childText = context.LineageEnded ? "只是后来没有留下能接班的后代，这条线到这里停住。" : "后来的人生也影响了孩子的底色。";
            var story = "他把「" + jobTitle + "」干成了长期饭碗。" + prospectText + "，所以日子慢慢有了形状；只是" + pressureText + "，很多选择都带着代价。";
            if (context.StoryFragments.Count > 0)
            {
                story += context.StoryFragments[0];
            }

            return story + childText;
        }

        private string BuildFinalCareer(SimulationContext context)
        {
            var title = CurrentJobTitle(context);
            if (context.FitScore >= 85 && context.ProspectScore >= 70)
            {
                return "资深" + title;
            }

            if (context.JobScore >= 75 && context.FamilyReputation >= 60)
            {
                return title + "主管";
            }

            if (context.LifeRisk >= 70 && context.ProspectScore < 45)
            {
                return "转行后的" + title;
            }

            return title;
        }

        private static string CurrentJobTitle(SimulationContext context)
        {
            if (context.State.CurrentJob != null && !string.IsNullOrEmpty(context.State.CurrentJob.JobTitle))
            {
                return context.State.CurrentJob.JobTitle;
            }

            return context.Job == null || string.IsNullOrEmpty(context.Job.DisplayName) ? "新岗位" : context.Job.DisplayName;
        }

        private void NormalizeMilestones(SimulationContext context)
        {
            var minAge = context.State.Worker.Age + 1;
            var maxAge = context.State.Worker.Age + Math.Max(1, context.WorkYears);
            for (var i = 0; i < context.Milestones.Count; i++)
            {
                var milestone = context.Milestones[i];
                if (milestone.Age <= 0)
                {
                    milestone.Age = Clamp(minAge + (i + 1) * Math.Max(1, context.WorkYears) / (context.Milestones.Count + 1), minAge, maxAge);
                }
                else
                {
                    milestone.Age = Clamp(milestone.Age, minAge, maxAge);
                }
            }

            context.Milestones.Sort((left, right) => left.Age.CompareTo(right.Age));
        }

        private static string PressureLabel(int pressure)
        {
            if (pressure >= 75)
            {
                return "很高";
            }

            if (pressure >= 60)
            {
                return "偏高";
            }

            return pressure >= 40 ? "适中" : "较低";
        }

        private static string ProspectLabel(int prospect)
        {
            if (prospect >= 80)
            {
                return "爆发";
            }

            if (prospect >= 62)
            {
                return "上升";
            }

            return prospect >= 42 ? "平稳" : "收缩";
        }

        private string RollPersonality(SimulationContext context, int index)
        {
            if (context.LifePressure >= 70)
            {
                return index % 2 == 0 ? "谨慎" : "较真";
            }

            if (context.FamilyStability >= 65)
            {
                return index % 2 == 0 ? "随和" : "开朗";
            }

            if (context.IndustryInsight >= 60)
            {
                return "灵活";
            }

            var options = new[] { "沉稳", "谨慎", "开朗", "灵活", "随和" };
            return options[Math.Abs(index + Roll(0, options.Length - 1, context.Random)) % options.Length];
        }

        private string BuildTraitSummary(HeirProfile heir, SimulationContext context)
        {
            if (heir.Tags.Count > 0)
            {
                return heir.Tags[0].DisplayName;
            }

            if (context.FamilyStability >= 60)
            {
                return "家庭较稳";
            }

            return context.SpecialOpportunity >= 35 ? "有翻盘机会" : "路线未定";
        }

        private CompanyDefinition FindCompany(PaperShiftRunState state)
        {
            if (database == null || state == null)
            {
                return null;
            }

            var companyId = state.CurrentJob == null ? string.Empty : state.CurrentJob.CompanyId;
            if (string.IsNullOrEmpty(companyId) && state.Interview != null)
            {
                companyId = state.Interview.CompanyId;
            }

            return database.FindCompany(companyId);
        }

        private JobDefinition FindJob(PaperShiftRunState state, CompanyDefinition company)
        {
            if (state == null)
            {
                return null;
            }

            var jobId = state.CurrentJob == null ? string.Empty : state.CurrentJob.JobId;
            if (company != null)
            {
                var job = company.FindJob(jobId);
                if (job != null)
                {
                    return job;
                }
            }

            return database == null ? null : database.FindJob(state.Interview.CompanyId, state.Interview.JobId);
        }

        private static int ScoreValue(HireSettlementScore score, string id)
        {
            if (score == null)
            {
                return 0;
            }

            if (id == "total")
            {
                return score.TotalPoints;
            }

            var item = score.Find(id);
            return item == null ? 0 : item.Score;
        }

        private static int ScoreValue(SimulationContext context, string id)
        {
            return ScoreValue(context.Score, string.IsNullOrEmpty(id) ? "total" : id);
        }

        private static int LaterLifeValue(SimulationContext context, string key)
        {
            switch ((key ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "stability":
                case "family":
                    return context.FamilyStability;
                case "education":
                    return context.EducationResource;
                case "industry":
                    return context.IndustryInsight;
                case "pressure":
                    return context.LifePressure;
                case "care":
                    return context.ParentCare;
                case "reputation":
                    return context.FamilyReputation;
                case "risk":
                    return context.LifeRisk;
                case "opportunity":
                    return context.SpecialOpportunity;
                case "child_chance":
                    return context.ChildChance;
                default:
                    return 0;
            }
        }

        private static void AddStatDelta(Dictionary<string, int> deltas, string statId, int value)
        {
            if (string.IsNullOrEmpty(statId) || value == 0)
            {
                return;
            }

            deltas.TryGetValue(statId, out var current);
            deltas[statId] = current + value;
        }

        private static int StatDelta(SimulationContext context, string statId)
        {
            return context.HeirStatDeltas.TryGetValue(statId, out var value) ? value : 0;
        }

        private static void AddHeirTagGrant(SimulationContext context, string tagId, string displayName)
        {
            if (string.IsNullOrEmpty(tagId))
            {
                return;
            }

            for (var i = 0; i < context.HeirTags.Count; i++)
            {
                if (context.HeirTags[i].TagId == tagId)
                {
                    return;
                }
            }

            context.HeirTags.Add(new HeirTagGrant { TagId = tagId, DisplayName = displayName });
        }

        private void AddHeirTag(HeirProfile heir, string tagId, string displayName, int year)
        {
            if (heir == null || string.IsNullOrEmpty(tagId))
            {
                return;
            }

            for (var i = 0; i < heir.Tags.Count; i++)
            {
                if (heir.Tags[i].TagId == tagId)
                {
                    return;
                }
            }

            var tag = database == null ? null : database.FindTag(tagId);
            heir.Tags.Add(new TagInstance
            {
                TagId = tagId,
                DisplayName = string.IsNullOrEmpty(displayName) ? (tag == null ? tagId : tag.DisplayName) : displayName,
                Scope = tag == null ? TagScope.Worker : tag.Scope,
                RarityId = tag == null ? "normal" : tag.RarityId,
                AcquiredYear = year,
                Stacks = 1
            });
        }

        private static void AddMilestone(SimulationContext context, int age, string title, string body)
        {
            if (context == null || string.IsNullOrEmpty(title))
            {
                return;
            }

            for (var i = 0; i < context.Milestones.Count; i++)
            {
                if (context.Milestones[i].Title == title)
                {
                    return;
                }
            }

            context.Milestones.Add(new LaterLifeMilestone { Age = age, Title = title, Body = string.IsNullOrEmpty(body) ? title : body });
        }

        private static void AddMilestoneIfNeeded(SimulationContext context, int age, string title, string body)
        {
            if (context == null || context.Milestones.Count >= 6)
            {
                return;
            }

            AddMilestone(context, age, title, body);
        }

        private static void AddUnique(List<string> values, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            for (var i = 0; i < values.Count; i++)
            {
                if (values[i] == value)
                {
                    return;
                }
            }

            values.Add(value);
        }

        private static string Pick(string[] values, string fallback, Random random)
        {
            if (values == null || values.Length == 0 || random == null)
            {
                return fallback;
            }

            return values[random.Next(0, values.Length)];
        }

        private static int Roll(int minInclusive, int maxInclusive, Random random)
        {
            if (random == null)
            {
                return minInclusive;
            }

            return random.Next(minInclusive, maxInclusive + 1);
        }

        private static int NormalizeMonth(int month)
        {
            if (month < 1)
            {
                return 1;
            }

            return month > 12 ? 12 : month;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }

        private sealed class HeirTagGrant
        {
            public string TagId;
            public string DisplayName;
        }

        private sealed class SimulationContext
        {
            public PaperShiftRunState State;
            public PaperShiftDatabase Database;
            public Random Random;
            public CompanyDefinition Company;
            public JobDefinition Job;
            public HireSettlementScore Score;
            public int JobScore;
            public int IncomeScore;
            public int ProspectScore;
            public int EfficiencyScore;
            public int FitScore;
            public int TotalScore;
            public int FamilyStability;
            public int EducationResource;
            public int IndustryInsight;
            public int LifePressure;
            public int ParentCare;
            public int FamilyReputation;
            public int LifeRisk;
            public int SpecialOpportunity;
            public int ChildChance;
            public int ChildCountBonus;
            public int WorkYears;
            public int WorkYearsDelta;
            public int HeirStressDelta;
            public string FinalCareer;
            public bool LineageEnded;
            public readonly Dictionary<string, int> HeirStatDeltas = new Dictionary<string, int>();
            public readonly List<HeirTagGrant> HeirTags = new List<HeirTagGrant>();
            public readonly List<LaterLifeMilestone> Milestones = new List<LaterLifeMilestone>();
            public readonly List<string> StoryFragments = new List<string>();
        }
    }
}
