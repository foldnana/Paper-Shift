using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace PaperShift.Presenter
{
    public enum PaperShiftTweenCascadeDirection
    {
        Up,
        Down,
        Left,
        Right,
        None
    }

    [DisallowMultipleComponent]
    public sealed class PaperShiftTweenChildCascade : MonoBehaviour
    {
        public RectTransform Root;
        public PaperShiftTweenCascadeDirection Direction = PaperShiftTweenCascadeDirection.Up;
        public float Duration = 0.22f;
        public float Delay = 0.04f;
        public float Offset = 18f;
        public float StartScale = 0.97f;
        public Ease Ease = Ease.OutCubic;
        public bool OnlyActiveChildren = true;
        public bool UseUnscaledTime = true;

        private readonly List<RectTransform> children = new List<RectTransform>();
        private readonly List<Vector2> basePositions = new List<Vector2>();
        private readonly List<Vector3> baseScales = new List<Vector3>();
        private readonly List<Tween> tweens = new List<Tween>();
        private bool captured;

        private void Reset()
        {
            Root = GetComponent<RectTransform>();
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
            KillTweens();
            Restore();
        }

        public void Play()
        {
            if (Root == null)
            {
                return;
            }

            KillTweens();
            Restore();

            for (var i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (child == null || (OnlyActiveChildren && !child.gameObject.activeInHierarchy))
                {
                    continue;
                }

                var basePosition = basePositions[i];
                var baseScale = baseScales[i];
                child.anchoredPosition = basePosition + DirectionOffset();
                child.localScale = baseScale * StartScale;

                var sequence = DOTween.Sequence()
                    .SetUpdate(UseUnscaledTime)
                    .SetDelay(i * Delay);
                sequence.Join(child.DOAnchorPos(basePosition, Duration).SetEase(Ease));
                sequence.Join(child.DOScale(baseScale, Duration).SetEase(Ease.OutBack));
                tweens.Add(sequence);
            }
        }

        private void Capture()
        {
            if (captured)
            {
                return;
            }

            if (Root == null)
            {
                Root = GetComponent<RectTransform>();
            }

            if (Root == null)
            {
                return;
            }

            children.Clear();
            basePositions.Clear();
            baseScales.Clear();
            for (var i = 0; i < Root.childCount; i++)
            {
                var child = Root.GetChild(i) as RectTransform;
                if (child == null)
                {
                    continue;
                }

                children.Add(child);
                basePositions.Add(child.anchoredPosition);
                baseScales.Add(child.localScale);
            }

            captured = true;
        }

        private Vector2 DirectionOffset()
        {
            switch (Direction)
            {
                case PaperShiftTweenCascadeDirection.Up:
                    return new Vector2(0f, -Offset);
                case PaperShiftTweenCascadeDirection.Down:
                    return new Vector2(0f, Offset);
                case PaperShiftTweenCascadeDirection.Left:
                    return new Vector2(Offset, 0f);
                case PaperShiftTweenCascadeDirection.Right:
                    return new Vector2(-Offset, 0f);
                default:
                    return Vector2.zero;
            }
        }

        private void Restore()
        {
            for (var i = 0; i < children.Count; i++)
            {
                if (children[i] == null)
                {
                    continue;
                }

                children[i].anchoredPosition = basePositions[i];
                children[i].localScale = baseScales[i];
            }
        }

        private void KillTweens()
        {
            for (var i = 0; i < tweens.Count; i++)
            {
                if (tweens[i] != null)
                {
                    tweens[i].Kill();
                }
            }

            tweens.Clear();
        }
    }
}
