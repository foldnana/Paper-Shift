using System.Collections.Generic;
using System.IO;
using PaperShift.Controller;
using PaperShift.Model;
using PaperShift.Presenter;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PaperShift.EditorTools
{
    public static class PaperShiftResumeSceneInstaller
    {
        private const string ScenePath = "Assets/Scenes/PaperShiftUI.unity";
        private const string AutoInstallRequestPath = "Temp/install_resume_ui.request";
        private static Font defaultFont;

        [InitializeOnLoadMethod]
        private static void AutoInstallIfRequested()
        {
            if (Application.isBatchMode || !File.Exists(AutoInstallRequestPath))
            {
                return;
            }

            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(AutoInstallRequestPath))
                {
                    return;
                }

                File.Delete(AutoInstallRequestPath);
                EditorSceneManager.SaveOpenScenes();
                Install(true);
            };
        }

        [MenuItem("Paper Shift/Install Info Resume UI")]
        public static void InstallCurrentScene()
        {
            Install(false);
        }

        public static void InstallPaperShiftUI()
        {
            Install(true);
        }

        private static void Install(bool openScene)
        {
            if (openScene)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var controller = Object.FindObjectOfType<PaperShiftSceneController>();
            if (controller == null)
            {
                throw new System.InvalidOperationException("PaperShiftSceneController not found.");
            }

            var screen = FindScreen(controller, PaperShiftScreen.Resume);
            if (screen == null)
            {
                throw new System.InvalidOperationException("Resume screen not found.");
            }

            Undo.RegisterFullObjectHierarchyUndo(screen.gameObject, "Install Info Resume UI");
            ClearChildren(screen.transform);
            var binder = GetOrAdd<PaperShiftResumeScreenBinder>(screen.gameObject);
            binder.Screen = PaperShiftScreen.Resume;

            Build(screen.transform, binder);
            EditorUtility.SetDirty(screen.gameObject);
            EditorUtility.SetDirty(binder);
            EditorSceneManager.MarkSceneDirty(screen.gameObject.scene);

            if (openScene)
            {
                EditorSceneManager.SaveScene(screen.gameObject.scene);
            }
        }

        private static PaperShiftScreenView FindScreen(PaperShiftSceneController controller, PaperShiftScreen screen)
        {
            for (var i = 0; controller.ScreenViews != null && i < controller.ScreenViews.Length; i++)
            {
                var view = controller.ScreenViews[i];
                if (view != null && view.Screen == screen)
                {
                    return view;
                }
            }

            return null;
        }

        private static void Build(Transform root, PaperShiftResumeScreenBinder binder)
        {
            var rows = new List<PaperShiftInfoRowBinding>();

            var back = CreateButton(root, "Back Button", "<", new Vector2(91f, -44f), new Vector2(42f, 42f), PaperShiftTheme.White, PaperShiftTheme.Ink, 22, new Vector2(0f, 1f));
            binder.BackButton = back;

            var title = CreateText(root, "Title", "隐瞒信息", 21, PaperShiftTheme.Ink, TextAnchor.MiddleLeft);
            SetRect(title.rectTransform, new Vector2(128f, -45f), new Vector2(180f, 42f), new Vector2(0f, 1f));

            var coinIcon = CreateRounded(root, "Coin Icon", PaperShiftTheme.Gold, 18f);
            SetRect(coinIcon, new Vector2(360f, -44f), new Vector2(28f, 28f), new Vector2(0f, 1f));
            Stretch(CreateText(coinIcon, "Coin Icon Text", "币", 15, PaperShiftTheme.White, TextAnchor.MiddleCenter).rectTransform);

            binder.CoinText = CreateText(root, "Coin Amount", "4,515", 18, PaperShiftTheme.Ink, TextAnchor.MiddleLeft);
            SetRect(binder.CoinText.rectTransform, new Vector2(392f, -45f), new Vector2(130f, 36f), new Vector2(0f, 1f));

            var card = CreateRounded(root, "Info Resume Card", PaperShiftTheme.Paper, 18f);
            SetRect(card, new Vector2(0f, -404f), new Vector2(398f, 652f), new Vector2(0.5f, 1f));

            var ribbon = CreateRounded(card, "Generation Ribbon", PaperShiftTheme.Blue, 0f);
            SetRect(ribbon, new Vector2(-176f, -24f), new Vector2(76f, 36f), new Vector2(0.5f, 1f));
            ribbon.localRotation = Quaternion.Euler(0f, 0f, 315f);
            binder.GenerationText = CreateText(ribbon, "Generation Text", "第1代", 16, PaperShiftTheme.White, TextAnchor.MiddleCenter);
            Stretch(binder.GenerationText.rectTransform);

            binder.HeaderNameText = CreateText(card, "Worker Name", "孙 方舟", 22, PaperShiftTheme.Ink, TextAnchor.MiddleLeft);
            SetRect(binder.HeaderNameText.rectTransform, new Vector2(-96f, -54f), new Vector2(210f, 34f), new Vector2(0.5f, 1f));

            binder.HeaderMetaText = CreateText(card, "Worker Meta", "男 24 岁 现代城市", 18, PaperShiftTheme.Ink, TextAnchor.MiddleLeft);
            SetRect(binder.HeaderMetaText.rectTransform, new Vector2(-96f, -88f), new Vector2(250f, 30f), new Vector2(0.5f, 1f));

            var avatar = CreateRounded(card, "Avatar", PaperShiftTheme.White, 14f);
            SetRect(avatar, new Vector2(132f, -70f), new Vector2(86f, 86f), new Vector2(0.5f, 1f));
            var face = CreateRounded(avatar, "Face", PaperShiftTheme.Hex("#f0a43d"), 16f);
            SetRect(face, Vector2.zero, new Vector2(58f, 58f), new Vector2(0.5f, 0.5f));
            Stretch(CreateText(face, "Face Dot", "●", 28, PaperShiftTheme.Hex("#ffd08f"), TextAnchor.MiddleCenter).rectTransform);

            rows.Add(CreateInfoRow(card, "appearance", "形象", "★ 稀有!", "较好 6分", -154f));
            rows.Add(CreateInfoRow(card, "height", "身高", "★ 稀有!", "181cm", -201f));
            rows.Add(CreateInfoRow(card, "education", "教育", "", "普通本科", -248f));
            rows.Add(CreateInfoRow(card, "family", "家境", "", "小康", -295f));
            rows.Add(CreateInfoRow(card, "career", "职业", "★ 稀有!", "金融业", -342f));
            rows.Add(CreateInfoRow(card, "income", "月入", "★ 超稀有!", "5万元", -389f));

            CreateTrait(card, "勤俭节约", new Vector2(-116f, -462f), false);
            CreateTrait(card, "独具匠心", new Vector2(88f, -462f), true);
            CreateTrait(card, "随心所欲", new Vector2(-116f, -514f), false);

            var footer = CreateRounded(card, "Edit Count Footer", PaperShiftTheme.White, 16f);
            SetRect(footer, new Vector2(0f, -602f), new Vector2(366f, 66f), new Vector2(0.5f, 1f));
            var prompt = CreateText(footer, "Prompt", "是否有想隐瞒或夸大的信息？", 16, PaperShiftTheme.Ink, TextAnchor.MiddleLeft);
            SetRect(prompt.rectTransform, new Vector2(-76f, 0f), new Vector2(220f, 46f), new Vector2(0.5f, 0.5f));
            binder.EditedCountText = CreateText(footer, "Edited Count", "0/3 <size=14>可选</size>", 34, PaperShiftTheme.Ink, TextAnchor.MiddleRight);
            binder.RiskText = binder.EditedCountText;
            SetRect(binder.EditedCountText.rectTransform, new Vector2(116f, 0f), new Vector2(116f, 48f), new Vector2(0.5f, 0.5f));

            binder.SendResumeButton = CreateButton(root, "Prepare Interview Button", "准备面试", new Vector2(0f, 58f), new Vector2(398f, 58f), PaperShiftTheme.White, PaperShiftTheme.Ink, 20, new Vector2(0.5f, 0f));
            binder.InfoRows = rows.ToArray();
            binder.IntentButtons = new PaperShiftSelectableTextBinding[0];
            binder.ResumeLines = new PaperShiftResumeLineBinding[0];
            binder.TagPoolRoot = null;
        }

        private static PaperShiftInfoRowBinding CreateInfoRow(Transform parent, string id, string label, string badge, string value, float y)
        {
            var hide = CreateButton(parent, "Hide " + id, "-", new Vector2(-170f, y), new Vector2(34f, 34f), PaperShiftTheme.White, PaperShiftTheme.MutedInk, 22, new Vector2(0.5f, 1f));
            var row = CreateRounded(parent, "Info Row " + id, string.IsNullOrEmpty(badge) ? PaperShiftTheme.White : PaperShiftTheme.BlueLight, 8f);
            SetRect(row, new Vector2(18f, y), new Vector2(310f, 42f), new Vector2(0.5f, 1f));
            var labelText = CreateText(row, "Label", label, 16, PaperShiftTheme.MutedInk, TextAnchor.MiddleLeft);
            SetRect(labelText.rectTransform, new Vector2(-120f, 0f), new Vector2(78f, 34f), new Vector2(0.5f, 0.5f));
            var badgeText = CreateText(row, "Badge", badge, 16, PaperShiftTheme.White, TextAnchor.MiddleLeft);
            SetRect(badgeText.rectTransform, new Vector2(-42f, 0f), new Vector2(84f, 34f), new Vector2(0.5f, 0.5f));
            var valueText = CreateText(row, "Value", value, 17, PaperShiftTheme.Ink, TextAnchor.MiddleCenter);
            SetRect(valueText.rectTransform, new Vector2(62f, 0f), new Vector2(150f, 34f), new Vector2(0.5f, 0.5f));
            var exaggerate = CreateButton(parent, "Exaggerate " + id, "★", new Vector2(184f, y), new Vector2(34f, 34f), PaperShiftTheme.White, PaperShiftTheme.Blue, 18, new Vector2(0.5f, 1f));

            return new PaperShiftInfoRowBinding
            {
                FieldId = id,
                Background = row.GetComponent<Graphic>(),
                LabelText = labelText,
                BadgeText = badgeText,
                ValueText = valueText,
                HideButton = hide,
                ExaggerateButton = exaggerate
            };
        }

        private static void CreateTrait(Transform parent, string text, Vector2 position, bool rare)
        {
            var chip = CreateRounded(parent, "Trait " + text, rare ? PaperShiftTheme.BlueTicket : PaperShiftTheme.White, 7f);
            SetRect(chip, position, new Vector2(112f, 36f), new Vector2(0.5f, 1f));
            Stretch(CreateText(chip, "Label", rare ? "★ " + text : text, 16, rare ? PaperShiftTheme.Ink : PaperShiftTheme.MutedInk, TextAnchor.MiddleCenter).rectTransform);
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size, Color background, Color textColor, int fontSize, Vector2 anchor)
        {
            var rect = CreateRounded(parent, name, background, Mathf.Min(size.x, size.y) * 0.22f);
            SetRect(rect, position, size, anchor);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Graphic>();
            Stretch(CreateText(rect, "Label", label, fontSize, textColor, TextAnchor.MiddleCenter).rectTransform);
            return button;
        }

        private static RectTransform CreateRounded(Transform parent, string name, Color color, float radius)
        {
            var rect = CreateRect(parent, name);
            var graphic = rect.gameObject.AddComponent<RoundedRectGraphic>();
            graphic.color = color;
            graphic.CornerRadius = radius;
            graphic.raycastTarget = true;
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

        private static Text CreateText(Transform parent, string name, string value, int fontSize, Color color, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform));
            SetLayerRecursive(go, LayerMask.NameToLayer("UI"));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            var text = go.AddComponent<Text>();
            text.font = defaultFont;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = value;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size, Vector2 anchor)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void ClearChildren(Transform root)
        {
            for (var i = root.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(root.GetChild(i).gameObject);
            }
        }

        private static T GetOrAdd<T>(GameObject gameObject) where T : Component
        {
            var component = gameObject.GetComponent<T>();
            return component == null ? gameObject.AddComponent<T>() : component;
        }

        private static void SetLayerRecursive(GameObject gameObject, int layer)
        {
            gameObject.layer = layer;
            for (var i = 0; i < gameObject.transform.childCount; i++)
            {
                SetLayerRecursive(gameObject.transform.GetChild(i).gameObject, layer);
            }
        }
    }
}
