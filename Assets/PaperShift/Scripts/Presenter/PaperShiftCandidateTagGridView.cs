using System;
using System.Collections.Generic;
using PaperShift.Domain;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PaperShift.Presenter
{
    internal sealed class PaperShiftCandidateTagGridView
    {
        private const int MaxSlots = 12;
        private const string TagPrefabPath = "Assets/PaperShift/Prefab/标签.prefab";
        private const string EmptySlotPrefabPath = "Assets/PaperShift/Prefab/Empty Slot.prefab";
        private const string RuntimeItemPrefix = "Runtime Status Tag Slot - ";

        public GameObject TagPrefab;
        public GameObject EmptySlotPrefab;

        public void Refresh(Transform tagsRoot, IList<TagInstance> tags)
        {
            if (tagsRoot == null)
            {
                return;
            }

            ResolvePrefabs();
            ConfigureGrid(tagsRoot);
            HideOriginalItems(tagsRoot);
            ClearRuntimeItems(tagsRoot);

            var tagCount = tags == null ? 0 : Mathf.Min(tags.Count, MaxSlots);
            for (var i = 0; i < MaxSlots; i++)
            {
                if (i < tagCount && TagPrefab != null)
                {
                    var item = UnityEngine.Object.Instantiate(TagPrefab, tagsRoot, false);
                    item.name = RuntimeItemPrefix + "Tag " + tags[i].TagId;
                    item.SetActive(true);
                    ConfigureTag(item.transform, tags[i], tagsRoot);
                    continue;
                }

                if (i < tagCount)
                {
                    CreateFallbackTag(tagsRoot, tags[i]);
                    continue;
                }

                if (EmptySlotPrefab != null)
                {
                    var item = UnityEngine.Object.Instantiate(EmptySlotPrefab, tagsRoot, false);
                    item.name = RuntimeItemPrefix + "Empty " + i;
                    item.SetActive(true);
                    ConfigureSlot(item.transform, tagsRoot);
                    continue;
                }

                CreateFallbackSlot(tagsRoot, i);
            }
        }

        private void ResolvePrefabs()
        {
#if UNITY_EDITOR
            if (TagPrefab == null)
            {
                TagPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TagPrefabPath);
            }

            if (EmptySlotPrefab == null)
            {
                EmptySlotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EmptySlotPrefabPath);
            }
#endif
        }

        private static void ConfigureGrid(Transform tagsRoot)
        {
            if (tagsRoot is RectTransform rect)
            {
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, 132f);
            }

            var grid = tagsRoot.GetComponent<GridLayoutGroup>();
            if (grid == null)
            {
                grid = tagsRoot.gameObject.AddComponent<GridLayoutGroup>();
            }

            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.cellSize = new Vector2(96f, 36f);
            grid.spacing = new Vector2(8f, 8f);
            grid.childAlignment = TextAnchor.UpperLeft;
        }

        private static void ConfigureTag(Transform root, TagInstance tag, Transform tagsRoot)
        {
            ConfigureItemLayout(root, tagsRoot);
            SetTagName(root, tag.DisplayName);
            SetRarityTicket(root, tag.RarityId);
            SetActive(root, "Rarity Note", false);

            var buttons = root.GetComponentsInChildren<Button>(true);
            for (var i = 0; i < buttons.Length; i++)
            {
                buttons[i].onClick.RemoveAllListeners();
                buttons[i].interactable = false;
            }
        }

        private static void ConfigureSlot(Transform root, Transform tagsRoot)
        {
            ConfigureItemLayout(root, tagsRoot);
        }

        private static void CreateFallbackTag(Transform tagsRoot, TagInstance tag)
        {
            var grid = tagsRoot.GetComponent<GridLayoutGroup>();
            var cellSize = grid == null ? new Vector2(96f, 36f) : grid.cellSize;
            var color = TagColor(tag.RarityId);
            var border = TagBorder(tag.RarityId);
            var root = CreateRounded(tagsRoot, RuntimeItemPrefix + "Tag " + tag.TagId, color, 7f);
            ConfigureItemLayout(root, tagsRoot);
            AddOutline(root.gameObject, border, 2f);
            AddText(root, "Label", tag.DisplayName, 12, PaperShiftTheme.Hex("#3b3f43"), TextAnchor.MiddleCenter, new RectOffset(8, 8, 0, 0));
            if (root is RectTransform rect)
            {
                rect.sizeDelta = cellSize;
            }
        }

        private static void CreateFallbackSlot(Transform tagsRoot, int index)
        {
            var root = CreateRounded(tagsRoot, RuntimeItemPrefix + "Empty " + index, PaperShiftTheme.Slot, 8f);
            ConfigureItemLayout(root, tagsRoot);
            AddOutline(root.gameObject, PaperShiftTheme.Hex("#d8e0e6"), 2f);
        }

        private static void ConfigureItemLayout(Transform root, Transform tagsRoot)
        {
            var grid = tagsRoot.GetComponent<GridLayoutGroup>();
            var cellSize = grid == null ? new Vector2(96f, 36f) : grid.cellSize;

            if (root is RectTransform rect)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = cellSize;
                rect.localScale = Vector3.one;
            }

            var layout = root.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = root.gameObject.AddComponent<LayoutElement>();
            }

            layout.preferredWidth = cellSize.x;
            layout.preferredHeight = cellSize.y;

            var rects = root.GetComponentsInChildren<RectTransform>(true);
            for (var i = 0; i < rects.Length; i++)
            {
                if (!rects[i].name.StartsWith("Ticket ", StringComparison.Ordinal))
                {
                    continue;
                }

                rects[i].sizeDelta = new Vector2(cellSize.x - 4f, cellSize.y);
                var ticketLayout = rects[i].GetComponent<LayoutElement>();
                if (ticketLayout != null)
                {
                    ticketLayout.preferredWidth = cellSize.x - 4f;
                    ticketLayout.preferredHeight = cellSize.y;
                }
            }

            var labels = root.GetComponentsInChildren<Text>(true);
            for (var i = 0; i < labels.Length; i++)
            {
                if (labels[i].transform.name != "Label")
                {
                    continue;
                }

                labels[i].fontSize = 12;
                labels[i].resizeTextForBestFit = true;
                labels[i].resizeTextMinSize = 8;
                labels[i].resizeTextMaxSize = 12;
            }
        }

        private static void SetTagName(Transform root, string displayName)
        {
            var labels = root.GetComponentsInChildren<Text>(true);
            for (var i = 0; i < labels.Length; i++)
            {
                if (labels[i].transform.name == "Label")
                {
                    labels[i].text = displayName;
                }
            }
        }

        private static void SetRarityTicket(Transform root, string rarityId)
        {
            var rare = IsRarity(rarityId, "rare");
            var superRare = IsRarity(rarityId, "super_rare");
            var normal = IsRarity(rarityId, "normal") || (!rare && !superRare);
            SetActive(root, "Ticket 普通", normal);
            SetActive(root, "Ticket 稀有", rare);
            SetActive(root, "Ticket 超稀有", superRare);
        }

        private static bool IsRarity(string rarityId, string expected)
        {
            return string.Equals(rarityId, expected, StringComparison.OrdinalIgnoreCase);
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

        private static Color TagBorder(string rarityId)
        {
            if (IsRarity(rarityId, "rare"))
            {
                return PaperShiftTheme.Hex("#315e77");
            }

            if (IsRarity(rarityId, "super_rare"))
            {
                return PaperShiftTheme.Hex("#5a367b");
            }

            return PaperShiftTheme.Hex("#42474b");
        }

        private static void HideOriginalItems(Transform tagsRoot)
        {
            for (var i = 0; i < tagsRoot.childCount; i++)
            {
                var child = tagsRoot.GetChild(i);
                if (!child.name.StartsWith(RuntimeItemPrefix, StringComparison.Ordinal))
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        private static void ClearRuntimeItems(Transform tagsRoot)
        {
            for (var i = tagsRoot.childCount - 1; i >= 0; i--)
            {
                var child = tagsRoot.GetChild(i);
                if (!child.name.StartsWith(RuntimeItemPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                child.gameObject.SetActive(false);
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(child.gameObject);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        private static void SetActive(Transform root, string name, bool active)
        {
            var target = Find(root, name);
            if (target != null)
            {
                target.gameObject.SetActive(active);
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
                var result = Find(root.GetChild(i), name);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static RectTransform CreateRounded(Transform parent, string name, Color color, float radius)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            var graphic = go.AddComponent<RoundedRectGraphic>();
            graphic.color = color;
            graphic.CornerRadius = radius;
            return rect;
        }

        private static void AddOutline(GameObject target, Color color, float distance)
        {
            var outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(distance, -distance);
            outline.useGraphicAlpha = true;
        }

        private static Text AddText(Transform parent, string name, string body, int fontSize, Color color, TextAnchor alignment, RectOffset padding)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding.left, padding.bottom);
            rect.offsetMax = new Vector2(-padding.right, -padding.top);

            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            text.alignment = alignment;
            text.supportRichText = true;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 8;
            text.resizeTextMaxSize = fontSize;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.text = body;
            return text;
        }
    }
}
