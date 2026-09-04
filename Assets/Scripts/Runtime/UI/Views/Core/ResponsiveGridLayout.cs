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
        private float _lastAvailableWidth = float.NaN;
        private int _lastActiveChildCount = -1;

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
            _displayScale = resolvedScale;
            RefreshLayout();
        }

        private void OnEnable() => RefreshLayout();
        private void OnValidate() => RefreshLayout();
        private void OnRectTransformDimensionsChange() => RefreshLayout();
        private void OnTransformChildrenChanged() => RefreshLayout();

        private void LateUpdate()
        {
            _grid ??= GetComponent<GridLayoutGroup>();
            var rect = transform as RectTransform;
            if (_grid == null || rect == null)
            {
                return;
            }

            var availableWidth = GetAvailableWidth(rect);
            var activeChildCount = GetActiveChildCount();
            if (!Mathf.Approximately(_lastAvailableWidth, availableWidth)
                || _lastActiveChildCount != activeChildCount)
            {
                RefreshLayout();
            }
        }

        public void RefreshLayout()
        {
            _grid ??= GetComponent<GridLayoutGroup>();
            _layoutElement ??= GetComponent<LayoutElement>();
            var rect = transform as RectTransform;
            if (_grid == null || _layoutElement == null || rect == null) return;

            // A responsive grid consumes the width assigned by its parent. If
            // GridLayoutGroup reports its calculated cell width as a preferred
            // width, ContentSizeFitter creates a feedback loop that makes the
            // section wider every time the surrounding pane changes size.
            _layoutElement.minWidth = 0f;
            _layoutElement.preferredWidth = 0f;
            _layoutElement.flexibleWidth = 1f;

            var availableWidth = GetAvailableWidth(rect);
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

            var activeChildren = GetActiveChildCount();
            _lastAvailableWidth = availableWidth;
            _lastActiveChildCount = activeChildren;

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

        private float GetAvailableWidth(RectTransform rect)
        {
            return Mathf.Max(1f, rect.rect.width - _grid.padding.horizontal);
        }

        private int GetActiveChildCount()
        {
            var count = 0;
            foreach (Transform child in transform)
            {
                if (child.gameObject.activeSelf)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
