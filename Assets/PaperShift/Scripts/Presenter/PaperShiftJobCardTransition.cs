using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PaperShift.Presenter
{
    public sealed class PaperShiftJobCardTransition : MonoBehaviour
    {
        private const string OverlayName = "State Transition Overlay";
        private const float FadeInSeconds = 0.18f;
        private const float HoldSeconds = 1.10f;
        private const float FadeOutSeconds = 0.36f;

        private readonly List<Graphic> graphics = new List<Graphic>();
        private readonly List<Color> baseColors = new List<Color>();
        private RectTransform overlay;
        private RectTransform ribbon;
        private Text iconText;
        private Text titleText;
        private Text detailText;
        private float startedAt;
        private bool playing;
        private bool bound;

        public void Show(string icon, string title, string detail, Color accent)
        {
            BindSceneObjects();
            if (overlay == null)
            {
                return;
            }

            SetText(iconText, icon);
            SetText(titleText, title);
            SetText(detailText, detail);
            SetGraphicColor(ribbon, accent);
            CaptureBaseColors();
            overlay.gameObject.SetActive(true);
            SetAlpha(0f);
            startedAt = Time.unscaledTime;
            playing = true;
        }

        private void LateUpdate()
        {
            if (!playing || overlay == null)
            {
                return;
            }

            var elapsed = Time.unscaledTime - startedAt;
            var total = FadeInSeconds + HoldSeconds + FadeOutSeconds;
            if (elapsed >= total)
            {
                SetAlpha(0f);
                overlay.gameObject.SetActive(false);
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

            if (ribbon != null)
            {
                var pop = Mathf.Clamp01(elapsed / FadeInSeconds);
                var scaleY = Mathf.Lerp(0.88f, 1f, Mathf.SmoothStep(0f, 1f, pop));
                ribbon.localScale = new Vector3(1f, scaleY, 1f);
                ribbon.anchoredPosition = new Vector2(0f, Mathf.Sin(elapsed * 10f) * 1.4f);
            }
        }

        private void BindSceneObjects()
        {
            var found = FindChild(transform, OverlayName) as RectTransform;
            if (found == null)
            {
                return;
            }

            if (bound && overlay == found)
            {
                return;
            }

            overlay = found;
            overlay.SetAsLastSibling();
            ribbon = FindChild(overlay, "Transition Ribbon") as RectTransform;
            iconText = FindText(overlay, "Icon");
            titleText = FindText(overlay, "Title");
            detailText = FindText(overlay, "Detail");

            graphics.Clear();
            overlay.GetComponentsInChildren(true, graphics);
            CaptureBaseColors();
            bound = true;
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

        private static Text FindText(Transform root, string name)
        {
            var child = FindChild(root, name);
            return child == null ? null : child.GetComponent<Text>();
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == name)
            {
                return root;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var result = FindChild(root.GetChild(i), name);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
