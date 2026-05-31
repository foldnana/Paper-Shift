using System;
using UnityEngine;
using UnityEngine.UI;

namespace PaperShift.Presenter
{
    public sealed class PaperShiftResumeInfoViewReferences : MonoBehaviour
    {
        [Header("Header")]
        public Text CoinText;
        public Text HeaderNameText;
        public Text HeaderMetaText;
        public Text GenerationText;

        [Header("Actions")]
        public Button BackButton;
        public Button SendResumeButton;

        [Header("Info Items")]
        public PaperShiftResumeInfoItemView[] InfoItems = new PaperShiftResumeInfoItemView[0];

        [Header("Selected Tags")]
        public Transform TagContentRoot;
        public PaperShiftResumeTagItemViewReferences TagPrefab;
        public bool HideExistingTagChildrenBeforeRefresh = true;
        public GameObject[] ExistingTagItemsToHide = new GameObject[0];

        [Header("Footer")]
        public Text EditedCountText;
        public Text RiskText;
    }

    [Serializable]
    public sealed class PaperShiftResumeInfoItemView
    {
        public string FieldId;
        public GameObject Root;
        public Text LabelText;
        public Text ValueText;
        public GameObject NormalState;
        public GameObject RareState;
        public GameObject SuperRareState;
        public Button HideButton;
        public Button ExaggerateButton;
    }

}
