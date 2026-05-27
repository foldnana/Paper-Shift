using UnityEngine;
using UnityEngine.UI;

namespace PaperShift.Presenter
{
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("Paper Shift UI/Ellipse Graphic")]
    public sealed class EllipseGraphic : MaskableGraphic
    {
        [SerializeField, Range(12, 96)] private int segments = 40;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = GetPixelAdjustedRect();
            var center = rect.center;
            var radius = new Vector2(rect.width * 0.5f, rect.height * 0.5f);

            var centerIndex = vh.currentVertCount;
            AddVertex(vh, center);

            for (var i = 0; i <= segments; i++)
            {
                var angle = (i / (float)segments) * Mathf.PI * 2f;
                AddVertex(vh, center + new Vector2(Mathf.Cos(angle) * radius.x, Mathf.Sin(angle) * radius.y));
            }

            for (var i = 1; i <= segments; i++)
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
    }
}
