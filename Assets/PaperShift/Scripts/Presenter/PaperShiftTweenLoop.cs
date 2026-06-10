using DG.Tweening;
using UnityEngine;

namespace PaperShift.Presenter
{
    public enum PaperShiftTweenLoopStyle
    {
        FloatY,
        Breathe,
        TinyRotate
    }

    [DisallowMultipleComponent]
    public sealed class PaperShiftTweenLoop : MonoBehaviour
    {
        public RectTransform Target;
        public PaperShiftTweenLoopStyle Style = PaperShiftTweenLoopStyle.FloatY;
        public float Amount = 6f;
        public float Duration = 1.8f;
        public float Delay = 0f;
        public Ease Ease = Ease.InOutSine;
        public bool UseUnscaledTime = true;

        private Vector2 basePosition;
        private Vector3 baseScale;
        private Vector3 baseRotation;
        private bool captured;
        private Tween tween;

        private void Reset()
        {
            Target = GetComponent<RectTransform>();
        }

        private void Awake()
        {
            Capture();
        }

        private void OnEnable()
        {
            Capture();
            Play();
        }

        private void OnDisable()
        {
            KillTween();
            Restore();
        }

        private void Play()
        {
            if (Target == null)
            {
                return;
            }

            KillTween();
            Restore();

            switch (Style)
            {
                case PaperShiftTweenLoopStyle.FloatY:
                    tween = Target.DOAnchorPosY(basePosition.y + Amount, Duration)
                        .SetEase(Ease)
                        .SetLoops(-1, LoopType.Yoyo);
                    break;
                case PaperShiftTweenLoopStyle.Breathe:
                    tween = Target.DOScale(baseScale * (1f + Amount * 0.01f), Duration)
                        .SetEase(Ease)
                        .SetLoops(-1, LoopType.Yoyo);
                    break;
                case PaperShiftTweenLoopStyle.TinyRotate:
                    tween = Target.DOLocalRotate(baseRotation + new Vector3(0f, 0f, Amount), Duration)
                        .SetEase(Ease)
                        .SetLoops(-1, LoopType.Yoyo);
                    break;
            }

            if (tween != null)
            {
                tween.SetDelay(Delay).SetUpdate(UseUnscaledTime);
            }
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

            basePosition = Target.anchoredPosition;
            baseScale = Target.localScale;
            baseRotation = Target.localEulerAngles;
            captured = true;
        }

        private void Restore()
        {
            if (Target == null || !captured)
            {
                return;
            }

            Target.anchoredPosition = basePosition;
            Target.localScale = baseScale;
            Target.localEulerAngles = baseRotation;
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
