using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PaperShift.Presenter
{
    public sealed class PaperShiftInheritanceViewReferences : MonoBehaviour
    {
        [Header("Header")]
        public Text CoinText;

        [Header("Life Summary")]
        public PaperShiftTextBinding[] SummaryTexts = new PaperShiftTextBinding[0];
        public PaperShiftMilestoneBinding[] Milestones = new PaperShiftMilestoneBinding[0];

        [Header("Heir Choice")]
        public PaperShiftInheritanceHeirCardReferences[] HeirCards = new PaperShiftInheritanceHeirCardReferences[0];
        public PaperShiftTextBinding[] SelectedHeirTexts = new PaperShiftTextBinding[0];
        public Text[] InitialTagTexts = new Text[0];

        [Header("Actions")]
        public Button ContinueButton;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying || !MilestoneBindingsNeedRepair())
            {
                return;
            }

            AutoBindMilestonesFromLayout();
        }

        private bool MilestoneBindingsNeedRepair()
        {
            if (Milestones == null || Milestones.Length < 6)
            {
                return true;
            }

            for (var i = 0; i < 6; i++)
            {
                if (Milestones[i] == null || Milestones[i].AgeText == null || Milestones[i].BodyText == null)
                {
                    return true;
                }
            }

            return false;
        }

        private void AutoBindMilestonesFromLayout()
        {
            var seed = FindSummaryText("marriageText") ?? FindSummaryText("houseText") ?? FindSummaryText("childText");
            if (seed == null || seed.transform.parent == null || seed.transform.parent.parent == null)
            {
                return;
            }

            var root = seed.transform.parent.parent;
            var nodes = new List<RectTransform>();
            for (var i = 0; i < root.childCount; i++)
            {
                var node = root.GetChild(i) as RectTransform;
                if (node == null || node.childCount < 2)
                {
                    continue;
                }

                if (TextAt(node, 0) != null && TextAt(node, 1) != null)
                {
                    nodes.Add(node);
                }
            }

            if (nodes.Count < 6)
            {
                return;
            }

            nodes.Sort((left, right) =>
            {
                var y = right.anchoredPosition.y.CompareTo(left.anchoredPosition.y);
                return y != 0 ? y : left.anchoredPosition.x.CompareTo(right.anchoredPosition.x);
            });

            Milestones = new PaperShiftMilestoneBinding[6];
            for (var i = 0; i < 6; i++)
            {
                Milestones[i] = new PaperShiftMilestoneBinding
                {
                    AgeText = TextAt(nodes[i], 0),
                    BodyText = TextAt(nodes[i], 1)
                };
            }

            EditorUtility.SetDirty(this);
        }

        private Text FindSummaryText(string id)
        {
            if (SummaryTexts == null)
            {
                return null;
            }

            for (var i = 0; i < SummaryTexts.Length; i++)
            {
                if (SummaryTexts[i] != null && SummaryTexts[i].Id == id)
                {
                    return SummaryTexts[i].Text;
                }
            }

            return null;
        }

        private static Text TextAt(Transform parent, int index)
        {
            return parent == null || index < 0 || index >= parent.childCount ? null : parent.GetChild(index).GetComponent<Text>();
        }
#endif
    }
}
