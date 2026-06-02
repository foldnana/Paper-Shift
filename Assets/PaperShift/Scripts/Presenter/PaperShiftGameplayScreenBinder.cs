using System.Collections;
using System.Collections.Generic;
using PaperShift.Domain;
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

        private const float EventLogMessageSeconds = 3.2f;
        private const float EventLogLineSpacing = 6f;
        private const string RuntimeEventLogLineName = "Runtime Event Log Line";
        private const string LegacyRuntimeEventLogLineName = "Runtime Log Line";
        private readonly PaperShiftCandidateTagGridView candidateTagGridView = new PaperShiftCandidateTagGridView();
        private readonly List<GameObject> activeEventLogLines = new List<GameObject>();
        private Transform eventLogRoot;
        private GameObject eventLogLineTemplate;
        private bool eventLogSceneItemsHidden;
        private int eventLogGeneration = -1;

        private void Reset()
        {
            Screen = PaperShiftScreen.JobSearch;
            GameplayView = GetComponent<PaperShiftGameplayViewReferences>();
        }

        public override void BindActions()
        {
            var gameplay = ResolveGameplayView();
            if (gameplay == null)
            {
                return;
            }

            Bind(gameplay.StartInterviewButton, PrepareInterview);
            Bind(gameplay.JobProgressButton, ApplyInterview);
            Bind(gameplay.ReapplyButton, () => Host.BeginReapplyJobWithTransition());
            Bind(gameplay.StartWorkButton, () =>
            {
                AdvanceProbation();
            });

            var bottomStatus = ResolveBottomStatusBar();
            if (bottomStatus != null && bottomStatus.InterviewStatus != null)
            {
                Bind(bottomStatus.InterviewStatus.ActionButton, ApplyInterview);
            }

            if (bottomStatus != null && bottomStatus.WorkStatus != null)
            {
                Bind(bottomStatus.WorkStatus.ActionButton, ApplyRegularization);
            }
        }

        public override void RefreshView()
        {
            ClearEventLogIfGenerationChanged();

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
            ClearEventLogIfGenerationChanged();
            PlayJobCardTransition(screen);
        }

        private void OnDisable()
        {
            ClearEventLogMessages();
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
            SetGameplayActionsVisible(startInterview: false, reapply: true, startWork: true);
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

            ResolveEventLogLineTemplate(ResolveEventLogRoot());
        }

        private void PrepareInterview()
        {
            var outcome = Presenter.PrepareInterview(out var message);
            if (!string.IsNullOrEmpty(message) && outcome != InterviewStepOutcome.Event)
            {
                ShowEventLogMessage(message);
            }

            RefreshAll();
        }

        private void ApplyInterview()
        {
            var outcome = Presenter.ApplyInterview(out var message);
            if (!string.IsNullOrEmpty(message) && outcome != InterviewStepOutcome.Event)
            {
                if (outcome == InterviewStepOutcome.Continue)
                {
                    ShowEventLogMessage(message);
                }
                else
                {
                    ShowTransitionMessage("面试结果", message, outcome == InterviewStepOutcome.Passed ? PaperShiftTheme.Hex("#9fd9f3") : PaperShiftTheme.Hex("#a6dcf7"));
                }
            }

            RefreshAll();
        }

        private void AdvanceProbation()
        {
            var outcome = Presenter.AdvanceProbation(out var message);
            if (!string.IsNullOrEmpty(message) && outcome != ProbationStepOutcome.Event)
            {
                if (outcome == ProbationStepOutcome.Continue)
                {
                    ShowEventLogMessage(message);
                }
                else
                {
                    ShowTransitionMessage("试用期", message, PaperShiftTheme.Hex("#9fd9f3"));
                }
            }

            RefreshAll();
        }

        private void ApplyRegularization()
        {
            var outcome = Presenter.ApplyRegularization(out var message);
            if (!string.IsNullOrEmpty(message) && outcome != ProbationStepOutcome.Event)
            {
                if (outcome == ProbationStepOutcome.Continue)
                {
                    ShowEventLogMessage(message);
                }
                else
                {
                    ShowTransitionMessage("申请入职", message, outcome == ProbationStepOutcome.Passed ? PaperShiftTheme.Hex("#9fd9f3") : PaperShiftTheme.Hex("#a6dcf7"));
                }
            }

            RefreshAll();
        }

        private void ShowTransitionMessage(string title, string detail, Color accent)
        {
            var gameplay = ResolveGameplayView();
            if (gameplay != null && gameplay.JobTransition != null)
            {
                gameplay.JobTransition.Show("↻", title, detail, accent);
                return;
            }

            ShowBanner(detail);
        }

        private void ShowEventLogMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            var root = ResolveEventLogRoot();
            var template = ResolveEventLogLineTemplate(root);
            if (root == null || template == null)
            {
                ShowBanner(message);
                return;
            }

            root.gameObject.SetActive(true);
            var line = Instantiate(template, root);
            line.name = RuntimeEventLogLineName;
            line.SetActive(true);
            ConfigureEventLogLineRect(line);

            var text = line.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.text = message;
            }

            activeEventLogLines.Add(line);
            ReflowEventLogLines();
            StartCoroutine(AnimateEventLogLine(line, EventLogMessageSeconds));
        }

        private IEnumerator AnimateEventLogLine(GameObject line, float holdSeconds)
        {
            if (line == null)
            {
                yield break;
            }

            var graphics = new List<Graphic>();
            line.GetComponentsInChildren(true, graphics);
            var baseColors = new List<Color>();
            for (var i = 0; i < graphics.Count; i++)
            {
                baseColors.Add(graphics[i].color);
            }

            var baseScale = line.transform.localScale;
            const float fadeInSeconds = 0.16f;
            const float fadeOutSeconds = 0.28f;

            var elapsed = 0f;
            while (elapsed < fadeInSeconds && line != null)
            {
                var t = Mathf.Clamp01(elapsed / fadeInSeconds);
                SetGraphicAlpha(graphics, baseColors, Mathf.SmoothStep(0f, 1f, t));
                line.transform.localScale = Vector3.Lerp(baseScale * 0.96f, baseScale, Mathf.SmoothStep(0f, 1f, t));
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            SetGraphicAlpha(graphics, baseColors, 1f);
            if (line != null)
            {
                line.transform.localScale = baseScale;
            }

            yield return new WaitForSecondsRealtime(holdSeconds);

            elapsed = 0f;
            while (elapsed < fadeOutSeconds && line != null)
            {
                var t = Mathf.Clamp01(elapsed / fadeOutSeconds);
                SetGraphicAlpha(graphics, baseColors, Mathf.SmoothStep(1f, 0f, t));
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (line != null)
            {
                activeEventLogLines.Remove(line);
                DestroyEventLogLine(line);
                ReflowEventLogLines();
                if (activeEventLogLines.Count == 0 && eventLogRoot != null)
                {
                    eventLogRoot.gameObject.SetActive(false);
                }
            }
        }

        private void ClearEventLogIfGenerationChanged()
        {
            if (State == null)
            {
                return;
            }

            if (eventLogGeneration == State.Generation)
            {
                return;
            }

            eventLogGeneration = State.Generation;
            ClearEventLogMessages();
        }

        private void ClearEventLogMessages()
        {
            for (var i = activeEventLogLines.Count - 1; i >= 0; i--)
            {
                DestroyEventLogLine(activeEventLogLines[i]);
            }

            activeEventLogLines.Clear();

            var root = eventLogRoot != null ? eventLogRoot : ResolveEventLogRoot();
            if (root == null)
            {
                return;
            }

            var template = ResolveEventLogLineTemplate(root);
            for (var i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                if (child == null || child.gameObject == template)
                {
                    continue;
                }

                if (child.name == RuntimeEventLogLineName || child.name == LegacyRuntimeEventLogLineName)
                {
                    DestroyEventLogLine(child.gameObject);
                }
            }

            root.gameObject.SetActive(false);
        }

        private void ConfigureEventLogLineRect(GameObject line)
        {
            var rect = line == null ? null : line.GetComponent<RectTransform>();
            if (rect == null)
            {
                return;
            }

            var height = EventLogLineHeight(rect);
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(0f, height);
            rect.anchoredPosition = Vector2.zero;
        }

        private void ReflowEventLogLines()
        {
            for (var i = activeEventLogLines.Count - 1; i >= 0; i--)
            {
                if (activeEventLogLines[i] == null)
                {
                    activeEventLogLines.RemoveAt(i);
                }
            }

            var offset = 0f;
            for (var i = activeEventLogLines.Count - 1; i >= 0; i--)
            {
                var rect = activeEventLogLines[i].GetComponent<RectTransform>();
                if (rect == null)
                {
                    continue;
                }

                rect.SetAsLastSibling();
                rect.anchoredPosition = new Vector2(0f, offset);
                offset += EventLogLineHeight(rect) + EventLogLineSpacing;
            }
        }

        private static float EventLogLineHeight(RectTransform rect)
        {
            if (rect == null)
            {
                return 30f;
            }

            if (rect.sizeDelta.y > 0f)
            {
                return rect.sizeDelta.y;
            }

            var preferred = LayoutUtility.GetPreferredHeight(rect);
            return preferred > 0f ? preferred : 30f;
        }

        private static void SetGraphicAlpha(List<Graphic> graphics, List<Color> baseColors, float alpha)
        {
            for (var i = 0; i < graphics.Count; i++)
            {
                if (graphics[i] == null)
                {
                    continue;
                }

                var color = i < baseColors.Count ? baseColors[i] : graphics[i].color;
                color.a *= alpha;
                graphics[i].color = color;
            }
        }

        private static void DestroyEventLogLine(GameObject line)
        {
            if (line == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(line);
            }
            else
            {
                DestroyImmediate(line);
            }
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
                    new UiPair("身高", PaperShiftWorkerAttributes.DisplayValue(worker, PaperShiftWorkerAttributes.Height)),
                    new UiPair("形象", PaperShiftWorkerAttributes.DisplayValue(worker, PaperShiftWorkerAttributes.Appearance)),
                    new UiPair("教育", PaperShiftWorkerAttributes.DisplayValue(worker, PaperShiftWorkerAttributes.Education)),
                    new UiPair("专业", PaperShiftWorkerAttributes.DisplayValue(worker, PaperShiftWorkerAttributes.Major)),
                    new UiPair("能力", PaperShiftWorkerAttributes.DisplayValue(worker, PaperShiftWorkerAttributes.Ability)),
                    new UiPair("家境", PaperShiftWorkerAttributes.DisplayValue(worker, PaperShiftWorkerAttributes.Family)),
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
                Badge = "推进",
                Corner = "面试",
                Name = EmptyFallback(State.Interview.CompanyName, "还没有公司"),
                Subtitle = EmptyFallback(State.Interview.JobTitle, "先投递简历"),
                RingText = "认可 " + State.Interview.Satisfaction + "%",
                Rows = new List<UiPair>
                {
                    new UiPair("公司", EmptyFallback(State.Interview.CompanyName, "待投递")),
                    new UiPair("岗位", EmptyFallback(State.Interview.JobTitle, "未知")),
                    new UiPair("月薪", State.Interview.Salary.ToString()),
                    new UiPair("认可度", State.Interview.Satisfaction + "%"),
                    new UiPair("简历风险", State.Resume.DeceptionRisk + "%"),
                    new UiPair("状态", State.Interview.Satisfaction >= 70 ? "有希望" : "推进中")
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
                return "认可已达标";
            }

            if (Presenter != null && Presenter.LastInterviewSatisfactionDelta != 0)
            {
                var delta = Presenter.LastInterviewSatisfactionDelta;
                return "认可度 " + (delta > 0 ? "+" : string.Empty) + delta + "%";
            }

            return "面试认可度";
        }

        private CandidateUiData BuildJobCardData()
        {
            return new CandidateUiData
            {
                Badge = State.HasActiveJob ? "试用" : "待业",
                Corner = "试用期",
                Name = EmptyFallback(State.CurrentJob.CompanyName, "暂无工作"),
                Subtitle = EmptyFallback(State.CurrentJob.JobTitle, "去投递简历"),
                RingText = "认可 " + State.CurrentJob.PromotionProgress + "%",
                Rows = new List<UiPair>
                {
                    new UiPair("公司", EmptyFallback(State.CurrentJob.CompanyName, "暂无")),
                    new UiPair("岗位", EmptyFallback(State.CurrentJob.JobTitle, "暂无")),
                    new UiPair("月薪", State.CurrentJob.Salary.ToString()),
                    new UiPair("阶段", "试用中"),
                    new UiPair("认可度", State.CurrentJob.PromotionProgress + "%"),
                    new UiPair("状态", State.CurrentJob.PromotionProgress >= 70 ? "有希望" : "观察中")
                },
                Tags = CurrentJobTags(),
                ProgressPercent = State.CurrentJob.PromotionProgress + "%",
                ProgressLabel = "试用认可度",
                ProgressFill = Mathf.Clamp01(State.CurrentJob.PromotionProgress / 100f)
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
            if (State == null || gameplay == null || gameplay.JobTransition == null || gameplay.JobTransition.IsPlaying)
            {
                return;
            }

            switch (screen)
            {
                case PaperShiftScreen.JobSearch:
                    gameplay.JobTransition.Show("↻", "你进入了面试期", "正在推进面试机会...", PaperShiftTheme.Hex("#9ed8f7"));
                    break;
                case PaperShiftScreen.Work:
                    gameplay.JobTransition.Show("↻", "你进入了试用期", "正在适应新的工作节奏...", PaperShiftTheme.Hex("#9fd9f3"));
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
            var bottomStatusBar = ResolveBottomStatusBar();
            if (bottomStatusBar == null)
            {
                SetButtonInteractable(ResolveGameplayView() == null ? null : ResolveGameplayView().JobProgressButton, interviewActionEnabled);
                return;
            }

            bottomStatusBar.Refresh(State, interviewActionEnabled && !string.IsNullOrEmpty(State.Interview.JobId));
            bottomStatusBar.SetInterviewInteractable(interviewActionEnabled);
            bottomStatusBar.SetWorkInteractable(State.Phase == PaperShiftPhase.Probation && State.HasActiveJob);
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

        private PaperShiftBottomStatusBarView ResolveBottomStatusBar()
        {
            if (BottomStatusBar == null)
            {
                var gameplay = ResolveGameplayView();
                BottomStatusBar = gameplay == null ? null : gameplay.BottomStatusBar;
            }

            return BottomStatusBar;
        }

        private Transform ResolveEventLogRoot()
        {
            var gameplay = ResolveGameplayView();
            if (gameplay != null && gameplay.SelfEventLog != null)
            {
                eventLogRoot = gameplay.SelfEventLog;
                HideInitialEventLogItems(eventLogRoot);
                return eventLogRoot;
            }

            eventLogRoot = null;
            return null;
        }

        private void HideInitialEventLogItems(Transform root)
        {
            if (eventLogSceneItemsHidden || root == null)
            {
                return;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child != null)
                {
                    child.gameObject.SetActive(false);
                }
            }

            eventLogSceneItemsHidden = true;
        }

        private GameObject ResolveEventLogLineTemplate(Transform root)
        {
            var gameplay = ResolveGameplayView();
            if (gameplay != null && gameplay.EventLogLinePrefab != null)
            {
                eventLogLineTemplate = gameplay.EventLogLinePrefab;
                return eventLogLineTemplate;
            }

            if (eventLogLineTemplate != null)
            {
                return eventLogLineTemplate;
            }

            if (root == null)
            {
                return null;
            }

            if (root.childCount <= 0)
            {
                return null;
            }

            eventLogLineTemplate = root.GetChild(0).gameObject;
            eventLogLineTemplate.SetActive(false);
            return eventLogLineTemplate;
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
