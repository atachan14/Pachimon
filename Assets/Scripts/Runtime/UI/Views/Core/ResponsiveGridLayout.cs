using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.UI
{
    [ExecuteAlways]
    [RequireComponent(typeof(GridLayoutGroup), typeof(LayoutElement))]
    public sealed class ResponsiveGridLayout : MonoBehaviour
    {
        [SerializeField, Min(0)] private int _fixedColumnCount;
        [SerializeField, Min(1f)] private float _minimumCellWidth = 96f;
        [SerializeField, Min(1f)] private float _cellHeight = 38f;

        private GridLayoutGroup _grid;
        private LayoutElement _layoutElement;
        private float _displayScale = 1f;

        public void Configure(int fixedColumnCount, float minimumCellWidth, float cellHeight)
        {
            _fixedColumnCount = fixedColumnCount;
            _minimumCellWidth = minimumCellWidth;
            _cellHeight = cellHeight;
            RefreshLayout();
        }

        public void SetDisplayScale(float displayScale)
        {
            var resolvedScale = Mathf.Max(1f, displayScale);
            if (Mathf.Approximately(_displayScale, resolvedScale))
            {
                return;
            }

            _displayScale = resolvedScale;
            RefreshLayout();
        }

        private void OnEnable() => RefreshLayout();
        private void OnValidate() => RefreshLayout();
        private void OnRectTransformDimensionsChange() => RefreshLayout();
        private void OnTransformChildrenChanged() => RefreshLayout();

        public void RefreshLayout()
        {
            _grid ??= GetComponent<GridLayoutGroup>();
            _layoutElement ??= GetComponent<LayoutElement>();
            var rect = transform as RectTransform;
            if (_grid == null || _layoutElement == null || rect == null) return;

            var availableWidth = Mathf.Max(1f, rect.rect.width - _grid.padding.horizontal);
            var columns = _fixedColumnCount > 0
                ? _fixedColumnCount
                : Mathf.Max(1, Mathf.FloorToInt(
                    (availableWidth + _grid.spacing.x)
                    / (_minimumCellWidth + _grid.spacing.x)));
            var cellWidth = Mathf.Max(
                1f,
                (availableWidth - (_grid.spacing.x * (columns - 1))) / columns);
            _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _grid.constraintCount = columns;
            var scaledCellHeight = _cellHeight * _displayScale;
            _grid.cellSize = new Vector2(cellWidth, scaledCellHeight);

            var activeChildren = 0;
            foreach (Transform child in transform)
            {
                if (child.gameObject.activeSelf) activeChildren++;
            }

            var rows = Mathf.CeilToInt(activeChildren / (float)columns);
            var preferredHeight = _grid.padding.vertical
                + (rows * scaledCellHeight)
                + (Mathf.Max(0, rows - 1) * _grid.spacing.y);
            if (!Mathf.Approximately(_layoutElement.preferredHeight, preferredHeight))
            {
                _layoutElement.minHeight = 0f;
                _layoutElement.preferredHeight = preferredHeight;
                _layoutElement.flexibleHeight = 0f;
                LayoutRebuilder.MarkLayoutForRebuild(rect);
            }
        }
    }
}
