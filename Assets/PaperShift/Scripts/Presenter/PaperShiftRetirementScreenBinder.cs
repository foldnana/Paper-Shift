using PaperShift.Domain;
using PaperShift.Model;
using PaperShift.Runtime;
using UnityEngine.UI;

namespace PaperShift.Presenter
{
    public sealed class PaperShiftRetirementScreenBinder : PaperShiftScreenBinderBase
    {
        public Text CoinText;
        public Text ReasonTitleText;
        public PaperShiftTextBinding[] SettlementTexts = new PaperShiftTextBinding[0];
        public PaperShiftHireScoreRowBinding[] ScoreRows = new PaperShiftHireScoreRowBinding[0];
        public Text ScoreTotalText;
        public Button FinishButton;

        private void Reset()
        {
            Screen = PaperShiftScreen.Retirement;
        }

        public override void BindActions()
        {
            Bind(FinishButton, () =>
            {
                if (SceneController != null)
                {
                    SceneController.ShowInheritance();
                }

                RefreshAll();
            });
        }

        public override void RefreshView()
        {
            if (State == null)
            {
                return;
            }

            var score = HireSettlementScoreCalculator.Calculate(State, Database);
            Set(CoinText, score.TotalPoints.ToString("N0"));
            Set(ReasonTitleText, "恭喜，你入职了");

            var jobTitle = string.IsNullOrEmpty(State.Retirement.FinalJobTitle)
                ? State.CurrentJob.JobTitle
                : State.Retirement.FinalJobTitle;
            if (string.IsNullOrEmpty(jobTitle))
            {
                jobTitle = "新岗位";
            }

            var companyName = string.IsNullOrEmpty(State.CurrentJob.CompanyName) ? "新公司" : State.CurrentJob.CompanyName;
            var salary = State.CurrentJob.Salary > 0 ? State.CurrentJob.Salary : State.Interview.Salary;

            Set(SettlementTexts, "workerName", State.Worker.FullName);
            Set(SettlementTexts, "workerMeta", State.Worker.Gender + " " + State.Worker.Age + "岁 " + State.Worker.EraName);
            Set(SettlementTexts, "jobTitle", jobTitle);
            Set(SettlementTexts, "companyName", companyName);
            Set(SettlementTexts, "job", jobTitle);
            Set(SettlementTexts, "salary", salary.ToString("N0") + "元");
            Set(SettlementTexts, "totalReward", score.TotalPoints.ToString("N0"));
            Set(ScoreTotalText, score.TotalPoints.ToString("N0"));
            RefreshScoreRows(score);
        }

        private void RefreshScoreRows(HireSettlementScore score)
        {
            if (ScoreRows == null || score == null)
            {
                return;
            }

            for (var i = 0; i < ScoreRows.Length; i++)
            {
                var row = ScoreRows[i];
                if (row == null)
                {
                    continue;
                }

                var item = score.Find(row.Id);
                if (row.Root != null)
                {
                    row.Root.SetActive(item != null);
                }

                if (item == null)
                {
                    continue;
                }

                Set(row.ValueText, item.ValueText);
                Set(row.PointsText, item.Points.ToString("N0"));
                Set(row.NormalTierText, "普通");
                Set(row.RareTierText, "★ 稀有!");
                Set(row.SuperRareTierText, "★★超稀有!");

                SetTierVisible(row.NormalTierRoot, item.Tier == HireSettlementScoreTier.Normal);
                SetTierVisible(row.RareTierRoot, item.Tier == HireSettlementScoreTier.Rare);
                SetTierVisible(row.SuperRareTierRoot, item.Tier == HireSettlementScoreTier.SuperRare);
            }
        }

        private static void SetTierVisible(UnityEngine.GameObject root, bool visible)
        {
            if (root != null)
            {
                root.SetActive(visible);
            }
        }
    }
}
