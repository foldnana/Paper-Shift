using System.Collections.Generic;
using PaperShift.Data;
using PaperShift.Domain;

namespace PaperShift.Presenter
{
    public sealed class UiPair
    {
        public readonly string Label;
        public readonly string Value;

        public UiPair(string label, string value)
        {
            Label = label;
            Value = value;
        }
    }

    public sealed class CandidateUiData
    {
        public string Badge;
        public string Corner;
        public string Name;
        public string Subtitle;
        public string RingText;
        public List<UiPair> Rows = new List<UiPair>();
        public List<string> Tags = new List<string>();
        public List<string> Logs = new List<string>();
        public string ProgressPercent;
        public string ProgressLabel;
        public float ProgressFill;
    }

    internal static class PaperShiftUiFormatter
    {
        public static string Score(int value)
        {
            return (value / 20f).ToString("0.0") + "分";
        }

        public static string Rank(int value)
        {
            if (value >= 80)
            {
                return "优秀";
            }

            if (value >= 60)
            {
                return "熟练";
            }

            if (value >= 40)
            {
                return "普通";
            }

            return "较弱";
        }

        public static string EducationLabel(WorkerProfile worker)
        {
            return PaperShiftWorkerAttributes.EducationLabel(worker.GetStat(PaperShiftWorkerAttributes.Education));
        }

        public static string FamilyLabel(int value)
        {
            return PaperShiftWorkerAttributes.FamilyLabel(value);
        }

        public static string BestStatLabel(WorkerProfile worker, PaperShiftDatabase database)
        {
            return PaperShiftWorkerAttributes.BestAttributeLabel(worker);
        }

        public static string IntentLabel(string id)
        {
            switch (id)
            {
                case "ai_intent": return "AI/技术岗";
                case "remote_first": return "远程优先";
                case "salary_high": return "月薪过万";
                default: return id;
            }
        }

        public static string JobTagLabel(string id)
        {
            switch (id)
            {
                case "ai": return "AI行业";
                case "remote": return "远程办公";
                case "office": return "办公室";
                case "physical": return "体力要求";
                case "local": return "本地机会";
                default: return id;
            }
        }

        public static ResumePackagingMode ModeFromIndex(int index)
        {
            switch (index)
            {
                case 0: return ResumePackagingMode.Normal;
                case 1: return ResumePackagingMode.Hide;
                case 2: return ResumePackagingMode.Exaggerate;
                default: return ResumePackagingMode.Fake;
            }
        }

        public static string EmptyFallback(string value, string fallback)
        {
            return string.IsNullOrEmpty(value) ? fallback : value;
        }
    }
}
