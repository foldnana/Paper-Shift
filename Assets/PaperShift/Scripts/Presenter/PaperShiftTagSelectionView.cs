using System;
using System.Collections;
using System.Collections.Generic;
using PaperShift.Data;
using UnityEngine;
using UnityEngine.UI;

namespace PaperShift.Presenter
{
    internal sealed class PaperShiftTagSelectionView
    {
        private const string RuntimeRowPrefix = "Runtime Tag Row - ";

        private readonly List<GameObject> runtimeRows = new List<GameObject>();
        private readonly List<PaperShiftTagChoiceItemViewReferences> runtimeRowViews = new List<PaperShiftTagChoiceItemViewReferences>();
        private readonly List<GameObject> originalRows = new List<GameObject>();
        private bool originalRowsCached;

        public PaperShiftTagChoiceItemViewReferences TagRowPrefab;
        public bool HideExistingRowsBeforeRefresh = true;

        public void Refresh(Transform listRoot, PaperShiftGamePresenter presenter, Action onSelectionChanged)
        {
            if (listRoot == null || presenter == null || presenter.State == null || TagRowPrefab == null)
            {
                return;
            }

            CacheOriginalRows(listRoot);
            if (HideExistingRowsBeforeRefresh)
            {
                HideOriginalRows();
            }

            ClearRuntimeRows();

            for (var i = 0; i < presenter.CurrentTagChoices.Count; i++)
            {
                var tag = presenter.CurrentTagChoices[i];
                var row = UnityEngine.Object.Instantiate(TagRowPrefab, listRoot, false);
                row.name = RuntimeRowPrefix + tag.Id;
                row.gameObject.SetActive(true);
                runtimeRows.Add(row.gameObject);
                runtimeRowViews.Add(row);
                ConfigureRow(row, presenter, tag, onSelectionChanged);
            }
        }

        public IEnumerator PlayRollAnimation(
            PaperShiftGamePresenter presenter,
            IList<TagDefinition> spinPool,
            Action onSelectionChanged,
            float tickSeconds,
            float rowSettleSeconds)
        {
            if (presenter == null || presenter.CurrentTagChoices == null || runtimeRowViews.Count == 0)
            {
                yield break;
            }

            if (spinPool == null || spinPool.Count == 0)
            {
                spinPool = presenter.CurrentTagChoices;
            }

            SetRowsInteractable(false);

            var tick = Mathf.Max(0.01f, tickSeconds);
            var rowSettle = Mathf.Max(0.01f, rowSettleSeconds);
            var settledRows = 0;
            while (settledRows < runtimeRowViews.Count)
            {
                var stopAt = Time.unscaledTime + rowSettle;
                while (Time.unscaledTime < stopAt)
                {
                    for (var i = settledRows; i < runtimeRowViews.Count; i++)
                    {
                        var preview = spinPool[UnityEngine.Random.Range(0, spinPool.Count)];
                        ConfigurePreview(runtimeRowViews[i], preview);
                    }

                    yield return new WaitForSecondsRealtime(tick);
                }

                if (settledRows < presenter.CurrentTagChoices.Count)
                {
                    ConfigureRow(runtimeRowViews[settledRows], presenter, presenter.CurrentTagChoices[settledRows], onSelectionChanged);
                    SetRowInteractable(runtimeRowViews[settledRows], true);
                }

                settledRows++;
            }

            for (var i = 0; i < runtimeRowViews.Count && i < presenter.CurrentTagChoices.Count; i++)
            {
                ConfigureRow(runtimeRowViews[i], presenter, presenter.CurrentTagChoices[i], onSelectionChanged);
                SetRowInteractable(runtimeRowViews[i], true);
            }
        }

        public string CurrentSignature(PaperShiftGamePresenter presenter)
        {
            if (presenter == null || presenter.CurrentTagChoices == null)
            {
                return string.Empty;
            }

            var result = string.Empty;
            for (var i = 0; i < presenter.CurrentTagChoices.Count; i++)
            {
                var tag = presenter.CurrentTagChoices[i];
                result += (tag == null ? string.Empty : tag.Id) + "|";
            }

            return result;
        }

        private void CacheOriginalRows(Transform listRoot)
        {
            if (originalRowsCached)
            {
                return;
            }

            originalRowsCached = true;
            for (var i = 0; i < listRoot.childCount; i++)
            {
                var child = listRoot.GetChild(i);
                if (!child.name.StartsWith(RuntimeRowPrefix, StringComparison.Ordinal))
                {
                    originalRows.Add(child.gameObject);
                }
            }
        }

        private void HideOriginalRows()
        {
            for (var i = 0; i < originalRows.Count; i++)
            {
                if (originalRows[i] != null)
                {
                    originalRows[i].SetActive(false);
                }
            }
        }

        private void ClearRuntimeRows()
        {
            for (var i = runtimeRows.Count - 1; i >= 0; i--)
            {
                var row = runtimeRows[i];
                if (row == null)
                {
                    continue;
                }

                row.SetActive(false);
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(row);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(row);
                }
            }

            runtimeRows.Clear();
            runtimeRowViews.Clear();
        }

        private void ConfigureRow(PaperShiftTagChoiceItemViewReferences row, PaperShiftGamePresenter presenter, TagDefinition tag, Action onSelectionChanged)
        {
            var selected = presenter.State.Worker.HasTag(tag.Id);
            Set(row.LabelText, tag.DisplayName);
            Set(row.DescriptionText, tag.Description);
            ApplyRarity(row, tag.RarityId);
            ApplySelected(row, selected);
            BindButtons(row, () =>
            {
                presenter.ToggleStartingTag(tag.Id);
                onSelectionChanged?.Invoke();
            });
        }

        private static void ConfigurePreview(PaperShiftTagChoiceItemViewReferences row, TagDefinition tag)
        {
            if (row == null || tag == null)
            {
                return;
            }

            Set(row.LabelText, tag.DisplayName);
            Set(row.DescriptionText, tag.Description);
            ApplyRarity(row, tag.RarityId);
            ApplySelected(row, false);
            SetRowInteractable(row, false);
        }

        private void SetRowsInteractable(bool interactable)
        {
            for (var i = 0; i < runtimeRowViews.Count; i++)
            {
                SetRowInteractable(runtimeRowViews[i], interactable);
            }
        }

        private static void SetRowInteractable(PaperShiftTagChoiceItemViewReferences row, bool interactable)
        {
            if (row == null || row.ActionButtons == null)
            {
                return;
            }

            for (var i = 0; i < row.ActionButtons.Length; i++)
            {
                if (row.ActionButtons[i] != null)
                {
                    row.ActionButtons[i].interactable = interactable;
                }
            }
        }

        private static void ApplyRarity(PaperShiftTagChoiceItemViewReferences row, string rarityId)
        {
            var rare = IsRarity(rarityId, "rare");
            var superRare = IsRarity(rarityId, "super_rare");
            SetActive(row.NormalState, !rare && !superRare);
            SetActive(row.RareState, rare);
            SetActive(row.SuperRareState, superRare);
        }

        private static void ApplySelected(PaperShiftTagChoiceItemViewReferences row, bool selected)
        {
            SetActive(row.UnselectedState, !selected);
            SetActive(row.SelectedState, selected);

            if (row.SelectedBadges != null)
            {
                for (var i = 0; i < row.SelectedBadges.Length; i++)
                {
                    SetActive(row.SelectedBadges[i], selected);
                }
            }

            if (row.Background != null)
            {
                row.Background.color = selected ? PaperShiftTheme.Hex("#e9f7ff") : PaperShiftTheme.White;
            }
        }

        private static void BindButtons(PaperShiftTagChoiceItemViewReferences row, UnityEngine.Events.UnityAction onClick)
        {
            if (row.ActionButtons == null)
            {
                return;
            }

            for (var i = 0; i < row.ActionButtons.Length; i++)
            {
                var button = row.ActionButtons[i];
                if (button == null)
                {
                    continue;
                }

                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(onClick);
            }
        }

        private static bool IsRarity(string rarityId, string expected)
        {
            return string.Equals(rarityId, expected, StringComparison.OrdinalIgnoreCase);
        }

        private static void Set(Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }
    }
}
