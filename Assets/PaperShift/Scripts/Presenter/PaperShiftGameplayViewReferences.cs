using UnityEngine;
using UnityEngine.UI;

namespace PaperShift.Presenter
{
    public sealed class PaperShiftGameplayViewReferences : MonoBehaviour
    {
        public Transform SelfCard;
        public Transform JobCard;
        public Transform SelfTagsRoot;
        public Transform SelfEventLog;
        public PaperShiftJobCardTransition JobTransition;
        public Button StartInterviewButton;
        public Button ReapplyButton;
        public Button StartWorkButton;
        public Button JobProgressButton;
        public Text CalendarYearText;
        public Text CalendarMonthText;

        public Transform Root
        {
            get { return transform; }
        }

        public bool IsComplete(out string missingField)
        {
            if (SelfCard == null) { missingField = nameof(SelfCard); return false; }
            if (JobCard == null) { missingField = nameof(JobCard); return false; }
            if (SelfTagsRoot == null) { missingField = nameof(SelfTagsRoot); return false; }
            if (JobTransition == null) { missingField = nameof(JobTransition); return false; }
            if (StartInterviewButton == null) { missingField = nameof(StartInterviewButton); return false; }
            if (ReapplyButton == null) { missingField = nameof(ReapplyButton); return false; }
            if (StartWorkButton == null) { missingField = nameof(StartWorkButton); return false; }
            missingField = string.Empty;
            return true;
        }
    }
}
