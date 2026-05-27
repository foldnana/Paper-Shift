using UnityEngine;
using UnityEngine.UI;

namespace PaperShift.Presenter
{
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("Paper Shift UI/Rounded Rect Graphic")]
    public sealed class RoundedRectGraphic : MaskableGraphic
    {
        [SerializeField] private float cornerRadius = 12f;
        [SerializeField, Range(1, 16)] private int cornerSegments = 6;

        public float CornerRadius
        {
            get => cornerRadius;
            set
            {
                cornerRadius = Mathf.Max(0f, value);
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = GetPixelAdjustedRect();
            var radius = Mathf.Min(cornerRadius, rect.width * 0.5f, rect.height * 0.5f);

            if (radius <= 0.01f)
            {
                AddQuad(vh, rect.xMin, rect.yMin, rect.xMax, rect.yMax, color);
                return;
            }

            AddQuad(vh, rect.xMin + radius, rect.yMin, rect.xMax - radius, rect.yMax, color);
            AddQuad(vh, rect.xMin, rect.yMin + radius, rect.xMin + radius, rect.yMax - radius, color);
            AddQuad(vh, rect.xMax - radius, rect.yMin + radius, rect.xMax, rect.yMax - radius, color);

            AddCorner(vh, new Vector2(rect.xMin + radius, rect.yMin + radius), radius, 180f, 270f);
            AddCorner(vh, new Vector2(rect.xMax - radius, rect.yMin + radius), radius, 270f, 360f);
            AddCorner(vh, new Vector2(rect.xMax - radius, rect.yMax - radius), radius, 0f, 90f);
            AddCorner(vh, new Vector2(rect.xMin + radius, rect.yMax - radius), radius, 90f, 180f);
        }

        private void AddCorner(VertexHelper vh, Vector2 center, float radius, float fromDegrees, float toDegrees)
        {
            var centerIndex = vh.currentVertCount;
            AddVertex(vh, center);

            var steps = Mathf.Max(1, cornerSegments);
            for (var i = 0; i <= steps; i++)
            {
                var t = i / (float)steps;
                var angle = Mathf.Lerp(fromDegrees, toDegrees, t) * Mathf.Deg2Rad;
                AddVertex(vh, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }

            for (var i = 1; i <= steps; i++)
            {
                vh.AddTriangle(centerIndex, centerIndex + i, centerIndex + i + 1);
            }
        }

        private void AddVertex(VertexHelper vh, Vector2 position)
        {
            var vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.position = position;
            vh.AddVert(vertex);
        }

        private static void AddQuad(VertexHelper vh, float xMin, float yMin, float xMax, float yMax, Color32 color)
        {
            if (xMax <= xMin || yMax <= yMin)
            {
                return;
            }

            var index = vh.currentVertCount;
            var vertex = UIVertex.simpleVert;
            vertex.color = color;

            vertex.position = new Vector3(xMin, yMin);
            vh.AddVert(vertex);
            vertex.position = new Vector3(xMin, yMax);
            vh.AddVert(vertex);
            vertex.position = new Vector3(xMax, yMax);
            vh.AddVert(vertex);
            vertex.position = new Vector3(xMax, yMin);
            vh.AddVert(vertex);

            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index, index + 2, index + 3);
        }
    }
}
