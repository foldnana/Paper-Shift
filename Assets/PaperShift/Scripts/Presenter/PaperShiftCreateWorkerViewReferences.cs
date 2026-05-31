using System;
using UnityEngine;
using UnityEngine.UI;

namespace PaperShift.Presenter
{
    public sealed class PaperShiftCreateWorkerViewReferences : MonoBehaviour
    {
        [Header("Header")]
        public Text CoinText;
        public Text LastNameText;
        public Text FirstNameText;
        public Text FullNameText;

        [Header("Base Info")]
        public Text GenderText;
        public Text PersonalityText;
        public Text AgeText;
        public Text EraText;

        [Header("Attributes")]
        public PaperShiftCreateAttributeView[] Attributes = new PaperShiftCreateAttributeView[0];

        [Header("Era")]
        public PaperShiftEraTileBinding[] EraTiles = new PaperShiftEraTileBinding[0];

        [Header("Actions")]
        public Button RandomButton;
        public Button RandomNameButton;
        public Button PlayButton;
        public Button CustomButton;
        public Button NextButton;
    }

    [Serializable]
    public sealed class PaperShiftCreateAttributeView
    {
        public string FieldId;
        public Text LabelText;
        public Text ValueText;
        public GameObject NormalState;
        public GameObject RareState;
        public GameObject SuperRareState;
    }
}
