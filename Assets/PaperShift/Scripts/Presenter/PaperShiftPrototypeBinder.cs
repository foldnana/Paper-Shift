using System.Collections;
using System.Collections.Generic;
using PaperShift.Controller;
using PaperShift.Data;
using PaperShift.Domain;
using PaperShift.Model;
using PaperShift.Runtime;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static PaperShift.Presenter.PaperShiftUiFormatter;

namespace PaperShift.Presenter
{
    public sealed class PaperShiftPrototypeBinder : MonoBehaviour
    {
        public PaperShiftGamePresenter Presenter;
        public PaperShiftSceneController SceneController;
        public GameObject TagRowPrefab;
        public GameObject ResumeTagPrefab;
        public GameObject StatusTagPrefab;
        public GameObject EmptySlotPrefab;
        public PaperShiftGameplayViewReferences GameplayView;

        private readonly string[] eraIds = { "agrarian", "industrial", "modern", "near_future", "far_future", "post_work" };
        private readonly string[] intentIds = { "ai_intent", "remote_first", "salary_high" };
        private readonly string[] resumeFieldIds = { "education", "experience", "ability", "tags", "salary" };
        private readonly string[] budgetIds = { "food", "housing", "romance", "education", "savings" };
        private readonly string[] optionButtonNames = { "Option Apply", "Option Stay", "Option Save" };
        private readonly PaperShiftTagSelectionView tagSelectionView = new PaperShiftTagSelectionView();
        private readonly PaperShiftResumeTagListView resumeTagListView = new PaperShiftResumeTagListView();
        private readonly PaperShiftCandidateTagGridView candidateTagGridView = new PaperShiftCandidateTagGridView();

        private PaperShiftScreen activeScreen;
        private RectTransform bannerRoot;
        private Text bannerText;
        private float bannerHideAt;
        private Coroutine reapplyJobRoutine;
        private bool suppressNextStateTransition;

        private PaperShiftDatabase Database
        {
            get { return Presenter == null ? null : Presenter.ActiveDatabase; }
        }

        private PaperShiftRunState State
        {
            get { return Presenter == null ? null : Presenter.State; }
        }

        private void Start()
        {
            if (Presenter == null)
            {
                Presenter = GetComponent<PaperShiftGamePresenter>();
            }

            if (SceneController == null)
            {
                SceneController = GetComponent<PaperShiftSceneController>();
            }

            GameplayView = ResolveGameplayView();
            CreateBanner();
            BindStaticButtons();
            RefreshAll();
        }

        private void LateUpdate()
        {
            if (SceneController == null || State == null)
            {
                return;
            }

            var current = CurrentScreen();
            if (current != activeScreen)
            {
                activeScreen = current;
                RefreshAll();
                if (suppressNextStateTransition)
                {
                    suppressNextStateTransition = false;
                }
                else
                {
                    PlayJobCardTransition(current);
                }
            }

            if (bannerRoot != null && bannerRoot.gameObject.activeSelf && Time.unscaledTime >= bannerHideAt)
            {
                bannerRoot.gameObject.SetActive(false);
            }
        }

        public void RefreshAll()
        {
            if (Presenter == null || State == null)
            {
                return;
            }

            RefreshCreate();
            RefreshTags();
            RefreshResume();
            RefreshJobSearch();
            RefreshFailure();
            RefreshWork();
            RefreshBudget();
            RefreshNewsModal();
            RefreshRetirement();
        }

        private void BindStaticButtons()
        {
            var create = Screen(PaperShiftScreen.Create);
            BindButton(create, "Random Button", () =>
            {
                Presenter.RandomizeWorker();
                SceneController.ShowCreate();
                RefreshAll();
            });
            BindButton(create, "Random Name Button", () =>
            {
                Presenter.RandomizeWorker();
                SceneController.ShowCreate();
                RefreshAll();
            });
            BindButton(create, "Play Button", () =>
            {
                Presenter.RollTagsAndShow();
                RefreshAll();
            });
            BindButton(create, "Custom Button", () =>
            {
                Presenter.RollTagsAndShow();
                RefreshAll();
            });
            BindButton(create, "Next Button", () =>
            {
                Presenter.RollTagsAndShow();
                RefreshAll();
            });

            var resume = Screen(PaperShiftScreen.Resume);
            BindButton(resume, "Send Resume Button", () =>
            {
                Presenter.FindInterviewAndShow();
                RefreshAll();
            });

            var gameplay = EnsureGameplayView();
            if (gameplay != null)
            {
                BindButton(gameplay.StartInterviewButton, AskInterviewResult);
                BindButton(gameplay.JobProgressButton, AskInterviewResult);
                BindButton(gameplay.ReapplyButton, BeginReapplyJobWithTransition);
                BindButton(gameplay.StartWorkButton, () =>
                {
                    Presenter.CompleteWorkYear();
                    RefreshAll();
                });
            }

            var budget = Screen(PaperShiftScreen.Budget);
            BindButton(budget, "Save Budget Button", () =>
            {
                Presenter.SaveBudgetAndReturnToWork();
                ShowLatestBanner("预算已保存。");
                RefreshAll();
            });

            var retirement = Screen(PaperShiftScreen.Retirement);
            BindButton(retirement, "Finish Retirement Button", () =>
            {
                Presenter.StartNextGeneration(0);
                RefreshAll();
            });
        }

        private void RefreshCreate()
        {
            var screen = Screen(PaperShiftScreen.Create);
            if (screen == null)
            {
                return;
            }

            var worker = State.Worker;
            SetInfoValue(screen, "性别", worker.Gender + "　锁定");
            SetInfoValue(screen, "年代", worker.EraName);
            SetInfoValue(screen, "年龄", worker.Age + " 岁");
            SetInfoValue(screen, "体魄", Score(worker.GetStat("body")));
            SetInfoValue(screen, "识字", Rank(worker.GetStat("literacy")));
            SetInfoValue(screen, "逻辑", worker.GetStat("logic").ToString());
            SetInfoValue(screen, "社交", worker.GetStat("social").ToString());
            SetInfoValue(screen, "学历", EducationLabel(worker));
            SetInfoValue(screen, "家境", FamilyLabel(worker.GetStat("family")));
            SetInfoValue(screen, "优势", "★ " + BestStatLabel(worker, Database) + " / " + WorkerTagSummary(worker));
            SetInfoValue(screen, "资产", "★ 启动资金 " + worker.Money);
            SetNameRow(screen, "姓", worker.LastName);
            SetNameRow(screen, "名", worker.FirstName);
            SetCoin(screen, worker.Money.ToString("N0"));

            var eraPicker = Find(screen, "Era Picker");
            if (eraPicker == null)
            {
                return;
            }

            for (var i = 0; i < eraPicker.childCount && i < eraIds.Length; i++)
            {
                var tile = eraPicker.GetChild(i);
                var eraId = eraIds[i];
                var selected = worker.EraId == eraId;
                var graphic = tile.GetComponent<Graphic>();
                if (graphic != null)
                {
                    graphic.color = selected ? PaperShiftTheme.BlueLight : PaperShiftTheme.White;
                }

                var text = FindText(tile, "Label");
                if (text != null)
                {
                    var era = Database.FindEra(eraId);
                    text.text = era == null ? text.text : era.DisplayName.Replace("时代", "\n时代").Replace("城市", "\n城市").Replace("工业", "\n工业").Replace("农耕", "\n农耕").Replace("智能", "\n智能").Replace("星际", "\n星际");
                    text.color = selected ? Color.white : PaperShiftTheme.Hex("#3a4350");
                }

                var button = tile.GetComponent<Button>();
                if (button == null)
                {
                    button = tile.gameObject.AddComponent<Button>();
                    button.targetGraphic = graphic;
                }

                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    Presenter.SetEraAndRandomize(eraId);
                    RefreshAll();
                });
            }
        }

        private void RefreshTags()
        {
            var screen = Screen(PaperShiftScreen.Tags);
            if (screen == null)
            {
                return;
            }

            Presenter.EnsureTagChoices();
            SetTitle(screen, "选择" + State.Worker.FullName + "的标签");
            SetCoin(screen, State.Worker.Money.ToString("N0"));

            var list = Find(screen, "Tag List");
            if (list != null)
            {
                tagSelectionView.TagRowPrefab = TagRowPrefab;
                tagSelectionView.Refresh(list, Presenter, () =>
                {
                    RefreshTags();
                    RefreshCreate();
                    RefreshResume();
                });
            }

            BindButton(screen, "Free Refresh", () =>
            {
                Presenter.RollTagsAndShow();
                RefreshTags();
            });
            BindButton(screen, "Super Refresh", () =>
            {
                Presenter.RollTagsAndShow();
                ShowBanner("时代机会刷新，出现了一组新标签。");
                RefreshTags();
            });
            BindButton(screen, "Confirm Tags Button", () =>
            {
                Presenter.ContinueToResume();
                RefreshAll();
            }, "确认标签 " + State.Worker.Tags.Count + "/" + Presenter.StartingTagLimit);
        }

        private void RefreshResume()
        {
            var screen = Screen(PaperShiftScreen.Resume);
            if (screen == null)
            {
                return;
            }

            SetCoin(screen, State.Worker.Money.ToString("N0"));
            SetText(Find(screen, "Header"), "Name", State.Worker.LastName + " " + State.Worker.FirstName + "\n<size=18>" + State.Worker.Gender + " " + State.Worker.Age + " 岁 " + State.Worker.EraName + "</size>");
            SetText(Find(screen, "Generation Badge"), "Label", "第" + State.Generation + "代");
            SetText(Find(screen, "Resume Risk Footer"), "Count", "<size=40>" + State.Resume.DeceptionRisk + "%</size>  <size=13>识破风险</size>");

            var intentTags = Find(screen, "Intent Tags");
            if (intentTags != null)
            {
                for (var i = 0; i < intentTags.childCount && i < intentIds.Length; i++)
                {
                    var ticket = intentTags.GetChild(i);
                    var intentId = intentIds[i];
                    var active = State.Resume.IntentTagIds.Contains(intentId);
                    SetTicketText(ticket, IntentLabel(intentId));
                    SetSelectableGraphic(ticket, active);
                    var button = ticket.GetComponent<Button>();
                    if (button == null)
                    {
                        button = ticket.gameObject.AddComponent<Button>();
                        button.targetGraphic = ticket.GetComponent<Graphic>();
                    }

                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() =>
                    {
                        Presenter.ToggleResumeIntent(intentId);
                        RefreshResume();
                    });
                }
            }

            RefreshResumePackaging(screen);
            RefreshSelectedTags(screen);
        }

        private void RefreshResumePackaging(Transform screen)
        {
            var lines = Find(screen, "Resume Lines");
            if (lines == null)
            {
                return;
            }

            for (var lineIndex = 0; lineIndex < lines.childCount && lineIndex < resumeFieldIds.Length; lineIndex++)
            {
                var line = lines.GetChild(lineIndex);
                var fieldId = resumeFieldIds[lineIndex];
                var choice = State.Resume.GetOrCreateChoice(fieldId);
                SetText(line, "Value", ResumeFieldValue(fieldId));
                var actions = Find(line, "Actions");
                if (actions == null)
                {
                    continue;
                }

                var actualIndex = ActualResumeOptionIndex(fieldId, actions.childCount);
                for (var chipIndex = 0; chipIndex < actions.childCount; chipIndex++)
                {
                    var chip = actions.GetChild(chipIndex);
                    var mode = ResumeModeFromComparison(chipIndex, actualIndex);
                    var selected = choice.OptionIndex == chipIndex;
                    var selectedIndex = chipIndex;
                    var selectedMode = mode;
                    ApplyResumeChipStyle(chip, chipIndex, actualIndex, selected);
                    var button = chip.GetComponent<Button>();
                    if (button == null)
                    {
                        button = chip.gameObject.AddComponent<Button>();
                        button.targetGraphic = chip.GetComponent<Graphic>();
                    }

                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() =>
                    {
                        Presenter.SetResumePackaging(fieldId, selectedMode, selectedIndex);
                        RefreshResume();
                    });
                }
            }
        }

        private string ResumeFieldValue(string fieldId)
        {
            switch (fieldId)
            {
                case "education":
                    return EducationLabel(State.Worker);
                case "experience":
                    return ResumeExperienceLabel();
                case "ability":
                    return "逻辑" + State.Worker.GetStat("logic") + " / 社交" + State.Worker.GetStat("social");
                case "tags":
                    return WorkerTagSummary(State.Worker);
                case "salary":
                    return ExpectedSalaryLabel();
                default:
                    return string.Empty;
            }
        }

        private string ResumeExperienceLabel()
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

        private string ExpectedSalaryLabel()
        {
            var baseSalary = 6000 + State.Worker.GetStat("logic") * 55 + State.Worker.GetStat("social") * 35 + State.Worker.GetStat("education") * 50;
            baseSalary = Mathf.RoundToInt(baseSalary / 500f) * 500;
            return "期望 " + Mathf.Max(4000, baseSalary - 2500) + "-" + Mathf.Max(6000, baseSalary + 3500);
        }

        private void RefreshSelectedTags(Transform screen)
        {
            var tagPool = FindResumeTagPool(screen);
            if (tagPool == null)
            {
                return;
            }

            resumeTagListView.TagPrefab = ResumeTagPrefab;
            resumeTagListView.Refresh(tagPool, Presenter, RefreshResume, () =>
            {
                ShowBanner("最多只能隐藏 3 个标签");
            });
        }

        private Transform FindResumeTagPool(Transform screen)
        {
            var tagPool = Find(screen, "标签池");
            if (tagPool != null)
            {
                return tagPool;
            }

            return Find(screen, "Selected Tags");
        }

        private int ActualResumeOptionIndex(string fieldId, int optionCount)
        {
            if (optionCount <= 0)
            {
                return 0;
            }

            int score;
            switch (fieldId)
            {
                case "education":
                    score = State.Worker.GetStat("education");
                    break;
                case "experience":
                    score = Mathf.RoundToInt((State.Worker.GetStat("logic") + State.Worker.GetStat("social") + State.Worker.GetStat("body")) / 3f);
                    break;
                case "ability":
                    score = Mathf.RoundToInt((State.Worker.GetStat("logic") + State.Worker.GetStat("social")) / 2f);
                    break;
                case "tags":
                    score = Mathf.Clamp(State.Worker.Tags.Count * 22, 0, 100);
                    break;
                case "salary":
                    score = Mathf.RoundToInt((State.Worker.GetStat("logic") + State.Worker.GetStat("social") + State.Worker.GetStat("education")) / 3f);
                    break;
                default:
                    score = 50;
                    break;
            }

            return Mathf.Clamp(score * optionCount / 101, 0, optionCount - 1);
        }

        private ResumePackagingMode ResumeModeFromComparison(int optionIndex, int actualIndex)
        {
            var delta = optionIndex - actualIndex;
            if (delta == 0)
            {
                return ResumePackagingMode.Normal;
            }

            if (delta < 0)
            {
                return ResumePackagingMode.Hide;
            }

            return delta >= 2 ? ResumePackagingMode.Fake : ResumePackagingMode.Exaggerate;
        }

        private void ApplyResumeChipStyle(Transform chip, int optionIndex, int actualIndex, bool selected)
        {
            var palette = PaperShiftResumeStyle.PaletteByComparison(optionIndex, actualIndex, selected);
            var graphic = chip.GetComponent<Graphic>();
            if (graphic != null)
            {
                graphic.color = palette.Background;
            }

            var outline = chip.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = palette.Border;
            }

            var text = FindText(chip, "Label");
            if (text != null)
            {
                text.color = palette.Text;
            }
        }

        private void RefreshJobSearch()
        {
            var gameplay = EnsureGameplayView();
            if (gameplay == null)
            {
                Debug.LogWarning("PaperShift gameplay view references are not assigned.");
                return;
            }

            if (!gameplay.IsComplete(out var missingField))
            {
                Debug.LogWarning("PaperShift gameplay view reference is missing: " + missingField, gameplay);
                return;
            }

            RefreshCandidateCard(gameplay.SelfCard, BuildWorkerCardData("求职者"));
            RefreshWorkerStatusTags(gameplay);
            RefreshCandidateCard(gameplay.JobCard, BuildInterviewCardData());
            SetGameplayActionsVisible(startInterview: true, reapply: true, startWork: false);
            RefreshCalendar(gameplay.Root);
        }

        private void RefreshFailure()
        {
            var gameplay = EnsureGameplayView();
            if (gameplay == null || CurrentScreen() != PaperShiftScreen.InterviewFailure)
            {
                return;
            }

            RefreshCandidateCard(gameplay.SelfCard, BuildWorkerCardData("求职者"));
            RefreshWorkerStatusTags(gameplay);
            RefreshCandidateCard(gameplay.JobCard, BuildInterviewCardData());
            SetGameplayActionsVisible(startInterview: false, reapply: true, startWork: false);
            RefreshCalendar(gameplay.Root);
        }

        private void RefreshWork()
        {
            var gameplay = EnsureGameplayView();
            if (gameplay == null || CurrentScreen() != PaperShiftScreen.Work)
            {
                return;
            }

            RefreshCandidateCard(gameplay.SelfCard, BuildWorkerCardData(State.CurrentJob.JobTitle));
            RefreshWorkerStatusTags(gameplay);
            RefreshCandidateCard(gameplay.JobCard, BuildJobCardData());
            SetGameplayActionsVisible(startInterview: false, reapply: false, startWork: true);
            RefreshCalendar(gameplay.Root);
        }

        private void PlayJobCardTransition(PaperShiftScreen screen)
        {
            var gameplay = EnsureGameplayView();
            if (State == null || gameplay == null)
            {
                return;
            }

            switch (screen)
            {
                case PaperShiftScreen.JobSearch:
                    ShowJobTransition(
                        gameplay.JobTransition,
                        "⌛",
                        "你进入了面试期",
                        State.Interview.Round <= 0 ? "正在联系面试官……" : "正在等待面试结果……",
                        PaperShiftTheme.Hex("#9ed8f7"));
                    break;
                case PaperShiftScreen.Work:
                    ShowJobTransition(
                        gameplay.JobTransition,
                        "⌛",
                        "你进入了打工期",
                        "正在适应新的工作节奏……",
                        PaperShiftTheme.Hex("#9fd9f3"));
                    break;
                case PaperShiftScreen.InterviewFailure:
                    ShowJobTransition(
                        gameplay.JobTransition,
                        "⌛",
                        "你进入了空窗期",
                        "正在重新联系公司……",
                        PaperShiftTheme.Hex("#a6dcf7"));
                    break;
            }
        }

        private void ShowJobTransition(PaperShiftJobCardTransition transition, string icon, string title, string detail, Color accent)
        {
            if (transition == null)
            {
                return;
            }

            transition.Show(icon, title, detail, accent);
        }

        private void BeginReapplyJobWithTransition()
        {
            if (reapplyJobRoutine != null)
            {
                return;
            }

            reapplyJobRoutine = StartCoroutine(ReapplyJobAfterTransition());
        }

        private IEnumerator ReapplyJobAfterTransition()
        {
            var gameplay = EnsureGameplayView();
            var transition = gameplay == null ? null : gameplay.JobTransition;

            if (transition != null && transition.ShowPreauthored())
            {
                SetGameplayActionsInteractable(false);
                yield return new WaitForSecondsRealtime(PaperShiftJobCardTransition.TotalSeconds);
                SetGameplayActionsInteractable(true);
            }

            var beforeReapplyScreen = CurrentScreen();
            Presenter.FindInterviewAndShow();
            suppressNextStateTransition = beforeReapplyScreen != CurrentScreen();
            RefreshAll();
            reapplyJobRoutine = null;
        }

        private void SetGameplayActionsInteractable(bool interactable)
        {
            var gameplay = EnsureGameplayView();
            if (gameplay == null)
            {
                return;
            }

            SetButtonInteractable(gameplay.StartInterviewButton, interactable);
            SetButtonInteractable(gameplay.ReapplyButton, interactable);
            SetButtonInteractable(gameplay.StartWorkButton, interactable);
            SetButtonInteractable(gameplay.JobProgressButton, interactable);
        }

        private void SetButtonInteractable(Button button, bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }

        private void SetGameplayActionsVisible(bool startInterview, bool reapply, bool startWork)
        {
            var gameplay = EnsureGameplayView();
            if (gameplay == null)
            {
                return;
            }

            SetButtonVisible(gameplay.StartInterviewButton, startInterview);
            SetButtonVisible(gameplay.ReapplyButton, reapply);
            SetButtonVisible(gameplay.StartWorkButton, startWork);
        }

        private void SetButtonVisible(Button button, bool visible)
        {
            if (button != null)
            {
                button.gameObject.SetActive(visible);
            }
        }

        private Transform GameplayScreen(PaperShiftScreen screen)
        {
            var view = Screen(screen);
            if (view != null)
            {
                return view;
            }

            return SceneController != null &&
                SceneController.CurrentScreen == screen &&
                (screen == PaperShiftScreen.InterviewFailure || screen == PaperShiftScreen.Work)
                ? Screen(PaperShiftScreen.JobSearch)
                : null;
        }

        private PaperShiftGameplayViewReferences ResolveGameplayView()
        {
            if (GameplayView != null)
            {
                return GameplayView;
            }

            if (SceneController == null || SceneController.ScreenViews == null)
            {
                return null;
            }

            for (var i = 0; i < SceneController.ScreenViews.Length; i++)
            {
                var screenView = SceneController.ScreenViews[i];
                if (screenView == null)
                {
                    continue;
                }

                var references = screenView.GetComponent<PaperShiftGameplayViewReferences>();
                if (references != null)
                {
                    return references;
                }
            }

            return null;
        }

        private PaperShiftGameplayViewReferences EnsureGameplayView()
        {
            if (GameplayView == null)
            {
                GameplayView = ResolveGameplayView();
            }

            return GameplayView;
        }

        private void RefreshBudget()
        {
            var screen = Screen(PaperShiftScreen.Budget);
            if (screen == null)
            {
                return;
            }

            SetCoin(screen, State.Worker.Money.ToString("N0"));
            SetSettlementValue(screen, "预计月薪", State.CurrentJob.Salary.ToString());
            SetSettlementValue(screen, "固定房租", EstimateRent().ToString());
            SetSettlementValue(screen, "可分配金额", Mathf.Max(0, State.CurrentJob.Salary - EstimateRent()).ToString());
            RefreshBudgetSlider(screen, "饮食", "food", State.Budget.Food);
            RefreshBudgetSlider(screen, "住房", "housing", State.Budget.Housing);
            RefreshBudgetSlider(screen, "恋爱", "romance", State.Budget.Romance);
            RefreshBudgetSlider(screen, "教育", "education", State.Budget.Education);
            RefreshBudgetSlider(screen, "存款", "savings", State.Budget.Savings);
            SetImpact(screen, 0, "恋爱事件\n<size=21><color=#ff4f8f>+" + Mathf.RoundToInt(State.Budget.Romance * 0.8f) + "%</color></size>");
            SetImpact(screen, 1, "结婚事件\n<size=21><color=#ff4f8f>+" + Mathf.RoundToInt(State.Budget.Romance * 0.45f) + "%</color></size>");
            SetImpact(screen, 2, "生子事件\n<size=21><color=#ff8a00>+" + Mathf.RoundToInt(State.Budget.Romance * 0.3f) + "%</color></size>");
            SetImpact(screen, 3, "后代成长\n<size=21><color=#249ee8>+" + Mathf.RoundToInt(State.Budget.Education * 0.75f) + "%</color></size>");
        }

        private void RefreshWorkerStatusTags(PaperShiftGameplayViewReferences gameplay)
        {
            if (gameplay == null || State == null || State.Worker == null)
            {
                return;
            }

            candidateTagGridView.TagPrefab = StatusTagPrefab;
            candidateTagGridView.EmptySlotPrefab = EmptySlotPrefab;
            candidateTagGridView.Refresh(gameplay.SelfTagsRoot, State.Worker.Tags);

            if (gameplay.SelfEventLog != null)
            {
                gameplay.SelfEventLog.gameObject.SetActive(false);
            }
        }

        private void RefreshNewsModal()
        {
            var screen = Screen(PaperShiftScreen.News);
            if (screen == null)
            {
                return;
            }

            var pending = Presenter.PendingEvent;
            SetText(Find(screen, "Modal Title"), "Label", pending == null ? "通知" : pending.Event.DisplayName);
            SetText(Find(screen, "Modal Text"), "Text", pending == null ? LastLogOr("暂时没有新的重要事件。") : pending.Event.Body);

            for (var i = 0; i < optionButtonNames.Length; i++)
            {
                var buttonRoot = Find(screen, optionButtonNames[i]);
                if (buttonRoot == null)
                {
                    continue;
                }

                if (pending == null || i >= pending.Options.Length)
                {
                    buttonRoot.gameObject.SetActive(i == 0);
                    SetButtonLabel(buttonRoot, i == 0 ? "继续" : string.Empty);
                    BindButton(buttonRoot, null, () =>
                    {
                        SceneController.ShowWork();
                        RefreshAll();
                    });
                    continue;
                }

                var optionIndex = i;
                buttonRoot.gameObject.SetActive(true);
                SetButtonLabel(buttonRoot, pending.Options[i].Label);
                BindButton(buttonRoot, null, () =>
                {
                    Presenter.ChoosePendingEventOption(optionIndex);
                    ShowLatestBanner("事件已结算。");
                    RefreshAll();
                });
            }
        }

        private void RefreshRetirement()
        {
            var screen = Screen(PaperShiftScreen.Retirement);
            if (screen == null)
            {
                return;
            }

            SetCoin(screen, State.Worker.Money.ToString("N0"));
            SetText(screen, "Reason Title", "退场原因：" + State.Retirement.ReasonText);
            SetSettlementValue(screen, "工作年限", State.Retirement.WorkYears + " 年");
            SetSettlementValue(screen, "最终职业", string.IsNullOrEmpty(State.Retirement.FinalJobTitle) ? "暂无" : State.Retirement.FinalJobTitle);
            SetSettlementValue(screen, "留下存款", State.Retirement.FinalSavings.ToString());
            SetSettlementValue(screen, "精神状态", State.Worker.Stress >= 70 ? "快撑不住" : "还能聊天");
        }

        private void AskInterviewResult()
        {
            if (Presenter.AskInterviewResult(out var message))
            {
                ShowBanner(message);
            }
            else
            {
                ShowBanner(message);
            }

            RefreshAll();
        }

        private CandidateUiData BuildWorkerCardData(string subtitleRole)
        {
            var worker = State.Worker;
            var rows = new List<UiPair>
            {
                new UiPair("体魄", Score(worker.GetStat("body"))),
                new UiPair("逻辑", worker.GetStat("logic").ToString()),
                new UiPair("学历", EducationLabel(worker)),
                new UiPair("家境", FamilyLabel(worker.GetStat("family"))),
                new UiPair("存款", worker.Money.ToString()),
                new UiPair("压力", worker.Stress.ToString())
            };

            return new CandidateUiData
            {
                Badge = worker.Age + "岁",
                Corner = "第" + State.Generation + "代",
                Name = worker.FullName,
                Subtitle = worker.Gender + " " + subtitleRole,
                RingText = "压力 " + worker.Stress,
                Rows = rows,
                Tags = new List<string>(),
                ProgressPercent = string.Empty,
                ProgressLabel = string.Empty,
                ProgressFill = 0f,
                Logs = LastLogs(3)
            };
        }

        private CandidateUiData BuildInterviewCardData()
        {
            var rows = new List<UiPair>
            {
                new UiPair("公司", EmptyFallback(State.Interview.CompanyName, "待投递")),
                new UiPair("岗位", EmptyFallback(State.Interview.JobTitle, "未知")),
                new UiPair("月薪", State.Interview.Salary.ToString()),
                new UiPair("轮次", State.Interview.Round + "/" + Mathf.Max(1, State.Interview.MaxRounds)),
                new UiPair("满意", State.Interview.Satisfaction.ToString()),
                new UiPair("风险", State.Resume.DeceptionRisk + "%")
            };

            return new CandidateUiData
            {
                Badge = Mathf.Max(1, State.Interview.Round + 1) + "轮",
                Corner = "面试",
                Name = EmptyFallback(State.Interview.CompanyName, "还没有公司"),
                Subtitle = EmptyFallback(State.Interview.JobTitle, "先投递简历"),
                RingText = "满意 " + State.Interview.Satisfaction,
                Rows = rows,
                Tags = InterviewTags(),
                ProgressPercent = State.Interview.Satisfaction + "%",
                ProgressLabel = State.Interview.HasOffer ? "可入职" : "面试满意度",
                ProgressFill = State.Interview.Satisfaction / 100f
            };
        }

        private CandidateUiData BuildJobCardData()
        {
            var rows = new List<UiPair>
            {
                new UiPair("公司", EmptyFallback(State.CurrentJob.CompanyName, "暂无")),
                new UiPair("岗位", EmptyFallback(State.CurrentJob.JobTitle, "暂无")),
                new UiPair("月薪", State.CurrentJob.Salary.ToString()),
                new UiPair("年限", State.CurrentJob.WorkYears + " 年"),
                new UiPair("升职", State.CurrentJob.PromotionProgress + "%"),
                new UiPair("离职", State.CurrentJob.QuitRisk + "%")
            };

            return new CandidateUiData
            {
                Badge = State.HasActiveJob ? "在职" : "待业",
                Corner = "岗位",
                Name = EmptyFallback(State.CurrentJob.CompanyName, "暂无工作"),
                Subtitle = EmptyFallback(State.CurrentJob.JobTitle, "去投递简历"),
                RingText = "强度 " + State.CurrentJob.Intensity,
                Rows = rows,
                Tags = CurrentJobTags(),
                ProgressPercent = State.CurrentJob.PromotionProgress + "%",
                ProgressLabel = State.CurrentJob.QuitRisk > State.CurrentJob.PromotionProgress ? "离职风险" : "升职进度",
                ProgressFill = Mathf.Clamp01((State.CurrentJob.QuitRisk > State.CurrentJob.PromotionProgress ? State.CurrentJob.QuitRisk : State.CurrentJob.PromotionProgress) / 100f)
            };
        }

        private void RefreshCandidateCard(Transform card, CandidateUiData data)
        {
            if (card == null || data == null)
            {
                return;
            }

            SetText(Find(card, "Badge"), "Text", data.Badge);
            SetText(card, "Corner", data.Corner);
            SetText(Find(card, "Top Info"), "Name", data.Name + "\n<size=18>" + data.Subtitle + "</size>");
            SetText(Find(card, "Ring Label"), "Text", data.RingText);

            var miniGrid = Find(card, "Mini Grid");
            if (miniGrid != null)
            {
                var rowIndex = 0;
                for (var i = 0; i < miniGrid.childCount && rowIndex < data.Rows.Count; i++)
                {
                    var row = miniGrid.GetChild(i);
                    if (!row.name.StartsWith("Info "))
                    {
                        continue;
                    }

                    row.gameObject.SetActive(true);
                    SetText(row, "Label", data.Rows[rowIndex].Label);
                    SetText(row, "Value", data.Rows[rowIndex].Value);
                    rowIndex++;
                }
            }

            var tags = Find(card, "Tags");
            if (tags != null)
            {
                var ticketIndex = 0;
                for (var i = 0; i < tags.childCount; i++)
                {
                    var ticket = tags.GetChild(i);
                    if (!ticket.name.StartsWith("Ticket"))
                    {
                        continue;
                    }

                    if (ticketIndex >= data.Tags.Count)
                    {
                        ticket.gameObject.SetActive(false);
                    }
                    else
                    {
                        ticket.gameObject.SetActive(true);
                        SetTicketText(ticket, data.Tags[ticketIndex]);
                    }

                    ticketIndex++;
                }
            }

            var footer = Find(card, "Progress Footer");
            if (footer != null && !string.IsNullOrEmpty(data.ProgressPercent))
            {
                SetText(footer, "Percent", "<size=32>" + data.ProgressPercent + "</size> " + data.ProgressLabel);
                var fill = Find(footer, "Fill");
                if (fill is RectTransform rect)
                {
                    rect.anchorMax = new Vector2(Mathf.Clamp01(data.ProgressFill), 1f);
                }

                SetButtonLabel(Find(footer, "Progress Button"), State.Phase == PaperShiftPhase.Interview ? "询问结果" : "预算");
            }

            var log = Find(card, "Event Log");
            if (log != null && data.Logs != null)
            {
                var logIndex = 0;
                for (var i = 0; i < log.childCount; i++)
                {
                    var line = log.GetChild(i);
                    if (logIndex >= data.Logs.Count)
                    {
                        line.gameObject.SetActive(false);
                    }
                    else
                    {
                        line.gameObject.SetActive(true);
                        SetText(line, "Text", data.Logs[logIndex]);
                    }

                    logIndex++;
                }
            }
        }

        private void RefreshBudgetSlider(Transform screen, string label, string budgetId, int value)
        {
            var row = Find(screen, "Budget " + label);
            if (row == null)
            {
                return;
            }

            SetText(row, "Value", value + "%");
            var slider = row.GetComponentInChildren<Slider>(true);
            if (slider == null)
            {
                return;
            }

            slider.onValueChanged.RemoveAllListeners();
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(next =>
            {
                Presenter.SetBudgetCategory(budgetId, Mathf.RoundToInt(next));
                SetText(row, "Value", Mathf.RoundToInt(next) + "%");
                RefreshBudget();
            });
        }

        private void BindButton(Transform root, string buttonName, UnityAction action, string label = null)
        {
            if (root == null)
            {
                return;
            }

            var target = string.IsNullOrEmpty(buttonName) ? root : Find(root, buttonName);
            if (target == null)
            {
                return;
            }

            var button = target.GetComponent<Button>();
            if (button == null)
            {
                button = target.gameObject.AddComponent<Button>();
                button.targetGraphic = target.GetComponent<Graphic>();
            }

            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(action);
            if (label != null)
            {
                SetButtonLabel(target, label);
            }
        }

        private void BindButton(Button button, UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(action);
        }

        private Transform Screen(PaperShiftScreen screen)
        {
            if (SceneController == null || SceneController.ScreenViews == null)
            {
                return null;
            }

            for (var i = 0; i < SceneController.ScreenViews.Length; i++)
            {
                var view = SceneController.ScreenViews[i];
                if (view != null && view.Screen == screen)
                {
                    return view.transform;
                }
            }

            return null;
        }

        private PaperShiftScreen CurrentScreen()
        {
            if (SceneController != null)
            {
                return SceneController.CurrentScreen;
            }

            if (SceneController == null || SceneController.ScreenViews == null)
            {
                return activeScreen;
            }

            for (var i = 0; i < SceneController.ScreenViews.Length; i++)
            {
                var view = SceneController.ScreenViews[i];
                if (view != null && view.gameObject.activeSelf)
                {
                    return view.Screen;
                }
            }

            return activeScreen;
        }

        private void SetTagRow(Transform row, TagDefinition tag, bool selected)
        {
            var graphic = row.GetComponent<Graphic>();
            if (graphic != null)
            {
                graphic.color = selected ? PaperShiftTheme.Hex("#e9f7ff") : PaperShiftTheme.White;
            }

            var outline = row.GetComponent<Outline>();
            if (outline == null)
            {
                outline = row.gameObject.AddComponent<Outline>();
            }

            outline.enabled = selected;
            outline.effectColor = PaperShiftTheme.Blue;
            outline.effectDistance = new Vector2(2f, -2f);
            SetText(row, "Description", tag.Description);
            SetTicketText(row, tag.DisplayName);
            var badge = Find(row, "Selected Badge");
            if (badge != null)
            {
                badge.gameObject.SetActive(selected);
            }
        }

        private void SetSelectableGraphic(Transform root, bool active)
        {
            if (root == null)
            {
                return;
            }

            var graphic = root.GetComponent<Graphic>();
            if (graphic != null)
            {
                graphic.color = active ? PaperShiftTheme.BlueLight : PaperShiftTheme.Hex("#f4f8fb");
            }

            var outline = root.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = active ? PaperShiftTheme.Blue : PaperShiftTheme.Hex("#d7e0e6");
            }
        }

        private void SetInfoValue(Transform root, string label, string value)
        {
            SetText(Find(root, "Info " + label), "Value", value);
        }

        private void SetNameRow(Transform root, string label, string value)
        {
            SetText(Find(root, "Name Row " + label), "Value", value);
        }

        private void SetSettlementValue(Transform root, string label, string value)
        {
            SetText(Find(root, "Settlement " + label), "Value", value);
        }

        private void SetImpact(Transform root, int index, string text)
        {
            var grid = Find(root, "Impact Grid");
            if (grid == null || index < 0 || index >= grid.childCount)
            {
                return;
            }

            SetText(grid.GetChild(index), "Label", text);
        }

        private void SetCoin(Transform root, string value)
        {
            SetText(Find(root, "Coin"), "Amount", value);
        }

        private void SetTitle(Transform root, string value)
        {
            SetText(Find(root, "Topline"), "Title", value);
        }

        private void SetButtonLabel(Transform buttonRoot, string value)
        {
            SetText(buttonRoot, "Label", value);
        }

        private void SetTicketText(Transform root, string value)
        {
            SetText(root, "Label", value);
        }

        private void SetText(Transform root, string childName, string value)
        {
            var text = FindText(root, childName);
            if (text != null)
            {
                text.text = value;
            }
        }

        private Text FindText(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == childName)
            {
                var selfText = root.GetComponent<Text>();
                if (selfText != null)
                {
                    return selfText;
                }
            }

            var target = Find(root, childName);
            if (target == null)
            {
                return null;
            }

            var text = target.GetComponent<Text>();
            if (text != null)
            {
                return text;
            }

            var nested = Find(target, "Text");
            if (nested != null && nested.GetComponent<Text>() != null)
            {
                return nested.GetComponent<Text>();
            }

            nested = Find(target, "Label");
            return nested == null ? null : nested.GetComponent<Text>();
        }

        private Transform Find(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name))
            {
                return null;
            }

            if (root.name == name)
            {
                return root;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var result = Find(root.GetChild(i), name);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private void RefreshCalendar(Transform screen)
        {
            SetText(Find(screen, "Calendar"), "Text", State.CurrentYear.ToString());
            SetText(Find(screen, "Calendar"), "Month", "1");
        }

        private List<string> InterviewTags()
        {
            var tags = new List<string>();
            var company = Database.FindCompany(State.Interview.CompanyId);
            var job = Database.FindJob(State.Interview.CompanyId, State.Interview.JobId);
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
            var job = Database.FindJob(State.CurrentJob.CompanyId, State.CurrentJob.JobId);
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

        private string LastLogOr(string fallback)
        {
            return State.Logs.Count == 0 ? fallback : State.Logs[State.Logs.Count - 1].Text;
        }

        private void ShowLatestBanner(string fallback)
        {
            ShowBanner(State.Banners.Count == 0 ? fallback : State.Banners[State.Banners.Count - 1]);
        }

        private void ShowBanner(string text)
        {
            if (bannerRoot == null || bannerText == null)
            {
                return;
            }

            bannerText.text = text;
            bannerRoot.gameObject.SetActive(true);
            bannerRoot.SetAsLastSibling();
            bannerHideAt = Time.unscaledTime + 2.4f;
        }

        private void CreateBanner()
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                return;
            }

            var root = new GameObject("Runtime Banner", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            bannerRoot = root.GetComponent<RectTransform>();
            bannerRoot.anchorMin = new Vector2(0.5f, 1f);
            bannerRoot.anchorMax = new Vector2(0.5f, 1f);
            bannerRoot.pivot = new Vector2(0.5f, 1f);
            bannerRoot.anchoredPosition = new Vector2(0f, -126f);
            bannerRoot.sizeDelta = new Vector2(408f, 86f);
            var graphic = root.AddComponent<RoundedRectGraphic>();
            graphic.color = PaperShiftTheme.Hex("#263746", 0.94f);
            graphic.CornerRadius = 18f;

            var textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(root.transform, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(18f, 8f);
            textRect.offsetMax = new Vector2(-18f, -8f);
            bannerText = textObject.AddComponent<Text>();
            bannerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            bannerText.fontSize = 20;
            bannerText.alignment = TextAnchor.MiddleCenter;
            bannerText.color = Color.white;
            bannerText.supportRichText = true;
            bannerRoot.gameObject.SetActive(false);
        }

        private int EstimateRent()
        {
            return Mathf.Max(1200, State.CurrentJob.Salary * 25 / 100);
        }

        private string WorkerTagSummary(WorkerProfile worker)
        {
            if (worker.Tags == null || worker.Tags.Count == 0)
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
    }
}
