using PaperShift.Model;
using PaperShift.Presenter;
using UnityEngine;
using UnityEngine.UI;

namespace PaperShift.Controller
{
    public sealed class PaperShiftSceneController : MonoBehaviour
    {
        public PaperShiftScreen InitialScreen = PaperShiftScreen.Create;
        public PaperShiftScreenView[] ScreenViews = new PaperShiftScreenView[0];

        private void Awake()
        {
            ShowScreen(InitialScreen);
        }

        public void ShowCreate() => ShowScreen(PaperShiftScreen.Create);
        public void ShowTags() => ShowScreen(PaperShiftScreen.Tags);
        public void ShowResume() => ShowScreen(PaperShiftScreen.Resume);
        public void ShowJobSearch() => ShowScreen(PaperShiftScreen.JobSearch);
        public void ShowInterviewFailure() => ShowScreen(PaperShiftScreen.InterviewFailure);
        public void ShowWork() => ShowScreen(PaperShiftScreen.Work);
        public void ShowBudget() => ShowScreen(PaperShiftScreen.Budget);
        public void ShowNews() => ShowScreen(PaperShiftScreen.News);
        public void ShowRetirement() => ShowScreen(PaperShiftScreen.Retirement);

        public void ShowScreen(PaperShiftScreen screen)
        {
            PaperShiftScreenView activeView = null;

            foreach (var view in ScreenViews)
            {
                if (view == null)
                {
                    continue;
                }

                var isActive = view.Screen == screen;
                view.gameObject.SetActive(isActive);
                if (isActive)
                {
                    activeView = view;
                }
            }

            if (activeView != null)
            {
                ResetScrollRects(activeView.transform);
            }
        }

        private static void ResetScrollRects(Transform root)
        {
            Canvas.ForceUpdateCanvases();

            foreach (var scroll in root.GetComponentsInChildren<ScrollRect>(true))
            {
                scroll.StopMovement();
                scroll.verticalNormalizedPosition = 1f;
                scroll.horizontalNormalizedPosition = 0f;
                if (scroll.content != null)
                {
                    scroll.content.anchoredPosition = Vector2.zero;
                }
            }

            Canvas.ForceUpdateCanvases();
        }
    }
}
