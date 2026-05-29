using PaperShift.Model;
using UnityEngine;
using UnityEngine.UI;

namespace PaperShift.Presenter
{
    public sealed class PaperShiftBudgetScreenBinder : PaperShiftScreenBinderBase
    {
        public Text CoinText;
        public PaperShiftTextBinding[] SettlementTexts = new PaperShiftTextBinding[0];
        public PaperShiftBudgetRowBinding[] BudgetRows = new PaperShiftBudgetRowBinding[0];
        public Text[] ImpactTexts = new Text[0];
        public Button SaveButton;

        private void Reset()
        {
            Screen = PaperShiftScreen.Budget;
        }

        public override void BindActions()
        {
            Bind(SaveButton, () =>
            {
                Presenter.SaveBudgetAndReturnToWork();
                ShowBanner(State.Banners.Count == 0 ? "预算已保存。" : State.Banners[State.Banners.Count - 1]);
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
            Set(SettlementTexts, "salary", State.CurrentJob.Salary.ToString());
            Set(SettlementTexts, "rent", EstimateRent().ToString());
            Set(SettlementTexts, "distributable", Mathf.Max(0, State.CurrentJob.Salary - EstimateRent()).ToString());
            RefreshBudgetRows();
            SetImpact(0, "恋爱事件\n<size=21><color=#ff4f8f>+" + Mathf.RoundToInt(State.Budget.Romance * 0.8f) + "%</color></size>");
            SetImpact(1, "结婚事件\n<size=21><color=#ff4f8f>+" + Mathf.RoundToInt(State.Budget.Romance * 0.45f) + "%</color></size>");
            SetImpact(2, "生子事件\n<size=21><color=#ff8a00>+" + Mathf.RoundToInt(State.Budget.Romance * 0.3f) + "%</color></size>");
            SetImpact(3, "后代成长\n<size=21><color=#249ee8>+" + Mathf.RoundToInt(State.Budget.Education * 0.75f) + "%</color></size>");
        }

        private void RefreshBudgetRows()
        {
            for (var i = 0; i < BudgetRows.Length; i++)
            {
                var row = BudgetRows[i];
                if (row == null || string.IsNullOrEmpty(row.BudgetId))
                {
                    continue;
                }

                var value = State.Budget.GetCategory(row.BudgetId);
                Set(row.ValueText, value + "%");
                if (row.Slider == null)
                {
                    continue;
                }

                row.Slider.onValueChanged.RemoveAllListeners();
                row.Slider.SetValueWithoutNotify(value);
                var budgetId = row.BudgetId;
                row.Slider.onValueChanged.AddListener(next =>
                {
                    Presenter.SetBudgetCategory(budgetId, Mathf.RoundToInt(next));
                    RefreshView();
                });
            }
        }

        private void SetImpact(int index, string value)
        {
            if (ImpactTexts != null && index >= 0 && index < ImpactTexts.Length && ImpactTexts[index] != null)
            {
                ImpactTexts[index].text = value;
            }
        }

        private int EstimateRent()
        {
            return Mathf.Max(1200, State.CurrentJob.Salary * 25 / 100);
        }
    }
}
