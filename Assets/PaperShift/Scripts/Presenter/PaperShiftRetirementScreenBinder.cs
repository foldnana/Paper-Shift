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
                Presenter.StartNextGeneration(0);
                RefreshAll();
            });
        }

        public override void RefreshView()
        {
            if (State == null)
            {
                return;
            }

            Set(CoinText, State.Worker.Money.ToString("N0"));
            Set(ReasonTitleText, "退场原因：" + State.Retirement.ReasonText);
            Set(SettlementTexts, "workYears", State.Retirement.WorkYears + " 年");
            Set(SettlementTexts, "finalJob", string.IsNullOrEmpty(State.Retirement.FinalJobTitle) ? "暂无" : State.Retirement.FinalJobTitle);
            Set(SettlementTexts, "savings", State.Retirement.FinalSavings.ToString());
            Set(SettlementTexts, "mental", State.Worker.Stress >= 70 ? "快撑不住" : "还能聊天");
        }
    }
}
