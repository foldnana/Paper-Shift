using PaperShift.Domain;
using PaperShift.Model;
using UnityEngine.UI;

namespace PaperShift.Presenter
{
    public sealed class PaperShiftRetirementScreenBinder : PaperShiftScreenBinderBase
    {
        public Text CoinText;
        public Text ReasonTitleText;
        public PaperShiftTextBinding[] SettlementTexts = new PaperShiftTextBinding[0];
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

            Set(CoinText, State.Retirement.FinalSavings.ToString("N0"));
            Set(ReasonTitleText, "恭喜，你入职了");

            var jobTitle = string.IsNullOrEmpty(State.Retirement.FinalJobTitle)
                ? State.CurrentJob.JobTitle
                : State.Retirement.FinalJobTitle;
            if (string.IsNullOrEmpty(jobTitle))
            {
                jobTitle = "新岗位";
            }

            var companyName = string.IsNullOrEmpty(State.CurrentJob.CompanyName) ? "新公司" : State.CurrentJob.CompanyName;
            var salary = State.CurrentJob.Salary > 0 ? State.CurrentJob.Salary : State.Retirement.FinalSavings - State.Worker.Money;
            if (salary < 0)
            {
                salary = 0;
            }

            Set(SettlementTexts, "workerName", State.Worker.FullName);
            Set(SettlementTexts, "workerMeta", State.Worker.Gender + " " + State.Worker.Age + "岁 " + State.Worker.EraName);
            Set(SettlementTexts, "jobTitle", jobTitle);
            Set(SettlementTexts, "companyName", companyName);
            Set(SettlementTexts, "appearance", PaperShiftWorkerAttributes.DisplayValue(State.Worker, PaperShiftWorkerAttributes.Appearance));
            Set(SettlementTexts, "height", PaperShiftWorkerAttributes.DisplayValue(State.Worker, PaperShiftWorkerAttributes.Height));
            Set(SettlementTexts, "education", PaperShiftWorkerAttributes.DisplayValue(State.Worker, PaperShiftWorkerAttributes.Education));
            Set(SettlementTexts, "family", PaperShiftWorkerAttributes.DisplayValue(State.Worker, PaperShiftWorkerAttributes.Family));
            Set(SettlementTexts, "job", jobTitle);
            Set(SettlementTexts, "salary", salary.ToString("N0") + "元");
            Set(SettlementTexts, "totalReward", salary.ToString("N0"));
        }
    }
}
