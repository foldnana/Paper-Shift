using UnityEngine;
using UnityEngine.UI;

namespace PaperShift.Presenter
{
    public sealed class PaperShiftInheritanceViewReferences : MonoBehaviour
    {
        [Header("Header")]
        public Text CoinText;

        [Header("Life Summary")]
        public PaperShiftTextBinding[] SummaryTexts = new PaperShiftTextBinding[0];

        [Header("Heir Choice")]
        public PaperShiftInheritanceHeirCardReferences[] HeirCards = new PaperShiftInheritanceHeirCardReferences[0];
        public PaperShiftTextBinding[] SelectedHeirTexts = new PaperShiftTextBinding[0];
        public Text[] InitialTagTexts = new Text[0];

        [Header("Actions")]
        public Button ContinueButton;
    }
}
