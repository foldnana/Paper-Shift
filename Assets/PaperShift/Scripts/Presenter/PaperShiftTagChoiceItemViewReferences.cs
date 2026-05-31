using UnityEngine;
using UnityEngine.UI;

namespace PaperShift.Presenter
{
    public sealed class PaperShiftTagChoiceItemViewReferences : MonoBehaviour
    {
        public Text LabelText;
        public Text DescriptionText;
        public GameObject NormalState;
        public GameObject RareState;
        public GameObject SuperRareState;
        public GameObject SelectedState;
        public GameObject UnselectedState;
        public GameObject[] SelectedBadges = new GameObject[0];
        public Button[] ActionButtons = new Button[0];
        public Graphic Background;
    }
}
