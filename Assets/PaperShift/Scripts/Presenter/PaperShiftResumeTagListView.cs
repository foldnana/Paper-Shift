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
    internal sealed class PaperShiftResumeTagListView
    {
        private const string TagPrefabPath = "Assets/PaperShift/Prefab/标签.prefab";
        private const string RuntimeTagPrefix = "Runtime Resume Tag - ";

        private readonly List<GameObject> runtimeTags = new List<GameObject>();
        private readonly List<GameObject> originalTags = new List<GameObject>();
        private bool originalTagsCached;
        private Transform cachedRoot;

        public GameObject TagPrefab;

        public void Refresh(Transform listRoot, PaperShiftGamePresenter presenter, Action onChanged, Action onLimitReached)
        {
            if (listRoot == null || presenter == null || presenter.State == null || presenter.State.Worker == null)
            {
                return;
            }

            ResolvePrefab();
            if (TagPrefab == null)
            {
                RefreshExistingTags(listRoot, presenter, onChanged, onLimitReached);
                return;
            }

            ResetCacheIfRootChanged(listRoot);
            CacheOriginalTags(listRoot);
            HideOriginalTags();
            ClearRuntimeTags();

            for (var i = 0; i < presenter.State.Worker.Tags.Count; i++)
            {
                var tag = presenter.State.Worker.Tags[i];
                var item = UnityEngine.Object.Instantiate(TagPrefab, listRoot, false);
                item.name = RuntimeTagPrefix + tag.TagId;
                item.SetActive(true);
                runtimeTags.Add(item);
                ConfigureTag(item.transform, presenter, tag, onChanged, onLimitReached);
            }
        }

        private void ResolvePrefab()
        {
            if (TagPrefab != null)
            {
                return;
            }

#if UNITY_EDITOR
            TagPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TagPrefabPath);
#endif
        }

        private void ConfigureTag(Transform root, PaperShiftGamePresenter presenter, TagInstance tag, Action onChanged, Action onLimitReached)
        {
            var hidden = presenter.State.Resume.HiddenTagIds != null && presenter.State.Resume.HiddenTagIds.Contains(tag.TagId);
            SetTagName(root, tag.DisplayName);
            SetRarityTicket(root, tag.RarityId);
            SetTransparency(root, hidden);
            BindButton(root, () =>
            {
                if (!presenter.ToggleResumeHiddenTag(tag.TagId))
                {
                    onLimitReached?.Invoke();
                }

                onChanged?.Invoke();
            });
        }

        private void SetRarityTicket(Transform root, string rarityId)
        {
            var rare = IsRarity(rarityId, "rare");
            var superRare = IsRarity(rarityId, "super_rare");
            var normal = IsRarity(rarityId, "normal") || (!rare && !superRare);
            SetActive(root, "Ticket 普通", normal);
            SetActive(root, "Ticket 稀有", rare);
            SetActive(root, "Ticket 超稀有", superRare);
        }

        private void SetTagName(Transform root, string displayName)
        {
            var texts = root.GetComponentsInChildren<Text>(true);
            for (var i = 0; i < texts.Length; i++)
            {
                if (texts[i].transform.name == "Label")
                {
                    texts[i].text = displayName;
                }
            }
        }

        private void SetTransparency(Transform root, bool hidden)
        {
            var group = root.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = root.gameObject.AddComponent<CanvasGroup>();
            }

            group.alpha = hidden ? 0.42f : 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }

        private void BindButton(Transform root, UnityEngine.Events.UnityAction onClick)
        {
            var button = root.GetComponent<Button>();
            if (button == null)
            {
                button = root.gameObject.AddComponent<Button>();
                button.targetGraphic = root.GetComponent<Graphic>();
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(onClick);
        }

        private void RefreshExistingTags(Transform listRoot, PaperShiftGamePresenter presenter, Action onChanged, Action onLimitReached)
        {
            for (var i = 0; i < listRoot.childCount; i++)
            {
                var child = listRoot.GetChild(i);
                if (i >= presenter.State.Worker.Tags.Count)
                {
                    child.gameObject.SetActive(false);
                    continue;
                }

                child.gameObject.SetActive(true);
                ConfigureTag(child, presenter, presenter.State.Worker.Tags[i], onChanged, onLimitReached);
            }
        }

        private void CacheOriginalTags(Transform listRoot)
        {
            if (originalTagsCached)
            {
                return;
            }

            originalTagsCached = true;
            for (var i = 0; i < listRoot.childCount; i++)
            {
                var child = listRoot.GetChild(i);
                if (!child.name.StartsWith(RuntimeTagPrefix, StringComparison.Ordinal))
                {
                    originalTags.Add(child.gameObject);
                }
            }
        }

        private void ResetCacheIfRootChanged(Transform listRoot)
        {
            if (cachedRoot == listRoot)
            {
                return;
            }

            cachedRoot = listRoot;
            originalTagsCached = false;
            originalTags.Clear();
            ClearRuntimeTags();
        }

        private void HideOriginalTags()
        {
            for (var i = 0; i < originalTags.Count; i++)
            {
                if (originalTags[i] != null)
                {
                    originalTags[i].SetActive(false);
                }
            }
        }

        private void ClearRuntimeTags()
        {
            for (var i = runtimeTags.Count - 1; i >= 0; i--)
            {
                var item = runtimeTags[i];
                if (item == null)
                {
                    continue;
                }

                item.SetActive(false);
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(item);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(item);
                }
            }

            runtimeTags.Clear();
        }

        private static bool IsRarity(string rarityId, string expected)
        {
            return string.Equals(rarityId, expected, StringComparison.OrdinalIgnoreCase);
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
