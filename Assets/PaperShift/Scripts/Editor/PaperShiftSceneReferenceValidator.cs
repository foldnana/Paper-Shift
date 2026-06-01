using System.Collections.Generic;
using PaperShift.Controller;
using PaperShift.Model;
using PaperShift.Presenter;
using UnityEditor;
using UnityEngine;

namespace PaperShift.Editor
{
    public static class PaperShiftSceneReferenceValidator
    {
        [MenuItem("Paper Shift/Validate Scene View References")]
        public static void ValidateCurrentSceneReferences()
        {
            var issues = new List<string>();
            var controller = Object.FindObjectOfType<PaperShiftSceneController>(true);
            if (controller == null)
            {
                Debug.LogError("Paper Shift scene validation failed: no PaperShiftSceneController found.");
                return;
            }

            Require(controller.ScreenViews != null && controller.ScreenViews.Length > 0, "SceneController.ScreenViews is empty.", issues);

            var presenter = controller.GetComponent<PaperShiftGamePresenter>();
            var host = controller.GetComponent<PaperShiftPrototypeBinder>();
            Require(presenter != null, "UI Controller is missing PaperShiftGamePresenter.", issues);
            Require(host != null, "UI Controller is missing PaperShiftPrototypeBinder.", issues);

            if (host != null)
            {
                Require(host.SceneController != null, "PrototypeBinder.SceneController is missing.", issues);
                Require(host.Presenter != null, "PrototypeBinder.Presenter is missing.", issues);
                Require(host.ScreenBinders != null && host.ScreenBinders.Length > 0, "PrototypeBinder.ScreenBinders is empty.", issues);
                Require(host.TagRowPrefab != null, "PrototypeBinder.TagRowPrefab is missing. Tag rows may be instantiated at runtime only from this prefab.", issues);
                Require(host.StatusTagPrefab != null, "PrototypeBinder.StatusTagPrefab is missing. Repeated tag UI may be instantiated at runtime only from this prefab.", issues);
                Require(host.EmptySlotPrefab != null, "PrototypeBinder.EmptySlotPrefab is missing. Empty slots may be instantiated at runtime only from this prefab.", issues);
                Require(host.BannerRoot != null, "PrototypeBinder.BannerRoot is missing.", issues);
                Require(host.BannerText != null, "PrototypeBinder.BannerText is missing.", issues);
                ValidateBinders(host.ScreenBinders, issues);
            }

            if (issues.Count == 0)
            {
                Debug.Log("Paper Shift scene validation passed.");
                return;
            }

            for (var i = 0; i < issues.Count; i++)
            {
                Debug.LogWarning("Paper Shift scene validation: " + issues[i]);
            }
        }

        private static void ValidateBinders(PaperShiftScreenBinderBase[] binders, ICollection<string> issues)
        {
            if (binders == null)
            {
                return;
            }

            for (var i = 0; i < binders.Length; i++)
            {
                var binder = binders[i];
                if (binder == null)
                {
                    issues.Add("ScreenBinders contains a null entry at index " + i + ".");
                    continue;
                }

                if (binder is PaperShiftGameplayScreenBinder gameplay)
                {
                    ValidateGameplay(gameplay, issues);
                }
                else if (binder is PaperShiftCreateScreenBinder create)
                {
                    ValidateCreate(create, issues);
                }
                else if (binder is PaperShiftTagsScreenBinder tags)
                {
                    ValidateTags(tags, issues);
                }
                else if (binder is PaperShiftResumeScreenBinder resume)
                {
                    ValidateResume(resume, issues);
                }
                else if (binder is PaperShiftBudgetScreenBinder budget)
                {
                    Require(budget.SaveButton != null, "Budget.SaveButton is missing.", issues);
                    Require(budget.BudgetRows != null && budget.BudgetRows.Length > 0, "Budget.BudgetRows is empty.", issues);
                }
                else if (binder is PaperShiftNewsScreenBinder news)
                {
                    Require(news.TitleText != null, "News.TitleText is missing.", issues);
                    Require(news.BodyText != null, "News.BodyText is missing.", issues);
                    Require(news.OptionButtons != null && news.OptionButtons.Length > 0, "News.OptionButtons is empty.", issues);
                }
                else if (binder is PaperShiftRetirementScreenBinder retirement)
                {
                    Require(retirement.FinishButton != null, "Retirement.FinishButton is missing.", issues);
                }
                else if (binder is PaperShiftInheritanceScreenBinder inheritance)
                {
                    ValidateInheritance(inheritance, issues);
                }
            }
        }

        private static void ValidateGameplay(PaperShiftGameplayScreenBinder binder, ICollection<string> issues)
        {
            var view = binder.GameplayView;
            Require(view != null, "Gameplay.GameplayView is missing.", issues);
            if (view != null && !view.IsComplete(out var missing))
            {
                issues.Add("GameplayView is missing " + missing + ".");
            }

            Require(binder.SelfCardView != null, "Gameplay.SelfCardView is missing.", issues);
            Require(binder.JobCardView != null, "Gameplay.JobCardView is missing.", issues);
        }

        private static void ValidateCreate(PaperShiftCreateScreenBinder binder, ICollection<string> issues)
        {
            Require(binder.View != null || (binder.Texts != null && binder.Texts.Length > 0), "Create screen has no View or legacy text bindings.", issues);
            Require(HasButton(binder.View == null ? null : binder.View.NextButton) || HasButton(binder.NextButton), "Create.NextButton is missing.", issues);
        }

        private static void ValidateTags(PaperShiftTagsScreenBinder binder, ICollection<string> issues)
        {
            var view = binder.View;
            Require((view != null && view.TagListRoot != null) || binder.TagListRoot != null, "Tags.TagListRoot is missing.", issues);
            Require((view != null && view.StartJobButton != null) || binder.StartJobButton != null, "Tags.StartJobButton is missing.", issues);
        }

        private static void ValidateResume(PaperShiftResumeScreenBinder binder, ICollection<string> issues)
        {
            var view = binder.View;
            Require((view != null && view.SendResumeButton != null) || binder.SendResumeButton != null, "Resume.SendResumeButton is missing.", issues);
            if (view != null)
            {
                Require(view.TagContentRoot != null, "Resume.TagContentRoot is missing.", issues);
                Require(view.TagPrefab != null, "Resume.TagPrefab is missing. Resume tag items may be instantiated at runtime only from this prefab.", issues);
            }
        }

        private static void ValidateInheritance(PaperShiftInheritanceScreenBinder binder, ICollection<string> issues)
        {
            var view = binder.View;
            Require(view != null, "Inheritance.View is missing.", issues);
            if (view != null)
            {
                Require(view.HeirCards != null && view.HeirCards.Length > 0, "Inheritance.HeirCards is empty.", issues);
                Require(view.ContinueButton != null, "Inheritance.ContinueButton is missing.", issues);
            }
        }

        private static bool HasButton(UnityEngine.UI.Button button)
        {
            return button != null;
        }

        private static void Require(bool condition, string issue, ICollection<string> issues)
        {
            if (!condition)
            {
                issues.Add(issue);
            }
        }
    }
}
