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
    [InitializeOnLoad]
    public static class PaperShiftInheritanceSceneInstaller
    {
        private const string ScreenName = "传承界面";
        private static readonly Font Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        static PaperShiftInheritanceSceneInstaller()
        {
            EditorApplication.delayCall += InstallIfMissingInOpenScene;
        }

        [MenuItem("Paper Shift/Install Inheritance UI")]
        public static void InstallFromMenu()
        {
            Install(true);
        }

        public static void InstallPaperShiftUiScene()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/PaperShiftUI.unity", OpenSceneMode.Single);
            Install(true);
        }

        private static void InstallIfMissingInOpenScene()
        {
            var controller = Object.FindObjectOfType<PaperShiftSceneController>(true);
            if (controller == null || FindScreen(PaperShiftScreen.Inheritance) != null)
            {
                return;
            }

            Install(false);
        }

        private static void Install(bool force)
        {
            var controller = Object.FindObjectOfType<PaperShiftSceneController>(true);
            var host = Object.FindObjectOfType<PaperShiftPrototypeBinder>(true);
            var retirement = FindScreen(PaperShiftScreen.Retirement);
            if (controller == null || host == null || retirement == null)
            {
                return;
            }

            var existing = FindScreen(PaperShiftScreen.Inheritance);
            if (existing != null)
            {
                if (!force)
                {
                    return;
                }

                Object.DestroyImmediate(existing.gameObject);
            }

            var parent = retirement.transform.parent;
            var screenRoot = CreateRounded(parent, ScreenName, PaperShiftTheme.PagePurple, 0f);
            screenRoot.gameObject.SetActive(false);
            Stretch(screenRoot);
            screenRoot.SetAsLastSibling();

            var screenView = screenRoot.gameObject.AddComponent<PaperShiftScreenView>();
            screenView.Screen = PaperShiftScreen.Inheritance;
            var view = screenRoot.gameObject.AddComponent<PaperShiftInheritanceViewReferences>();
            var binder = screenRoot.gameObject.AddComponent<PaperShiftInheritanceScreenBinder>();
            binder.Screen = PaperShiftScreen.Inheritance;
            binder.View = view;

            BuildInheritanceScreen(screenRoot, view);
            AppendScreenView(controller, screenView);
            AppendBinder(host, binder);

            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(host);
            EditorUtility.SetDirty(screenRoot.gameObject);
            EditorSceneManager.MarkSceneDirty(screenRoot.gameObject.scene);
            EditorSceneManager.SaveScene(screenRoot.gameObject.scene);
        }

        private static void BuildInheritanceScreen(RectTransform root, PaperShiftInheritanceViewReferences view)
        {
            var summary = new List<PaperShiftTextBinding>();
            var selected = new List<PaperShiftTextBinding>();

            var topbar = CreateRect(root, "顶部信息");
            AnchorTop(topbar, 36f, 30f, 23f, 23f);
            var back = CreateRounded(topbar, "返回图标", PaperShiftTheme.White, 14f);
            Anchor(back, new Vector2(14f, -18f), new Vector2(28f, 28f), new Vector2(0f, 1f));
            AddText(back, "Icon", "<", 18, PaperShiftTheme.Ink, TextAnchor.MiddleCenter);
            AddText(topbar, "标题", "人生总结", 17, PaperShiftTheme.Ink, TextAnchor.MiddleLeft, new RectOffset(43, 0, 0, 0));
            CreateCoin(topbar, new Vector2(205f, -18f));
            view.CoinText = AddText(topbar, "金币数", "4,515", 15, PaperShiftTheme.Ink, TextAnchor.MiddleLeft, new RectOffset(229, 0, 0, 0));

            var ribbon = CreateRounded(root, "结束横幅", PaperShiftTheme.Blue, 0f);
            AnchorTopCenter(ribbon, 347f, 62f, 84f);
            AddShadow(ribbon.gameObject, new Vector2(0f, -5f), PaperShiftTheme.Hex("#2a4a70", 0.32f));
            AddText(ribbon, "标题", "这一代打工人生结束了", 22, PaperShiftTheme.White, TextAnchor.MiddleCenter);

            var summaryCard = CreateRounded(root, "人生推演卡", PaperShiftTheme.Hex("#f8fdff", 0.96f), 18f);
            AnchorTopCenter(summaryCard, 347f, 298f, 160f);
            AddShadow(summaryCard.gameObject, new Vector2(0f, -3f), PaperShiftTheme.Hex("#4a4565", 0.18f));
            AddText(summaryCard, "标题", "系统推演：后来的人生", 18, PaperShiftTheme.Hex("#4c535b"), TextAnchor.UpperCenter, new RectOffset(0, 0, 10, 0));

            var metricsRoot = CreateRect(summaryCard, "推演指标");
            AnchorTopCenter(metricsRoot, 321f, 58f, 45f);
            var metrics = metricsRoot.gameObject.AddComponent<GridLayoutGroup>();
            metrics.cellSize = new Vector2(75.75f, 58f);
            metrics.spacing = new Vector2(6f, 0f);
            metrics.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            metrics.constraintCount = 4;

            CreateMetric(metricsRoot, "最终职业", "job", "AI训练师", summary);
            CreateMetric(metricsRoot, "工作年限", "years", "18年", summary);
            CreateMetric(metricsRoot, "压力", "pressure", "偏高", summary);
            CreateMetric(metricsRoot, "前景", "prospect", "上升", summary);

            var story = CreateRounded(summaryCard, "人生短评", PaperShiftTheme.White, 12f);
            AnchorTopCenter(story, 321f, 75f, 113f);
            AddOutline(story.gameObject, PaperShiftTheme.Line, 1f);
            summary.Add(Binding("story", AddText(story, "内容", "他把这份工作干成了长期饭碗。行业还在往上走，所以日子比刚入职时稳了许多；只是压力也跟着留下来。", 13, PaperShiftTheme.Ink, TextAnchor.MiddleLeft, new RectOffset(12, 12, 0, 0))));

            var lifeRoot = CreateRect(summaryCard, "人生节点");
            AnchorTopCenter(lifeRoot, 321f, 52f, 198f);
            var lifeGrid = lifeRoot.gameObject.AddComponent<GridLayoutGroup>();
            lifeGrid.cellSize = new Vector2(102.33f, 52f);
            lifeGrid.spacing = new Vector2(7f, 0f);
            lifeGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            lifeGrid.constraintCount = 3;

            CreateLifeEvent(lifeRoot, "marriageAge", "32岁", "marriageText", "结婚\n生活合并账本", summary);
            CreateLifeEvent(lifeRoot, "houseAge", "36岁", "houseText", "买房\n家境变得稳定", summary);
            CreateLifeEvent(lifeRoot, "childAge", "38岁", "childText", "生子\n下一代登场", summary);

            var choose = CreateRounded(root, "下一代选择卡", PaperShiftTheme.Hex("#f8fdff", 0.96f), 18f);
            AnchorTopCenter(choose, 347f, 321f, 470f);
            AddShadow(choose.gameObject, new Vector2(0f, -3f), PaperShiftTheme.Hex("#4a4565", 0.18f));
            AddText(choose, "标题", "从孩子里选择下一代", 18, PaperShiftTheme.Hex("#4c535b"), TextAnchor.UpperCenter, new RectOffset(0, 0, 10, 0));

            var childrenRoot = CreateRect(choose, "孩子列表");
            AnchorTopCenter(childrenRoot, 311f, 72f, 44f);
            var childGrid = childrenRoot.gameObject.AddComponent<GridLayoutGroup>();
            childGrid.cellSize = new Vector2(72.5f, 72f);
            childGrid.spacing = new Vector2(7f, 0f);
            childGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            childGrid.constraintCount = 4;

            var cards = new PaperShiftInheritanceHeirCardReferences[4];
            for (var i = 0; i < cards.Length; i++)
            {
                cards[i] = CreateHeirCard(childrenRoot, i);
            }

            view.HeirCards = cards;

            var profile = CreateRounded(choose, "孩子属性格子", PaperShiftTheme.White, 12f);
            AnchorTopCenter(profile, 321f, 126f, 125f);
            AddOutline(profile.gameObject, PaperShiftTheme.Line, 1f);
            var statsRoot = CreateRect(profile, "属性列表");
            Stretch(statsRoot, 9f, 9f, 9f, 9f);
            var statsGrid = statsRoot.gameObject.AddComponent<GridLayoutGroup>();
            statsGrid.cellSize = new Vector2(99f, 28f);
            statsGrid.spacing = new Vector2(4f, 5f);
            statsGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            statsGrid.constraintCount = 3;

            CreateStat(statsRoot, "姓名", "name", "孙小澈", selected);
            CreateStat(statsRoot, "性别", "gender", "女", selected);
            CreateStat(statsRoot, "年龄", "age", "24岁", selected);
            CreateStat(statsRoot, "家境", "family", "小康", selected);
            CreateStat(statsRoot, "教育", "education", "普通本科", selected);
            CreateStat(statsRoot, "形象", "appearance", "较善5分", selected);
            CreateStat(statsRoot, "能力", "ability", "会用AI", selected);
            CreateStat(statsRoot, "身高", "height", "168cm", selected);
            CreateStat(statsRoot, "性格", "personality", "谨慎", selected);
            CreateStat(statsRoot, "月入", "income", "0元", selected);

            var tagRoot = CreateRect(choose, "孩子初始标签");
            AnchorTopCenter(tagRoot, 321f, 31f, 260f);
            var tagGrid = tagRoot.gameObject.AddComponent<GridLayoutGroup>();
            tagGrid.cellSize = new Vector2(75.75f, 28f);
            tagGrid.spacing = new Vector2(6f, 0f);
            tagGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            tagGrid.constraintCount = 4;
            view.InitialTagTexts = new[]
            {
                CreateTag(tagRoot, "AI耳濡目染", PaperShiftTheme.BlueTicket),
                CreateTag(tagRoot, "城市生活", PaperShiftTheme.PurpleTicket),
                CreateTag(tagRoot, "谨慎做事", PaperShiftTheme.White),
                CreateTag(tagRoot, "父母期望", PaperShiftTheme.White)
            };

            view.ContinueButton = CreateButton(choose, "开启下一代按钮", "选好了，开启下一代");
            AnchorTopCenter(view.ContinueButton.GetComponent<RectTransform>(), 321f, 52f, 303f);

            view.SummaryTexts = summary.ToArray();
            view.SelectedHeirTexts = selected.ToArray();
        }

        private static void CreateMetric(Transform parent, string label, string id, string value, ICollection<PaperShiftTextBinding> bindings)
        {
            var root = CreateRounded(parent, "指标 " + label, PaperShiftTheme.White, 10f);
            AddOutline(root.gameObject, PaperShiftTheme.Line, 1f);
            AddText(root, "标签", label, 11, PaperShiftTheme.MutedInk, TextAnchor.UpperCenter, new RectOffset(0, 0, 8, 0));
            bindings.Add(Binding(id, AddText(root, "值", value, 14, PaperShiftTheme.Ink, TextAnchor.LowerCenter, new RectOffset(0, 0, 0, 9))));
        }

        private static void CreateLifeEvent(Transform parent, string ageId, string ageValue, string textId, string textValue, ICollection<PaperShiftTextBinding> bindings)
        {
            var root = CreateRounded(parent, "节点 " + ageValue, PaperShiftTheme.White, 10f);
            AddOutline(root.gameObject, PaperShiftTheme.Line, 1f);
            bindings.Add(Binding(ageId, AddText(root, "年龄", ageValue, 13, PaperShiftTheme.Blue, TextAnchor.UpperCenter, new RectOffset(0, 0, 6, 0))));
            bindings.Add(Binding(textId, AddText(root, "事件", textValue, 12, PaperShiftTheme.Ink, TextAnchor.LowerCenter, new RectOffset(4, 4, 0, 6))));
        }

        private static PaperShiftInheritanceHeirCardReferences CreateHeirCard(Transform parent, int index)
        {
            var root = CreateRounded(parent, "孩子卡片 " + (index + 1), PaperShiftTheme.White, 10f);
            AddShadow(root.gameObject, new Vector2(0f, -2f), PaperShiftTheme.Hex("#000000", 0.16f));
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = root.GetComponent<Graphic>();

            var selected = CreateRounded(root, "选中框", PaperShiftTheme.Hex("#ffffff", 0f), 10f);
            Stretch(selected);
            AddOutline(selected.gameObject, PaperShiftTheme.Ink, 2f);
            selected.gameObject.SetActive(index == 0);

            CreatePortrait(root, index % 2 == 1);
            var name = AddText(root, "姓名", index == 0 ? "孙小澈" : "孩子", 11, PaperShiftTheme.Ink, TextAnchor.LowerCenter, new RectOffset(2, 2, 0, 5));
            var meta = AddText(root, "年龄", "24岁", 10, PaperShiftTheme.MutedInk, TextAnchor.LowerCenter, new RectOffset(2, 2, 0, 20));

            var refs = root.gameObject.AddComponent<PaperShiftInheritanceHeirCardReferences>();
            refs.Button = button;
            refs.SelectedState = selected.gameObject;
            refs.NameText = name;
            refs.MetaText = meta;
            return refs;
        }

        private static void CreatePortrait(Transform parent, bool blue)
        {
            var portrait = CreateRounded(parent, "头像", blue ? PaperShiftTheme.BlueTicket : PaperShiftTheme.Hex("#e9b347"), 12f);
            Anchor(portrait, new Vector2(0f, 12f), new Vector2(42f, 42f), new Vector2(0.5f, 0.5f));
            AddOutline(portrait.gameObject, PaperShiftTheme.White, 2f);
            var hair = CreateRounded(portrait, "头发", PaperShiftTheme.Hex("#253970"), 6f);
            Anchor(hair, new Vector2(0f, 9f), new Vector2(22f, 13f), new Vector2(0.5f, 0.5f));
            var face = CreateRounded(portrait, "脸", PaperShiftTheme.Hex("#ffd0c3"), 10f);
            Anchor(face, new Vector2(0f, -4f), new Vector2(20f, 20f), new Vector2(0.5f, 0.5f));
            AddText(face, "表情", "• •", 9, PaperShiftTheme.Ink, TextAnchor.MiddleCenter);
        }

        private static void CreateStat(Transform parent, string label, string id, string value, ICollection<PaperShiftTextBinding> bindings)
        {
            var root = CreateRounded(parent, "属性 " + label, PaperShiftTheme.Hex("#f7fbff"), 7f);
            AddOutline(root.gameObject, PaperShiftTheme.Line, 1f);
            var shade = CreateRounded(root, "标签底", PaperShiftTheme.Hex("#f0f0f0"), 7f);
            AnchorLeft(shade, 34f);
            AddText(shade, "标签", label, 11, PaperShiftTheme.MutedInk, TextAnchor.MiddleCenter);
            bindings.Add(Binding(id, AddText(root, "值", value, 11, PaperShiftTheme.Ink, TextAnchor.MiddleCenter, new RectOffset(34, 0, 0, 0))));
        }

        private static Text CreateTag(Transform parent, string label, Color color)
        {
            var root = CreateRounded(parent, "初始标签 " + label, color, 8f);
            AddOutline(root.gameObject, color == PaperShiftTheme.White ? PaperShiftTheme.Ink : PaperShiftTheme.Blue, 2f);
            return AddText(root, "文字", label, 12, PaperShiftTheme.Ink, TextAnchor.MiddleCenter, new RectOffset(3, 3, 0, 0));
        }

        private static Button CreateButton(Transform parent, string name, string label)
        {
            var root = CreateRounded(parent, name, PaperShiftTheme.White, 14f);
            AddOutline(root.gameObject, PaperShiftTheme.Ink, 3f);
            AddShadow(root.gameObject, new Vector2(0f, -2f), PaperShiftTheme.Hex("#000000", 0.16f));
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = root.GetComponent<Graphic>();
            AddText(root, "文字", label, 18, PaperShiftTheme.Ink, TextAnchor.MiddleCenter);
            return button;
        }

        private static void CreateCoin(Transform parent, Vector2 position)
        {
            var coin = CreateRounded(parent, "金币", PaperShiftTheme.Gold, 10.5f);
            Anchor(coin, position, new Vector2(21f, 21f), new Vector2(0f, 1f));
            AddOutline(coin.gameObject, PaperShiftTheme.Orange, 2f);
            AddText(coin, "火", "A", 11, PaperShiftTheme.White, TextAnchor.MiddleCenter);
        }

        private static PaperShiftTextBinding Binding(string id, Text text)
        {
            return new PaperShiftTextBinding { Id = id, Text = text };
        }

        private static PaperShiftScreenView FindScreen(PaperShiftScreen screen)
        {
            var screens = Object.FindObjectsOfType<PaperShiftScreenView>(true);
            for (var i = 0; i < screens.Length; i++)
            {
                if (screens[i] != null && screens[i].Screen == screen)
                {
                    return screens[i];
                }
            }

            return null;
        }

        private static void AppendScreenView(PaperShiftSceneController controller, PaperShiftScreenView view)
        {
            var views = new List<PaperShiftScreenView>();
            if (controller.ScreenViews != null)
            {
                views.AddRange(controller.ScreenViews);
            }

            views.RemoveAll(item => item == null || item.Screen == PaperShiftScreen.Inheritance);
            views.Add(view);
            controller.ScreenViews = views.ToArray();
        }

        private static void AppendBinder(PaperShiftPrototypeBinder host, PaperShiftScreenBinderBase binder)
        {
            var binders = new List<PaperShiftScreenBinderBase>();
            if (host.ScreenBinders != null)
            {
                binders.AddRange(host.ScreenBinders);
            }

            binders.RemoveAll(item => item == null || item.Screen == PaperShiftScreen.Inheritance);
            binders.Add(binder);
            host.ScreenBinders = binders.ToArray();
        }

        private static RectTransform CreateRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.layer = parent == null ? 5 : parent.gameObject.layer;
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static RectTransform CreateRounded(Transform parent, string name, Color color, float radius)
        {
            var rect = CreateRect(parent, name);
            var graphic = rect.gameObject.AddComponent<RoundedRectGraphic>();
            graphic.color = color;
            graphic.CornerRadius = radius;
            return rect;
        }

        private static Text AddText(Transform parent, string name, string value, int size, Color color, TextAnchor anchor, RectOffset padding = null)
        {
            var rect = CreateRect(parent, name);
            Stretch(rect);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            text.alignment = anchor;
            text.raycastTarget = false;
            text.supportRichText = true;
            text.text = value;

            if (padding != null)
            {
                rect.offsetMin = new Vector2(padding.left, padding.bottom);
                rect.offsetMax = new Vector2(-padding.right, -padding.top);
            }

            return text;
        }

        private static void AddOutline(GameObject go, Color color, float distance)
        {
            var outline = go.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(distance, -distance);
        }

        private static void AddShadow(GameObject go, Vector2 distance, Color color)
        {
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
        }

        private static void Stretch(RectTransform rect)
        {
            Stretch(rect, 0f, 0f, 0f, 0f);
        }

        private static void Stretch(RectTransform rect, float left, float top, float right, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void AnchorTop(RectTransform rect, float height, float top, float left, float right)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2((left - right) * 0.5f, -top);
            rect.sizeDelta = new Vector2(-(left + right), height);
        }

        private static void AnchorTopCenter(RectTransform rect, float width, float height, float top)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -top);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void Anchor(RectTransform rect, Vector2 position, Vector2 size, Vector2 anchor)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void AnchorLeft(RectTransform rect, float width)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(width, 0f);
        }
    }
}
