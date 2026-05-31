using UnityEngine;
using UnityEngine.UI;

namespace PaperShift.Presenter
{
    public sealed class PaperShiftTagsViewReferences : MonoBehaviour
    {
        [Header("Header")]
        public Text TitleText;
        public Text CoinText;

        [Header("Tag List")]
        public Transform TagListRoot;
        public PaperShiftTagChoiceItemViewReferences TagRowPrefab;
        [Min(1)] public int TagChoiceCount = 7;
        public bool HideExistingRowsBeforeRefresh = true;

        [Header("Actions")]
        public Button FreeRefreshButton;
        public Button SuperRefreshButton;
        public GameObject ConfirmPromptRoot;
        public Button ConfirmButton;
        public Text ConfirmLabel;
        public Button StartJobButton;
        public Text StartJobLabel;
    }
}
