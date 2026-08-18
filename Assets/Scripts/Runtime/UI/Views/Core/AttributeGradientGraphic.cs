using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class AttributeGradientGraphic : MaskableGraphic
    {
        private readonly List<Color> _colors = new();

        public void SetColors(IReadOnlyList<Color> colors)
        {
            _colors.Clear();
            if (colors != null)
            {
                for (var index = 0; index < colors.Count; index++)
                {
                    _colors.Add(colors[index]);
                }
            }

            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            if (_colors.Count == 0)
            {
                return;
            }

            var rect = GetPixelAdjustedRect();
            var segmentWidth = rect.width / _colors.Count;
            for (var segment = 0; segment < _colors.Count; segment++)
            {
                var xMin = rect.xMin + segmentWidth * segment;
                var xMax = segment == _colors.Count - 1
                    ? rect.xMax
                    : rect.xMin + segmentWidth * (segment + 1);
                var color = _colors[segment];
                var firstVertex = vertexHelper.currentVertCount;
                vertexHelper.AddVert(
                    new Vector3(xMin, rect.yMin),
                    color,
                    Vector2.zero);
                vertexHelper.AddVert(
                    new Vector3(xMin, rect.yMax),
                    color,
                    Vector2.up);
                vertexHelper.AddVert(
                    new Vector3(xMax, rect.yMin),
                    color,
                    Vector2.right);
                vertexHelper.AddVert(
                    new Vector3(xMax, rect.yMax),
                    color,
                    Vector2.one);
                var bottomLeft = firstVertex;
                var topLeft = firstVertex + 1;
                var bottomRight = firstVertex + 2;
                var topRight = firstVertex + 3;
                vertexHelper.AddTriangle(bottomLeft, topLeft, topRight);
                vertexHelper.AddTriangle(bottomLeft, topRight, bottomRight);
            }
        }
    }
}
