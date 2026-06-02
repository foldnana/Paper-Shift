using System;
using PaperShift.Domain;
using UnityEngine;

namespace PaperShift.Data
{
    [CreateAssetMenu(menuName = "Paper Shift/Game Database", fileName = "PaperShiftDatabase")]
    public sealed class PaperShiftDatabase : ScriptableObject
    {
        public EraDefinition[] Eras = new EraDefinition[0];
        public RarityDefinition[] Rarities = new RarityDefinition[0];
        public StatDefinition[] Stats = new StatDefinition[0];
        public TagDefinition[] Tags = new TagDefinition[0];
        public WorkTagDefinition[] WorkTags = new WorkTagDefinition[0];
        public CompanyDefinition[] Companies = new CompanyDefinition[0];
        public GameEventDefinition[] Events = new GameEventDefinition[0];
        public string[] LastNames = new string[0];
        public string[] MaleFirstNames = new string[0];
        public string[] FemaleFirstNames = new string[0];

        public EraDefinition FindEra(string id)
        {
            for (var i = 0; i < Eras.Length; i++)
            {
                if (Eras[i].Id == id)
                {
                    return Eras[i];
                }
            }

            return null;
        }

        public StatDefinition FindStat(string id)
        {
            for (var i = 0; i < Stats.Length; i++)
            {
                if (Stats[i].Id == id)
                {
                    return Stats[i];
                }
            }

            return null;
        }

        public TagDefinition FindTag(string id)
        {
            for (var i = 0; i < Tags.Length; i++)
            {
                if (Tags[i].Id == id)
                {
                    return Tags[i];
                }
            }

            return null;
        }

        public WorkTagDefinition FindWorkTag(string id)
        {
            for (var i = 0; i < WorkTags.Length; i++)
            {
                if (WorkTags[i].Id == id)
                {
                    return WorkTags[i];
                }
            }

            return null;
        }

        public CompanyDefinition FindCompany(string id)
        {
            for (var i = 0; i < Companies.Length; i++)
            {
                if (Companies[i].Id == id)
                {
                    return Companies[i];
                }
            }

            return null;
        }

        public JobDefinition FindJob(string companyId, string jobId)
        {
            var company = FindCompany(companyId);
            if (company == null)
            {
                return null;
            }

            return company.FindJob(jobId);
        }
    }

    [Serializable]
    public sealed class EraDefinition
    {
        public string Id;
        public string DisplayName;
        public int StartYear;
        public int EndYear;
        public string Description;
        public string[] StartingTagPool = new string[0];
    }

    [Serializable]
    public sealed class RarityDefinition
    {
        public string Id = "normal";
        public string DisplayName = "普通";
        public int Weight = 70;
        public Color Color = Color.white;
    }

    [Serializable]
    public sealed class StatDefinition
    {
        public string Id;
        public string DisplayName;
        public int MinValue = 0;
        public int MaxValue = 100;
        public int StartMin = 20;
        public int StartMax = 80;
        public bool HigherIsBetter = true;
        public string Description;
    }

    [Serializable]
    public sealed class TagDefinition
    {
        public string Id;
        public string DisplayName;
        [TextArea] public string Description;
        public TagScope Scope = TagScope.Worker;
        public TagPolarity Polarity = TagPolarity.Neutral;
        public string RarityId = "normal";
        public bool Unique = true;
        public string[] EraIds = new string[0];
        public ConditionDefinition[] Conditions = new ConditionDefinition[0];
        public EffectDefinition[] Effects = new EffectDefinition[0];
        public ConditionalEffectDefinition[] ConditionalEffects = new ConditionalEffectDefinition[0];
    }

    [Serializable]
    public sealed class ConditionalEffectDefinition
    {
        public GameEventPhase Phase = GameEventPhase.Any;
        public string[] WorkTagIds = new string[0];
        public ConditionDefinition[] Conditions = new ConditionDefinition[0];
        public EffectDefinition[] Effects = new EffectDefinition[0];
    }

    [Serializable]
    public sealed class WorkTagDefinition
    {
        public string Id;
        public string DisplayName;
        [TextArea] public string Description;
        public WorkRequirementDefinition[] Requirements = new WorkRequirementDefinition[0];
        public EffectDefinition[] Effects = new EffectDefinition[0];
        public EventWeightDefinition[] EventWeights = new EventWeightDefinition[0];
    }

    [Serializable]
    public sealed class WorkRequirementDefinition
    {
        public RequirementTarget Target = RequirementTarget.FitDimension;
        public FitDimension Dimension = FitDimension.Execution;
        public string Key;
        public CompareOperator Operator = CompareOperator.GreaterOrEqual;
        public int IntValue = 50;
        public bool HardFail;
        public int RecognitionOnPass;
        public int RecognitionOnFail = -8;
        public int StressOnPass;
        public int StressOnFail = 4;
        public string FailEventId;
    }

    [Serializable]
    public sealed class EventWeightDefinition
    {
        public string EventId;
        public int WeightDelta;
        public ConditionDefinition[] Conditions = new ConditionDefinition[0];
    }

    [Serializable]
    public sealed class CompanyDefinition
    {
        public string Id;
        public string DisplayName;
        public string Industry;
        public string[] EraIds = new string[0];
        public string[] TagIds = new string[0];
        public JobDefinition[] Jobs = new JobDefinition[0];

        public JobDefinition FindJob(string id)
        {
            for (var i = 0; i < Jobs.Length; i++)
            {
                if (Jobs[i].Id == id)
                {
                    return Jobs[i];
                }
            }

            return null;
        }

        public bool HasTag(string tagId)
        {
            for (var i = 0; i < TagIds.Length; i++)
            {
                if (TagIds[i] == tagId)
                {
                    return true;
                }
            }

            return false;
        }
    }

    [Serializable]
    public sealed class JobDefinition
    {
        public string Id;
        public string DisplayName;
        public string[] IntentTagIds = new string[0];
        public string[] TagIds = new string[0];
        public int SalaryMin = 6000;
        public int SalaryMax = 12000;
        public int Difficulty = 40;
        public int OfferThreshold = 70;
        public int WorkIntensity = 40;
        public int PromotionBase = 8;
        public StatRequirement[] Requirements = new StatRequirement[0];

        public bool HasTag(string tagId)
        {
            for (var i = 0; i < TagIds.Length; i++)
            {
                if (TagIds[i] == tagId)
                {
                    return true;
                }
            }

            return false;
        }
    }

    [Serializable]
    public sealed class StatRequirement
    {
        public string StatId;
        public int MinValue;
        public int Weight = 1;
        public string MissingTagId;
    }

    [Serializable]
    public sealed class GameEventDefinition
    {
        public string Id;
        public string DisplayName;
        [TextArea] public string Body;
        public GameEventPhase Phase = GameEventPhase.Any;
        public EventNoticeType NoticeType = EventNoticeType.Log;
        public int BaseWeight = 10;
        public int CooldownYears = 0;
        public ConditionDefinition[] Conditions = new ConditionDefinition[0];
        public EventOptionDefinition[] Options = new EventOptionDefinition[0];
    }

    [Serializable]
    public sealed class EventOptionDefinition
    {
        public string Id;
        public string Label;
        public bool RunCheckpointAfterChoice;
        public ConditionDefinition[] Conditions = new ConditionDefinition[0];
        public EffectDefinition[] Effects = new EffectDefinition[0];
    }

    [Serializable]
    public sealed class ConditionDefinition
    {
        public ConditionKind Kind = ConditionKind.Always;
        public string Key;
        public CompareOperator Operator = CompareOperator.GreaterOrEqual;
        public int IntValue;
        public float FloatValue;
        public string TextValue;
        public bool Invert;
    }

    [Serializable]
    public sealed class EffectDefinition
    {
        public EffectTiming Timing = EffectTiming.Immediate;
        public EffectKind Kind = EffectKind.None;
        public string Key;
        public int IntValue;
        public float FloatValue;
        public string TextValue;
        public string SecondaryText;
        public TagScope TagScope = TagScope.Worker;
        public RunEndReason EndReason = RunEndReason.None;
        public bool Temporary;
        public int DurationYears;
    }
}
