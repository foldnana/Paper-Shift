using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PaperShift.Presenter
{
    [DisallowMultipleComponent]
    public sealed class PaperShiftTweenButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        public RectTransform Target;
        public float HoverScale = 1.025f;
        public float PressScale = 0.94f;
        public float ScaleDuration = 0.12f;
        public float PunchStrength = 0.08f;
        public float PunchDuration = 0.18f;
        public bool UseUnscaledTime = true;

        private Vector3 baseScale;
        private bool captured;
        private bool hovering;
        private Tween tween;
        private Selectable selectable;

        private void Reset()
        {
            Target = GetComponent<RectTransform>();
        }

        private void Awake()
        {
            selectable = GetComponent<Selectable>();
            Capture();
        }

        private void OnEnable()
        {
            Capture();
            Restore();
        }

        private void OnDisable()
        {
            KillTween();
            hovering = false;
            Restore();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!CanAnimate())
            {
                return;
            }

            hovering = true;
            ScaleTo(baseScale * HoverScale, Ease.OutCubic);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovering = false;
            if (!CanAnimate())
            {
                return;
            }

            ScaleTo(baseScale, Ease.OutCubic);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!CanAnimate())
            {
                return;
            }

            ScaleTo(baseScale * PressScale, Ease.OutCubic);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!CanAnimate())
            {
                return;
            }

            ScaleTo(hovering ? baseScale * HoverScale : baseScale, Ease.OutBack);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!CanAnimate() || PunchStrength <= 0f)
            {
                return;
            }

            KillTween();
            tween = Target.DOPunchScale(baseScale * PunchStrength, PunchDuration, 6, 0.6f)
                .SetUpdate(UseUnscaledTime)
                .OnComplete(() => ScaleTo(hovering ? baseScale * HoverScale : baseScale, Ease.OutCubic));
        }

        private bool CanAnimate()
        {
            return Target != null && (selectable == null || selectable.IsInteractable());
        }

        private void ScaleTo(Vector3 scale, Ease ease)
        {
            KillTween();
            tween = Target.DOScale(scale, ScaleDuration)
                .SetEase(ease)
                .SetUpdate(UseUnscaledTime);
        }

        private void Capture()
        {
            if (captured)
            {
                return;
            }

            if (Target == null)
            {
                Target = GetComponent<RectTransform>();
            }

            if (Target == null)
            {
                return;
            }

            baseScale = Target.localScale;
            captured = true;
        }

        private void Restore()
        {
            if (Target != null && captured)
            {
                Target.localScale = baseScale;
            }
        }

        private void KillTween()
        {
            if (tween == null)
            {
                return;
            }

            tween.Kill();
            tween = null;
        }
    }
}
