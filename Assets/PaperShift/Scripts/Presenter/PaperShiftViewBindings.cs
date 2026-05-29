using PaperShift.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace PaperShift.Presenter
{
    [System.Serializable]
    public sealed class PaperShiftTextBinding
    {
        public string Id;
        public Text Text;

        public void Set(string value)
        {
            if (Text != null)
            {
                Text.text = value;
            }
        }
    }

    [System.Serializable]
    public sealed class PaperShiftButtonBinding
    {
        public string Id;
        public Button Button;
        public Text Label;

        public void SetLabel(string value)
        {
            if (Label != null)
            {
                Label.text = value;
            }
        }

        public void SetVisible(bool visible)
        {
            if (Button != null)
            {
                Button.gameObject.SetActive(visible);
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (Button != null)
            {
                Button.interactable = interactable;
            }
        }
    }

    [System.Serializable]
    public sealed class PaperShiftEraTileBinding
    {
        public string EraId;
        public Button Button;
        public Graphic Background;
        public Text Label;
    }

    [System.Serializable]
    public sealed class PaperShiftSelectableTextBinding
    {
        public string Id;
        public Button Button;
        public Graphic Background;
        public Outline Outline;
        public Text Label;
    }

    [System.Serializable]
    public sealed class PaperShiftResumeOptionBinding
    {
        public Button Button;
        public Graphic Background;
        public Outline Outline;
        public Text Label;
    }

    [System.Serializable]
    public sealed class PaperShiftResumeLineBinding
    {
        public string FieldId;
        public Text Value;
        public PaperShiftResumeOptionBinding[] Options = new PaperShiftResumeOptionBinding[0];
    }

    [System.Serializable]
    public sealed class PaperShiftBudgetRowBinding
    {
        public string BudgetId;
        public Slider Slider;
        public Text ValueText;
    }

    [System.Serializable]
    public sealed class PaperShiftCardRowBinding
    {
        public Text Label;
        public Text Value;

        public void Set(UiPair pair)
        {
            if (Label != null)
            {
                Label.text = pair == null ? string.Empty : pair.Label;
            }

            if (Value != null)
            {
                Value.text = pair == null ? string.Empty : pair.Value;
            }
        }
    }

    [System.Serializable]
    public sealed class PaperShiftTagSlotBinding
    {
        public GameObject Root;
        public Text Label;
    }

}
