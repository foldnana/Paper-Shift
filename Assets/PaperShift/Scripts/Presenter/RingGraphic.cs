using UnityEngine;
using UnityEngine.UI;

namespace PaperShift.Presenter
{
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("Paper Shift UI/Ring Graphic")]
    public sealed class RingGraphic : MaskableGraphic
    {
        [SerializeField] private Color baseColor = new Color(0.86f, 0.89f, 0.91f, 1f);
        [SerializeField] private Color accentColor = new Color(0.14f, 0.8f, 0.55f, 1f);
        [SerializeField, Range(4f, 40f)] private float thickness = 10f;
        [SerializeField, Range(0f, 360f)] private float accentStartDegrees = 180f;
        [SerializeField, Range(0f, 360f)] private float accentDegrees = 180f;
        [SerializeField, Range(24, 128)] private int segments = 72;

        public Color AccentColor
        {
            get => accentColor;
            set
            {
                accentColor = value;
                SetVerticesDirty();
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            thickness = Mathf.Clamp(thickness, 4f, 40f);
            accentStartDegrees = Mathf.Clamp(accentStartDegrees, 0f, 360f);
            accentDegrees = Mathf.Clamp(accentDegrees, 0f, 360f);
            segments = Mathf.Clamp(segments, 24, 128);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            DrawArc(vh, 0f, 360f, baseColor);
            DrawArc(vh, accentStartDegrees, accentDegrees, accentColor);
        }

        private void DrawArc(VertexHelper vh, float startDegrees, float degrees, Color32 arcColor)
        {
            var rect = GetPixelAdjustedRect();
            var center = rect.center;
            var outer = Mathf.Min(rect.width, rect.height) * 0.5f;
            var inner = Mathf.Max(0f, outer - thickness);
            var steps = Mathf.Max(2, Mathf.CeilToInt(segments * Mathf.Abs(degrees) / 360f));
            var start = startDegrees * Mathf.Deg2Rad;
            var sweep = degrees * Mathf.Deg2Rad;

            for (var i = 0; i < steps; i++)
            {
                var a0 = start + sweep * (i / (float)steps);
                var a1 = start + sweep * ((i + 1) / (float)steps);
                AddRingQuad(vh, center, inner, outer, a0, a1, arcColor);
            }
        }

        private static void AddRingQuad(VertexHelper vh, Vector2 center, float inner, float outer, float a0, float a1, Color32 arcColor)
        {
            var index = vh.currentVertCount;
            var vertex = UIVertex.simpleVert;
            vertex.color = arcColor;

            vertex.position = center + new Vector2(Mathf.Cos(a0) * inner, Mathf.Sin(a0) * inner);
            vh.AddVert(vertex);
            vertex.position = center + new Vector2(Mathf.Cos(a0) * outer, Mathf.Sin(a0) * outer);
            vh.AddVert(vertex);
            vertex.position = center + new Vector2(Mathf.Cos(a1) * outer, Mathf.Sin(a1) * outer);
            vh.AddVert(vertex);
            vertex.position = center + new Vector2(Mathf.Cos(a1) * inner, Mathf.Sin(a1) * inner);
            vh.AddVert(vertex);

            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index, index + 2, index + 3);
        }
    }
}
