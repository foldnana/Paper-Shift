using PaperShift.Model;
using PaperShift.Runtime;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PaperShift.Presenter
{
    public sealed class PaperShiftNewsScreenBinder : PaperShiftScreenBinderBase
    {
        public GameObject ModalRoot;
        public Text TitleText;
        public Text BodyText;
        public PaperShiftButtonBinding[] OptionButtons = new PaperShiftButtonBinding[0];

        private void Reset()
        {
            Screen = PaperShiftScreen.News;
            ModalRoot = gameObject;
        }

        public override void RefreshView()
        {
            var pending = Presenter == null ? null : Presenter.PendingEvent;
            var hasResult = Presenter != null && Presenter.HasPendingEventResult;
            var shouldShow = pending != null || hasResult || (SceneController != null && SceneController.NewsPopupRequested);
            SetActive(ModalRoot == null ? gameObject : ModalRoot, shouldShow);
            if (!shouldShow)
            {
                return;
            }

            if (pending != null)
            {
                ShowPendingEvent(pending);
                return;
            }

            if (hasResult)
            {
                ShowEventResult();
                return;
            }

            ShowNotice();
        }

        private void ShowPendingEvent(TriggeredEvent pending)
        {
            Set(TitleText, pending.Event == null ? "事件" : pending.Event.DisplayName);
            Set(BodyText, pending.Event == null ? string.Empty : pending.Event.Body);
            if (pending.Options == null || pending.Options.Length == 0)
            {
                ShowContinueOnly("继续", () =>
                {
                    Presenter.ContinuePendingEvent();
                    RefreshAll();
                });
                return;
            }

            for (var i = 0; i < OptionButtons.Length; i++)
            {
                var button = OptionButtons[i];
                if (button == null || button.Button == null)
                {
                    continue;
                }

                if (pending.Options == null || i >= pending.Options.Length)
                {
                    button.SetVisible(false);
                    button.SetLabel(string.Empty);
                    continue;
                }

                var optionIndex = i;
                button.SetVisible(true);
                button.SetLabel(pending.Options[i].Label);
                Bind(button.Button, () =>
                {
                    Presenter.ChoosePendingEventOption(optionIndex);
                    RefreshAll();
                });
            }
        }

        private void ShowEventResult()
        {
            Set(TitleText, Presenter.PendingEventResultTitle);
            Set(BodyText, Presenter.PendingEventResultBody);
            ShowContinueOnly("继续", () =>
            {
                Presenter.ContinueAfterEventResult();
                RefreshAll();
            });
        }

        private void ShowNotice()
        {
            Set(TitleText, "通知");
            Set(BodyText, LastLogOr("暂时没有新的重要事件。"));
            ShowContinueOnly("继续", () =>
            {
                if (SceneController != null)
                {
                    SceneController.HideNews();
                }

                RefreshAll();
            });
        }

        private void ShowContinueOnly(string label, UnityAction action)
        {
            for (var i = 0; i < OptionButtons.Length; i++)
            {
                var button = OptionButtons[i];
                if (button == null || button.Button == null)
                {
                    continue;
                }

                button.SetVisible(i == 0);
                button.SetLabel(i == 0 ? label : string.Empty);
                if (i == 0)
                {
                    Bind(button.Button, action);
                }
            }
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }
    }
}
