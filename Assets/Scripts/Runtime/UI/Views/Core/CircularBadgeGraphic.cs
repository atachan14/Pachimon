using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public sealed class CircularBadgeGraphic : MaskableGraphic
    {
        [SerializeField] private Color _fillColor = Color.white;
        [SerializeField] private Color _borderColor = Color.red;
        [SerializeField, Min(0f)] private float _borderWidth = 3f;
        [SerializeField, Range(12, 64)] private int _segments = 40;

        public void Configure(Color fillColor, Color borderColor, float borderWidth)
        {
            _fillColor = fillColor;
            _borderColor = borderColor;
            _borderWidth = Mathf.Max(0f, borderWidth);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            var rect = GetPixelAdjustedRect();
            var radius = Mathf.Min(rect.width, rect.height) * 0.5f;
            if (radius <= 0f)
            {
                return;
            }

            var center = rect.center;
            var innerRadius = Mathf.Max(0f, radius - _borderWidth);
            AddDisc(vertexHelper, center, innerRadius, _fillColor);
            AddRing(vertexHelper, center, innerRadius, radius, _borderColor);
        }

        private void AddDisc(
            VertexHelper vertexHelper,
            Vector2 center,
            float radius,
            Color color)
        {
            var centerIndex = vertexHelper.currentVertCount;
            vertexHelper.AddVert(center, color, Vector2.zero);
            for (var index = 0; index < _segments; index++)
            {
                vertexHelper.AddVert(
                    center + Direction(index) * radius,
                    color,
                    Vector2.zero);
            }

            for (var index = 0; index < _segments; index++)
            {
                vertexHelper.AddTriangle(
                    centerIndex,
                    centerIndex + 1 + index,
                    centerIndex + 1 + ((index + 1) % _segments));
            }
        }

        private void AddRing(
            VertexHelper vertexHelper,
            Vector2 center,
            float innerRadius,
            float outerRadius,
            Color color)
        {
            var startIndex = vertexHelper.currentVertCount;
            for (var index = 0; index < _segments; index++)
            {
                var direction = Direction(index);
                vertexHelper.AddVert(
                    center + direction * innerRadius,
                    color,
                    Vector2.zero);
                vertexHelper.AddVert(
                    center + direction * outerRadius,
                    color,
                    Vector2.zero);
            }

            for (var index = 0; index < _segments; index++)
            {
                var next = (index + 1) % _segments;
                var inner = startIndex + index * 2;
                var outer = inner + 1;
                var nextInner = startIndex + next * 2;
                var nextOuter = nextInner + 1;
                vertexHelper.AddTriangle(inner, outer, nextOuter);
                vertexHelper.AddTriangle(inner, nextOuter, nextInner);
            }
        }

        private Vector2 Direction(int index)
        {
            var angle = Mathf.PI * 2f * index / _segments;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }
    }
}
