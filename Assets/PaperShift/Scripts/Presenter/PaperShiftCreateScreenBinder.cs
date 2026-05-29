using PaperShift.Model;
using UnityEngine;
using UnityEngine.UI;
using static PaperShift.Presenter.PaperShiftUiFormatter;

namespace PaperShift.Presenter
{
    public sealed class PaperShiftCreateScreenBinder : PaperShiftScreenBinderBase
    {
        public PaperShiftTextBinding[] Texts = new PaperShiftTextBinding[0];
        public PaperShiftEraTileBinding[] EraTiles = new PaperShiftEraTileBinding[0];
        public Button RandomButton;
        public Button RandomNameButton;
        public Button PlayButton;
        public Button CustomButton;
        public Button NextButton;

        private void Reset()
        {
            Screen = PaperShiftScreen.Create;
        }

        public override void BindActions()
        {
            Bind(RandomButton, Randomize);
            Bind(RandomNameButton, Randomize);
            Bind(PlayButton, BeginWorkerFlow);
            Bind(CustomButton, BeginWorkerFlow);
            Bind(NextButton, BeginWorkerFlow);
        }

        public override void RefreshView()
        {
            if (State == null || State.Worker == null)
            {
                return;
            }

            var worker = State.Worker;
            Set(Texts, "gender", worker.Gender + "  锁定");
            Set(Texts, "era", worker.EraName);
            Set(Texts, "age", worker.Age + " 岁");
            Set(Texts, "body", Score(worker.GetStat("body")));
            Set(Texts, "literacy", Rank(worker.GetStat("literacy")));
            Set(Texts, "logic", worker.GetStat("logic").ToString());
            Set(Texts, "social", worker.GetStat("social").ToString());
            Set(Texts, "education", EducationLabel(worker));
            Set(Texts, "family", FamilyLabel(worker.GetStat("family")));
            Set(Texts, "advantage", "★" + BestStatLabel(worker, Database) + " / " + WorkerTagSummary(worker));
            Set(Texts, "asset", "★启动资金 " + worker.Money);
            Set(Texts, "lastName", worker.LastName);
            Set(Texts, "firstName", worker.FirstName);
            Set(Texts, "coin", worker.Money.ToString("N0"));

            for (var i = 0; i < EraTiles.Length; i++)
            {
                var tile = EraTiles[i];
                if (tile == null || string.IsNullOrEmpty(tile.EraId))
                {
                    continue;
                }

                var selected = worker.EraId == tile.EraId;
                if (tile.Background != null)
                {
                    tile.Background.color = selected ? PaperShiftTheme.BlueLight : PaperShiftTheme.White;
                }

                if (tile.Label != null)
                {
                    var era = Database == null ? null : Database.FindEra(tile.EraId);
                    tile.Label.text = era == null ? tile.Label.text : FormatEraLabel(era.DisplayName);
                    tile.Label.color = selected ? Color.white : PaperShiftTheme.Hex("#3a4350");
                }

                var eraId = tile.EraId;
                Bind(tile.Button, () =>
                {
                    Presenter.SetEraAndRandomize(eraId);
                    RefreshAll();
                });
            }
        }

        private void Randomize()
        {
            Presenter.RandomizeWorker();
            SceneController.ShowCreate();
            RefreshAll();
        }

        private void BeginWorkerFlow()
        {
            Presenter.RollTagsAndShow();
            RefreshAll();
        }

        private static string FormatEraLabel(string value)
        {
            return value
                .Replace("时代", "\n时代")
                .Replace("城市", "\n城市")
                .Replace("工业", "\n工业")
                .Replace("农耕", "\n农耕")
                .Replace("智能", "\n智能")
                .Replace("星际", "\n星际");
        }
    }
}
