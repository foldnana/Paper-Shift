using System.Collections;
using System.Collections.Generic;
using PaperShift.Controller;
using PaperShift.Model;
using UnityEngine;
using UnityEngine.UI;

namespace PaperShift.Presenter
{
    public sealed class PaperShiftPrototypeBinder : MonoBehaviour
    {
        public PaperShiftGamePresenter Presenter;
        public PaperShiftSceneController SceneController;

        [Header("Prefab references shared by page binders")]
        public GameObject TagRowPrefab;
        public GameObject ResumeTagPrefab;
        public GameObject StatusTagPrefab;
        public GameObject EmptySlotPrefab;

        [Header("Scene page binders")]
        public PaperShiftScreenBinderBase[] ScreenBinders = new PaperShiftScreenBinderBase[0];
        public PaperShiftGameplayScreenBinder GameplayBinder;
        public PaperShiftGameplayViewReferences GameplayView;

        [Header("Runtime banner")]
        public RectTransform BannerRoot;
        public Text BannerText;

        private readonly Dictionary<PaperShiftScreen, List<PaperShiftScreenBinderBase>> bindersByScreen =
            new Dictionary<PaperShiftScreen, List<PaperShiftScreenBinderBase>>();

        private PaperShiftScreen activeScreen;
        private float bannerHideAt;
        private Coroutine reapplyJobRoutine;
        private bool suppressNextStateTransition;

        private void Start()
        {
            ResolveSceneServices();
            CollectSceneBinders();
            InitializeBinders();
            HideBanner();
            RefreshAll();
        }

        private void LateUpdate()
        {
            if (SceneController == null)
            {
                return;
            }

            var current = SceneController.CurrentScreen;
            if (current != activeScreen)
            {
                activeScreen = current;
                RefreshAll();
                if (suppressNextStateTransition)
                {
                    suppressNextStateTransition = false;
                }
                else
                {
                    NotifyScreenBecameActive(current);
                }
            }

            if (BannerRoot != null && BannerRoot.gameObject.activeSelf && Time.unscaledTime >= bannerHideAt)
            {
                BannerRoot.gameObject.SetActive(false);
            }
        }

        public void RefreshAll()
        {
            for (var i = 0; i < ScreenBinders.Length; i++)
            {
                if (ScreenBinders[i] != null)
                {
                    ScreenBinders[i].RefreshView();
                }
            }
        }

        public void RefreshScreen(PaperShiftScreen screen)
        {
            if (!bindersByScreen.TryGetValue(screen, out var binders))
            {
                return;
            }

            for (var i = 0; i < binders.Count; i++)
            {
                if (binders[i] != null)
                {
                    binders[i].RefreshView();
                }
            }
        }

        public void ShowBanner(string text)
        {
            if (BannerRoot == null || BannerText == null || string.IsNullOrEmpty(text))
            {
                return;
            }

            BannerText.text = text;
            BannerRoot.gameObject.SetActive(true);
            BannerRoot.SetAsLastSibling();
            bannerHideAt = Time.unscaledTime + 2.4f;
        }

        public void BeginReapplyJobWithTransition()
        {
            if (reapplyJobRoutine != null)
            {
                return;
            }

            reapplyJobRoutine = StartCoroutine(ReapplyJobAfterTransition());
        }

        private IEnumerator ReapplyJobAfterTransition()
        {
            if (GameplayBinder != null && GameplayBinder.ShowPreauthoredTransition())
            {
                GameplayBinder.SetActionsInteractable(false);
                yield return new WaitForSecondsRealtime(PaperShiftJobCardTransition.TotalSeconds);
                GameplayBinder.SetActionsInteractable(true);
            }

            var beforeReapplyScreen = SceneController == null ? PaperShiftScreen.JobSearch : SceneController.CurrentScreen;
            Presenter.FindInterviewAndShow();
            suppressNextStateTransition = SceneController != null && beforeReapplyScreen != SceneController.CurrentScreen;
            RefreshAll();
            reapplyJobRoutine = null;
        }

        private void ResolveSceneServices()
        {
            if (Presenter == null)
            {
                Presenter = GetComponent<PaperShiftGamePresenter>();
            }

            if (SceneController == null)
            {
                SceneController = GetComponent<PaperShiftSceneController>();
            }

            if (GameplayBinder == null && GameplayView != null)
            {
                GameplayBinder = GameplayView.GetComponent<PaperShiftGameplayScreenBinder>();
            }
        }

        private void CollectSceneBinders()
        {
            if (ScreenBinders == null || ScreenBinders.Length == 0)
            {
                var collected = new List<PaperShiftScreenBinderBase>();
                if (SceneController != null && SceneController.ScreenViews != null)
                {
                    for (var i = 0; i < SceneController.ScreenViews.Length; i++)
                    {
                        var view = SceneController.ScreenViews[i];
                        if (view == null)
                        {
                            continue;
                        }

                        view.GetComponents<PaperShiftScreenBinderBase>(collected);
                    }
                }

                ScreenBinders = collected.ToArray();
            }

            if (GameplayBinder == null)
            {
                for (var i = 0; i < ScreenBinders.Length; i++)
                {
                    if (ScreenBinders[i] is PaperShiftGameplayScreenBinder gameplayBinder)
                    {
                        GameplayBinder = gameplayBinder;
                        break;
                    }
                }
            }
        }

        private void InitializeBinders()
        {
            bindersByScreen.Clear();
            for (var i = 0; i < ScreenBinders.Length; i++)
            {
                var binder = ScreenBinders[i];
                if (binder == null)
                {
                    continue;
                }

                binder.Initialize(this, Presenter, SceneController);
                if (!bindersByScreen.TryGetValue(binder.Screen, out var binders))
                {
                    binders = new List<PaperShiftScreenBinderBase>();
                    bindersByScreen.Add(binder.Screen, binders);
                }

                binders.Add(binder);
            }
        }

        private void NotifyScreenBecameActive(PaperShiftScreen screen)
        {
            var notifiedGameplay = false;
            if (GameplayBinder != null &&
                (screen == PaperShiftScreen.JobSearch || screen == PaperShiftScreen.Work || screen == PaperShiftScreen.InterviewFailure))
            {
                GameplayBinder.OnScreenBecameActive(screen);
                notifiedGameplay = true;
            }

            if (!bindersByScreen.TryGetValue(screen, out var binders))
            {
                return;
            }

            for (var i = 0; i < binders.Count; i++)
            {
                if (binders[i] != null && (!notifiedGameplay || binders[i] != GameplayBinder))
                {
                    binders[i].OnScreenBecameActive(screen);
                }
            }
        }

        private void HideBanner()
        {
            if (BannerRoot != null)
            {
                BannerRoot.gameObject.SetActive(false);
            }
        }
    }
}
