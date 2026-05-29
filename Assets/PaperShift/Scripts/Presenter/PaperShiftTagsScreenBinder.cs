using PaperShift.Model;
using UnityEngine;
using UnityEngine.UI;

namespace PaperShift.Presenter
{
    public sealed class PaperShiftTagsScreenBinder : PaperShiftScreenBinderBase
    {
        public Text TitleText;
        public Text CoinText;
        public Transform TagListRoot;
        public Button FreeRefreshButton;
        public Button SuperRefreshButton;
        public Button ConfirmButton;
        public Text ConfirmLabel;

        private readonly PaperShiftTagSelectionView tagSelectionView = new PaperShiftTagSelectionView();

        private void Reset()
        {
            Screen = PaperShiftScreen.Tags;
        }

        public override void BindActions()
        {
            Bind(FreeRefreshButton, () =>
            {
                Presenter.RollTagsAndShow();
                RefreshView();
            });
            Bind(SuperRefreshButton, () =>
            {
                Presenter.RollTagsAndShow();
                ShowBanner("时代机会刷新，出现了一组新标签。");
                RefreshView();
            });
            Bind(ConfirmButton, () =>
            {
                Presenter.ContinueToResume();
                RefreshAll();
            });
        }

        public override void RefreshView()
        {
            if (State == null || Presenter == null)
            {
                return;
            }

            Presenter.EnsureTagChoices();
            Set(TitleText, "选择" + State.Worker.FullName + "的标签");
            Set(CoinText, State.Worker.Money.ToString("N0"));
            Set(ConfirmLabel, "确认标签 " + State.Worker.Tags.Count + "/" + Presenter.StartingTagLimit);

            if (TagListRoot != null)
            {
                tagSelectionView.TagRowPrefab = Host == null ? null : Host.TagRowPrefab;
                tagSelectionView.Refresh(TagListRoot, Presenter, () =>
                {
                    RefreshView();
                    Host.RefreshScreen(PaperShiftScreen.Create);
                    Host.RefreshScreen(PaperShiftScreen.Resume);
                });
            }
        }
    }
}
