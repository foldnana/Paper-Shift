using System.Collections.Generic;
using PaperShift.Model;
using PaperShift.Runtime;
using UnityEngine;
using UnityEngine.UI;
using static PaperShift.Presenter.PaperShiftUiFormatter;

namespace PaperShift.Presenter
{
    public sealed class PaperShiftGameplayScreenBinder : PaperShiftScreenBinderBase
    {
        public PaperShiftGameplayViewReferences GameplayView;
        public PaperShiftCandidateCardView SelfCardView;
        public PaperShiftCandidateCardView JobCardView;
        public PaperShiftBottomStatusBarView BottomStatusBar;

        private readonly PaperShiftCandidateTagGridView candidateTagGridView = new PaperShiftCandidateTagGridView();

        private void Reset()
        {
            Screen = PaperShiftScreen.JobSearch;
            GameplayView = GetComponent<PaperShiftGameplayViewReferences>();
            BottomStatusBar = GetComponentInChildren<PaperShiftBottomStatusBarView>(true);
        }

        public override void BindActions()
        {
            var gameplay = ResolveGameplayView();
            if (gameplay == null)
            {
                return;
            }

            Bind(gameplay.StartInterviewButton, AdvanceInterview);
            Bind(gameplay.JobProgressButton, AdvanceInterview);
            Bind(gameplay.ReapplyButton, () => Host.BeginReapplyJobWithTransition());
            Bind(gameplay.StartWorkButton, () =>
            {
                Presenter.CompleteWorkYear();
                RefreshAll();
            });
        }

        public override void RefreshView()
        {
            var current = SceneController == null ? Screen : SceneController.CurrentScreen;
            if (current == PaperShiftScreen.Work)
            {
                RefreshWork();
            }
            else if (current == PaperShiftScreen.InterviewFailure)
            {
                RefreshFailure();
            }
            else
            {
                RefreshJobSearch();
            }
        }

        public override void OnScreenBecameActive(PaperShiftScreen screen)
        {
            PlayJobCardTransition(screen);
        }

        public void SetActionsInteractable(bool interactable)
        {
            var gameplay = ResolveGameplayView();
            if (gameplay == null)
            {
                return;
            }

            SetButtonInteractable(gameplay.StartInterviewButton, interactable);
            SetButtonInteractable(gameplay.ReapplyButton, interactable);
            SetButtonInteractable(gameplay.StartWorkButton, interactable);
            SetButtonInteractable(gameplay.JobProgressButton, interactable);
        }

        public bool ShowPreauthoredTransition()
        {
            var gameplay = ResolveGameplayView();
            return gameplay != null && gameplay.JobTransition != null && gameplay.JobTransition.ShowPreauthored();
        }

        private void RefreshJobSearch()
        {
            RefreshCards(BuildWorkerCardData("求职者"), BuildInterviewCardData());
            SetGameplayActionsVisible(startInterview: true, reapply: true, startWork: false);
            RefreshBottomStatusBar(interviewActionEnabled: true);
            RefreshCalendar();
        }

        private void RefreshFailure()
        {
            RefreshCards(BuildWorkerCardData("求职者"), BuildInterviewCardData());
            SetGameplayActionsVisible(startInterview: false, reapply: true, startWork: false);
            RefreshBottomStatusBar(interviewActionEnabled: false);
            RefreshCalendar();
        }

        private void RefreshWork()
        {
            RefreshCards(BuildWorkerCardData(State.CurrentJob.JobTitle), BuildJobCardData());
            SetGameplayActionsVisible(startInterview: false, reapply: false, startWork: true);
            RefreshBottomStatusBar(interviewActionEnabled: false);
            RefreshCalendar();
        }

        private void RefreshCards(CandidateUiData self, CandidateUiData job)
        {
            var gameplay = ResolveGameplayView();
            if (gameplay == null)
            {
                Debug.LogWarning("PaperShift gameplay view references are not assigned.", this);
                return;
            }

            if (!gameplay.IsComplete(out var missingField))
            {
                Debug.LogWarning("PaperShift gameplay view reference is missing: " + missingField, gameplay);
                return;
            }

            if (SelfCardView != null)
            {
                SelfCardView.Refresh(self, State.Phase);
            }

            if (JobCardView != null)
            {
                JobCardView.Refresh(job, State.Phase);
            }

            RefreshWorkerStatusTags(gameplay);
        }

        private void RefreshWorkerStatusTags(PaperShiftGameplayViewReferences gameplay)
        {
            candidateTagGridView.TagPrefab = Host == null ? null : Host.StatusTagPrefab;
            candidateTagGridView.EmptySlotPrefab = Host == null ? null : Host.EmptySlotPrefab;
            candidateTagGridView.Refresh(gameplay.SelfTagsRoot, State.Worker.Tags);

            if (gameplay.SelfEventLog != null)
            {
                gameplay.SelfEventLog.gameObject.SetActive(false);
            }
        }

        private void AdvanceInterview()
        {
            var outcome = Presenter.AdvanceInterview(out var message);
            if (!string.IsNullOrEmpty(message) && outcome != InterviewStepOutcome.Event)
            {
                ShowBanner(message);
            }

            RefreshAll();
        }

        private CandidateUiData BuildWorkerCardData(string subtitleRole)
        {
            var worker = State.Worker;
            return new CandidateUiData
            {
                Badge = worker.Age + "岁",
                Corner = "第" + State.Generation + "代",
                Name = worker.FullName,
                Subtitle = worker.Gender + " " + subtitleRole,
                RingText = "压力 " + worker.Stress,
                Rows = new List<UiPair>
                {
                    new UiPair("体魄", Score(worker.GetStat("body"))),
                    new UiPair("逻辑", worker.GetStat("logic").ToString()),
                    new UiPair("学历", EducationLabel(worker)),
                    new UiPair("家境", FamilyLabel(worker.GetStat("family"))),
                    new UiPair("存款", worker.Money.ToString()),
                    new UiPair("压力", worker.Stress.ToString())
                },
                Tags = new List<string>(),
                ProgressPercent = string.Empty,
                ProgressLabel = string.Empty,
                ProgressFill = 0f,
                Logs = LastLogs(3)
            };
        }

        private CandidateUiData BuildInterviewCardData()
        {
            return new CandidateUiData
            {
                Badge = Mathf.Max(1, State.Interview.Round + 1) + "轮",
                Corner = "面试",
                Name = EmptyFallback(State.Interview.CompanyName, "还没有公司"),
                Subtitle = EmptyFallback(State.Interview.JobTitle, "先投递简历"),
                RingText = "满意 " + State.Interview.Satisfaction,
                Rows = new List<UiPair>
                {
                    new UiPair("公司", EmptyFallback(State.Interview.CompanyName, "待投递")),
                    new UiPair("岗位", EmptyFallback(State.Interview.JobTitle, "未知")),
                    new UiPair("月薪", State.Interview.Salary.ToString()),
                    new UiPair("轮次", State.Interview.Round + "/" + Mathf.Max(1, State.Interview.MaxRounds)),
                    new UiPair("满意", State.Interview.Satisfaction.ToString()),
                    new UiPair("风险", State.Resume.DeceptionRisk + "%")
                },
                Tags = InterviewTags(),
                ProgressPercent = State.Interview.Satisfaction + "%",
                ProgressLabel = InterviewProgressLabel(),
                ProgressFill = State.Interview.Satisfaction / 100f
            };
        }

        private string InterviewProgressLabel()
        {
            if (State.Interview.HasOffer)
            {
                return "可入职";
            }

            if (Presenter != null &&
                Presenter.LastInterviewRound == State.Interview.Round &&
                State.Interview.Round > 0 &&
                Presenter.LastInterviewSatisfactionDelta != 0)
            {
                var delta = Presenter.LastInterviewSatisfactionDelta;
                return "面试满意度 " + (delta > 0 ? "+" : string.Empty) + delta;
            }

            return "面试满意度";
        }

        private CandidateUiData BuildJobCardData()
        {
            return new CandidateUiData
            {
                Badge = State.HasActiveJob ? "在职" : "待业",
                Corner = "岗位",
                Name = EmptyFallback(State.CurrentJob.CompanyName, "暂无工作"),
                Subtitle = EmptyFallback(State.CurrentJob.JobTitle, "去投递简历"),
                RingText = "强度 " + State.CurrentJob.Intensity,
                Rows = new List<UiPair>
                {
                    new UiPair("公司", EmptyFallback(State.CurrentJob.CompanyName, "暂无")),
                    new UiPair("岗位", EmptyFallback(State.CurrentJob.JobTitle, "暂无")),
                    new UiPair("月薪", State.CurrentJob.Salary.ToString()),
                    new UiPair("年限", State.CurrentJob.WorkYears + " 年"),
                    new UiPair("升职", State.CurrentJob.PromotionProgress + "%"),
                    new UiPair("离职", State.CurrentJob.QuitRisk + "%")
                },
                Tags = CurrentJobTags(),
                ProgressPercent = State.CurrentJob.PromotionProgress + "%",
                ProgressLabel = State.CurrentJob.QuitRisk > State.CurrentJob.PromotionProgress ? "离职风险" : "升职进度",
                ProgressFill = Mathf.Clamp01((State.CurrentJob.QuitRisk > State.CurrentJob.PromotionProgress ? State.CurrentJob.QuitRisk : State.CurrentJob.PromotionProgress) / 100f)
            };
        }

        private List<string> InterviewTags()
        {
            var tags = new List<string>();
            var company = Database == null ? null : Database.FindCompany(State.Interview.CompanyId);
            var job = Database == null ? null : Database.FindJob(State.Interview.CompanyId, State.Interview.JobId);
            if (company != null)
            {
                tags.Add(company.Industry);
            }

            if (job != null)
            {
                for (var i = 0; i < job.TagIds.Length && tags.Count < 4; i++)
                {
                    tags.Add(JobTagLabel(job.TagIds[i]));
                }
            }

            return tags;
        }

        private List<string> CurrentJobTags()
        {
            var tags = new List<string>();
            var job = Database == null ? null : Database.FindJob(State.CurrentJob.CompanyId, State.CurrentJob.JobId);
            if (job != null)
            {
                for (var i = 0; i < job.TagIds.Length && tags.Count < 4; i++)
                {
                    tags.Add(JobTagLabel(job.TagIds[i]));
                }
            }

            return tags;
        }

        private List<string> LastLogs(int count)
        {
            var logs = new List<string>();
            var start = Mathf.Max(0, State.Logs.Count - count);
            for (var i = start; i < State.Logs.Count; i++)
            {
                logs.Add(State.Logs[i].Text);
            }

            return logs;
        }

        private void PlayJobCardTransition(PaperShiftScreen screen)
        {
            var gameplay = ResolveGameplayView();
            if (State == null || gameplay == null || gameplay.JobTransition == null)
            {
                return;
            }

            switch (screen)
            {
                case PaperShiftScreen.JobSearch:
                    gameplay.JobTransition.Show("↻", "你进入了面试期", State.Interview.Round <= 0 ? "正在联系面试官..." : "正在等待面试结果...", PaperShiftTheme.Hex("#9ed8f7"));
                    break;
                case PaperShiftScreen.Work:
                    gameplay.JobTransition.Show("↻", "你进入了打工期", "正在适应新的工作节奏...", PaperShiftTheme.Hex("#9fd9f3"));
                    break;
                case PaperShiftScreen.InterviewFailure:
                    gameplay.JobTransition.Show("↻", "你进入了空窗期", "正在重新联系公司...", PaperShiftTheme.Hex("#a6dcf7"));
                    break;
            }
        }

        private void SetGameplayActionsVisible(bool startInterview, bool reapply, bool startWork)
        {
            var gameplay = ResolveGameplayView();
            if (gameplay == null)
            {
                return;
            }

            SetButtonVisible(gameplay.StartInterviewButton, startInterview);
            SetButtonVisible(gameplay.ReapplyButton, reapply);
            SetButtonVisible(gameplay.StartWorkButton, startWork);
            SetButtonInteractable(gameplay.JobProgressButton, startInterview);
        }

        private void RefreshBottomStatusBar(bool interviewActionEnabled)
        {
            if (BottomStatusBar == null)
            {
                BottomStatusBar = GetComponentInChildren<PaperShiftBottomStatusBarView>(true);
            }

            if (BottomStatusBar == null)
            {
                SetButtonInteractable(ResolveGameplayView() == null ? null : ResolveGameplayView().JobProgressButton, interviewActionEnabled);
                return;
            }

            BottomStatusBar.Refresh(State, interviewActionEnabled && State.Interview.Round > 0);
            BottomStatusBar.SetInterviewInteractable(interviewActionEnabled);
        }

        private void RefreshCalendar()
        {
            var gameplay = ResolveGameplayView();
            if (gameplay == null)
            {
                return;
            }

            Set(gameplay.CalendarYearText, State.CurrentYear.ToString());
            Set(gameplay.CalendarMonthText, "1");
        }

        private PaperShiftGameplayViewReferences ResolveGameplayView()
        {
            if (GameplayView == null)
            {
                GameplayView = GetComponent<PaperShiftGameplayViewReferences>();
            }

            return GameplayView;
        }

        private static void SetButtonVisible(Button button, bool visible)
        {
            if (button != null)
            {
                button.gameObject.SetActive(visible);
            }
        }

        private static void SetButtonInteractable(Button button, bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }
    }
}
