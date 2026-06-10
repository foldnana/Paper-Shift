using DG.Tweening;
using UnityEngine;

namespace PaperShift.Presenter
{
    public enum PaperShiftTweenEntranceStyle
    {
        SlideUp,
        SlideDown,
        SlideLeft,
        SlideRight,
        Pop,
        CardRise,
        ScaleIn,
        None
    }

    [DisallowMultipleComponent]
    public sealed class PaperShiftTweenOnEnable : MonoBehaviour
    {
        public RectTransform Target;
        public PaperShiftTweenEntranceStyle Style = PaperShiftTweenEntranceStyle.SlideUp;
        public float Duration = 0.28f;
        public float Delay = 0f;
        public bool AddSiblingDelay = false;
        public float SiblingDelay = 0.025f;
        public float Offset = 34f;
        public float StartScale = 0.96f;
        public Ease Ease = Ease.OutCubic;
        public bool UseUnscaledTime = true;

        private Vector2 basePosition;
        private Vector3 baseScale;
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

        public void Play()
        {
            if (Target == null || Style == PaperShiftTweenEntranceStyle.None)
            {
                return;
            }

            KillTween();
            Restore();

            var delay = Delay + (AddSiblingDelay && Target.parent != null ? Target.GetSiblingIndex() * SiblingDelay : 0f);
            var sequence = DOTween.Sequence()
                .SetUpdate(UseUnscaledTime)
                .SetDelay(delay);

            switch (Style)
            {
                case PaperShiftTweenEntranceStyle.SlideUp:
                    Target.anchoredPosition = basePosition + new Vector2(0f, -Offset);
                    sequence.Join(Target.DOAnchorPos(basePosition, Duration).SetEase(Ease));
                    break;
                case PaperShiftTweenEntranceStyle.SlideDown:
                    Target.anchoredPosition = basePosition + new Vector2(0f, Offset);
                    sequence.Join(Target.DOAnchorPos(basePosition, Duration).SetEase(Ease));
                    break;
                case PaperShiftTweenEntranceStyle.SlideLeft:
                    Target.anchoredPosition = basePosition + new Vector2(Offset, 0f);
                    sequence.Join(Target.DOAnchorPos(basePosition, Duration).SetEase(Ease));
                    break;
                case PaperShiftTweenEntranceStyle.SlideRight:
                    Target.anchoredPosition = basePosition + new Vector2(-Offset, 0f);
                    sequence.Join(Target.DOAnchorPos(basePosition, Duration).SetEase(Ease));
                    break;
                case PaperShiftTweenEntranceStyle.Pop:
                    Target.localScale = baseScale * StartScale;
                    sequence.Join(Target.DOScale(baseScale, Duration).SetEase(Ease.OutBack));
                    break;
                case PaperShiftTweenEntranceStyle.CardRise:
                    Target.anchoredPosition = basePosition + new Vector2(0f, -Offset);
                    Target.localScale = baseScale * StartScale;
                    sequence.Join(Target.DOAnchorPos(basePosition, Duration).SetEase(Ease));
                    sequence.Join(Target.DOScale(baseScale, Duration).SetEase(Ease.OutBack));
                    break;
                case PaperShiftTweenEntranceStyle.ScaleIn:
                    Target.localScale = baseScale * StartScale;
                    sequence.Join(Target.DOScale(baseScale, Duration).SetEase(Ease));
                    break;
            }

            tween = sequence;
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
