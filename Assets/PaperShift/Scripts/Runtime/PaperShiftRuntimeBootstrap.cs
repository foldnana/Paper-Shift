using PaperShift.Controller;
using PaperShift.Presenter;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PaperShift.Runtime
{
    public static class PaperShiftRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AttachPresenter()
        {
            var controller = Object.FindObjectOfType<PaperShiftSceneController>();
            if (controller == null)
            {
                return;
            }

            var presenter = controller.GetComponent<PaperShiftGamePresenter>();
            if (presenter == null)
            {
                presenter = controller.gameObject.AddComponent<PaperShiftGamePresenter>();
            }

            presenter.SceneController = controller;

            var binder = controller.GetComponent<PaperShiftPrototypeBinder>();
            if (binder == null)
            {
                binder = controller.gameObject.AddComponent<PaperShiftPrototypeBinder>();
            }

            binder.Presenter = presenter;
            binder.SceneController = controller;
            if (binder.GameplayView == null)
            {
                binder.GameplayView = Object.FindObjectOfType<PaperShiftGameplayViewReferences>(true);
            }

            ResolveBinderPrefabs(binder);
        }

        private static void ResolveBinderPrefabs(PaperShiftPrototypeBinder binder)
        {
            if (binder == null)
            {
                return;
            }

#if UNITY_EDITOR
            binder.TagRowPrefab = binder.TagRowPrefab == null
                ? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PaperShift/Prefab/Tag Row Item--标签选择预制体.prefab")
                : binder.TagRowPrefab;
            binder.ResumeTagPrefab = binder.ResumeTagPrefab == null
                ? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PaperShift/Prefab/标签.prefab")
                : binder.ResumeTagPrefab;
            binder.StatusTagPrefab = binder.StatusTagPrefab == null
                ? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PaperShift/Prefab/标签.prefab")
                : binder.StatusTagPrefab;
            binder.EmptySlotPrefab = binder.EmptySlotPrefab == null
                ? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PaperShift/Prefab/Empty Slot.prefab")
                : binder.EmptySlotPrefab;
#endif
        }
    }
}
