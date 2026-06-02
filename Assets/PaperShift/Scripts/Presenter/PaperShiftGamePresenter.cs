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
        public string InitialEraId = string.Empty;
        public int StartingTagChoiceCount = 6;
        public int StartingTagLimit = 3;
        public PaperShiftRunState State;
        public List<TagDefinition> CurrentTagChoices = new List<TagDefinition>();
        public int LastInterviewRecognitionDelta { get; private set; }

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
            service.RandomizeWorker(State, string.Empty);
            CurrentTagChoices = service.RollStartingTags(State, StartingTagChoiceCount);
            ShowCreate();
        }

        public void RollTagsAndShow()
        {
            ClearSelectedStartingTags();
            CurrentTagChoices = service.RollStartingTags(State, StartingTagChoiceCount);
            State.Phase = PaperShiftPhase.SelectTags;
            ShowTags();
        }

        private void ClearSelectedStartingTags()
        {
            if (State == null || State.Worker == null || State.Worker.Tags == null)
            {
                return;
            }

            State.Worker.Tags.Clear();
            if (State.Resume != null && State.Resume.HiddenTagIds != null)
            {
                State.Resume.HiddenTagIds.Clear();
            }
        }

        public void EnsureTagChoices()
        {
            if (CurrentTagChoices == null)
            {
                CurrentTagChoices = new List<TagDefinition>();
            }

            if (CurrentTagChoices.Count == 0 || CurrentTagChoices.Count != StartingTagChoiceCount)
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
            service.SetResumeIntent(State, intentTagId, false);
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
            FindInterviewAndShowInternal();
        }

        private bool FindInterviewAndShowInternal()
        {
            pendingEvent = null;
            service.RestartJobSearch(State);
            if (service.FindInterviewOffer(State))
            {
                LastInterviewRecognitionDelta = 0;
                ShowJobSearch();
                return true;
            }

            return false;
        }

        public InterviewStepOutcome PrepareInterview(out string message)
        {
            message = string.Empty;
            var result = service.PrepareInterviewStep(State);
            if (result == null)
            {
                return InterviewStepOutcome.Failed;
            }

            pendingEvent = result.TriggeredEvent;
            message = result.Message;
            LastInterviewRecognitionDelta = result.RecognitionDelta;
            ShowJobSearch();
            return result.Outcome;
        }

        public InterviewStepOutcome ApplyInterview(out string message)
        {
            message = string.Empty;
            var result = service.ApplyInterview(State);
            if (result == null)
            {
                return InterviewStepOutcome.Failed;
            }

            pendingEvent = result.TriggeredEvent;
            message = result.Message;
            LastInterviewRecognitionDelta = result.RecognitionDelta;
            switch (result.Outcome)
            {
                case InterviewStepOutcome.Event:
                    ShowNews();
                    break;
                case InterviewStepOutcome.Passed:
                    ShowWork();
                    break;
                case InterviewStepOutcome.Failed:
                    var failureMessage = message;
                    if (FindInterviewAndShowInternal())
                    {
                        message = failureMessage + "\n已自动换一家公司继续面试。";
                    }
                    else
                    {
                        ShowResume();
                    }
                    break;
                default:
                    ShowJobSearch();
                    break;
            }

            return result.Outcome;
        }

        public InterviewStepOutcome AdvanceInterview(out string message)
        {
            return ApplyInterview(out message);
        }

        public void AdvanceInterview()
        {
            ApplyInterview(out _);
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
                var failureMessage = message;
                if (FindInterviewAndShowInternal())
                {
                    message = failureMessage + "\n已自动换一家公司继续面试。";
                }
                else
                {
                    ShowResume();
                }
            }

            return success;
        }

        public void AcceptOffer()
        {
            if (service.StartProbation(State))
            {
                ShowWork();
            }
            else
            {
                ShowJobSearch();
            }
        }

        public ProbationStepOutcome AdvanceProbation(out string message)
        {
            message = string.Empty;
            var result = service.AdvanceProbationStep(State);
            if (result == null)
            {
                return ProbationStepOutcome.Failed;
            }

            pendingEvent = result.TriggeredEvent;
            message = result.Message;
            switch (result.Outcome)
            {
                case ProbationStepOutcome.Event:
                    ShowNews();
                    break;
                case ProbationStepOutcome.Passed:
                    ShowRetirement();
                    break;
                case ProbationStepOutcome.Failed:
                    var failureMessage = message;
                    if (FindInterviewAndShowInternal())
                    {
                        message = failureMessage + "\n已自动换一家公司继续面试。";
                    }
                    else
                    {
                        ShowResume();
                    }
                    break;
                default:
                    ShowWork();
                    break;
            }

            return result.Outcome;
        }

        public void AdvanceProbation()
        {
            AdvanceProbation(out _);
        }

        public ProbationStepOutcome ApplyRegularization(out string message)
        {
            message = string.Empty;
            var result = service.ApplyRegularization(State);
            if (result == null)
            {
                return ProbationStepOutcome.Failed;
            }

            pendingEvent = result.TriggeredEvent;
            message = result.Message;
            switch (result.Outcome)
            {
                case ProbationStepOutcome.Event:
                    ShowNews();
                    break;
                case ProbationStepOutcome.Passed:
                    ShowRetirement();
                    break;
                case ProbationStepOutcome.Failed:
                    var failureMessage = message;
                    if (FindInterviewAndShowInternal())
                    {
                        message = failureMessage + "\n已自动换一家公司继续面试。";
                    }
                    else
                    {
                        ShowResume();
                    }
                    break;
                default:
                    ShowWork();
                    break;
            }

            return result.Outcome;
        }

        public void ApplyRegularization()
        {
            ApplyRegularization(out _);
        }

        public void CompleteWorkYear()
        {
            AdvanceProbation(out _);
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

            var result = service.ChooseEventOption(State, pendingEvent, pendingEvent.Options[optionIndex].Id);
            pendingEvent = null;
            NavigateAfterEventChoice(result);
        }

        private void NavigateAfterEventChoice(EventOptionChoiceResult result)
        {
            if (TryShowFollowUpEvent(result))
            {
                return;
            }

            var checkpoint = result == null ? null : result.CheckpointResult;
            if (checkpoint != null)
            {
                NavigateAfterEventCheckpoint(checkpoint);
                return;
            }

            NavigateAfterCurrentRunState(allowAutoJobRefresh: true);
        }

        private bool TryShowFollowUpEvent(EventOptionChoiceResult result)
        {
            return result != null && TryShowFollowUpEvent(result.TriggeredEvent);
        }

        private bool TryShowFollowUpEvent(TriggeredEvent triggeredEvent)
        {
            if (triggeredEvent == null)
            {
                return false;
            }

            pendingEvent = triggeredEvent;
            ShowNews();
            return true;
        }

        private void NavigateAfterEventCheckpoint(FlowCheckpointResult checkpoint)
        {
            if (State.Phase == PaperShiftPhase.Retirement)
            {
                ShowRetirement();
                return;
            }

            switch (checkpoint.Outcome)
            {
                case FlowCheckpointOutcome.Event:
                    if (!TryShowFollowUpEvent(checkpoint.TriggeredEvent))
                    {
                        NavigateAfterCurrentRunState(allowAutoJobRefresh: true);
                    }
                    break;
                case FlowCheckpointOutcome.Passed:
                case FlowCheckpointOutcome.Continue:
                    NavigateAfterCurrentRunState(allowAutoJobRefresh: false);
                    break;
                case FlowCheckpointOutcome.Failed:
                    MoveToNextInterviewOrResume();
                    break;
                default:
                    NavigateAfterCurrentRunState(allowAutoJobRefresh: true);
                    break;
            }
        }

        private void NavigateAfterCurrentRunState(bool allowAutoJobRefresh)
        {
            if (State.Phase == PaperShiftPhase.Retirement)
            {
                ShowRetirement();
                return;
            }

            if (State.HasActiveJob)
            {
                ShowWork();
                return;
            }

            if (HasInterviewOpportunity())
            {
                ShowJobSearch();
                return;
            }

            if (allowAutoJobRefresh && State.Phase == PaperShiftPhase.Interview)
            {
                MoveToNextInterviewOrResume();
                return;
            }

            ShowResume();
        }

        private void MoveToNextInterviewOrResume()
        {
            if (!FindInterviewAndShowInternal())
            {
                ShowResume();
            }
        }

        private bool HasInterviewOpportunity()
        {
            return State != null && State.Interview != null && !string.IsNullOrEmpty(State.Interview.JobId);
        }

        public void QuitJob()
        {
            FindInterviewAndShowInternal();
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
