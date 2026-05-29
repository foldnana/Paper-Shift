using PaperShift.Model;
using UnityEngine.UI;

namespace PaperShift.Presenter
{
    public sealed class PaperShiftNewsScreenBinder : PaperShiftScreenBinderBase
    {
        public Text TitleText;
        public Text BodyText;
        public PaperShiftButtonBinding[] OptionButtons = new PaperShiftButtonBinding[0];

        private void Reset()
        {
            Screen = PaperShiftScreen.News;
        }

        public override void RefreshView()
        {
            var pending = Presenter == null ? null : Presenter.PendingEvent;
            Set(TitleText, pending == null ? "通知" : pending.Event.DisplayName);
            Set(BodyText, pending == null ? LastLogOr("暂时没有新的重要事件。") : pending.Event.Body);

            for (var i = 0; i < OptionButtons.Length; i++)
            {
                var button = OptionButtons[i];
                if (button == null || button.Button == null)
                {
                    continue;
                }

                if (pending == null || i >= pending.Options.Length)
                {
                    button.SetVisible(i == 0);
                    button.SetLabel(i == 0 ? "继续" : string.Empty);
                    Bind(button.Button, () =>
                    {
                        SceneController.ShowWork();
                        RefreshAll();
                    });
                    continue;
                }

                var optionIndex = i;
                button.SetVisible(true);
                button.SetLabel(pending.Options[i].Label);
                Bind(button.Button, () =>
                {
                    Presenter.ChoosePendingEventOption(optionIndex);
                    ShowBanner("事件已结算。");
                    RefreshAll();
                });
            }
        }
    }
}
