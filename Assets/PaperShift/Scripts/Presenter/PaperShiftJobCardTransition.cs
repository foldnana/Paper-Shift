using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PaperShift.Presenter
{
    public sealed class PaperShiftJobCardTransition : MonoBehaviour
    {
        private const float FadeInSeconds = 0.18f;
        private const float HoldSeconds = 1.10f;
        private const float FadeOutSeconds = 0.36f;
        public const float TotalSeconds = FadeInSeconds + HoldSeconds + FadeOutSeconds;

        public RectTransform Overlay;
        public RectTransform Ribbon;
        public Text IconText;
        public Text TitleText;
        public Text DetailText;

        private readonly List<Graphic> graphics = new List<Graphic>();
        private readonly List<Color> baseColors = new List<Color>();
        private string authoredIcon;
        private string authoredTitle;
        private string authoredDetail;
        private Color authoredRibbonColor;
        private float startedAt;
        private bool playing;
        private bool bound;
        private bool capturedAuthoredState;

        private void Awake()
        {
            BindSceneReferences();
            if (Overlay != null)
            {
                Overlay.gameObject.SetActive(false);
            }
        }

        public void Show(string icon, string title, string detail, Color accent)
        {
            Begin(icon, title, detail, accent, true);
        }

        public bool ShowPreauthored()
        {
            return Begin(string.Empty, string.Empty, string.Empty, Color.clear, false);
        }

        private bool Begin(string icon, string title, string detail, Color accent, bool applyContent)
        {
            BindSceneReferences();
            if (Overlay == null)
            {
                return false;
            }

            if (playing)
            {
                RestoreBaseColors();
            }

            if (applyContent)
            {
                SetText(IconText, icon);
                SetText(TitleText, title);
                SetText(DetailText, detail);
                SetGraphicColor(Ribbon, accent);
            }
            else
            {
                RestoreAuthoredState();
            }

            CaptureBaseColors();
            Overlay.gameObject.SetActive(true);
            SetAlpha(0f);
            startedAt = Time.unscaledTime;
            playing = true;
            return true;
        }

        private void LateUpdate()
        {
            if (!playing || Overlay == null)
            {
                return;
            }

            var elapsed = Time.unscaledTime - startedAt;
            var total = FadeInSeconds + HoldSeconds + FadeOutSeconds;
            if (elapsed >= total)
            {
                SetAlpha(0f);
                RestoreBaseColors();
                Overlay.gameObject.SetActive(false);
                playing = false;
                return;
            }

            if (elapsed < FadeInSeconds)
            {
                SetAlpha(Mathf.SmoothStep(0f, 1f, elapsed / FadeInSeconds));
            }
            else if (elapsed < FadeInSeconds + HoldSeconds)
            {
                SetAlpha(1f);
            }
            else
            {
                var t = (elapsed - FadeInSeconds - HoldSeconds) / FadeOutSeconds;
                SetAlpha(Mathf.SmoothStep(1f, 0f, t));
            }

            if (Ribbon != null)
            {
                var pop = Mathf.Clamp01(elapsed / FadeInSeconds);
                var scaleY = Mathf.Lerp(0.88f, 1f, Mathf.SmoothStep(0f, 1f, pop));
                Ribbon.localScale = new Vector3(1f, scaleY, 1f);
                Ribbon.anchoredPosition = new Vector2(0f, Mathf.Sin(elapsed * 10f) * 1.4f);
            }
        }

        private void RestoreBaseColors()
        {
            for (var i = 0; i < graphics.Count && i < baseColors.Count; i++)
            {
                graphics[i].color = baseColors[i];
            }
        }

        private void BindSceneReferences()
        {
            if (Overlay == null)
            {
                return;
            }

            if (bound)
            {
                return;
            }

            Overlay.SetAsLastSibling();

            graphics.Clear();
            Overlay.GetComponentsInChildren(true, graphics);
            CaptureAuthoredState();
            CaptureBaseColors();
            bound = true;
        }

        private void CaptureAuthoredState()
        {
            if (capturedAuthoredState)
            {
                return;
            }

            authoredIcon = IconText == null ? string.Empty : IconText.text;
            authoredTitle = TitleText == null ? string.Empty : TitleText.text;
            authoredDetail = DetailText == null ? string.Empty : DetailText.text;

            var ribbonGraphic = Ribbon == null ? null : Ribbon.GetComponent<Graphic>();
            authoredRibbonColor = ribbonGraphic == null ? Color.white : ribbonGraphic.color;
            capturedAuthoredState = true;
        }

        private void RestoreAuthoredState()
        {
            if (!capturedAuthoredState)
            {
                return;
            }

            SetText(IconText, authoredIcon);
            SetText(TitleText, authoredTitle);
            SetText(DetailText, authoredDetail);

            var ribbonGraphic = Ribbon == null ? null : Ribbon.GetComponent<Graphic>();
            if (ribbonGraphic != null)
            {
                ribbonGraphic.color = authoredRibbonColor;
            }
        }

        private void CaptureBaseColors()
        {
            baseColors.Clear();
            for (var i = 0; i < graphics.Count; i++)
            {
                baseColors.Add(graphics[i].color);
            }
        }

        private void SetAlpha(float alpha)
        {
            for (var i = 0; i < graphics.Count; i++)
            {
                var color = i < baseColors.Count ? baseColors[i] : graphics[i].color;
                color.a *= alpha;
                graphics[i].color = color;
            }
        }

        private static void SetText(Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        private static void SetGraphicColor(Transform root, Color color)
        {
            if (root == null)
            {
                return;
            }

            var graphic = root.GetComponent<Graphic>();
            if (graphic != null)
            {
                var current = graphic.color;
                graphic.color = new Color(color.r, color.g, color.b, current.a);
            }
        }

    }
}
