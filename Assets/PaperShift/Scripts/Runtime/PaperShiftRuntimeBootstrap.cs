using PaperShift.Controller;
using PaperShift.Presenter;
using UnityEngine;

namespace PaperShift.Runtime
{
    public static class PaperShiftRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AttachPresenter()
        {
            var controller = Object.FindObjectOfType<PaperShiftSceneController>();
            if (controller == null)
            {
                return;
            }

            var presenter = controller.GetComponent<PaperShiftGamePresenter>();
            if (presenter == null)
            {
                presenter = controller.gameObject.AddComponent<PaperShiftGamePresenter>();
            }

            presenter.SceneController = controller;

            var binder = controller.GetComponent<PaperShiftPrototypeBinder>();
            if (binder == null)
            {
                binder = controller.gameObject.AddComponent<PaperShiftPrototypeBinder>();
            }

            binder.Presenter = presenter;
            binder.SceneController = controller;
        }
    }
}
