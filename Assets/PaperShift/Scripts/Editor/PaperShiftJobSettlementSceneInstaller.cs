using System.Collections.Generic;
using PaperShift.Model;
using PaperShift.Presenter;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace PaperShift.Editor
{
    public static class PaperShiftJobSettlementSceneInstaller
    {
        private const string LayoutName = "找到工作结算布局";
        private static readonly Font Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        [MenuItem("Paper Shift/Install Job Settlement UI")]
        public static void InstallFromMenu()
        {
            Install(true);
        }

        public static void InstallPaperShiftUiScene()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/PaperShiftUI.unity", OpenSceneMode.Single);
            Install(true);
        }

        private static void Install(bool force)
        {
            var screen = FindRetirementScreen();
            if (screen == null)
            {
                return;
            }

            var root = screen.transform;
            var existing = FindDirectChild(root, LayoutName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child != null)
                {
                    child.gameObject.SetActive(false);
                }
            }

            var binder = root.GetComponent<PaperShiftRetirementScreenBinder>();
            if (binder == null)
            {
                binder = root.gameObject.AddComponent<PaperShiftRetirementScreenBinder>();
            }

            screen.Screen = PaperShiftScreen.Retirement;
            binder.Screen = PaperShiftScreen.Retirement;
            SetScreenBackground(root);

            var bindings = new List<PaperShiftTextBinding>();
            var scoreRows = new List<PaperShiftHireScoreRowBinding>();
            var layout = CreateRect(root, LayoutName);
            Stretch(layout);
            layout.gameObject.SetActive(true);

            var header = CreateRect(layout, "顶部信息");
            AnchorTop(header, 72f, 0f);

            var back = CreateRounded(header, "返回图标", PaperShiftTheme.White, 18f);
            Anchor(back, new Vector2(54f, -35f), new Vector2(36f, 36f), new Vector2(0f, 1f));
            AddText(back, "Icon", "<", 20, PaperShiftTheme.Ink, TextAnchor.MiddleCenter);

            AddText(header, "标题", "入职", 22, PaperShiftTheme.Ink, TextAnchor.MiddleLeft, new RectOffset(96, 0, 0, 0));
            CreateCoin(header, new Vector2(294f, -36f));
            binder.CoinText = AddText(header, "金币数", "4,515", 19, PaperShiftTheme.Ink, TextAnchor.MiddleLeft, new RectOffset(318, 0, 0, 0));

            var hero = CreateRect(layout, "庆祝展示");
            AnchorTop(hero, 255f, 72f);
            BuildCelebration(hero, bindings);

            var ribbonShadow = CreateRounded(layout, "横幅阴影", PaperShiftTheme.Hex("#000000", 0.22f), 0f);
            AnchorTopCenter(ribbonShadow, 435f, 74f, 312f);

            var ribbon = CreateRounded(layout, "结算横幅", PaperShiftTheme.Purple, 0f);
            AnchorTopCenter(ribbon, 435f, 72f, 306f);
            binder.ReasonTitleText = AddText(ribbon, "标题", "恭喜，你入职了", 25, PaperShiftTheme.White, TextAnchor.MiddleCenter);

            var panel = CreateRounded(layout, "入职奖励面板", PaperShiftTheme.Hex("#fbf8ff"), 18f);
            AnchorTopCenter(panel, 431f, 388f, 384f);
            AddOutline(panel.gameObject, PaperShiftTheme.Hex("#bfa6db"), 1f);
            AddShadow(panel.gameObject, new Vector2(0f, -3f), PaperShiftTheme.Hex("#000000", 0.18f));

            AddText(panel, "分组标题", "入职奖励", 18, PaperShiftTheme.Purple, TextAnchor.UpperLeft, new RectOffset(16, 0, 12, 0));

            var rowsRoot = CreateRect(panel, "奖励列表");
            AnchorTopCenter(rowsRoot, 405f, 258f, 46f);
            var group = rowsRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            group.childAlignment = TextAnchor.UpperCenter;
            group.childForceExpandHeight = false;
            group.childForceExpandWidth = true;
            group.spacing = 8f;
            group.padding = new RectOffset(0, 0, 0, 0);
            var fitter = rowsRoot.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CreateRewardRow(rowsRoot, "岗位", "job", "新岗位 0分", scoreRows);
            CreateRewardRow(rowsRoot, "收入", "income", "月薪 0元 0分", scoreRows);
            CreateRewardRow(rowsRoot, "前景", "prospect", "成长空间一般 0分", scoreRows);
            CreateRewardRow(rowsRoot, "效率", "efficiency", "耗时 0个月 0分", scoreRows);
            CreateRewardRow(rowsRoot, "适配", "fit", "认可度 0% 0分", scoreRows);

            AddText(panel, "总计标签", "总计:", 17, PaperShiftTheme.Ink, TextAnchor.LowerLeft, new RectOffset(18, 0, 0, 18));
            CreateCoin(panel, new Vector2(351f, -354f), 22f);
            var totalText = AddText(panel, "总计数值", "0", 17, PaperShiftTheme.Ink, TextAnchor.LowerRight, new RectOffset(0, 28, 0, 18));
            bindings.Add(Binding("totalReward", totalText));

            var button = CreateButton(layout, "领取奖励按钮", "领取奖励", 431f, 67f);
            AnchorTopCenter(button.GetComponent<RectTransform>(), 431f, 67f, 800f);
            binder.FinishButton = button;

            binder.SettlementTexts = bindings.ToArray();
            binder.ScoreRows = scoreRows.ToArray();
            binder.ScoreTotalText = totalText;
            EditorUtility.SetDirty(binder);
            EditorUtility.SetDirty(root.gameObject);
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            EditorSceneManager.SaveScene(root.gameObject.scene);
        }

        private static void BuildCelebration(RectTransform parent, List<PaperShiftTextBinding> bindings)
        {
            var heart = CreateRounded(parent, "花环背景", PaperShiftTheme.Hex("#c8143d"), 70f);
            Anchor(heart, new Vector2(282f, -126f), new Vector2(226f, 166f), new Vector2(0f, 1f));
            AddShadow(heart.gameObject, new Vector2(0f, -4f), PaperShiftTheme.Hex("#000000", 0.16f));

            for (var i = 0; i < 14; i++)
            {
                var flower = CreateRounded(heart, "玫瑰花 " + (i + 1), i % 2 == 0 ? PaperShiftTheme.Hex("#e52754") : PaperShiftTheme.Hex("#9e1132"), 12f);
                var angle = i * Mathf.PI * 2f / 14f;
                var x = Mathf.Cos(angle) * 93f;
                var y = Mathf.Sin(angle) * 58f;
                Anchor(flower, new Vector2(113f + x, -83f + y), new Vector2(34f, 24f), new Vector2(0f, 1f));
            }

            AddText(parent, "装饰爱心", "✦  ✦      ❤      ✦  ✦", 24, PaperShiftTheme.White, TextAnchor.UpperCenter, new RectOffset(0, 0, 14, 0));

            var worker = CreatePersonCard(parent, "劳动者卡片", new Vector2(208f, -116f), PaperShiftTheme.Hex("#e9b347"));
            bindings.Add(Binding("workerName", AddText(worker, "姓名", "孙方舟", 15, PaperShiftTheme.Ink, TextAnchor.LowerCenter, new RectOffset(0, 0, 0, 8))));
            bindings.Add(Binding("workerMeta", AddText(parent, "劳动者信息", "男 24岁 现代城市", 16, PaperShiftTheme.Ink, TextAnchor.UpperCenter, new RectOffset(0, 0, 220, 0))));

            var job = CreatePersonCard(parent, "工作卡片", new Vector2(306f, -116f), PaperShiftTheme.Hex("#75c8f3"));
            bindings.Add(Binding("jobTitle", AddText(job, "岗位", "新岗位", 14, PaperShiftTheme.Ink, TextAnchor.LowerCenter, new RectOffset(0, 0, 0, 8))));
            bindings.Add(Binding("companyName", AddText(parent, "公司信息", "新公司", 16, PaperShiftTheme.Ink, TextAnchor.UpperCenter, new RectOffset(0, 0, 244, 0))));
        }

        private static RectTransform CreatePersonCard(Transform parent, string name, Vector2 position, Color portraitColor)
        {
            var card = CreateRounded(parent, name, PaperShiftTheme.White, 6f);
            Anchor(card, position, new Vector2(82f, 120f), new Vector2(0f, 1f));
            AddShadow(card.gameObject, new Vector2(0f, -2f), PaperShiftTheme.Hex("#000000", 0.18f));

            var portrait = CreateRounded(card, "头像", portraitColor, 18f);
            Anchor(portrait, new Vector2(41f, -40f), new Vector2(56f, 56f), new Vector2(0f, 1f));
            var face = CreateRounded(portrait, "脸", PaperShiftTheme.Hex("#ffd0c3"), 20f);
            Anchor(face, new Vector2(28f, -32f), new Vector2(30f, 30f), new Vector2(0f, 1f));
            AddText(face, "表情", "• •", 12, PaperShiftTheme.Ink, TextAnchor.MiddleCenter);
            return card;
        }

        private static void CreateRewardRow(Transform parent, string label, string id, string value, List<PaperShiftHireScoreRowBinding> scoreRows)
        {
            var row = CreateRounded(parent, "奖励 " + label, PaperShiftTheme.White, 8f);
            var layout = row.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 48f;

            var rare = CreateTierBadge(row, "稀有", "★ 稀有!", PaperShiftTheme.Hex("#caa5ff"));
            var superRare = CreateTierBadge(row, "超稀有", "★★超稀有!", PaperShiftTheme.Hex("#d7b8ff"));

            var labelShade = CreateRounded(row, "标签底", PaperShiftTheme.Hex("#f0f0f0"), 7f);
            AnchorLeft(labelShade, 58f, 48f, 0f);
            AddText(labelShade, "标签", label, 15, PaperShiftTheme.MutedInk, TextAnchor.MiddleCenter);

            var valueText = AddText(row, "值", value, 16, PaperShiftTheme.Ink, TextAnchor.MiddleCenter, new RectOffset(74, 72, 0, 0));

            CreateCoin(row, new Vector2(350f, -24f), 22f);
            var pointsText = AddText(row, "总计数值", "0", 15, PaperShiftTheme.Ink, TextAnchor.MiddleRight, new RectOffset(0, 28, 0, 0));
            if (scoreRows != null)
            {
                scoreRows.Add(new PaperShiftHireScoreRowBinding
                {
                    Id = id,
                    Root = row.gameObject,
                    ValueText = valueText,
                    PointsText = pointsText,
                    RareTierRoot = rare.gameObject,
                    SuperRareTierRoot = superRare.gameObject,
                    RareTierText = rare.GetComponentInChildren<Text>(true),
                    SuperRareTierText = superRare.GetComponentInChildren<Text>(true)
                });
            }
        }

        private static RectTransform CreateTierBadge(Transform parent, string name, string label, Color color)
        {
            var badge = CreateRounded(parent, name, color, 8f);
            Anchor(badge, new Vector2(144f, -24f), new Vector2(104f, 34f), new Vector2(0f, 1f));
            AddText(badge, "Badge", label, 16, PaperShiftTheme.White, TextAnchor.MiddleCenter);
            badge.gameObject.SetActive(false);
            return badge;
        }

        private static Button CreateButton(Transform parent, string name, string label, float width, float height)
        {
            var root = CreateRounded(parent, name, PaperShiftTheme.White, 16f);
            AddOutline(root.gameObject, PaperShiftTheme.Ink, 3f);
            AddShadow(root.gameObject, new Vector2(0f, -2f), PaperShiftTheme.Hex("#000000", 0.16f));
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = root.GetComponent<Graphic>();
            AddText(root, "文字", label, 22, PaperShiftTheme.Ink, TextAnchor.MiddleCenter);
            return button;
        }

        private static void CreateCoin(Transform parent, Vector2 position, float size = 24f)
        {
            var coin = CreateRounded(parent, "金币", PaperShiftTheme.Gold, size * 0.5f);
            Anchor(coin, position, new Vector2(size, size), new Vector2(0f, 1f));
            AddOutline(coin.gameObject, PaperShiftTheme.Orange, 2f);
            AddText(coin, "火", "♜", Mathf.RoundToInt(size * 0.7f), PaperShiftTheme.White, TextAnchor.MiddleCenter);
        }

        private static PaperShiftTextBinding Binding(string id, Text text)
        {
            return new PaperShiftTextBinding { Id = id, Text = text };
        }

        private static PaperShiftScreenView FindRetirementScreen()
        {
            var screens = Object.FindObjectsOfType<PaperShiftScreenView>(true);
            for (var i = 0; i < screens.Length; i++)
            {
                if (screens[i] != null && screens[i].Screen == PaperShiftScreen.Retirement)
                {
                    return screens[i];
                }
            }

            return null;
        }

        private static RectTransform FindDirectChild(Transform parent, string name)
        {
            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child != null && child.name == name)
                {
                    return child as RectTransform;
                }
            }

            return null;
        }

        private static RectTransform CreateRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
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

        private static void SetScreenBackground(Transform root)
        {
            var graphic = root.GetComponent<Graphic>();
            if (graphic != null)
            {
                graphic.color = PaperShiftTheme.PagePurple;
                EditorUtility.SetDirty(graphic);
            }
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void AnchorTop(RectTransform rect, float height, float top)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -top);
            rect.sizeDelta = new Vector2(0f, height);
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

        private static void AnchorLeft(RectTransform rect, float width, float height, float x)
        {
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(x, 0f);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
