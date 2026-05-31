using System.Collections.Generic;
using PaperShift.Domain;
using PaperShift.Model;
using UnityEngine;
using UnityEngine.UI;
using static PaperShift.Presenter.PaperShiftUiFormatter;

namespace PaperShift.Presenter
{
    public sealed class PaperShiftResumeScreenBinder : PaperShiftScreenBinderBase
    {
        private const int MaxEditedInfoCount = 3;

        public PaperShiftResumeInfoViewReferences View;

        [HideInInspector] public Text CoinText;
        [HideInInspector] public Text HeaderNameText;
        [HideInInspector] public Text HeaderMetaText;
        [HideInInspector] public Text GenerationText;
        [HideInInspector] public Text RiskText;
        [HideInInspector] public Text EditedCountText;
        [HideInInspector] public PaperShiftInfoRowBinding[] InfoRows = new PaperShiftInfoRowBinding[0];
        [HideInInspector] public Button BackButton;
        [HideInInspector] public Button SendResumeButton;

        [HideInInspector] public PaperShiftSelectableTextBinding[] IntentButtons = new PaperShiftSelectableTextBinding[0];
        [HideInInspector] public PaperShiftResumeLineBinding[] ResumeLines = new PaperShiftResumeLineBinding[0];
        [HideInInspector] public Transform TagPoolRoot;

        private readonly List<GameObject> runtimeTagItems = new List<GameObject>();

        private void Reset()
        {
            Screen = PaperShiftScreen.Resume;
            View = GetComponent<PaperShiftResumeInfoViewReferences>();
        }

        public override void BindActions()
        {
            ResolveView();

            Bind(ActiveBackButton(), () =>
            {
                if (SceneController != null)
                {
                    SceneController.ShowTags();
                }
            });

            Bind(ActiveSendButton(), () =>
            {
                Presenter.FindInterviewAndShow();
                RefreshAll();
            });

            BindInfoButtons();
        }

        public override void RefreshView()
        {
            if (State == null || State.Worker == null)
            {
                return;
            }

            ResolveView();
            RefreshHeader();
            RefreshInfoItems();
            RefreshWorkerTags();
            RefreshFooter();
        }

        private void ResolveView()
        {
            if (View == null)
            {
                View = GetComponent<PaperShiftResumeInfoViewReferences>();
            }
        }

        private void BindInfoButtons()
        {
            var viewItems = View == null ? null : View.InfoItems;
            if (viewItems != null && viewItems.Length > 0)
            {
                for (var i = 0; i < viewItems.Length; i++)
                {
                    var item = viewItems[i];
                    if (item == null || string.IsNullOrEmpty(item.FieldId))
                    {
                        continue;
                    }

                    var fieldId = PaperShiftWorkerAttributes.Canonicalize(item.FieldId);
                    Bind(item.HideButton, () => ToggleFieldMode(fieldId, ResumePackagingMode.Hide));
                    Bind(item.ExaggerateButton, () => ToggleFieldMode(fieldId, ResumePackagingMode.Exaggerate));
                }

                return;
            }

            for (var i = 0; i < InfoRows.Length; i++)
            {
                var row = InfoRows[i];
                if (row == null || string.IsNullOrEmpty(row.FieldId))
                {
                    continue;
                }

                var fieldId = PaperShiftWorkerAttributes.Canonicalize(row.FieldId);
                Bind(row.HideButton, () => ToggleFieldMode(fieldId, ResumePackagingMode.Hide));
                Bind(row.ExaggerateButton, () => ToggleFieldMode(fieldId, ResumePackagingMode.Exaggerate));
            }
        }

        private void RefreshHeader()
        {
            var worker = State.Worker;
            Set(ActiveCoinText(), worker.Money.ToString("N0"));
            Set(ActiveHeaderNameText(), worker.FullName);
            Set(ActiveHeaderMetaText(), worker.Gender + " " + worker.Age + " 岁 " + worker.EraName);
            Set(ActiveGenerationText(), "第" + State.Generation + "代");
        }

        private void RefreshInfoItems()
        {
            var viewItems = View == null ? null : View.InfoItems;
            if (viewItems != null && viewItems.Length > 0)
            {
                for (var i = 0; i < viewItems.Length; i++)
                {
                    RefreshInfoItem(viewItems[i]);
                }

                return;
            }

            for (var i = 0; i < InfoRows.Length; i++)
            {
                RefreshLegacyInfoRow(InfoRows[i]);
            }
        }

        private void RefreshInfoItem(PaperShiftResumeInfoItemView item)
        {
            if (item == null || string.IsNullOrEmpty(item.FieldId))
            {
                return;
            }

            var fieldId = PaperShiftWorkerAttributes.Canonicalize(item.FieldId);
            var choice = State.Resume.GetOrCreateChoice(fieldId);
            var mode = choice.OptionIndex < 0 ? ResumePackagingMode.Normal : choice.Mode;
            var rarityId = InfoRarityId(fieldId);
            var label = InfoLabel(fieldId);
            var value = InfoValue(fieldId, mode);

            if (item.Root != null)
            {
                item.Root.SetActive(true);
            }

            Set(item.LabelText, label);
            Set(item.ValueText, value);
            ApplyRarityState(item.NormalState, item.RareState, item.SuperRareState, rarityId);

            SetButtonStyle(item.HideButton, mode == ResumePackagingMode.Hide ? PaperShiftTheme.GrayButton : PaperShiftTheme.White, PaperShiftTheme.MutedInk);
            SetButtonStyle(item.ExaggerateButton, mode == ResumePackagingMode.Exaggerate ? PaperShiftTheme.Blue : PaperShiftTheme.White, mode == ResumePackagingMode.Exaggerate ? PaperShiftTheme.White : PaperShiftTheme.Blue);
        }

        private void RefreshLegacyInfoRow(PaperShiftInfoRowBinding row)
        {
            if (row == null || string.IsNullOrEmpty(row.FieldId))
            {
                return;
            }

            var fieldId = PaperShiftWorkerAttributes.Canonicalize(row.FieldId);
            var choice = State.Resume.GetOrCreateChoice(fieldId);
            var mode = choice.OptionIndex < 0 ? ResumePackagingMode.Normal : choice.Mode;
            var rarityId = InfoRarityId(fieldId);
            Set(row.LabelText, InfoLabel(fieldId));
            Set(row.ValueText, InfoValue(fieldId, mode));
            Set(row.BadgeText, InfoBadge(mode, rarityId));

            if (row.Background != null)
            {
                row.Background.color = RowColor(rarityId, mode);
            }

            SetButtonStyle(row.HideButton, mode == ResumePackagingMode.Hide ? PaperShiftTheme.GrayButton : PaperShiftTheme.White, PaperShiftTheme.MutedInk);
            SetButtonStyle(row.ExaggerateButton, mode == ResumePackagingMode.Exaggerate ? PaperShiftTheme.Blue : PaperShiftTheme.White, mode == ResumePackagingMode.Exaggerate ? PaperShiftTheme.White : PaperShiftTheme.Blue);
        }

        private void RefreshWorkerTags()
        {
            if (View == null || View.TagContentRoot == null || View.TagPrefab == null || State.Worker.Tags == null)
            {
                return;
            }

            ClearRuntimeTags();
            HideExistingTagItems();

            for (var i = 0; i < State.Worker.Tags.Count; i++)
            {
                var tag = State.Worker.Tags[i];
                if (tag == null)
                {
                    continue;
                }

                var item = Instantiate(View.TagPrefab, View.TagContentRoot, false);
                item.gameObject.SetActive(true);
                runtimeTagItems.Add(item.gameObject);
                ConfigureTagItem(item, tag);
            }
        }

        private void ConfigureTagItem(PaperShiftResumeTagItemViewReferences item, TagInstance tag)
        {
            Set(item.LabelText, tag.DisplayName);
            ApplyRarityState(item.NormalState, item.RareState, item.SuperRareState, tag.RarityId);

            Bind(item.HideButton, () => ToggleTagHidden(tag.TagId));
        }

        private void HideExistingTagItems()
        {
            if (View.ExistingTagItemsToHide != null)
            {
                for (var i = 0; i < View.ExistingTagItemsToHide.Length; i++)
                {
                    if (View.ExistingTagItemsToHide[i] != null)
                    {
                        View.ExistingTagItemsToHide[i].SetActive(false);
                    }
                }
            }

            if (!View.HideExistingTagChildrenBeforeRefresh || View.TagContentRoot == null)
            {
                return;
            }

            for (var i = 0; i < View.TagContentRoot.childCount; i++)
            {
                var child = View.TagContentRoot.GetChild(i).gameObject;
                if (!runtimeTagItems.Contains(child))
                {
                    child.SetActive(false);
                }
            }
        }

        private void ClearRuntimeTags()
        {
            for (var i = runtimeTagItems.Count - 1; i >= 0; i--)
            {
                var item = runtimeTagItems[i];
                if (item == null)
                {
                    continue;
                }

                item.SetActive(false);
                if (Application.isPlaying)
                {
                    Destroy(item);
                }
                else
                {
                    DestroyImmediate(item);
                }
            }

            runtimeTagItems.Clear();
        }

        private void RefreshFooter()
        {
            Set(ActiveEditedCountText(), EditedInfoCount() + "/" + MaxEditedInfoCount + " <size=14>可选</size>");
            Set(ActiveRiskText(), EditedInfoCount() + "/" + MaxEditedInfoCount + " 可选");
        }

        private void ToggleFieldMode(string fieldId, ResumePackagingMode requestedMode)
        {
            fieldId = PaperShiftWorkerAttributes.Canonicalize(fieldId);
            var choice = State.Resume.GetOrCreateChoice(fieldId);
            var currentMode = choice.OptionIndex < 0 ? ResumePackagingMode.Normal : choice.Mode;
            if (currentMode == requestedMode)
            {
                Presenter.SetResumePackaging(fieldId, ResumePackagingMode.Normal);
                RefreshView();
                return;
            }

            if (currentMode == ResumePackagingMode.Normal && EditedInfoCount() >= MaxEditedInfoCount)
            {
                ShowBanner("最多处理 3 条信息");
                return;
            }

            Presenter.SetResumePackaging(fieldId, requestedMode, 0);
            RefreshView();
        }

        private void ToggleTagHidden(string tagId)
        {
            var hidden = IsTagHidden(tagId);
            if (!hidden && EditedInfoCount() >= MaxEditedInfoCount)
            {
                ShowBanner("最多处理 3 条信息");
                return;
            }

            if (!Presenter.ToggleResumeHiddenTag(tagId))
            {
                ShowBanner("最多隐藏 3 个标签");
                return;
            }

            RefreshView();
        }

        private int EditedInfoCount()
        {
            var count = 0;
            if (State == null || State.Resume == null)
            {
                return count;
            }

            for (var i = 0; i < State.Resume.Packaging.Count; i++)
            {
                var choice = State.Resume.Packaging[i];
                if (choice != null && choice.OptionIndex >= 0 && choice.Mode != ResumePackagingMode.Normal)
                {
                    count++;
                }
            }

            if (State.Resume.HiddenTagIds != null)
            {
                count += State.Resume.HiddenTagIds.Count;
            }

            return count;
        }

        private bool IsTagHidden(string tagId)
        {
            return State.Resume.HiddenTagIds != null && State.Resume.HiddenTagIds.Contains(tagId);
        }

        private string InfoLabel(string fieldId)
        {
            return PaperShiftWorkerAttributes.DisplayName(fieldId);
        }

        private string InfoValue(string fieldId, ResumePackagingMode mode)
        {
            if (mode == ResumePackagingMode.Hide)
            {
                return "已隐藏";
            }

            return PaperShiftWorkerAttributes.DisplayValue(State.Worker, fieldId, mode == ResumePackagingMode.Exaggerate);
        }

        private static string InfoBadge(ResumePackagingMode mode, string rarityId)
        {
            if (mode == ResumePackagingMode.Hide)
            {
                return "隐瞒";
            }

            if (mode == ResumePackagingMode.Exaggerate)
            {
                return "★ 夸大!";
            }

            return RarityBadge(rarityId);
        }

        private string InfoRarityId(string fieldId)
        {
            return PaperShiftWorkerAttributes.RarityId(State.Worker, fieldId);
        }

        private static string RarityBadge(string rarityId)
        {
            if (IsRarity(rarityId, "super_rare"))
            {
                return "★ 超稀有!";
            }

            return IsRarity(rarityId, "rare") ? "★ 稀有!" : "普通";
        }

        private static bool IsRarity(string rarityId, string expected)
        {
            return string.Equals(rarityId, expected, System.StringComparison.OrdinalIgnoreCase);
        }

        private static Color TagColor(string rarityId)
        {
            if (IsRarity(rarityId, "rare"))
            {
                return PaperShiftTheme.BlueTicket;
            }

            if (IsRarity(rarityId, "super_rare"))
            {
                return PaperShiftTheme.PurpleTicket;
            }

            return PaperShiftTheme.White;
        }

        private static Color RowColor(string rarityId, ResumePackagingMode mode)
        {
            if (mode == ResumePackagingMode.Hide)
            {
                return PaperShiftTheme.Hex("#f4f4f4");
            }

            if (mode == ResumePackagingMode.Exaggerate)
            {
                return PaperShiftTheme.PurpleTicket;
            }

            return TagColor(rarityId);
        }

        private static void ApplyRarityState(
            GameObject normal,
            GameObject rare,
            GameObject superRare,
            string rarityId)
        {
            var isRare = IsRarity(rarityId, "rare");
            var isSuperRare = IsRarity(rarityId, "super_rare");
            SetActive(normal, !isRare && !isSuperRare);
            SetActive(rare, isRare);
            SetActive(superRare, isSuperRare);
        }

        private Text ActiveCoinText()
        {
            return View != null && View.CoinText != null ? View.CoinText : CoinText;
        }

        private Text ActiveHeaderNameText()
        {
            return View != null && View.HeaderNameText != null ? View.HeaderNameText : HeaderNameText;
        }

        private Text ActiveHeaderMetaText()
        {
            return View != null && View.HeaderMetaText != null ? View.HeaderMetaText : HeaderMetaText;
        }

        private Text ActiveGenerationText()
        {
            return View != null && View.GenerationText != null ? View.GenerationText : GenerationText;
        }

        private Text ActiveEditedCountText()
        {
            return View != null && View.EditedCountText != null ? View.EditedCountText : EditedCountText;
        }

        private Text ActiveRiskText()
        {
            return View != null && View.RiskText != null ? View.RiskText : RiskText;
        }

        private Button ActiveBackButton()
        {
            return View != null && View.BackButton != null ? View.BackButton : BackButton;
        }

        private Button ActiveSendButton()
        {
            return View != null && View.SendResumeButton != null ? View.SendResumeButton : SendResumeButton;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }

        private static void SetButtonStyle(Button button, Color background, Color textColor)
        {
            if (button == null)
            {
                return;
            }

            if (button.targetGraphic != null)
            {
                button.targetGraphic.color = background;
            }
        }
    }
}
