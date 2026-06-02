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

            var regularizationChance = state.HasActiveJob ? state.CurrentJob.PromotionProgress : 0;

            if (state.Phase == PaperShiftPhase.Probation && state.HasActiveJob)
            {
                SetItemVisible(WorkStatus, true);
                SetItemVisible(LayoffStatus, false);
                SetItemVisible(InterviewStatus, false);
                RefreshItem(WorkStatus, regularizationChance, "试用认可度");
                return;
            }

            SetItemVisible(WorkStatus, true);
            SetItemVisible(LayoffStatus, true);
            SetItemVisible(InterviewStatus, true);
            RefreshInactive(WorkStatus, "待进入试用期");
            RefreshInactive(LayoffStatus, "待评估风险");

            if (showInterviewProgress)
            {
                RefreshItem(InterviewStatus, state.Interview.Satisfaction, "面试认可度");
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

        public void SetWorkInteractable(bool interactable)
        {
            if (WorkStatus != null && WorkStatus.ActionButton != null)
            {
                WorkStatus.ActionButton.interactable = interactable;
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

        private static void RefreshInactive(PaperShiftBottomStatusItemBinding item, string label)
        {
            if (item == null)
            {
                return;
            }

            item.RefreshInactive(label);
        }

        private static void SetItemVisible(PaperShiftBottomStatusItemBinding item, bool visible)
        {
            if (item == null)
            {
                return;
            }

            item.SetVisible(visible);
        }
    }

    [System.Serializable]
    public sealed class PaperShiftBottomStatusItemBinding
    {
        public GameObject Root;
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

        public void SetVisible(bool visible)
        {
            var root = ResolveRoot();
            if (root != null)
            {
                root.SetActive(visible);
            }
        }

        private GameObject ResolveRoot()
        {
            if (Root != null)
            {
                return Root;
            }

            if (PercentText != null && PercentText.transform.parent != null)
            {
                return PercentText.transform.parent.gameObject;
            }

            if (ActionButton != null)
            {
                return ActionButton.gameObject;
            }

            return ProgressBarRoot;
        }
    }
}
