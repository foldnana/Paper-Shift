using System.Collections.Generic;
using PaperShift.Controller;
using PaperShift.Model;
using PaperShift.Presenter;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PaperShift.Editor
{
    public static class PaperShiftSceneReferenceInstaller
    {
        [MenuItem("Paper Shift/Install Scene View References")]
        public static void InstallCurrentSceneReferences()
        {
            var controller = Object.FindObjectOfType<PaperShiftSceneController>();
            if (controller == null)
            {
                Debug.LogWarning("Paper Shift scene reference install skipped: no PaperShiftSceneController found.");
                return;
            }

            var host = GetOrAdd<PaperShiftPrototypeBinder>(controller.gameObject);
            host.SceneController = controller;
            host.Presenter = GetOrAdd<PaperShiftGamePresenter>(controller.gameObject);

            var binders = new List<PaperShiftScreenBinderBase>();
            AddIfNotNull(binders, InstallCreate(controller));
            AddIfNotNull(binders, InstallTags(controller));
            AddIfNotNull(binders, InstallResume(controller));
            var gameplay = InstallGameplay(controller, host);
            AddIfNotNull(binders, gameplay);
            AddIfNotNull(binders, InstallBudget(controller));
            AddIfNotNull(binders, InstallNews(controller));
            AddIfNotNull(binders, InstallRetirement(controller));

            host.ScreenBinders = binders.ToArray();
            host.GameplayBinder = gameplay;
            host.GameplayView = gameplay == null ? null : gameplay.GameplayView;
            var bannerRoot = Find(Screen(controller, PaperShiftScreen.JobSearch), "提示信息");
            host.BannerRoot = bannerRoot as RectTransform;
            host.BannerText = TextUnder(bannerRoot, "Text");

            EditorUtility.SetDirty(host);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("Paper Shift scene view references installed.");
        }

        private static PaperShiftCreateScreenBinder InstallCreate(PaperShiftSceneController controller)
        {
            var root = Screen(controller, PaperShiftScreen.Create);
            if (root == null)
            {
                return null;
            }

            var binder = GetOrAdd<PaperShiftCreateScreenBinder>(root.gameObject);
            binder.Screen = PaperShiftScreen.Create;
            binder.Texts = new[]
            {
                TextBinding("gender", TextUnder(root, "Info 性别", "Value")),
                TextBinding("personality", TextUnder(root, "Info 性格", "Value")),
                TextBinding("era", TextUnder(root, "Info 年代", "Value")),
                TextBinding("age", TextUnder(root, "Info 年龄", "Value")),
                TextBinding("height", TextUnder(root, "Info 身高", "Value")),
                TextBinding("appearance", TextUnder(root, "Info 形象", "Value")),
                TextBinding("education", TextUnder(root, "Info 教育", "Value")),
                TextBinding("family", TextUnder(root, "Info 家境", "Value")),
                TextBinding("major", TextUnder(root, "Info 专业", "Value")),
                TextBinding("ability", TextUnder(root, "Info 能力", "Value")),
                TextBinding("advantage", TextUnder(root, "Info 优势", "Value")),
                TextBinding("asset", TextUnder(root, "Info 资产", "Value")),
                TextBinding("lastName", TextUnder(root, "Name Row 姓", "Value")),
                TextBinding("firstName", TextUnder(root, "Name Row 名", "Value")),
                TextBinding("coin", TextUnder(root, "Coin", "Amount"))
            };

            var eraIds = new[] { "timeline", "timeline", "timeline", "timeline", "timeline", "timeline" };
            var eraPicker = Find(root, "Era Picker");
            var tiles = new List<PaperShiftEraTileBinding>();
            for (var i = 0; eraPicker != null && i < eraPicker.childCount && i < eraIds.Length; i++)
            {
                var tile = eraPicker.GetChild(i);
                tiles.Add(new PaperShiftEraTileBinding
                {
                    EraId = eraIds[i],
                    Button = GetOrAdd<Button>(tile.gameObject),
                    Background = tile.GetComponent<Graphic>(),
                    Label = TextUnder(tile, "Label")
                });
            }

            binder.EraTiles = tiles.ToArray();
            binder.RandomButton = Button(root, "Random Button");
            binder.RandomNameButton = Button(root, "Random Name Button");
            binder.PlayButton = Button(root, "Play Button");
            binder.CustomButton = Button(root, "Custom Button");
            binder.NextButton = Button(root, "Next Button");
            Dirty(binder);
            return binder;
        }

        private static PaperShiftTagsScreenBinder InstallTags(PaperShiftSceneController controller)
        {
            var root = Screen(controller, PaperShiftScreen.Tags);
            if (root == null)
            {
                return null;
            }

            var binder = GetOrAdd<PaperShiftTagsScreenBinder>(root.gameObject);
            binder.Screen = PaperShiftScreen.Tags;
            binder.TitleText = TextUnder(root, "Topline", "Title");
            binder.CoinText = TextUnder(root, "Coin", "Amount");
            binder.TagListRoot = Find(root, "Tag List");
            binder.FreeRefreshButton = Button(root, "Free Refresh");
            binder.SuperRefreshButton = Button(root, "Super Refresh");
            binder.ConfirmButton = Button(root, "Confirm Tags Button");
            binder.ConfirmLabel = TextUnder(Find(root, "Confirm Tags Button"), "Label");
            Dirty(binder);
            return binder;
        }

        private static PaperShiftResumeScreenBinder InstallResume(PaperShiftSceneController controller)
        {
            var root = Screen(controller, PaperShiftScreen.Resume);
            if (root == null)
            {
                return null;
            }

            var binder = GetOrAdd<PaperShiftResumeScreenBinder>(root.gameObject);
            binder.Screen = PaperShiftScreen.Resume;
            binder.CoinText = TextUnder(root, "Coin", "Amount");
            binder.HeaderNameText = TextUnder(root, "Header", "Name");
            binder.GenerationText = TextUnder(root, "Generation Badge", "Label");
            binder.RiskText = TextUnder(root, "Resume Risk Footer", "Count");
            binder.SendResumeButton = Button(root, "Send Resume Button");
            binder.TagPoolRoot = Find(root, "标签池") ?? Find(root, "Selected Tags");

            var intentIds = new[] { "ai_intent", "remote_first", "salary_high" };
            var intentRoot = Find(root, "Intent Tags");
            var intents = new List<PaperShiftSelectableTextBinding>();
            for (var i = 0; intentRoot != null && i < intentRoot.childCount && i < intentIds.Length; i++)
            {
                var item = intentRoot.GetChild(i);
                intents.Add(new PaperShiftSelectableTextBinding
                {
                    Id = intentIds[i],
                    Button = GetOrAdd<Button>(item.gameObject),
                    Background = item.GetComponent<Graphic>(),
                    Outline = item.GetComponent<Outline>(),
                    Label = TextUnder(item, "Label")
                });
            }

            binder.IntentButtons = intents.ToArray();
            binder.ResumeLines = InstallResumeLines(root);
            Dirty(binder);
            return binder;
        }

        private static PaperShiftGameplayScreenBinder InstallGameplay(PaperShiftSceneController controller, PaperShiftPrototypeBinder host)
        {
            var root = Screen(controller, PaperShiftScreen.JobSearch);
            if (root == null)
            {
                return null;
            }

            var binder = GetOrAdd<PaperShiftGameplayScreenBinder>(root.gameObject);
            binder.Screen = PaperShiftScreen.JobSearch;
            binder.GameplayView = root.GetComponent<PaperShiftGameplayViewReferences>();
            if (binder.GameplayView != null)
            {
                binder.GameplayView.CalendarYearText = TextUnder(root, "Calendar", "Text");
                binder.GameplayView.CalendarMonthText = TextUnder(root, "Calendar", "Month");
                binder.GameplayView.SelfEventLog = Find(binder.GameplayView.SelfCard, "Event Log") ?? Find(root, "Event Log");
                binder.SelfCardView = InstallCandidateCard(binder.GameplayView.SelfCard);
                binder.JobCardView = InstallCandidateCard(binder.GameplayView.JobCard);
                binder.BottomStatusBar = InstallBottomStatusBar(root);
                binder.GameplayView.BottomStatusBar = binder.BottomStatusBar;
                host.GameplayView = binder.GameplayView;
                Dirty(binder.GameplayView);
            }

            Dirty(binder);
            return binder;
        }

        private static PaperShiftBudgetScreenBinder InstallBudget(PaperShiftSceneController controller)
        {
            var root = Screen(controller, PaperShiftScreen.Budget);
            if (root == null)
            {
                return null;
            }

            var binder = GetOrAdd<PaperShiftBudgetScreenBinder>(root.gameObject);
            binder.Screen = PaperShiftScreen.Budget;
            binder.CoinText = TextUnder(root, "Coin", "Amount");
            binder.SaveButton = Button(root, "Save Budget Button");
            binder.SettlementTexts = new[]
            {
                TextBinding("salary", TextUnder(root, "Settlement 预计月薪", "Value")),
                TextBinding("rent", TextUnder(root, "Settlement 固定房租", "Value")),
                TextBinding("distributable", TextUnder(root, "Settlement 可分配金额", "Value"))
            };
            binder.BudgetRows = new[]
            {
                BudgetRow("food", root, "Budget 饮食"),
                BudgetRow("housing", root, "Budget 住房"),
                BudgetRow("romance", root, "Budget 恋爱"),
                BudgetRow("education", root, "Budget 教育"),
                BudgetRow("savings", root, "Budget 存款")
            };

            var impactRoot = Find(root, "Impact Grid");
            var impacts = new List<Text>();
            for (var i = 0; impactRoot != null && i < impactRoot.childCount; i++)
            {
                impacts.Add(TextUnder(impactRoot.GetChild(i), "Label"));
            }

            binder.ImpactTexts = impacts.ToArray();
            Dirty(binder);
            return binder;
        }

        private static PaperShiftNewsScreenBinder InstallNews(PaperShiftSceneController controller)
        {
            var root = Screen(controller, PaperShiftScreen.News);
            if (root == null)
            {
                return null;
            }

            var binder = GetOrAdd<PaperShiftNewsScreenBinder>(root.gameObject);
            binder.Screen = PaperShiftScreen.News;
            binder.TitleText = TextUnder(root, "Modal Title", "Label");
            binder.BodyText = TextUnder(root, "Modal Text", "Text");
            binder.OptionButtons = new[]
            {
                ButtonBinding("apply", root, "Option Apply"),
                ButtonBinding("stay", root, "Option Stay"),
                ButtonBinding("save", root, "Option Save")
            };
            Dirty(binder);
            return binder;
        }

        private static PaperShiftRetirementScreenBinder InstallRetirement(PaperShiftSceneController controller)
        {
            var root = Screen(controller, PaperShiftScreen.Retirement);
            if (root == null)
            {
                return null;
            }

            var binder = GetOrAdd<PaperShiftRetirementScreenBinder>(root.gameObject);
            binder.Screen = PaperShiftScreen.Retirement;
            binder.CoinText = TextUnder(root, "Coin", "Amount");
            binder.ReasonTitleText = TextUnder(root, "Reason Title");
            binder.FinishButton = Button(root, "Finish Retirement Button");
            binder.SettlementTexts = new[]
            {
                TextBinding("workYears", TextUnder(root, "Settlement 工作年限", "Value")),
                TextBinding("finalJob", TextUnder(root, "Settlement 最终职业", "Value")),
                TextBinding("savings", TextUnder(root, "Settlement 留下存款", "Value")),
                TextBinding("stress", TextUnder(root, "Settlement 精神状态", "Value"))
            };
            Dirty(binder);
            return binder;
        }

        private static PaperShiftResumeLineBinding[] InstallResumeLines(Transform root)
        {
            var fieldIds = new[] { "education", "experience", "ability", "tags", "salary" };
            var linesRoot = Find(root, "Resume Lines");
            var lines = new List<PaperShiftResumeLineBinding>();
            for (var i = 0; linesRoot != null && i < linesRoot.childCount && i < fieldIds.Length; i++)
            {
                var line = linesRoot.GetChild(i);
                var actionsRoot = Find(line, "Actions");
                var options = new List<PaperShiftResumeOptionBinding>();
                for (var actionIndex = 0; actionsRoot != null && actionIndex < actionsRoot.childCount; actionIndex++)
                {
                    var action = actionsRoot.GetChild(actionIndex);
                    options.Add(new PaperShiftResumeOptionBinding
                    {
                        Button = GetOrAdd<Button>(action.gameObject),
                        Background = action.GetComponent<Graphic>(),
                        Outline = action.GetComponent<Outline>(),
                        Label = TextUnder(action, "Label")
                    });
                }

                lines.Add(new PaperShiftResumeLineBinding
                {
                    FieldId = fieldIds[i],
                    Value = TextUnder(line, "Value"),
                    Options = options.ToArray()
                });
            }

            return lines.ToArray();
        }

        private static PaperShiftCandidateCardView InstallCandidateCard(Transform card)
        {
            if (card == null)
            {
                return null;
            }

            var view = GetOrAdd<PaperShiftCandidateCardView>(card.gameObject);
            view.BadgeText = TextUnder(Find(card, "Badge"), "Text");
            view.CornerText = TextUnder(card, "Corner");
            view.TopInfoText = TextUnder(Find(card, "Top Info"), "Name");
            view.RingLabelText = TextUnder(Find(card, "Ring Label"), "Text");
            view.Rows = CardRows(card);
            view.Tags = CardTags(card);
            var footer = Find(card, "Progress Footer");
            view.ProgressText = TextUnder(footer, "Percent");
            view.ProgressFill = Find(footer, "Fill") as RectTransform;
            view.LogLines = CardLogLines(card);
            Dirty(view);
            return view;
        }

        private static PaperShiftBottomStatusBarView InstallBottomStatusBar(Transform root)
        {
            var statusRoot = Find(root, "底部状态栏");
            if (statusRoot == null)
            {
                return null;
            }

            var view = GetOrAdd<PaperShiftBottomStatusBarView>(statusRoot.gameObject);
            view.WorkStatus = StatusItem(Find(statusRoot, "工作状态"));
            view.LayoffStatus = StatusItem(Find(statusRoot, "裁员状态"));
            view.InterviewStatus = StatusItem(Find(statusRoot, "面试状态"));
            Dirty(view);
            return view;
        }

        private static PaperShiftBottomStatusItemBinding StatusItem(Transform root)
        {
            var progressBar = Find(root, "Progress Bar");
            return new PaperShiftBottomStatusItemBinding
            {
                PercentText = TextUnder(root, "Percent"),
                ProgressBarRoot = progressBar == null ? null : progressBar.gameObject,
                Fill = Find(progressBar, "Fill") as RectTransform,
                ActionButton = Button(root, "Progress Button")
            };
        }

        private static PaperShiftCardRowBinding[] CardRows(Transform card)
        {
            var root = Find(card, "Mini Grid");
            var rows = new List<PaperShiftCardRowBinding>();
            for (var i = 0; root != null && i < root.childCount; i++)
            {
                var row = root.GetChild(i);
                if (!row.name.StartsWith("Info "))
                {
                    continue;
                }

                rows.Add(new PaperShiftCardRowBinding
                {
                    Label = TextUnder(row, "Label"),
                    Value = TextUnder(row, "Value")
                });
            }

            return rows.ToArray();
        }

        private static PaperShiftTagSlotBinding[] CardTags(Transform card)
        {
            var root = Find(card, "Tags");
            var tags = new List<PaperShiftTagSlotBinding>();
            for (var i = 0; root != null && i < root.childCount; i++)
            {
                var ticket = root.GetChild(i);
                if (!ticket.name.StartsWith("Ticket"))
                {
                    continue;
                }

                tags.Add(new PaperShiftTagSlotBinding
                {
                    Root = ticket.gameObject,
                    Label = TextUnder(ticket, "Label")
                });
            }

            return tags.ToArray();
        }

        private static Text[] CardLogLines(Transform card)
        {
            var root = Find(card, "Event Log");
            var texts = new List<Text>();
            for (var i = 0; root != null && i < root.childCount; i++)
            {
                texts.Add(TextUnder(root.GetChild(i), "Text"));
            }

            return texts.ToArray();
        }

        private static PaperShiftBudgetRowBinding BudgetRow(string id, Transform root, string rowName)
        {
            var row = Find(root, rowName);
            return new PaperShiftBudgetRowBinding
            {
                BudgetId = id,
                Slider = row == null ? null : row.GetComponentInChildren<Slider>(true),
                ValueText = TextUnder(row, "Value")
            };
        }

        private static PaperShiftButtonBinding ButtonBinding(string id, Transform root, string buttonName)
        {
            var buttonRoot = Find(root, buttonName);
            return new PaperShiftButtonBinding
            {
                Id = id,
                Button = buttonRoot == null ? null : GetOrAdd<Button>(buttonRoot.gameObject),
                Label = TextUnder(buttonRoot, "Label")
            };
        }

        private static PaperShiftTextBinding TextBinding(string id, Text text)
        {
            return new PaperShiftTextBinding { Id = id, Text = text };
        }

        private static Button Button(Transform root, string name)
        {
            var target = Find(root, name);
            return target == null ? null : GetOrAdd<Button>(target.gameObject);
        }

        private static Text TextUnder(Transform root, string name)
        {
            var target = Find(root, name);
            if (target == null)
            {
                return null;
            }

            return target.GetComponent<Text>();
        }

        private static Text TextUnder(Transform root, string containerName, string childName)
        {
            return TextUnder(Find(root, containerName), childName);
        }

        private static Transform Screen(PaperShiftSceneController controller, PaperShiftScreen screen)
        {
            if (controller == null || controller.ScreenViews == null)
            {
                return null;
            }

            for (var i = 0; i < controller.ScreenViews.Length; i++)
            {
                var view = controller.ScreenViews[i];
                if (view != null && view.Screen == screen)
                {
                    return view.transform;
                }
            }

            return null;
        }

        private static Transform Find(Transform root, string name)
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
                var found = Find(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            return component == null ? target.AddComponent<T>() : component;
        }

        private static void AddIfNotNull(ICollection<PaperShiftScreenBinderBase> list, PaperShiftScreenBinderBase value)
        {
            if (value != null)
            {
                list.Add(value);
            }
        }

        private static void Dirty(Object target)
        {
            if (target != null)
            {
                EditorUtility.SetDirty(target);
            }
        }
    }
}
