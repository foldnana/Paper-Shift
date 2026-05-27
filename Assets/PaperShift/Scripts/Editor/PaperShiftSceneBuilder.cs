using System.Collections.Generic;
using PaperShift.Controller;
using PaperShift.Model;
using PaperShift.Presenter;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PaperShift.Editor
{
    public static class PaperShiftSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/PaperShiftUI.unity";
        private const string UiFontPath = "Assets/PaperShift/Fonts/NotoSansSC-VF.ttf";
        private const float Width = PaperShiftTheme.ReferenceWidth;
        private const float Height = PaperShiftTheme.ReferenceHeight;
        private const float PageWidth = 441f;
        private const float MarginX = 62f;

        private static Font defaultFont;
        private static PaperShiftSceneController controller;
        private static readonly List<PaperShiftScreenView> screenViews = new List<PaperShiftScreenView>();

        [MenuItem("Paper Shift/Rebuild Paper Shift UI Scene")]
        public static void Build()
        {
            defaultFont = AssetDatabase.LoadAssetAtPath<Font>(UiFontPath);
            if (defaultFont == null)
            {
                defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            var model = PaperShiftGameModel.CreatePrototype();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "PaperShiftUI";
            screenViews.Clear();

            CreateEventSystem();
            CreateCamera();

            var canvas = CreateCanvas();
            controller = new GameObject("UI Controller").AddComponent<PaperShiftSceneController>();

            BuildCreateScreen(canvas.transform, model);
            BuildTagsScreen(canvas.transform, model);
            BuildResumeScreen(canvas.transform, model);
            BuildJobSearchScreen(canvas.transform, model);
            BuildFailureScreen(canvas.transform, model);
            BuildWorkScreen(canvas.transform, model);
            BuildBudgetScreen(canvas.transform, model);
            BuildNewsScreen(canvas.transform, model);
            BuildRetirementScreen(canvas.transform, model);

            controller.ScreenViews = screenViews.ToArray();
            controller.InitialScreen = PaperShiftScreen.Create;
            foreach (var view in screenViews)
            {
                view.gameObject.SetActive(view.Screen == PaperShiftScreen.Create);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EnsureSceneInBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Paper Shift UI scene rebuilt: " + ScenePath);
        }

        private static void CreateCamera()
        {
            var cameraObject = new GameObject("UI Preview Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = PaperShiftTheme.Sky;
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            cameraObject.tag = "MainCamera";
        }

        private static void CreateEventSystem()
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private static Canvas CreateCanvas()
        {
            var canvasObject = new GameObject("PaperShiftUI_Canvas", typeof(RectTransform));
            SetLayerRecursive(canvasObject, LayerMask.NameToLayer("UI"));

            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(Width, Height);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            canvasObject.AddComponent<GraphicRaycaster>();

            var root = CreateRect(canvasObject.transform, "Game Frame");
            Stretch(root, 0f, 0f, 0f, 0f);

            return canvas;
        }

        private static PaperShiftScreenView CreateScreen(Transform canvasTransform, string name, PaperShiftScreen screen, Color background)
        {
            var screenObject = CreateRect(canvasTransform, name);
            Stretch(screenObject, 0f, 0f, 0f, 0f);
            var graphic = screenObject.gameObject.AddComponent<RoundedRectGraphic>();
            graphic.color = background;
            graphic.CornerRadius = 0f;

            var view = screenObject.gameObject.AddComponent<PaperShiftScreenView>();
            view.Screen = screen;
            screenViews.Add(view);
            return view;
        }

        private static void BuildCreateScreen(Transform canvasTransform, PaperShiftGameModel model)
        {
            var view = CreateScreen(canvasTransform, "Screen_CreateLaborer", PaperShiftScreen.Create, PaperShiftTheme.PageBlue);
            AddTopLine(view.transform, "创建劳动者", model.Worker.Coin, "‹", null);

            var scrollContent = CreateScroll(view.transform, "Create Scroll", 92f, 704f, PageWidth);
            var createCard = CreatePaperCard(scrollContent, "Create Worker Card", PageWidth, 672f, true);

            var avatar = CreateAvatarLock(createCard.transform, new Vector2(334f, -170f), PortraitKind.Worker, true);
            avatar.SetAsLastSibling();

            var eraGrid = CreateRect(createCard.transform, "Era Picker");
            AnchorTopLeft(eraGrid, PageWidth - 28f, 132f, 14f, 18f);
            var eraLayout = eraGrid.gameObject.AddComponent<GridLayoutGroup>();
            eraLayout.cellSize = new Vector2((PageWidth - 28f - 16f) / 3f, 62f);
            eraLayout.spacing = new Vector2(8f, 8f);
            eraLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            eraLayout.constraintCount = 3;

            for (var i = 0; i < model.Eras.Count; i++)
            {
                var tile = CreateRounded(eraGrid, "Era_" + model.Eras[i].Replace("\n", "_"), i == 2 ? PaperShiftTheme.BlueLight : PaperShiftTheme.White, 14f);
                tile.gameObject.AddComponent<LayoutElement>().preferredHeight = 62f;
                AddOutline(tile.gameObject, i == 2 ? PaperShiftTheme.Blue : PaperShiftTheme.Hex("#c8e9fb"), 2f);
                var text = AddText(tile, "Label", model.Eras[i], 14, i == 2 ? Color.white : PaperShiftTheme.Hex("#3a4350"), TextAnchor.MiddleCenter);
                text.resizeTextForBestFit = false;
            }

            var rows = CreateRect(createCard.transform, "Profile Rows");
            AnchorTopLeft(rows, PageWidth - 28f, 408f, 14f, 164f);
            var rowLayout = rows.gameObject.AddComponent<VerticalLayoutGroup>();
            rowLayout.spacing = 6f;
            rowLayout.childControlHeight = false;
            rowLayout.childControlWidth = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = false;

            CreateInfoRow(rows, "性别", "女　锁定", false, 54f, 17, 18);
            CreateInfoRow(rows, "年代", model.Worker.Era, false, 54f, 17, 18);
            CreateInfoRow(rows, "年龄", model.Worker.Age + " 岁", false, 54f, 17, 18);
            CreateInfoRow(rows, "体魄", model.Worker.Body, false, 54f, 17, 18);
            CreateInfoRow(rows, "识字", model.Worker.Literacy, false, 54f, 17, 18);
            CreateInfoRow(rows, "逻辑", model.Worker.Logic, false, 54f, 17, 18);
            CreateInfoRow(rows, "社交", model.Worker.Social, false, 54f, 17, 18);
            CreateInfoRow(rows, "学历", model.Worker.Education, false, 54f, 17, 18);
            CreateInfoRow(rows, "家境", model.Worker.Family, false, 54f, 17, 18);
            CreateInfoRow(rows, "优势", "★ 稀有! " + model.Worker.Advantage, true, 54f, 17, 18);
            CreateInfoRow(rows, "资产", "★ 稀有! " + model.Worker.Asset, true, 54f, 17, 18);

            CreateChoiceBar(createCard.transform, 14f, 600f);

            var nameCard = CreateRect(scrollContent, "Name Card");
            nameCard.gameObject.AddComponent<LayoutElement>().preferredHeight = 120f;
            var nameLayout = nameCard.gameObject.AddComponent<HorizontalLayoutGroup>();
            nameLayout.spacing = 8f;
            nameLayout.childControlHeight = true;
            nameLayout.childControlWidth = false;
            nameLayout.childForceExpandHeight = true;

            var fields = CreateRounded(nameCard, "Name Fields", PaperShiftTheme.White, 16f);
            fields.gameObject.AddComponent<LayoutElement>().preferredWidth = PageWidth - 92f;
            var fieldsLayout = fields.gameObject.AddComponent<VerticalLayoutGroup>();
            fieldsLayout.childControlHeight = false;
            fieldsLayout.childControlWidth = true;
            fieldsLayout.childForceExpandWidth = true;
            fieldsLayout.childForceExpandHeight = false;
            CreateNameRow(fields, "姓", model.Worker.LastName);
            CreateNameRow(fields, "名", model.Worker.FirstName);

            var dice = CreateRounded(nameCard, "Random Name Button", PaperShiftTheme.White, 18f);
            dice.gameObject.AddComponent<LayoutElement>().preferredWidth = 84f;
            CreateDiceIcon(dice, "Dice", new Vector2(48f, 48f), Vector2.zero);

            CreateButton(view.transform, "Next Button", "下一步", 0f, 892f, PageWidth, 64f, PaperShiftTheme.White, PaperShiftTheme.Ink, controller.ShowTags);
        }

        private static void BuildTagsScreen(Transform canvasTransform, PaperShiftGameModel model)
        {
            var view = CreateScreen(canvasTransform, "Screen_SelectTags", PaperShiftScreen.Tags, PaperShiftTheme.PageBlue);
            AddTopLine(view.transform, "选择李小满的标签", model.Worker.Coin, "⚙", null);

            var scrollContent = CreateScroll(view.transform, "Tags Scroll", 92f, 686f, PageWidth);
            var tagCard = CreatePaperCard(scrollContent, "Tag Choices Card", PageWidth, 552f, false);
            var list = CreateRect(tagCard.transform, "Tag List");
            Stretch(list, 16f, 16f, 16f, 16f);
            var layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 11f;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            foreach (var tag in model.AvailableTags)
            {
                CreateTagChoiceRow(list, tag);
            }

            var refreshArea = CreateRect(view.transform, "Refresh Area");
            SetTop(refreshArea, PageWidth - 32f, 92f, 0f, 786f);
            AddText(refreshArea, "Refresh Title", "换一组标签", 14, PaperShiftTheme.Ink, TextAnchor.UpperLeft, new RectOffset(0, 0, 0, 0));
            CreateButton(refreshArea, "Free Refresh", "免费刷新  1", -105f, 26f, 198f, 66f, PaperShiftTheme.White, PaperShiftTheme.Ink, null);
            CreateButton(refreshArea, "Super Refresh", "超级刷新\n时代变更，获得更好的标签!", 106f, 26f, 220f, 66f, PaperShiftTheme.Purple, Color.white, null, 16);

            CreateButton(view.transform, "Confirm Tags Button", "请选择3个标签，0/3", 0f, 892f, PageWidth, 64f, PaperShiftTheme.GrayButton, PaperShiftTheme.Hex("#505050"), controller.ShowResume, 23, false);
        }

        private static void BuildResumeScreen(Transform canvasTransform, PaperShiftGameModel model)
        {
            var view = CreateScreen(canvasTransform, "Screen_EditResume", PaperShiftScreen.Resume, PaperShiftTheme.PageBlue);
            AddTopLine(view.transform, "编辑简历", model.Worker.Coin, "‹", null);

            var content = CreateScroll(view.transform, "Resume Scroll", 92f, 790f, PageWidth);
            var card = CreatePaperCard(content, "Resume Summary Card", PageWidth, 810f, false);

            var ribbon = CreateRounded(card.transform, "Generation Ribbon", PaperShiftTheme.Blue, 4f);
            AnchorTopLeft(ribbon, 90f, 32f, -4f, 0f);
            ribbon.localEulerAngles = new Vector3(0f, 0f, -45f);
            AddText(ribbon, "Label", "第1代", 14, Color.white, TextAnchor.MiddleCenter);

            var header = CreateRect(card.transform, "Header");
            AnchorTopLeft(header, PageWidth - 48f, 104f, 24f, 22f);
            AddText(header, "Name", "李 小满\n<size=18>女 24 岁 现代城市</size>", 24, PaperShiftTheme.Ink, TextAnchor.MiddleLeft);
            CreateAvatarLock(header, new Vector2(322f, -6f), PortraitKind.Worker, false, 92f);

            var intent = CreateRounded(card.transform, "Resume Intent", PaperShiftTheme.White, 14f);
            AnchorTopLeft(intent, PageWidth - 48f, 92f, 24f, 134f);
            AddText(intent, "Title", "求职意向", 17, PaperShiftTheme.Ink, TextAnchor.UpperLeft, new RectOffset(12, 0, 8, 0));
            AddText(intent, "Hint", "决定刷到的岗位", 15, PaperShiftTheme.Blue, TextAnchor.UpperRight, new RectOffset(0, 12, 9, 0));
            var intentTags = CreateRect(intent, "Intent Tags");
            AnchorTopLeft(intentTags, PageWidth - 72f, 44f, 12f, 40f);
            var intentLayout = intentTags.gameObject.AddComponent<HorizontalLayoutGroup>();
            intentLayout.spacing = 7f;
            intentLayout.childControlWidth = false;
            intentLayout.childControlHeight = false;
            for (var i = 0; i < model.ResumeIntent.Count; i++)
            {
                CreateTicket(intentTags, model.ResumeIntent[i], i == 0 ? TagRarity.Rare : TagRarity.Normal, false);
            }

            var lineList = CreateRect(card.transform, "Resume Lines");
            AnchorTopLeft(lineList, PageWidth - 48f, 326f, 24f, 238f);
            var lineLayout = lineList.gameObject.AddComponent<VerticalLayoutGroup>();
            lineLayout.spacing = 6f;
            lineLayout.childControlHeight = false;
            lineLayout.childControlWidth = true;
            lineLayout.childForceExpandWidth = true;
            foreach (var resumeLine in model.ResumeLines)
            {
                CreateResumeLine(lineList, resumeLine);
            }

            var selectedTags = CreateRect(card.transform, "Selected Tags");
            AnchorTopLeft(selectedTags, PageWidth - 48f, 92f, 24f, 580f);
            var selectedLayout = selectedTags.gameObject.AddComponent<HorizontalLayoutGroup>();
            selectedLayout.spacing = 10f;
            selectedLayout.childControlWidth = false;
            selectedLayout.childControlHeight = false;
            selectedLayout.childForceExpandHeight = false;
            foreach (var tag in model.SelectedTags)
            {
                CreateMinus(selectedTags);
                CreateTicket(selectedTags, tag.Name, tag.Rarity, true);
            }

            var risk = CreateRounded(card.transform, "Resume Risk Footer", PaperShiftTheme.White, 22f);
            AnchorBottom(risk, PageWidth, 82f, 0f, 0f);
            AddText(risk, "Label", "简历包装风险", 17, PaperShiftTheme.Ink, TextAnchor.MiddleLeft, new RectOffset(22, 0, 0, 0));
            AddText(risk, "Count", "<size=40>2/5</size>  <size=13>已包装</size>", 18, PaperShiftTheme.Ink, TextAnchor.MiddleRight, new RectOffset(0, 22, 0, 0));

            CreateButton(view.transform, "Send Resume Button", "投递简历", 0f, 892f, PageWidth, 64f, PaperShiftTheme.White, PaperShiftTheme.Ink, controller.ShowJobSearch);
        }

        private static void BuildJobSearchScreen(Transform canvasTransform, PaperShiftGameModel model)
        {
            var view = CreateScreen(canvasTransform, "Screen_JobSearch", PaperShiftScreen.JobSearch, PaperShiftTheme.PageBlue);
            AddTopLine(view.transform, "求职", string.Empty, "⚙", controller.ShowNews, "!!\n新闻");

            var content = CreateScroll(view.transform, "Job Search Scroll", 92f, 760f, PageWidth);
            CreateCandidateCard(content, "Self Candidate Card", model.JobSearchSelf, 335f, false, false);
            CreateCandidateCard(content, "Interview Job Card", model.JobOffer, 330f, true, false);
            AddCalendarAndActions(view.transform, "2026", "5", "面试第2轮", controller.ShowInterviewFailure, "↻\n再投一家", controller.ShowJobSearch);
        }

        private static void BuildFailureScreen(Transform canvasTransform, PaperShiftGameModel model)
        {
            var view = CreateScreen(canvasTransform, "Screen_InterviewFailure", PaperShiftScreen.InterviewFailure, PaperShiftTheme.PageBlue);
            AddTopLine(view.transform, "求职", string.Empty, "⚙", controller.ShowNews, "!!\n新闻");

            var content = CreateScroll(view.transform, "Failure Scroll", 92f, 760f, PageWidth);
            CreateCandidateCard(content, "Failure Self Card", model.FailureSelf, 335f, false, false);
            CreateCandidateCard(content, "Rejected Job Card", model.FailureOffer, 240f, false, true);

            var banner = CreateRounded(view.transform, "Failure Banner", PaperShiftTheme.Hex("#c3c3c3", 0.88f), 0f);
            SetTop(banner, Width, 126f, 0f, 376f);
            banner.localEulerAngles = new Vector3(0f, 0f, -1f);
            AddText(banner, "Icon", "♡", 50, PaperShiftTheme.Purple, TextAnchor.MiddleLeft, new RectOffset(38, 0, 0, 0));
            AddText(banner, "Text", "对方认为你缺少岗位经验，不再和你联系。", 22, PaperShiftTheme.Hex("#3d3d3d"), TextAnchor.MiddleLeft, new RectOffset(112, 36, 0, 0));

            AddCalendarAndActions(view.transform, "2026", "6", "再投一家", controller.ShowJobSearch, null, null);
        }

        private static void BuildWorkScreen(Transform canvasTransform, PaperShiftGameModel model)
        {
            var view = CreateScreen(canvasTransform, "Screen_Work", PaperShiftScreen.Work, PaperShiftTheme.PagePurple);
            AddTopLine(view.transform, "打工", string.Empty, "⚙", controller.ShowBudget, "预算\n分配");

            var content = CreateScroll(view.transform, "Work Scroll", 92f, 760f, PageWidth);
            CreateCandidateCard(content, "Work Self Card", model.WorkSelf, 335f, false, false);
            CreateCandidateCard(content, "Current Job Card", model.WorkJob, 330f, true, false);
            AddCalendarAndActions(view.transform, "2031", "6", "再干一月", controller.ShowWork, "￥\n分配", controller.ShowBudget);
        }

        private static void BuildBudgetScreen(Transform canvasTransform, PaperShiftGameModel model)
        {
            var view = CreateScreen(canvasTransform, "Screen_Budget", PaperShiftScreen.Budget, PaperShiftTheme.PageBlue);
            AddTopLine(view.transform, "工资分配", "12,600", "‹", null);

            var content = CreateScroll(view.transform, "Budget Scroll", 92f, 790f, PageWidth);
            var card = CreatePaperCard(content, "Budget Card", PageWidth, 660f, false);
            var ribbon = CreateRounded(card.transform, "Ribbon", PaperShiftTheme.Blue, 0f);
            AnchorTopLeft(ribbon, PageWidth, 76f, 0f, 0f);
            AddText(ribbon, "Label", "拖动分配这个月工资", 23, Color.white, TextAnchor.MiddleCenter);

            var settlement = CreateSettlement(card.transform, "Salary Settlement", new[]
            {
                ("预计月薪", model.Budget.Salary, PaperShiftTheme.Green),
                ("固定房租", model.Budget.Rent, PaperShiftTheme.Red),
                ("可分配金额", model.Budget.Distributable, PaperShiftTheme.Blue)
            });
            AnchorTopLeft(settlement, PageWidth - 44f, 118f, 22f, 92f);

            var bars = CreateRect(card.transform, "Budget Sliders");
            AnchorTopLeft(bars, PageWidth - 44f, 166f, 22f, 226f);
            var barsLayout = bars.gameObject.AddComponent<VerticalLayoutGroup>();
            barsLayout.spacing = 9f;
            barsLayout.childControlHeight = false;
            barsLayout.childControlWidth = true;
            barsLayout.childForceExpandWidth = true;
            foreach (var item in model.Budget.Items)
            {
                CreateBudgetSlider(bars, item);
            }

            var impact = CreateRect(card.transform, "Impact Grid");
            AnchorTopLeft(impact, PageWidth - 44f, 134f, 22f, 404f);
            var impactGrid = impact.gameObject.AddComponent<GridLayoutGroup>();
            impactGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            impactGrid.constraintCount = 2;
            impactGrid.cellSize = new Vector2((PageWidth - 52f) * 0.5f, 58f);
            impactGrid.spacing = new Vector2(8f, 8f);
            foreach (var data in model.Budget.Impacts)
            {
                var tile = CreateRounded(impact, "Impact_" + data.Label, PaperShiftTheme.White, 10f);
                AddText(tile, "Label", data.Label + "\n<size=21><color=" + data.ColorHtml + ">" + data.Value + "</color></size>", 15, PaperShiftTheme.Ink, TextAnchor.MiddleCenter);
            }

            var notes = CreateRect(card.transform, "Budget Notes");
            AnchorTopLeft(notes, PageWidth - 44f, 212f, 22f, 548f);
            var notesLayout = notes.gameObject.AddComponent<VerticalLayoutGroup>();
            notesLayout.spacing = 8f;
            notesLayout.childControlHeight = false;
            notesLayout.childControlWidth = true;
            notesLayout.childForceExpandWidth = true;
            foreach (var note in model.Budget.Notes)
            {
                CreateTagChoiceRow(notes, note, 64f, 120f);
            }

            CreateButton(view.transform, "Save Budget Button", "保存，下月开干", 0f, 892f, PageWidth, 64f, PaperShiftTheme.Blue, Color.white, controller.ShowWork);
        }

        private static void BuildNewsScreen(Transform canvasTransform, PaperShiftGameModel model)
        {
            var view = CreateScreen(canvasTransform, "Screen_NewsModal", PaperShiftScreen.News, PaperShiftTheme.PagePurple);
            AddTopLine(view.transform, "打工", string.Empty, "⚙", null, "!!\n新闻");

            var content = CreateScroll(view.transform, "News Background Scroll", 92f, 760f, PageWidth);
            CreateCandidateCard(content, "News Self Card", model.NewsSelf, 280f, false, false);
            CreateCandidateCard(content, "News Job Card", model.NewsJob, 300f, false, false);
            AddCalendarAndActions(view.transform, "2033", "4", "再干一月", controller.ShowWork, "￥\n分配", controller.ShowBudget);

            var mask = CreateRounded(view.transform, "Modal Mask", PaperShiftTheme.Hex("#201a2a", 0.66f), 0f);
            Stretch(mask, 0f, 0f, 0f, 0f);
            mask.SetAsLastSibling();

            var modal = CreateRounded(mask, "News Modal", PaperShiftTheme.Hex("#97d5fb"), 24f);
            SetCenter(modal, 420f, 390f, 0f, 0f);
            AddOutline(modal.gameObject, PaperShiftTheme.Hex("#6ab7e7"), 4f);

            var title = CreateRounded(modal, "Modal Title", PaperShiftTheme.Hex("#309fe8"), 20f);
            SetTop(title, 120f, 36f, 0f, -26f);
            AddOutline(title.gameObject, PaperShiftTheme.Hex("#c6ecff"), 3f);
            AddText(title, "Label", "新闻", 20, Color.white, TextAnchor.MiddleCenter);

            var textBox = CreateRounded(modal, "Modal Text", PaperShiftTheme.White, 16f);
            AnchorTopLeft(textBox, 396f, 116f, 12f, 34f);
            AddText(textBox, "Text", "新职业「人机团队协调员」出现了。", 19, PaperShiftTheme.Ink, TextAnchor.MiddleCenter, new RectOffset(18, 18, 0, 0));

            CreateButton(modal, "Option Apply", "投相关岗位", 0f, 166f, 396f, 58f, PaperShiftTheme.White, PaperShiftTheme.Ink, controller.ShowJobSearch, 18);
            CreateButton(modal, "Option Stay", "先干当前工作", 0f, 238f, 396f, 58f, PaperShiftTheme.White, PaperShiftTheme.Ink, controller.ShowWork, 18);
            CreateButton(modal, "Option Save", "攒钱观望", 0f, 310f, 396f, 58f, PaperShiftTheme.White, PaperShiftTheme.Ink, controller.ShowBudget, 18);
        }

        private static void BuildRetirementScreen(Transform canvasTransform, PaperShiftGameModel model)
        {
            var view = CreateScreen(canvasTransform, "Screen_Retirement", PaperShiftScreen.Retirement, PaperShiftTheme.PageBlue);
            AddTopLine(view.transform, "退休结算", model.Retirement.Coin, "⚙", null);

            var content = CreateScroll(view.transform, "Retirement Scroll", 92f, 868f, PageWidth);

            var ribbon = CreateRounded(content, "Life End Ribbon", PaperShiftTheme.Blue, 0f);
            ribbon.gameObject.AddComponent<LayoutElement>().preferredHeight = 76f;
            AddText(ribbon, "Label", "这一代打工人生结束了", 23, Color.white, TextAnchor.MiddleCenter);

            var life = CreatePaperCard(content, "Life Summary", PageWidth, 308f, false);
            AddText(life.transform, "Reason Title", "退场原因：到龄退休", 22, PaperShiftTheme.Ink, TextAnchor.UpperCenter, new RectOffset(0, 0, 12, 0));
            var settlement = CreateSettlement(life.transform, "Retirement Settlement", new[]
            {
                ("工作年限", model.Retirement.WorkYears, PaperShiftTheme.Ink),
                ("最终职业", model.Retirement.FinalJob, PaperShiftTheme.Ink),
                ("留下存款", model.Retirement.Savings, PaperShiftTheme.Blue),
                ("精神状态", model.Retirement.MentalState, PaperShiftTheme.Green)
            });
            AnchorTopLeft(settlement, PageWidth - 36f, 140f, 18f, 52f);

            var reasons = CreateRect(life.transform, "Retirement Reasons");
            AnchorTopLeft(reasons, PageWidth - 36f, 116f, 18f, 204f);
            var grid = reasons.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.cellSize = new Vector2((PageWidth - 44f) * 0.5f, 58f);
            grid.spacing = new Vector2(8f, 8f);
            for (var i = 0; i < model.Retirement.Reasons.Length; i++)
            {
                var reason = CreateRounded(reasons, "Reason_" + i, i == 0 ? PaperShiftTheme.BlueLight : PaperShiftTheme.White, 12f);
                AddOutline(reason.gameObject, i == 0 ? PaperShiftTheme.Blue : PaperShiftTheme.Hex("#d8e5ed"), 2f);
                AddText(reason, "Label", model.Retirement.Reasons[i], 14, i == 0 ? Color.white : PaperShiftTheme.Ink, TextAnchor.MiddleCenter);
            }

            var parents = CreateRect(content, "Parents");
            parents.gameObject.AddComponent<LayoutElement>().preferredHeight = 142f;
            var parentLayout = parents.gameObject.AddComponent<HorizontalLayoutGroup>();
            parentLayout.spacing = 10f;
            parentLayout.childControlWidth = false;
            parentLayout.childControlHeight = false;
            parentLayout.childAlignment = TextAnchor.MiddleCenter;
            CreatePersonTile(parents, "李小满", PortraitKind.Worker);
            CreatePersonTile(parents, "陆远", PortraitKind.Male);

            var gen = CreateRounded(content, "Generation Pill", PaperShiftTheme.White, 14f);
            gen.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;
            AddText(gen, "Label", "第 2 代", 16, PaperShiftTheme.Blue, TextAnchor.MiddleCenter);

            var children = CreateRect(content, "Children Row");
            children.gameObject.AddComponent<LayoutElement>().preferredHeight = 86f;
            var childLayout = children.gameObject.AddComponent<HorizontalLayoutGroup>();
            childLayout.spacing = 10f;
            childLayout.childControlWidth = false;
            childLayout.childControlHeight = false;
            childLayout.childAlignment = TextAnchor.MiddleCenter;
            for (var i = 0; i < 4; i++)
            {
                var child = CreateRounded(children, "Child Option " + (i + 1), PaperShiftTheme.White, 12f);
                child.gameObject.AddComponent<LayoutElement>().preferredWidth = 74f;
                child.gameObject.AddComponent<LayoutElement>().preferredHeight = 74f;
                if (i == 0)
                {
                    AddOutline(child.gameObject, PaperShiftTheme.Ink, 3f);
                }
                CreatePortrait(child, PortraitKind.Baby, new Vector2(54f, 54f), Vector2.zero);
            }

            var childCard = CreatePaperCard(content, "Child Detail Card", PageWidth, 540f, false);
            var childHead = CreateRect(childCard.transform, "Child Head");
            AnchorTopLeft(childHead, PageWidth - 36f, 80f, 18f, 18f);
            CreatePortrait(childHead, PortraitKind.Baby, new Vector2(68f, 68f), new Vector2(34f, -40f));
            AddText(childHead, "Name", "姓名 李君语", 22, PaperShiftTheme.Ink, TextAnchor.MiddleLeft, new RectOffset(84, 0, 0, 0));
            CreateDiceIcon(childHead, "Dice", new Vector2(52f, 52f), new Vector2(PageWidth - 74f, -40f));

            var form = CreateRect(childCard.transform, "Child Form Grid");
            AnchorTopLeft(form, PageWidth - 36f, 104f, 18f, 112f);
            var formGrid = form.gameObject.AddComponent<GridLayoutGroup>();
            formGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            formGrid.constraintCount = 3;
            formGrid.cellSize = new Vector2((PageWidth - 44f) / 3f, 29f);
            formGrid.spacing = new Vector2(4f, 4f);
            foreach (var row in model.Retirement.ChildRows)
            {
                CreateInfoRow(form, row.Label, row.Value, false, 29f, 12, 13, 42f);
            }

            var tags = CreateRect(childCard.transform, "Child Tags");
            AnchorTopLeft(tags, PageWidth - 36f, 80f, 18f, 232f);
            var tagLayout = tags.gameObject.AddComponent<HorizontalLayoutGroup>();
            tagLayout.spacing = 7f;
            tagLayout.childControlWidth = false;
            tagLayout.childControlHeight = false;
            tagLayout.childAlignment = TextAnchor.MiddleCenter;
            foreach (var tag in model.Retirement.ChildTags)
            {
                CreateTicket(tags, tag.Name, tag.Rarity, false);
            }

            var nurture = CreateSettlement(childCard.transform, "Nurture Settlement", new[]
            {
                ("养育投入：教育", "35%", PaperShiftTheme.Blue),
                ("养育投入：健康", "25%", PaperShiftTheme.Green),
                ("养育投入：见识", "20%", PaperShiftTheme.Purple),
                ("影响", "下一代学习+，体魄+", PaperShiftTheme.Ink)
            });
            AnchorTopLeft(nurture, PageWidth - 36f, 142f, 18f, 324f);
            AddText(childCard.transform, "Quality Chance", "20% 几率能培养成优质后代", 18, PaperShiftTheme.Ink, TextAnchor.MiddleCenter, new RectOffset(0, 0, 460, 0));
            CreateButton(childCard.transform, "Cultivate Button", "培养", 0f, 470f, PageWidth - 36f, 54f, PaperShiftTheme.Blue, Color.white, null, 21);

            var inherit = CreatePaperCard(content, "Inheritance Card", PageWidth, 300f, false);
            AddText(inherit.transform, "Title", "退休财产分配", 22, PaperShiftTheme.Ink, TextAnchor.UpperCenter, new RectOffset(0, 0, 12, 0));
            var heirList = CreateRect(inherit.transform, "Heir List");
            AnchorTopLeft(heirList, PageWidth - 36f, 160f, 18f, 54f);
            var heirLayout = heirList.gameObject.AddComponent<VerticalLayoutGroup>();
            heirLayout.spacing = 8f;
            heirLayout.childControlHeight = false;
            heirLayout.childControlWidth = true;
            heirLayout.childForceExpandWidth = true;
            foreach (var heir in model.Retirement.Heirs)
            {
                CreateHeirRow(heirList, heir);
            }

            var inheritSettlement = CreateSettlement(inherit.transform, "Inheritance Settlement", new[]
            {
                ("可分配存款", "86,000", PaperShiftTheme.Ink),
                ("可留下标签", "AI先行者 / 会算账", PaperShiftTheme.Ink),
                ("可能留下负担", "房贷剩余 12万", PaperShiftTheme.Red)
            });
            AnchorTopLeft(inheritSettlement, PageWidth - 36f, 92f, 18f, 204f);

            var rank = CreateRect(content, "Rank Text");
            rank.gameObject.AddComponent<LayoutElement>().preferredHeight = 38f;
            AddText(rank, "Text", "即将开始第 <color=#ffd84a>2</color> 代，已击败 <color=#ffd84a>3.2%</color> 玩家", 18, Color.white, TextAnchor.MiddleCenter);

            var finish = CreateButton(content, "Finish Retirement Button", "退休完成，开启下一代", 0f, 0f, PageWidth, 64f, PaperShiftTheme.White, PaperShiftTheme.Ink, controller.ShowCreate);
            finish.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;
        }

        private static void AddTopLine(Transform screen, string title, string coin, string icon, UnityAction smallAction, string smallButtonText = null)
        {
            var line = CreateRect(screen, "Topline");
            SetTop(line, PageWidth, 46f, 0f, 34f);

            var round = CreateButton(line, "Header Icon", icon, -200f, 3f, 40f, 40f, PaperShiftTheme.White, PaperShiftTheme.Hex("#505963"), null, 24, false);
            round.name = icon == "‹" ? "Back Button" : "Settings Button";

            AddText(line, "Title", title, 22, PaperShiftTheme.Hex("#273447"), TextAnchor.MiddleLeft, new RectOffset(52, 0, 0, 0));

            if (!string.IsNullOrEmpty(smallButtonText))
            {
                CreateButton(line, "Top Right Button", smallButtonText, 198f, 1f, 44f, 44f, PaperShiftTheme.White, PaperShiftTheme.Ink, smallAction, 13, false);
                return;
            }

            if (!string.IsNullOrEmpty(coin))
            {
                var coinGroup = CreateRect(line, "Coin");
                AnchorCenterRight(coinGroup, 120f, 34f, 0f, 0f);
                CreateCoinIcon(coinGroup, new Vector2(24f, 24f), new Vector2(-44f, 0f));
                AddText(coinGroup, "Amount", coin, 20, PaperShiftTheme.Hex("#3a3a3a"), TextAnchor.MiddleLeft, new RectOffset(46, 0, 0, 0));
            }
        }

        private static RectTransform CreatePaperCard(Transform parent, string name, float width, float height, bool stackedShadow)
        {
            var card = CreateRounded(parent, name, PaperShiftTheme.Paper, 22f);
            var layout = card.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = height;
            layout.flexibleWidth = 0f;
            layout.flexibleHeight = 0f;
            AddShadow(card.gameObject, stackedShadow ? new Vector2(0f, -4f) : new Vector2(0f, -3f), PaperShiftTheme.Hex("#607d94", stackedShadow ? 0.55f : 0.35f));
            return card;
        }

        private static void CreateChoiceBar(Transform parent, float left, float top)
        {
            var bar = CreateRounded(parent, "Choice Bar", PaperShiftTheme.White, 13f);
            AnchorTopLeft(bar, PageWidth - 28f, 74f, left, top);
            AddOutline(bar.gameObject, PaperShiftTheme.Blue, 3f);
            CreateButton(bar, "Custom Button", "自定义", -95f, 5f, 182f, 64f, PaperShiftTheme.White, PaperShiftTheme.Ink, null, 21, false);
            CreateButton(bar, "Random Button", "·  随机", 92f, 5f, 222f, 64f, PaperShiftTheme.Blue, Color.white, controller.ShowTags, 21, false);
        }

        private static void CreateNameRow(Transform parent, string label, string value)
        {
            var row = CreateRect(parent, "Name Row " + label);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;
            AddText(row, "Label", label, 17, PaperShiftTheme.Hex("#4e5358"), TextAnchor.MiddleLeft, new RectOffset(8, 0, 0, 0));
            AddText(row, "Value", value, 18, PaperShiftTheme.Ink, TextAnchor.MiddleCenter);
            AddText(row, "Edit", "✎", 21, PaperShiftTheme.Blue, TextAnchor.MiddleRight, new RectOffset(0, 12, 0, 0));
        }

        private static RectTransform CreateTagChoiceRow(Transform parent, TagData tag, float height = 83f, float ticketColumnWidth = 154f)
        {
            var row = CreateRounded(parent, "Tag Row " + tag.Name, PaperShiftTheme.White, 10f);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            row.gameObject.AddComponent<Button>().targetGraphic = row.gameObject.GetComponent<Graphic>();

            var ticketSlot = CreateRect(row, "Ticket Slot");
            AnchorTopLeft(ticketSlot, ticketColumnWidth, height, 14f, 0f);
            CreateTicket(ticketSlot, tag.Name, tag.Rarity, true);

            var description = string.IsNullOrEmpty(tag.Description) ? tag.Name : tag.Description;
            AddText(row, "Description", description, 15, PaperShiftTheme.MutedInk, TextAnchor.MiddleLeft, new RectOffset(Mathf.RoundToInt(ticketColumnWidth + 18f), 8, 0, 0));
            return row;
        }

        private static void CreateResumeLine(Transform parent, ResumeLineData data)
        {
            var line = CreateRounded(parent, "Resume Line " + data.Label, PaperShiftTheme.White, 10f);
            line.gameObject.AddComponent<LayoutElement>().preferredHeight = 58f;
            AddText(line, "Label", data.Label, 15, PaperShiftTheme.Hex("#4e5358"), TextAnchor.MiddleLeft, new RectOffset(8, 0, 0, 0));
            AddText(line, "Value", data.Value, 16, PaperShiftTheme.Ink, TextAnchor.UpperLeft, new RectOffset(68, 0, 6, 0));

            var chips = CreateRect(line, "Actions");
            AnchorTopLeft(chips, PageWidth - 120f, 24f, 68f, 30f);
            var chipLayout = chips.gameObject.AddComponent<HorizontalLayoutGroup>();
            chipLayout.spacing = 5f;
            chipLayout.childControlWidth = false;
            chipLayout.childControlHeight = false;
            for (var i = 0; i < data.Actions.Length; i++)
            {
                CreateActionChip(chips, data.Actions[i], data.ActiveIndex == i, data.Tones[i]);
            }
        }

        private static void CreateActionChip(Transform parent, string label, bool active, ChipTone tone)
        {
            Color bg;
            Color border;
            Color text;

            if (active)
            {
                bg = PaperShiftTheme.BlueLight;
                border = PaperShiftTheme.Blue;
                text = Color.white;
            }
            else if (tone == ChipTone.Warn)
            {
                bg = PaperShiftTheme.Hex("#dcc1ff");
                border = PaperShiftTheme.Purple;
                text = PaperShiftTheme.Hex("#55306f");
            }
            else if (tone == ChipTone.Fake)
            {
                bg = PaperShiftTheme.Hex("#ffe6b7");
                border = PaperShiftTheme.Orange;
                text = PaperShiftTheme.Hex("#7b4a00");
            }
            else
            {
                bg = PaperShiftTheme.Hex("#f4f8fb");
                border = PaperShiftTheme.Hex("#d7e0e6");
                text = PaperShiftTheme.Hex("#58626c");
            }

            var chip = CreateRounded(parent, "Chip " + label, bg, 8f);
            chip.gameObject.AddComponent<LayoutElement>().preferredWidth = Mathf.Clamp(32f + label.Length * 12f, 50f, 112f);
            chip.gameObject.GetComponent<LayoutElement>().preferredHeight = 24f;
            AddOutline(chip.gameObject, border, 2f);
            AddText(chip, "Label", label, 12, text, TextAnchor.MiddleCenter);
        }

        private static void CreateCandidateCard(Transform parent, string name, CandidateCardData data, float height, bool includeProgress, bool disabled)
        {
            var card = CreatePaperCard(parent, name, PageWidth, height, false);
            if (disabled)
            {
                var canvasGroup = card.gameObject.AddComponent<CanvasGroup>();
                canvasGroup.alpha = 0.38f;
            }

            var badge = CreateRounded(card, "Badge", PaperShiftTheme.Blue, 18f);
            AnchorTopLeft(badge, 68f, 30f, 6f, -3f);
            AddOutline(badge.gameObject, PaperShiftTheme.White, 2f);
            AddText(badge, "Text", data.Badge, 20, Color.white, TextAnchor.MiddleCenter);
            AddText(card, "Corner", data.Corner, 16, PaperShiftTheme.Blue, TextAnchor.UpperRight, new RectOffset(0, 18, 16, 0));

            var top = CreateRect(card, "Top Info");
            AnchorTopLeft(top, PageWidth - 32f, 120f, 16f, 34f);
            CreateStressPortrait(top, data.Portrait, data.RingText, data.RingColor);
            AddText(top, "Name", data.Name + "\n<size=18>" + data.Subtitle + "</size>", 24, PaperShiftTheme.Ink, TextAnchor.UpperLeft, new RectOffset(102, 0, 0, 0));

            var miniGrid = CreateRect(top, "Mini Grid");
            AnchorTopLeft(miniGrid, PageWidth - 148f, data.Rows.Length <= 4 ? 64f : 92f, 102f, 62f);
            var grid = miniGrid.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.cellSize = new Vector2((PageWidth - 154f) * 0.5f, 27f);
            grid.spacing = new Vector2(3f, 3f);
            foreach (var row in data.Rows)
            {
                CreateInfoRow(miniGrid, row.Label, row.Value, row.IsRare, 27f, 13, 14, 52f);
            }

            if (data.Tags.Length > 0)
            {
                var tags = CreateRect(card, "Tags");
                AnchorTopLeft(tags, PageWidth - 32f, 86f, 16f, 168f);
                var tagGrid = tags.gameObject.AddComponent<GridLayoutGroup>();
                tagGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                tagGrid.constraintCount = 4;
                tagGrid.cellSize = new Vector2((PageWidth - 56f) * 0.25f, 36f);
                tagGrid.spacing = new Vector2(8f, 9f);

                foreach (var tag in data.Tags)
                {
                    CreateTicket(tags, tag.Name, tag.Rarity, false, 16, 36f);
                }

                var slotCount = Mathf.Max(0, 8 - data.Tags.Length);
                for (var i = 0; i < slotCount; i++)
                {
                    CreateSlot(tags);
                }
            }

            if (data.EventLines.Length > 0)
            {
                var log = CreateRect(card, "Event Log");
                var logHeight = data.EventLines.Length * 30f;
                AnchorBottom(log, PageWidth - 24f, logHeight, 0f, includeProgress ? 76f : 18f);
                var logLayout = log.gameObject.AddComponent<VerticalLayoutGroup>();
                logLayout.spacing = 3f;
                logLayout.childControlHeight = false;
                logLayout.childControlWidth = true;
                logLayout.childForceExpandWidth = true;
                foreach (var line in data.EventLines)
                {
                    var logLine = CreateRounded(log, "Log Line", PaperShiftTheme.Hex("#e2d1ff", 0.82f), 6f);
                    logLine.gameObject.AddComponent<LayoutElement>().preferredHeight = 27f;
                    AddOutline(logLine.gameObject, PaperShiftTheme.Hex("#baa3de", 0.9f), 1f);
                    AddText(logLine, "Text", line, 15, Color.white, TextAnchor.MiddleCenter);
                }
            }

            if (includeProgress && data.Progress != null)
            {
                CreateProgressFooter(card, data.Progress);
            }
        }

        private static void CreateProgressFooter(Transform card, ProgressData progress)
        {
            var footer = CreateRounded(card, "Progress Footer", PaperShiftTheme.Hex(progress.BarColorHtml), 18f);
            AnchorBottom(footer, PageWidth, 72f, 0f, 0f);
            AddText(footer, "Percent", "<size=32>" + progress.Percent + "</size> " + progress.Label, 18, Color.white, TextAnchor.UpperLeft, new RectOffset(16, 0, 8, 0));
            var bar = CreateRounded(footer, "Progress Bar", PaperShiftTheme.Hex("#0068b4", 0.38f), 8f);
            AnchorTopLeft(bar, 150f, 12f, 16f, 50f);
            var fill = CreateRounded(bar, "Fill", PaperShiftTheme.Hex(progress.FillColorHtml), 8f);
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(Mathf.Clamp01(progress.Fill), 1f);
            fill.pivot = new Vector2(0f, 0.5f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            CreateButton(footer, "Progress Button", progress.Button, 140f, 5f, 142f, 62f, PaperShiftTheme.White, PaperShiftTheme.Hex(progress.BarColorHtml), TargetAction(progress.ButtonTarget), 21, false);
        }

        private static void AddCalendarAndActions(Transform screen, string year, string month, string primary, UnityAction primaryAction, string secondary, UnityAction secondaryAction)
        {
            var calendar = CreateRounded(screen, "Calendar", PaperShiftTheme.White, 10f);
            SetBottomLeft(calendar, 58f, 72f, 72f, 18f);
            AddOutline(calendar.gameObject, PaperShiftTheme.Ink, 2f);
            var head = CreateRounded(calendar, "Year", PaperShiftTheme.Hex("#3b3f43"), 0f);
            AnchorTopLeft(head, 58f, 25f, 0f, 0f);
            AddText(head, "Text", year, 13, Color.white, TextAnchor.MiddleCenter);
            AddText(calendar, "Month", month, 31, PaperShiftTheme.Ink, TextAnchor.LowerCenter, new RectOffset(0, 0, 22, 0));

            var primaryWidth = secondary == null ? PageWidth - 82f : PageWidth - 160f;
            CreateButton(screen, "Primary Bottom Action", primary, secondary == null ? 38f : -39f, 897f, primaryWidth, 64f, PaperShiftTheme.White, PaperShiftTheme.Ink, primaryAction);
            if (secondary != null)
            {
                CreateButton(screen, "Secondary Bottom Action", secondary, 189f, 897f, 64f, 64f, PaperShiftTheme.White, PaperShiftTheme.Ink, secondaryAction, 13);
            }
        }

        private static RectTransform CreateSettlement(Transform parent, string name, (string label, string value, Color valueColor)[] rows)
        {
            var settlement = CreateRounded(parent, name, PaperShiftTheme.White, 16f);
            AddShadow(settlement.gameObject, new Vector2(0f, -3f), PaperShiftTheme.Hex("#638296", 0.22f));
            var layout = settlement.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 8, 8);
            layout.spacing = 0f;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            foreach (var row in rows)
            {
                var rowRoot = CreateRect(settlement, "Settlement " + row.label);
                rowRoot.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
                AddText(rowRoot, "Label", row.label, 15, PaperShiftTheme.Ink, TextAnchor.MiddleLeft);
                AddText(rowRoot, "Value", row.value, 15, row.valueColor, TextAnchor.MiddleRight);
            }
            return settlement;
        }

        private static void CreateBudgetSlider(Transform parent, BudgetItem item)
        {
            var row = CreateRect(parent, "Budget " + item.Label);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
            AddText(row, "Label", item.Label, 14, PaperShiftTheme.Ink, TextAnchor.MiddleLeft, new RectOffset(0, 0, 0, 0));
            AddText(row, "Value", item.Percent + "%", 14, PaperShiftTheme.Ink, TextAnchor.MiddleRight);

            var sliderRoot = CreateRect(row, "Slider");
            AnchorMiddle(sliderRoot, 270f, 22f, 54f, 0f);
            var slider = sliderRoot.gameObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.value = item.Percent;
            slider.wholeNumbers = true;

            var bg = CreateRounded(sliderRoot, "Background", PaperShiftTheme.Hex("#dcebf4"), 9f);
            Stretch(bg, 0f, 0f, 0f, 0f);
            var fillArea = CreateRect(sliderRoot, "Fill Area");
            Stretch(fillArea, 0f, 0f, 0f, 0f);
            var fill = CreateRounded(fillArea, "Fill", PaperShiftTheme.Hex(item.ColorHtml), 9f);
            Stretch(fill, 0f, 0f, 0f, 0f);
            var handleArea = CreateRect(sliderRoot, "Handle Slide Area");
            Stretch(handleArea, 0f, 0f, 0f, 0f);
            var handle = CreateRounded(handleArea, "Handle", PaperShiftTheme.White, 11f);
            SetCenter(handle, 22f, 22f, 0f, 0f);
            AddOutline(handle.gameObject, PaperShiftTheme.Hex(item.ColorHtml), 3f);

            slider.targetGraphic = handle.GetComponent<Graphic>();
            slider.fillRect = fill;
            slider.handleRect = handle;
        }

        private static void CreatePersonTile(Transform parent, string name, PortraitKind portrait)
        {
            var tile = CreateRounded(parent, "Person " + name, PaperShiftTheme.White, 13f);
            var layout = tile.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 104f;
            layout.preferredHeight = 132f;
            CreatePortrait(tile, portrait, new Vector2(72f, 72f), new Vector2(52f, -44f));
            AddText(tile, "Name", name, 15, PaperShiftTheme.Ink, TextAnchor.LowerCenter, new RectOffset(0, 0, 0, 8));
        }

        private static void CreateHeirRow(Transform parent, HeirData heir)
        {
            var row = CreateRounded(parent, "Heir " + heir.Name, PaperShiftTheme.White, 12f);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;
            CreatePortrait(row, PortraitKind.Baby, new Vector2(38f, 38f), new Vector2(30f, -24f));
            AddText(row, "Name", heir.Name + "\n<size=13><color=#667777>" + heir.Trait + "</color></size>", 15, PaperShiftTheme.Ink, TextAnchor.MiddleLeft, new RectOffset(66, 64, 0, 0));
            AddText(row, "Allocation", heir.Allocation, 16, PaperShiftTheme.Ink, TextAnchor.MiddleRight, new RectOffset(0, 12, 0, 0));
        }

        private static void CreateInfoRow(Transform parent, string label, string value, bool rare, float height, int labelSize, int valueSize, float labelWidth = 66f)
        {
            var row = CreateRounded(parent, "Info " + label, rare ? PaperShiftTheme.Hex("#91cff6") : PaperShiftTheme.White, height > 40f ? 9f : 5f);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            AddText(row, "Label", label, labelSize, PaperShiftTheme.Hex("#4e5358"), TextAnchor.MiddleLeft, new RectOffset(8, 0, 0, 0));

            var labelBg = CreateRounded(row, "Label Shade", PaperShiftTheme.Hex("#f0f0f0", 0.66f), height > 40f ? 9f : 5f);
            labelBg.anchorMin = new Vector2(0f, 0f);
            labelBg.anchorMax = new Vector2(0f, 1f);
            labelBg.pivot = new Vector2(0f, 0.5f);
            labelBg.sizeDelta = new Vector2(labelWidth, 0f);
            labelBg.anchoredPosition = Vector2.zero;
            labelBg.SetAsFirstSibling();

            AddText(row, "Value", value, valueSize, PaperShiftTheme.Ink, TextAnchor.MiddleCenter, new RectOffset(Mathf.RoundToInt(labelWidth), 6, 0, 0));
        }

        private static RectTransform CreateTicket(Transform parent, string label, TagRarity rarity, bool autoWidth, int fontSize = 20, float fixedHeight = 44f)
        {
            var color = rarity == TagRarity.Rare ? PaperShiftTheme.BlueTicket : rarity == TagRarity.SuperRare ? PaperShiftTheme.PurpleTicket : PaperShiftTheme.White;
            var border = rarity == TagRarity.Rare ? PaperShiftTheme.Hex("#315e77") : rarity == TagRarity.SuperRare ? PaperShiftTheme.Hex("#5a367b") : PaperShiftTheme.Hex("#42474b");
            var ticket = CreateRounded(parent, "Ticket " + label, color, 7f);
            var layout = ticket.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = fixedHeight;
            layout.preferredWidth = autoWidth ? Mathf.Clamp(30f + label.Length * fontSize, 72f, 150f) : -1f;
            AddOutline(ticket.gameObject, border, 2f);

            if (rarity != TagRarity.Normal)
            {
                AddText(ticket, "Star", "★", 15, Color.white, TextAnchor.UpperLeft, new RectOffset(5, 0, 3, 0));
            }

            AddText(ticket, "Label", label, fontSize, PaperShiftTheme.Hex("#3b3f43"), TextAnchor.MiddleCenter, new RectOffset(rarity == TagRarity.Normal ? 8 : 18, 8, 0, 0));
            return ticket;
        }

        private static void CreateSlot(Transform parent)
        {
            var slot = CreateRounded(parent, "Empty Slot", PaperShiftTheme.Slot, 8f);
            AddOutline(slot.gameObject, PaperShiftTheme.Hex("#d8e0e6"), 2f);
        }

        private static void CreateMinus(Transform parent)
        {
            var minus = CreateRounded(parent, "Minus", PaperShiftTheme.White, 18f);
            minus.gameObject.AddComponent<LayoutElement>().preferredWidth = 36f;
            minus.gameObject.GetComponent<LayoutElement>().preferredHeight = 36f;
            AddOutline(minus.gameObject, PaperShiftTheme.Hex("#d1d1d1"), 2f);
            AddText(minus, "Label", "−", 26, PaperShiftTheme.Hex("#b7b7b7"), TextAnchor.MiddleCenter);
        }

        private static RectTransform CreateAvatarLock(Transform parent, Vector2 anchoredPosition, PortraitKind portrait, bool locked, float size = 96f)
        {
            var avatar = CreateRounded(parent, "Avatar Frame", PaperShiftTheme.White, 18f);
            AnchorTopLeft(avatar, size, size + (locked ? 12f : 0f), anchoredPosition.x, -anchoredPosition.y);
            AddShadow(avatar.gameObject, new Vector2(0f, -3f), PaperShiftTheme.Hex("#000000", 0.16f));
            CreatePortrait(avatar, portrait, new Vector2(72f, 72f), new Vector2(size * 0.5f, -(size * 0.5f + (locked ? 6f : 0f))));
            if (locked)
            {
                var lockDot = CreateRounded(avatar, "Lock Dot", PaperShiftTheme.White, 14f);
                AnchorBottomRight(lockDot, 28f, 28f, 6f, 6f);
                AddText(lockDot, "Text", "锁", 13, PaperShiftTheme.Ink, TextAnchor.MiddleCenter);
            }
            return avatar;
        }

        private static void CreateStressPortrait(Transform parent, PortraitKind portrait, string text, Color ringColor)
        {
            var ring = CreateRect(parent, "Stress Ring");
            AnchorTopLeft(ring, 86f, 106f, 0f, 0f);
            var ringGraphic = ring.gameObject.AddComponent<RingGraphic>();
            ringGraphic.color = Color.white;
            ringGraphic.AccentColor = ringColor;
            CreatePortrait(ring, portrait, new Vector2(66f, 66f), new Vector2(43f, -43f));
            var label = CreateRounded(ring, "Ring Label", PaperShiftTheme.White, 11f);
            AnchorBottom(label, 66f, 26f, 0f, 0f);
            AddOutline(label.gameObject, PaperShiftTheme.Hex("#a5d3c5"), 2f);
            AddText(label, "Text", text, 14, PaperShiftTheme.Ink, TextAnchor.MiddleCenter);
        }

        private static void CreatePortrait(Transform parent, PortraitKind kind, Vector2 size, Vector2 localTopLeftCenter)
        {
            var portrait = CreateRounded(parent, "Portrait " + kind, kind == PortraitKind.Male ? PaperShiftTheme.Hex("#47a7ff") : kind == PortraitKind.Baby ? PaperShiftTheme.Hex("#f8c9d8") : PaperShiftTheme.Hex("#e9b347"), 18f);
            if (localTopLeftCenter == Vector2.zero)
            {
                SetCenter(portrait, size.x, size.y, 0f, 0f);
            }
            else
            {
                AnchorTopLeft(portrait, size.x, size.y, localTopLeftCenter.x - size.x * 0.5f, -localTopLeftCenter.y - size.y * 0.5f);
            }

            AddOutline(portrait.gameObject, PaperShiftTheme.White, 4f);
            var hairColor = kind == PortraitKind.Male ? PaperShiftTheme.Hex("#253970") : kind == PortraitKind.Baby ? PaperShiftTheme.Hex("#9b5a3c") : PaperShiftTheme.Hex("#8a4c2e");
            var hair = CreateRounded(portrait, "Hair", hairColor, 8f);
            AnchorTopLeft(hair, size.x, size.y * 0.36f, 0f, 0f);
            var face = portrait.gameObject.AddComponent<Mask>();
            face.showMaskGraphic = true;

            var skin = CreateRect(portrait, "Face");
            SetCenter(skin, size.x * 0.42f, size.y * 0.42f, 0f, -2f);
            var skinGraphic = skin.gameObject.AddComponent<EllipseGraphic>();
            skinGraphic.color = PaperShiftTheme.Hex("#ffd0c3");

            AddText(portrait, "Eyes", "•  •\n⌣", 16, PaperShiftTheme.Ink, TextAnchor.MiddleCenter, new RectOffset(0, 0, 10, 0));
        }

        private static void CreateDiceIcon(Transform parent, string name, Vector2 size, Vector2 position)
        {
            var dice = CreateRounded(parent, name, PaperShiftTheme.Hex("#eaf7ff"), 10f);
            if (position == Vector2.zero)
            {
                SetCenter(dice, size.x, size.y, 0f, 0f);
            }
            else
            {
                AnchorTopLeft(dice, size.x, size.y, position.x - size.x * 0.5f, -position.y - size.y * 0.5f);
            }
            dice.localEulerAngles = new Vector3(0f, 0f, 8f);
            AddText(dice, "Dot", "·", 35, PaperShiftTheme.Blue, TextAnchor.MiddleCenter);
        }

        private static void CreateCoinIcon(Transform parent, Vector2 size, Vector2 center)
        {
            var coin = CreateRect(parent, "Coin Icon");
            SetCenter(coin, size.x, size.y, center.x, center.y);
            var ellipse = coin.gameObject.AddComponent<EllipseGraphic>();
            ellipse.color = PaperShiftTheme.Hex("#ffb21c");
            AddOutline(coin.gameObject, PaperShiftTheme.Hex("#f08b00"), 2f);
            AddText(coin, "Mark", "Ⅱ", 13, Color.white, TextAnchor.MiddleCenter);
        }

        private static RectTransform CreateButton(Transform parent, string name, string label, float x, float top, float width, float height, Color background, Color textColor, UnityAction action, int fontSize = 23, bool blackOutline = true)
        {
            var buttonRect = CreateRounded(parent, name, background, Mathf.Min(18f, height * 0.25f));
            SetTop(buttonRect, width, height, x, top);
            if (blackOutline)
            {
                AddOutline(buttonRect.gameObject, PaperShiftTheme.Ink, 3f);
            }
            AddShadow(buttonRect.gameObject, new Vector2(0f, -2f), PaperShiftTheme.Hex("#000000", 0.16f));
            var button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonRect.GetComponent<Graphic>();
            if (action != null)
            {
                UnityEventTools.AddPersistentListener(button.onClick, action);
            }
            AddText(buttonRect, "Label", label, fontSize, textColor, TextAnchor.MiddleCenter);
            return buttonRect;
        }

        private static UnityAction TargetAction(PaperShiftScreen screen)
        {
            switch (screen)
            {
                case PaperShiftScreen.Create: return controller.ShowCreate;
                case PaperShiftScreen.Tags: return controller.ShowTags;
                case PaperShiftScreen.Resume: return controller.ShowResume;
                case PaperShiftScreen.JobSearch: return controller.ShowJobSearch;
                case PaperShiftScreen.InterviewFailure: return controller.ShowInterviewFailure;
                case PaperShiftScreen.Work: return controller.ShowWork;
                case PaperShiftScreen.Budget: return controller.ShowBudget;
                case PaperShiftScreen.News: return controller.ShowNews;
                case PaperShiftScreen.Retirement: return controller.ShowRetirement;
                default: return controller.ShowCreate;
            }
        }

        private static RectTransform CreateScroll(Transform parent, string name, float top, float height, float width)
        {
            var scrollRoot = CreateRect(parent, name);
            SetTop(scrollRoot, width, height, 0f, top);
            var scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;

            var viewport = CreateRect(scrollRoot, "Viewport");
            Stretch(viewport, 0f, 0f, 0f, 0f);
            viewport.gameObject.AddComponent<RectMask2D>();
            scroll.viewport = viewport;

            var content = CreateRect(viewport, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, height);

            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperCenter;

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = content;
            return content;
        }

        private static Text AddText(Transform parent, string name, string body, int fontSize, Color color, TextAnchor alignment, RectOffset padding = null)
        {
            var textRect = CreateRect(parent, name);
            Stretch(textRect, 0f, 0f, 0f, 0f);
            var text = textRect.gameObject.AddComponent<Text>();
            text.font = defaultFont;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            text.alignment = alignment;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = body;
            text.raycastTarget = false;

            if (padding != null)
            {
                textRect.offsetMin = new Vector2(padding.left, padding.bottom);
                textRect.offsetMax = new Vector2(-padding.right, -padding.top);
            }

            return text;
        }

        private static RectTransform CreateRounded(Transform parent, string name, Color color, float radius)
        {
            var rect = CreateRect(parent, name);
            var graphic = rect.gameObject.AddComponent<RoundedRectGraphic>();
            graphic.color = color;
            graphic.CornerRadius = radius;
            return rect;
        }

        private static RectTransform CreateRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            SetLayerRecursive(go, LayerMask.NameToLayer("UI"));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        private static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void SetTop(RectTransform rect, float width, float height, float x, float top)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, -top);
        }

        private static void SetCenter(RectTransform rect, float width, float height, float x, float y)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, y);
        }

        private static void AnchorTopLeft(RectTransform rect, float width, float height, float left, float top)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(left, -top);
        }

        private static void AnchorBottom(RectTransform rect, float width, float height, float x, float bottom)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, bottom);
        }

        private static void AnchorBottomRight(RectTransform rect, float width, float height, float right, float bottom)
        {
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(-right, bottom);
        }

        private static void SetBottomLeft(RectTransform rect, float width, float height, float left, float bottom)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(left, bottom);
        }

        private static void AnchorCenterRight(RectTransform rect, float width, float height, float right, float y)
        {
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(-right, y);
        }

        private static void AnchorMiddle(RectTransform rect, float width, float height, float x, float y)
        {
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, y);
        }

        private static void AddOutline(GameObject target, Color color, float distance)
        {
            var outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(distance, -distance);
            outline.useGraphicAlpha = true;
        }

        private static void AddShadow(GameObject target, Vector2 distance, Color color)
        {
            var shadow = target.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
        }

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            if (layer < 0)
            {
                return;
            }

            go.layer = layer;
            foreach (Transform child in go.transform)
            {
                SetLayerRecursive(child.gameObject, layer);
            }
        }

        private static void EnsureSceneInBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (var i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path == scenePath)
                {
                    scenes[i] = new EditorBuildSettingsScene(scenePath, true);
                    EditorBuildSettings.scenes = scenes.ToArray();
                    return;
                }
            }

            scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
