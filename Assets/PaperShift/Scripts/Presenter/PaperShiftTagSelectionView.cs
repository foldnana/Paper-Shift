using System;
using System.Collections.Generic;
using PaperShift.Data;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PaperShift.Presenter
{
    internal sealed class PaperShiftTagSelectionView
    {
        private const string TagRowPrefabPath = "Assets/PaperShift/Prefab/Tag Row Item--标签选择预制体.prefab";
        private const string RuntimeRowPrefix = "Runtime Tag Row - ";

        private readonly List<GameObject> runtimeRows = new List<GameObject>();
        private readonly List<GameObject> originalRows = new List<GameObject>();
        private bool originalRowsCached;

        public GameObject TagRowPrefab;

        public void Refresh(Transform listRoot, PaperShiftGamePresenter presenter, Action onSelectionChanged)
        {
            if (listRoot == null || presenter == null || presenter.State == null)
            {
                return;
            }

            ResolvePrefab();
            if (TagRowPrefab == null)
            {
                RefreshExistingRows(listRoot, presenter, onSelectionChanged);
                return;
            }

            CacheOriginalRows(listRoot);
            HideOriginalRows();
            ClearRuntimeRows();

            for (var i = 0; i < presenter.CurrentTagChoices.Count; i++)
            {
                var tag = presenter.CurrentTagChoices[i];
                var row = UnityEngine.Object.Instantiate(TagRowPrefab, listRoot, false);
                row.name = RuntimeRowPrefix + tag.Id;
                row.SetActive(true);
                runtimeRows.Add(row);
                ConfigurePrefabRow(row.transform, presenter, tag, onSelectionChanged);
            }
        }

        private void ResolvePrefab()
        {
            if (TagRowPrefab != null)
            {
                return;
            }

#if UNITY_EDITOR
            TagRowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TagRowPrefabPath);
#endif
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
        }

        private void ConfigurePrefabRow(Transform row, PaperShiftGamePresenter presenter, TagDefinition tag, Action onSelectionChanged)
        {
            var selected = presenter.State.Worker.HasTag(tag.Id);
            SetText(Find(row, "标签介绍"), tag.Description);
            SetTagName(row, tag.DisplayName);
            SetRarityTicket(row, tag);
            SetSelected(row, selected);
            BindButtons(row, () =>
            {
                presenter.ToggleStartingTag(tag.Id);
                onSelectionChanged?.Invoke();
            });
        }

        private void SetRarityTicket(Transform row, TagDefinition tag)
        {
            var rare = IsRarity(tag, "rare");
            var superRare = IsRarity(tag, "super_rare");
            var normal = IsRarity(tag, "normal") || (!rare && !superRare);
            SetTicket(row, "Ticket 普通", normal, tag.DisplayName);
            SetTicket(row, "Ticket 稀有", rare, tag.DisplayName);
            SetTicket(row, "Ticket 超稀有", superRare, tag.DisplayName);
        }

        private void SetTicket(Transform row, string ticketName, bool active, string displayName)
        {
            var ticket = Find(row, ticketName);
            if (ticket == null)
            {
                return;
            }

            ticket.gameObject.SetActive(active);
        }

        private void SetTagName(Transform row, string displayName)
        {
            var texts = row.GetComponentsInChildren<Text>(true);
            for (var i = 0; i < texts.Length; i++)
            {
                if (texts[i].transform.name == "Label")
                {
                    texts[i].text = displayName;
                }
            }
        }

        private void SetSelected(Transform row, bool selected)
        {
            SetActive(row, "未选选择框", !selected);
            SetActive(row, "选择框", selected);
            SetActiveAll(row, "Selected Badge", selected);
        }

        private void BindButtons(Transform row, UnityEngine.Events.UnityAction onClick)
        {
            var buttons = row.GetComponentsInChildren<Button>(true);
            if (buttons.Length == 0)
            {
                var button = row.gameObject.AddComponent<Button>();
                button.targetGraphic = row.GetComponent<Graphic>();
                buttons = new[] { button };
            }

            for (var i = 0; i < buttons.Length; i++)
            {
                buttons[i].onClick.RemoveAllListeners();
                buttons[i].onClick.AddListener(onClick);
            }
        }

        private void RefreshExistingRows(Transform listRoot, PaperShiftGamePresenter presenter, Action onSelectionChanged)
        {
            for (var i = 0; i < listRoot.childCount; i++)
            {
                var row = listRoot.GetChild(i);
                if (i >= presenter.CurrentTagChoices.Count)
                {
                    row.gameObject.SetActive(false);
                    continue;
                }

                row.gameObject.SetActive(true);
                var tag = presenter.CurrentTagChoices[i];
                var selected = presenter.State.Worker.HasTag(tag.Id);
                var graphic = row.GetComponent<Graphic>();
                if (graphic != null)
                {
                    graphic.color = selected ? PaperShiftTheme.Hex("#e9f7ff") : PaperShiftTheme.White;
                }

                SetText(Find(row, "Description"), tag.Description);
                SetText(Find(row, "标签介绍"), tag.Description);
                SetTagName(row, tag.DisplayName);
                SetSelected(row, selected);
                BindButtons(row, () =>
                {
                    presenter.ToggleStartingTag(tag.Id);
                    onSelectionChanged?.Invoke();
                });
            }
        }

        private static bool IsRarity(TagDefinition tag, string rarityId)
        {
            return string.Equals(tag.RarityId, rarityId, StringComparison.OrdinalIgnoreCase);
        }

        private static void SetActive(Transform root, string name, bool active)
        {
            var target = Find(root, name);
            if (target != null)
            {
                target.gameObject.SetActive(active);
            }
        }

        private static void SetActiveAll(Transform root, string name, bool active)
        {
            var targets = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < targets.Length; i++)
            {
                if (targets[i].name == name)
                {
                    targets[i].gameObject.SetActive(active);
                }
            }
        }

        private static void SetText(Transform target, string value)
        {
            if (target == null)
            {
                return;
            }

            var text = target.GetComponent<Text>();
            if (text != null)
            {
                text.text = value;
            }
        }

        private static Transform Find(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == name)
            {
                return root;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var found = Find(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
