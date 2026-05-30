using PaperShift.Controller;
using PaperShift.Data;
using PaperShift.Domain;
using PaperShift.Model;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static PaperShift.Presenter.PaperShiftUiFormatter;

namespace PaperShift.Presenter
{
    public abstract class PaperShiftScreenBinderBase : MonoBehaviour
    {
        public PaperShiftScreen Screen;

        protected PaperShiftPrototypeBinder Host { get; private set; }
        protected PaperShiftGamePresenter Presenter { get; private set; }
        protected PaperShiftSceneController SceneController { get; private set; }

        protected PaperShiftRunState State
        {
            get { return Presenter == null ? null : Presenter.State; }
        }

        protected PaperShiftDatabase Database
        {
            get { return Presenter == null ? null : Presenter.ActiveDatabase; }
        }

        public void Initialize(PaperShiftPrototypeBinder host, PaperShiftGamePresenter presenter, PaperShiftSceneController sceneController)
        {
            Host = host;
            Presenter = presenter;
            SceneController = sceneController;
            BindActions();
        }

        public virtual void BindActions()
        {
        }

        public abstract void RefreshView();

        public virtual void OnScreenBecameActive(PaperShiftScreen screen)
        {
        }

        protected void Bind(Button button, UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        protected void Set(Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        protected void Set(PaperShiftTextBinding[] bindings, string id, string value)
        {
            var binding = FindBinding(bindings, id);
            if (binding != null)
            {
                binding.Set(value);
            }
        }

        protected PaperShiftTextBinding FindBinding(PaperShiftTextBinding[] bindings, string id)
        {
            if (bindings == null)
            {
                return null;
            }

            for (var i = 0; i < bindings.Length; i++)
            {
                if (bindings[i] != null && bindings[i].Id == id)
                {
                    return bindings[i];
                }
            }

            return null;
        }

        protected string WorkerTagSummary(WorkerProfile worker)
        {
            if (worker == null || worker.Tags == null || worker.Tags.Count == 0)
            {
                return "未选择标签";
            }

            var count = Mathf.Min(3, worker.Tags.Count);
            var result = string.Empty;
            for (var i = 0; i < count; i++)
            {
                if (i > 0)
                {
                    result += "、";
                }

                result += worker.Tags[i].DisplayName;
            }

            return result;
        }

        protected string ExpectedSalaryLabel()
        {
            var baseSalary = 6000 + State.Worker.GetStat("logic") * 55 + State.Worker.GetStat("social") * 35 + State.Worker.GetStat("literacy") * 50;
            baseSalary = Mathf.RoundToInt(baseSalary / 500f) * 500;
            return "期望 " + Mathf.Max(4000, baseSalary - 2500) + "-" + Mathf.Max(6000, baseSalary + 3500);
        }

        protected string ResumeExperienceLabel()
        {
            var logic = State.Worker.GetStat("logic");
            var social = State.Worker.GetStat("social");
            var body = State.Worker.GetStat("body");
            if (logic >= 70)
            {
                return "做过自动化和数据相关兼职";
            }

            if (social >= 70)
            {
                return "做过销售和客户沟通兼职";
            }

            if (body >= 70)
            {
                return "做过体力和现场执行兼职";
            }

            return "做过基础兼职";
        }

        protected string LastLogOr(string fallback)
        {
            return State == null || State.Logs.Count == 0 ? fallback : State.Logs[State.Logs.Count - 1].Text;
        }

        protected void ShowBanner(string text)
        {
            if (Host != null)
            {
                Host.ShowBanner(text);
            }
        }

        protected void RefreshAll()
        {
            if (Host != null)
            {
                Host.RefreshAll();
            }
        }
    }
}
