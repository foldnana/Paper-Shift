using System.Collections.Generic;
using PaperShift.Controller;
using PaperShift.Data;
using PaperShift.Domain;
using PaperShift.Runtime;
using UnityEngine;

namespace PaperShift.Presenter
{
    public sealed class PaperShiftGamePresenter : MonoBehaviour
    {
        public PaperShiftDatabase Database;
        public PaperShiftSceneController SceneController;
        public string InitialEraId = "modern";
        public int StartingTagChoiceCount = 7;
        public int StartingTagLimit = 3;
        public PaperShiftRunState State;
        public List<TagDefinition> CurrentTagChoices = new List<TagDefinition>();

        private PaperShiftGameService service;
        private TriggeredEvent pendingEvent;

        public TriggeredEvent PendingEvent
        {
            get { return pendingEvent; }
        }

        public PaperShiftDatabase ActiveDatabase
        {
            get { return service == null ? Database : service.Database; }
        }

        private void Awake()
        {
            service = new PaperShiftGameService(Database);
            State = service.StartNewRun(InitialEraId);
            if (SceneController == null)
            {
                SceneController = FindObjectOfType<PaperShiftSceneController>();
            }
        }

        public void RandomizeWorker()
        {
            service.RandomizeWorker(State, State.Worker.EraId);
            CurrentTagChoices = service.RollStartingTags(State, StartingTagChoiceCount);
            ShowCreate();
        }

        public void SetEraAndRandomize(string eraId)
        {
            service.RandomizeWorker(State, eraId);
            CurrentTagChoices = service.RollStartingTags(State, StartingTagChoiceCount);
            ShowCreate();
        }

        public void RollTagsAndShow()
        {
            CurrentTagChoices = service.RollStartingTags(State, StartingTagChoiceCount);
            State.Phase = PaperShiftPhase.SelectTags;
            ShowTags();
        }

        public void EnsureTagChoices()
        {
            if (CurrentTagChoices == null)
            {
                CurrentTagChoices = new List<TagDefinition>();
            }

            if (CurrentTagChoices.Count == 0)
            {
                CurrentTagChoices = service.RollStartingTags(State, StartingTagChoiceCount);
            }
        }

        public void SelectStartingTag(string tagId)
        {
            service.SelectStartingTag(State, tagId, StartingTagLimit);
        }

        public void ToggleStartingTag(string tagId)
        {
            service.ToggleStartingTag(State, tagId, StartingTagLimit);
        }

        public void ContinueToResume()
        {
            State.Phase = PaperShiftPhase.EditResume;
            ShowResume();
        }

        public void ToggleResumeIntent(string intentTagId)
        {
            var enabled = !State.Resume.IntentTagIds.Contains(intentTagId);
            service.SetResumeIntent(State, intentTagId, enabled);
        }

        public void SetResumePackaging(string fieldId, ResumePackagingMode mode)
        {
            service.SetResumePackaging(State, fieldId, mode);
        }

        public void SetResumePackaging(string fieldId, ResumePackagingMode mode, int optionIndex)
        {
            service.SetResumePackaging(State, fieldId, mode, optionIndex);
        }

        public bool ToggleResumeHiddenTag(string tagId)
        {
            return service.ToggleResumeHiddenTag(State, tagId, 3);
        }

        public void SetBudgetCategory(string categoryId, int value)
        {
            State.Budget.SetCategory(categoryId, value);
        }

        public void FindInterviewAndShow()
        {
            if (service.FindInterviewOffer(State))
            {
                ShowJobSearch();
            }
        }

        public void AdvanceInterview()
        {
            pendingEvent = service.AdvanceInterview(State);
            if (pendingEvent != null)
            {
                ShowNews();
                return;
            }

            ShowJobSearch();
        }

        public bool AskInterviewResult(out string message)
        {
            pendingEvent = null;
            var success = service.ResolveInterviewResult(State, out message);
            if (success)
            {
                ShowWork();
            }
            else
            {
                if (SceneController != null)
                {
                    SceneController.ShowInterviewFailure();
                }
            }

            return success;
        }

        public void AcceptOffer()
        {
            if (service.AcceptOffer(State))
            {
                ShowWork();
            }
            else
            {
                ShowJobSearch();
            }
        }

        public void CompleteWorkYear()
        {
            pendingEvent = service.CompleteWorkYear(State);
            if (State.Phase == PaperShiftPhase.Retirement)
            {
                ShowRetirement();
                return;
            }

            if (pendingEvent != null)
            {
                ShowNews();
                return;
            }

            ShowBudget();
        }

        public void SaveBudgetAndReturnToWork()
        {
            pendingEvent = service.SaveBudget(State, State.Budget);
            if (pendingEvent != null)
            {
                ShowNews();
                return;
            }

            ShowWork();
        }

        public void ChoosePendingEventOption(int optionIndex)
        {
            if (pendingEvent == null || optionIndex < 0 || optionIndex >= pendingEvent.Options.Length)
            {
                return;
            }

            service.ChooseEventOption(State, pendingEvent, pendingEvent.Options[optionIndex].Id);
            pendingEvent = null;
            if (State.Phase == PaperShiftPhase.Retirement)
            {
                ShowRetirement();
            }
            else if (State.HasActiveJob)
            {
                ShowWork();
            }
            else
            {
                ShowResume();
            }
        }

        public void QuitJob()
        {
            service.QuitJob(State);
            ShowResume();
        }

        public void RetireNow()
        {
            service.Retire(State, RunEndReason.Quit);
            ShowRetirement();
        }

        public void StartNextGeneration(int heirIndex)
        {
            if (service.StartNextGeneration(State, heirIndex))
            {
                ShowCreate();
            }
        }

        private void ShowCreate()
        {
            if (SceneController != null)
            {
                SceneController.ShowCreate();
            }
        }

        private void ShowTags()
        {
            if (SceneController != null)
            {
                SceneController.ShowTags();
            }
        }

        private void ShowResume()
        {
            if (SceneController != null)
            {
                SceneController.ShowResume();
            }
        }

        private void ShowJobSearch()
        {
            if (SceneController != null)
            {
                SceneController.ShowJobSearch();
            }
        }

        private void ShowWork()
        {
            if (SceneController != null)
            {
                SceneController.ShowWork();
            }
        }

        private void ShowBudget()
        {
            if (SceneController != null)
            {
                SceneController.ShowBudget();
            }
        }

        private void ShowNews()
        {
            if (SceneController != null)
            {
                SceneController.ShowNews();
            }
        }

        private void ShowRetirement()
        {
            if (SceneController != null)
            {
                SceneController.ShowRetirement();
            }
        }
    }
}
