using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.UI
{
    public sealed class OverlayLayerCoordinator
    {
        private readonly List<Entry> _entries = new();

        public void Clear() => _entries.Clear();

        public void Register(RectTransform layer, Func<bool> isVisible)
        {
            if (layer == null || isVisible == null)
            {
                return;
            }

            for (var index = 0; index < _entries.Count; index++)
            {
                if (_entries[index].Layer == layer)
                {
                    _entries[index] = new Entry(layer, isVisible);
                    return;
                }
            }

            _entries.Add(new Entry(layer, isVisible));
        }

        public void BringToFront(RectTransform layer)
        {
            layer?.SetAsLastSibling();
        }

        public bool IsTop(RectTransform candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            RectTransform top = null;
            var highestSiblingIndex = int.MinValue;
            foreach (var entry in _entries)
            {
                if (entry.Layer == null || !entry.IsVisible())
                {
                    continue;
                }

                var siblingIndex = entry.Layer.GetSiblingIndex();
                if (siblingIndex > highestSiblingIndex)
                {
                    highestSiblingIndex = siblingIndex;
                    top = entry.Layer;
                }
            }

            return top == candidate;
        }

        private readonly struct Entry
        {
            public Entry(RectTransform layer, Func<bool> isVisible)
            {
                Layer = layer;
                IsVisible = isVisible;
            }

            public RectTransform Layer { get; }
            public Func<bool> IsVisible { get; }
        }
    }
}
