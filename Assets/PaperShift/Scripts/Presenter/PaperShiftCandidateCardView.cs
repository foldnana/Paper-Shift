using PaperShift.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace PaperShift.Presenter
{
    public sealed class PaperShiftCandidateCardView : MonoBehaviour
    {
        public Text BadgeText;
        public Text CornerText;
        public Text TopInfoText;
        public Text RingLabelText;
        public PaperShiftCardRowBinding[] Rows = new PaperShiftCardRowBinding[0];
        public PaperShiftTagSlotBinding[] Tags = new PaperShiftTagSlotBinding[0];
        public Text ProgressText;
        public RectTransform ProgressFill;
        public Text[] LogLines = new Text[0];

        public void Refresh(CandidateUiData data, PaperShiftPhase phase)
        {
            if (data == null)
            {
                return;
            }

            Set(BadgeText, data.Badge);
            Set(CornerText, data.Corner);
            Set(TopInfoText, data.Name + "\n<size=18>" + data.Subtitle + "</size>");
            Set(RingLabelText, data.RingText);

            for (var i = 0; i < Rows.Length; i++)
            {
                if (Rows[i] != null)
                {
                    Rows[i].Set(i < data.Rows.Count ? data.Rows[i] : null);
                }
            }

            for (var i = 0; i < Tags.Length; i++)
            {
                var slot = Tags[i];
                if (slot == null)
                {
                    continue;
                }

                var hasTag = i < data.Tags.Count;
                if (slot.Root != null)
                {
                    slot.Root.SetActive(hasTag);
                }

                if (hasTag)
                {
                    Set(slot.Label, data.Tags[i]);
                }
            }

            if (!string.IsNullOrEmpty(data.ProgressPercent))
            {
                Set(ProgressText, "<size=32>" + data.ProgressPercent + "</size> " + data.ProgressLabel);
                if (ProgressFill != null)
                {
                    ProgressFill.anchorMax = new Vector2(Mathf.Clamp01(data.ProgressFill), 1f);
                }
            }

            for (var i = 0; i < LogLines.Length; i++)
            {
                if (LogLines[i] == null)
                {
                    continue;
                }

                var hasLog = data.Logs != null && i < data.Logs.Count;
                LogLines[i].gameObject.SetActive(hasLog);
                if (hasLog)
                {
                    LogLines[i].text = data.Logs[i];
                }
            }
        }

        private static void Set(Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }
    }
}
