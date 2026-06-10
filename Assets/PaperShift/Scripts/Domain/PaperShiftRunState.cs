using System;
using System.Collections.Generic;

namespace PaperShift.Domain
{
    [Serializable]
    public sealed class PaperShiftRunState
    {
        public int Seed;
        public int CurrentYear;
        public int CurrentMonth = 1;
        public int GenerationStartYear;
        public int GenerationStartMonth = 1;
        public int Generation = 1;
        public PaperShiftPhase Phase = PaperShiftPhase.CreateWorker;
        public WorkerProfile Worker = new WorkerProfile();
        public ResumeProfile Resume = new ResumeProfile();
        public BudgetPlan Budget = new BudgetPlan();
        public InterviewState Interview = new InterviewState();
        public CurrentJobState CurrentJob = new CurrentJobState();
        public RetirementState Retirement = new RetirementState();
        public LaterLifeState LaterLife = new LaterLifeState();
        public List<GameLogEntry> Logs = new List<GameLogEntry>();
        public List<string> Banners = new List<string>();
        public List<string> SeenEventIds = new List<string>();
        public List<EventCooldown> EventCooldowns = new List<EventCooldown>();

        public bool HasActiveJob
        {
            get { return !string.IsNullOrEmpty(CurrentJob.JobId); }
        }

        public void AddLog(string text, EventNoticeType type = EventNoticeType.Log)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            Logs.Add(new GameLogEntry
            {
                Year = CurrentYear,
                Month = CurrentMonth,
                Text = text,
                NoticeType = type
            });

            if (type == EventNoticeType.Banner || type == EventNoticeType.Modal)
            {
                Banners.Add(text);
            }
        }

        public bool HasSeenEvent(string eventId)
        {
            return SeenEventIds.Contains(eventId);
        }

        public void MarkEventSeen(string eventId)
        {
            if (!string.IsNullOrEmpty(eventId) && !SeenEventIds.Contains(eventId))
            {
                SeenEventIds.Add(eventId);
            }
        }

        public int GetEventCooldown(string eventId)
        {
            for (var i = 0; i < EventCooldowns.Count; i++)
            {
                if (EventCooldowns[i].EventId == eventId)
                {
                    return EventCooldowns[i].RemainingYears;
                }
            }

            return 0;
        }

        public void SetEventCooldown(string eventId, int years)
        {
            if (string.IsNullOrEmpty(eventId) || years <= 0)
            {
                return;
            }

            for (var i = 0; i < EventCooldowns.Count; i++)
            {
                if (EventCooldowns[i].EventId == eventId)
                {
                    EventCooldowns[i].RemainingYears = years;
                    return;
                }
            }

            EventCooldowns.Add(new EventCooldown { EventId = eventId, RemainingYears = years });
        }

        public void TickEventCooldowns()
        {
            for (var i = EventCooldowns.Count - 1; i >= 0; i--)
            {
                EventCooldowns[i].RemainingYears--;
                if (EventCooldowns[i].RemainingYears <= 0)
                {
                    EventCooldowns.RemoveAt(i);
                }
            }
        }
    }

    [Serializable]
    public sealed class ResumeProfile
    {
        public List<string> IntentTagIds = new List<string>();
        public List<string> HiddenTagIds = new List<string>();
        public List<ResumePackagingChoice> Packaging = new List<ResumePackagingChoice>();
        public int DeceptionRisk;

        public ResumePackagingChoice GetOrCreateChoice(string fieldId)
        {
            for (var i = 0; i < Packaging.Count; i++)
            {
                if (Packaging[i].FieldId == fieldId)
                {
                    return Packaging[i];
                }
            }

            var choice = new ResumePackagingChoice
            {
                FieldId = fieldId,
                Mode = ResumePackagingMode.Normal,
                OptionIndex = -1
            };
            Packaging.Add(choice);
            return choice;
        }
    }

    [Serializable]
    public sealed class ResumePackagingChoice
    {
        public string FieldId;
        public ResumePackagingMode Mode;
        public int OptionIndex = -1;
    }

    [Serializable]
    public sealed class BudgetPlan
    {
        public int Food = 25;
        public int Housing = 25;
        public int Romance = 15;
        public int Education = 15;
        public int Savings = 20;

        public int Total
        {
            get { return Food + Housing + Romance + Education + Savings; }
        }

        public int GetCategory(string id)
        {
            switch (id)
            {
                case "food": return Food;
                case "housing": return Housing;
                case "romance": return Romance;
                case "education": return Education;
                case "savings": return Savings;
                default: return 0;
            }
        }

        public void SetCategory(string id, int value)
        {
            value = Clamp(value, 0, 100);
            switch (id)
            {
                case "food":
                    Food = value;
                    break;
                case "housing":
                    Housing = value;
                    break;
                case "romance":
                    Romance = value;
                    break;
                case "education":
                    Education = value;
                    break;
                case "savings":
                    Savings = value;
                    break;
            }
        }

        public void NormalizeTo100()
        {
            var total = Total;
            if (total <= 0)
            {
                Food = 20;
                Housing = 20;
                Romance = 20;
                Education = 20;
                Savings = 20;
                return;
            }

            Food = Food * 100 / total;
            Housing = Housing * 100 / total;
            Romance = Romance * 100 / total;
            Education = Education * 100 / total;
            Savings = 100 - Food - Housing - Romance - Education;
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
    public sealed class InterviewState
    {
        public string CompanyId;
        public string JobId;
        public string CompanyName;
        public string JobTitle;
        public int Recognition;
        public int OfferThreshold = 70;
        public int Salary;

        public bool HasOffer
        {
            get { return Recognition >= OfferThreshold; }
        }
    }

    [Serializable]
    public sealed class CurrentJobState
    {
        public string CompanyId;
        public string JobId;
        public string CompanyName;
        public string JobTitle;
        public int Salary;
        public int WorkYears;
        public int StartYear;
        public int StartMonth = 1;
        public int Recognition;
        public int Intensity;
    }

    [Serializable]
    public sealed class RetirementState
    {
        public RunEndReason Reason = RunEndReason.None;
        public string ReasonText;
        public int FinalSavings;
        public int WorkYears;
        public string FinalJobTitle;
    }

    [Serializable]
    public sealed class LaterLifeState
    {
        public bool Simulated;
        public bool LineageEnded;
        public string FinalCareer;
        public int WorkYears;
        public int PressureScore;
        public string PressureLabel;
        public int ProspectScore;
        public string ProspectLabel;
        public string StoryText;
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
        public int NextGenerationYear;
        public int NextGenerationMonth = 1;
        public List<LaterLifeMilestone> Milestones = new List<LaterLifeMilestone>();
        public List<string> StoryFragments = new List<string>();
    }

    [Serializable]
    public sealed class LaterLifeMilestone
    {
        public int Age;
        public string Title;
        public string Body;
    }

    [Serializable]
    public sealed class GameLogEntry
    {
        public int Year;
        public int Month;
        public string Text;
        public EventNoticeType NoticeType = EventNoticeType.Log;
    }

    [Serializable]
    public sealed class EventCooldown
    {
        public string EventId;
        public int RemainingYears;
    }
}
