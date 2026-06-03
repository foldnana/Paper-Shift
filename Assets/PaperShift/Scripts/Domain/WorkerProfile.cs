using System;
using System.Collections.Generic;

namespace PaperShift.Domain
{
    [Serializable]
    public sealed class WorkerProfile
    {
        public string Id;
        public string LastName;
        public string FirstName;
        public string Gender;
        public string Personality;
        public string EraId;
        public string EraName;
        public int Generation = 1;
        public int BirthYear;
        public int BirthMonth = 1;
        public int Age;
        public int Money;
        public int Stress;
        public int Health = 80;
        public List<StatValue> Stats = new List<StatValue>();
        public List<TagInstance> Tags = new List<TagInstance>();
        public List<HeirProfile> Heirs = new List<HeirProfile>();

        public string FullName
        {
            get { return LastName + FirstName; }
        }

        public int GetStat(string statId, int fallback = 0)
        {
            if (string.IsNullOrEmpty(statId))
            {
                return fallback;
            }

            for (var i = 0; i < Stats.Count; i++)
            {
                if (Stats[i].Id == statId)
                {
                    return Stats[i].Value;
                }
            }

            return fallback;
        }

        public void SetStat(string statId, int value)
        {
            if (string.IsNullOrEmpty(statId))
            {
                return;
            }

            for (var i = 0; i < Stats.Count; i++)
            {
                if (Stats[i].Id == statId)
                {
                    Stats[i].Value = value;
                    return;
                }
            }

            Stats.Add(new StatValue { Id = statId, Value = value });
        }

        public void AddStat(string statId, int delta, int min = 0, int max = 100)
        {
            SetStat(statId, Clamp(GetStat(statId) + delta, min, max));
        }

        public bool HasTag(string tagId)
        {
            return FindTag(tagId) != null;
        }

        public TagInstance FindTag(string tagId)
        {
            if (string.IsNullOrEmpty(tagId))
            {
                return null;
            }

            for (var i = 0; i < Tags.Count; i++)
            {
                if (Tags[i].TagId == tagId)
                {
                    return Tags[i];
                }
            }

            return null;
        }

        public void AddTag(TagInstance tag, bool unique = true)
        {
            if (tag == null || string.IsNullOrEmpty(tag.TagId))
            {
                return;
            }

            var existing = FindTag(tag.TagId);
            if (existing != null)
            {
                if (!unique)
                {
                    existing.Stacks = Math.Max(1, existing.Stacks + Math.Max(1, tag.Stacks));
                }

                existing.RemainingYears = Math.Max(existing.RemainingYears, tag.RemainingYears);
                return;
            }

            if (tag.Stacks <= 0)
            {
                tag.Stacks = 1;
            }

            Tags.Add(tag);
        }

        public bool RemoveTag(string tagId)
        {
            for (var i = Tags.Count - 1; i >= 0; i--)
            {
                if (Tags[i].TagId == tagId)
                {
                    Tags.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public void TickTemporaryTags()
        {
            for (var i = Tags.Count - 1; i >= 0; i--)
            {
                var tag = Tags[i];
                if (!tag.Temporary)
                {
                    continue;
                }

                tag.RemainingYears--;
                if (tag.RemainingYears <= 0)
                {
                    Tags.RemoveAt(i);
                }
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
    }

    [Serializable]
    public sealed class StatValue
    {
        public string Id;
        public int Value;
    }

    [Serializable]
    public sealed class TagInstance
    {
        public string TagId;
        public string DisplayName;
        public TagScope Scope = TagScope.Worker;
        public string RarityId = "normal";
        public int Stacks = 1;
        public int AcquiredYear;
        public bool Temporary;
        public int RemainingYears;
    }

    [Serializable]
    public sealed class HeirProfile
    {
        public string Id;
        public string Name;
        public string Gender;
        public int Age;
        public int InheritancePercent;
        public string TraitSummary;
        public List<StatValue> Stats = new List<StatValue>();
        public List<TagInstance> Tags = new List<TagInstance>();
    }
}
