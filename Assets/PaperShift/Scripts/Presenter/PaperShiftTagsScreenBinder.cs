using System.Collections;
using System.Collections.Generic;
using PaperShift.Data;
using PaperShift.Model;
using UnityEngine;
using UnityEngine.UI;

namespace PaperShift.Presenter
{
    public sealed class PaperShiftTagsScreenBinder : PaperShiftScreenBinderBase
    {
        public PaperShiftTagsViewReferences View;

        [HideInInspector] public Text TitleText;
        [HideInInspector] public Text CoinText;
        [HideInInspector] public Transform TagListRoot;
        [HideInInspector] public Button FreeRefreshButton;
        [HideInInspector] public Button SuperRefreshButton;
        [HideInInspector] public GameObject ConfirmPromptRoot;
        [HideInInspector] public Button ConfirmButton;
        [HideInInspector] public Text ConfirmLabel;
        [HideInInspector] public Button StartJobButton;
        [HideInInspector] public Text StartJobLabel;
        [HideInInspector] public int TagChoiceCount = 6;

        private readonly PaperShiftTagSelectionView tagSelectionView = new PaperShiftTagSelectionView();
        private Coroutine rollAnimationRoutine;
        private string lastChoiceSignature;
        private bool forceAnimateOnNextRefresh = true;

        private void Reset()
        {
            Screen = PaperShiftScreen.Tags;
            View = GetComponent<PaperShiftTagsViewReferences>();
        }

        public override void BindActions()
        {
            ResolveView();
            Bind(ActiveFreeRefreshButton(), () =>
            {
                ApplyTagChoiceCount();
                Presenter.RollTagsAndShow();
                forceAnimateOnNextRefresh = true;
                RefreshView();
            });
            Bind(ActiveSuperRefreshButton(), () =>
            {
                ApplyTagChoiceCount();
                Presenter.RollTagsAndShow();
                ShowBanner("标签机会刷新，出现了一组新标签。");
                forceAnimateOnNextRefresh = true;
                RefreshView();
            });
            Bind(ActiveConfirmButton(), TryContinueToResume);
            Bind(ActiveStartJobButton(), TryContinueToResume);
        }

        public override void RefreshView()
        {
            if (State == null || Presenter == null)
            {
                return;
            }

            ResolveView();
            ApplyTagChoiceCount();
            Presenter.EnsureTagChoices();
            Set(ActiveTitleText(), "选择" + State.Worker.FullName + "的标签");
            SetActive(ActiveCoinText() == null ? null : ActiveCoinText().gameObject, false);
            Set(ActiveConfirmLabel(), "确认标签 " + State.Worker.Tags.Count + "/" + Presenter.StartingTagLimit);
            Set(ActiveStartJobLabel(), "开始找工作");
            RefreshActionState();

            var listRoot = ActiveTagListRoot();
            if (listRoot == null)
            {
                return;
            }

            var choiceSignature = tagSelectionView.CurrentSignature(Presenter);
            var shouldAnimate = ActiveAnimateTagRows() && (forceAnimateOnNextRefresh || choiceSignature != lastChoiceSignature);
            lastChoiceSignature = choiceSignature;
            forceAnimateOnNextRefresh = false;

            StopRollAnimation();
            tagSelectionView.TagRowPrefab = ActiveTagRowPrefab();
            tagSelectionView.HideExistingRowsBeforeRefresh = true;
            tagSelectionView.Refresh(listRoot, Presenter, () =>
            {
                RefreshView();
                Host.RefreshScreen(PaperShiftScreen.Create);
                Host.RefreshScreen(PaperShiftScreen.Resume);
            });

            if (shouldAnimate && CanPlayRollAnimation())
            {
                StartRollAnimation();
            }
        }

        public override void OnScreenBecameActive(PaperShiftScreen screen)
        {
            if (screen != PaperShiftScreen.Tags)
            {
                return;
            }

            forceAnimateOnNextRefresh = true;
            RefreshView();
        }

        private void TryContinueToResume()
        {
            if (State.Worker.Tags.Count < Presenter.StartingTagLimit)
            {
                ShowBanner("请选择 " + Presenter.StartingTagLimit + " 个标签");
                RefreshView();
                return;
            }

            Presenter.ContinueToResume();
            RefreshAll();
        }

        private void RefreshActionState()
        {
            var selectedEnoughTags = State.Worker.Tags.Count >= Presenter.StartingTagLimit;
            SetActive(ActiveConfirmPromptRoot(), !selectedEnoughTags);
            SetButtonVisible(ActiveStartJobButton(), selectedEnoughTags);
        }

        private void StartRollAnimation()
        {
            if (!CanPlayRollAnimation())
            {
                return;
            }

            StopRollAnimation();
            rollAnimationRoutine = StartCoroutine(RunRollAnimation());
        }

        private bool CanPlayRollAnimation()
        {
            return Application.isPlaying &&
                isActiveAndEnabled &&
                gameObject.activeInHierarchy &&
                SceneController != null &&
                SceneController.CurrentScreen == PaperShiftScreen.Tags;
        }

        private IEnumerator RunRollAnimation()
        {
            SetRollActionsInteractable(false);
            yield return tagSelectionView.PlayRollAnimation(
                Presenter,
                ActiveSpinPool(),
                () =>
                {
                    RefreshView();
                    Host.RefreshScreen(PaperShiftScreen.Create);
                    Host.RefreshScreen(PaperShiftScreen.Resume);
                },
                ActiveRollTickSeconds(),
                ActiveRowSettleSeconds());

            rollAnimationRoutine = null;
            SetRollActionsInteractable(true);
            RefreshActionState();
        }

        private void StopRollAnimation()
        {
            if (rollAnimationRoutine == null)
            {
                return;
            }

            StopCoroutine(rollAnimationRoutine);
            rollAnimationRoutine = null;
            SetRollActionsInteractable(true);
        }

        private IList<TagDefinition> ActiveSpinPool()
        {
            if (Database != null && Database.Tags != null && Database.Tags.Length > 0)
            {
                return Database.Tags;
            }

            return Presenter.CurrentTagChoices;
        }

        private void SetRollActionsInteractable(bool interactable)
        {
            SetInteractable(ActiveFreeRefreshButton(), interactable);
            SetInteractable(ActiveSuperRefreshButton(), interactable);
            SetInteractable(ActiveConfirmButton(), interactable);
            SetInteractable(ActiveStartJobButton(), interactable);
        }

        private void ResolveView()
        {
            if (View == null)
            {
                View = GetComponent<PaperShiftTagsViewReferences>();
            }
        }

        private void ApplyTagChoiceCount()
        {
            Presenter.StartingTagChoiceCount = 6;
        }

        private PaperShiftTagChoiceItemViewReferences ActiveTagRowPrefab()
        {
            if (View != null && View.TagRowPrefab != null)
            {
                return View.TagRowPrefab;
            }

            return Host == null || Host.TagRowPrefab == null ? null : Host.TagRowPrefab.GetComponent<PaperShiftTagChoiceItemViewReferences>();
        }

        private Text ActiveTitleText()
        {
            return View != null && View.TitleText != null ? View.TitleText : TitleText;
        }

        private Text ActiveCoinText()
        {
            return View != null && View.CoinText != null ? View.CoinText : CoinText;
        }

        private Transform ActiveTagListRoot()
        {
            return View != null && View.TagListRoot != null ? View.TagListRoot : TagListRoot;
        }

        private Button ActiveFreeRefreshButton()
        {
            return View != null && View.FreeRefreshButton != null ? View.FreeRefreshButton : FreeRefreshButton;
        }

        private Button ActiveSuperRefreshButton()
        {
            return View != null && View.SuperRefreshButton != null ? View.SuperRefreshButton : SuperRefreshButton;
        }

        private Button ActiveConfirmButton()
        {
            return View != null && View.ConfirmButton != null ? View.ConfirmButton : ConfirmButton;
        }

        private GameObject ActiveConfirmPromptRoot()
        {
            if (View != null && View.ConfirmPromptRoot != null)
            {
                return View.ConfirmPromptRoot;
            }

            if (ConfirmPromptRoot != null)
            {
                return ConfirmPromptRoot;
            }

            var confirmButton = ActiveConfirmButton();
            return confirmButton == null ? null : confirmButton.gameObject;
        }

        private Text ActiveConfirmLabel()
        {
            return View != null && View.ConfirmLabel != null ? View.ConfirmLabel : ConfirmLabel;
        }

        private bool ActiveAnimateTagRows()
        {
            return View == null || View.AnimateTagRows;
        }

        private float ActiveRollTickSeconds()
        {
            return View == null ? 0.055f : View.RollTickSeconds;
        }

        private float ActiveRowSettleSeconds()
        {
            return View == null ? 0.22f : View.RowSettleSeconds;
        }

        private Button ActiveStartJobButton()
        {
            if (View != null && View.StartJobButton != null)
            {
                return View.StartJobButton;
            }

            return StartJobButton;
        }

        private Text ActiveStartJobLabel()
        {
            if (View != null && View.StartJobLabel != null)
            {
                return View.StartJobLabel;
            }

            return StartJobLabel;
        }

        private static void SetActive(GameObject target, bool isActive)
        {
            if (target != null && target.activeSelf != isActive)
            {
                target.SetActive(isActive);
            }
        }

        private static void SetButtonVisible(Button button, bool isVisible)
        {
            if (button != null)
            {
                SetActive(button.gameObject, isVisible);
                button.interactable = isVisible;
            }
        }

        private static void SetInteractable(Button button, bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }
    }
}
