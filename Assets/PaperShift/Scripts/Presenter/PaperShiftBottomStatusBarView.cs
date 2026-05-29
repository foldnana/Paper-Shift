using PaperShift.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace PaperShift.Presenter
{
    public sealed class PaperShiftBottomStatusBarView : MonoBehaviour
    {
        public PaperShiftBottomStatusItemBinding WorkStatus;
        public PaperShiftBottomStatusItemBinding LayoffStatus;
        public PaperShiftBottomStatusItemBinding InterviewStatus;

        public void Refresh(PaperShiftRunState state, bool showInterviewProgress)
        {
            if (state == null)
            {
                return;
            }

            var promotionProgress = state.HasActiveJob ? state.CurrentJob.PromotionProgress : 0;
            var quitRisk = state.HasActiveJob ? state.CurrentJob.QuitRisk : 0;

            RefreshItem(WorkStatus, promotionProgress, "升职进度");
            RefreshItem(LayoffStatus, quitRisk, "被裁风险");

            if (showInterviewProgress)
            {
                RefreshItem(InterviewStatus, state.Interview.Satisfaction, "面试满意度");
            }
            else if (InterviewStatus != null)
            {
                InterviewStatus.RefreshInactive("待开始面试");
            }
        }

        public void SetInterviewInteractable(bool interactable)
        {
            if (InterviewStatus != null && InterviewStatus.ActionButton != null)
            {
                InterviewStatus.ActionButton.interactable = interactable;
            }
        }

        private static void RefreshItem(PaperShiftBottomStatusItemBinding item, int percent, string label)
        {
            if (item == null)
            {
                return;
            }

            item.Refresh(Mathf.Clamp(percent, 0, 100), label);
        }
    }

    [System.Serializable]
    public sealed class PaperShiftBottomStatusItemBinding
    {
        public Text PercentText;
        public GameObject ProgressBarRoot;
        public RectTransform Fill;
        public Button ActionButton;

        public void Refresh(int percent, string label)
        {
            if (PercentText != null)
            {
                PercentText.text = "<size=32>" + percent + "%</size> " + label;
            }

            if (Fill != null)
            {
                Fill.anchorMax = new Vector2(percent / 100f, 1f);
            }

            if (ProgressBarRoot != null)
            {
                ProgressBarRoot.SetActive(true);
            }
        }

        public void RefreshInactive(string label)
        {
            if (PercentText != null)
            {
                PercentText.text = label;
            }

            if (Fill != null)
            {
                Fill.anchorMax = new Vector2(0f, 1f);
            }

            if (ProgressBarRoot != null)
            {
                ProgressBarRoot.SetActive(false);
            }
        }
    }
}
