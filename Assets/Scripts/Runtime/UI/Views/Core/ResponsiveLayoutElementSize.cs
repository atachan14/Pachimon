using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.UI
{
    [RequireComponent(typeof(LayoutElement))]
    internal sealed class ResponsiveLayoutElementSize : MonoBehaviour
    {
        private LayoutElement _layoutElement;
        private float _baseMinimumHeight;
        private float _basePreferredHeight;

        public void Configure(float minimumHeight, float preferredHeight)
        {
            _layoutElement ??= GetComponent<LayoutElement>();
            _baseMinimumHeight = minimumHeight;
            _basePreferredHeight = preferredHeight;
            SetDisplayScale(1f);
        }

        public void SetDisplayScale(float displayScale)
        {
            _layoutElement ??= GetComponent<LayoutElement>();
            if (_layoutElement == null)
            {
                return;
            }

            var scale = Mathf.Max(1f, displayScale);
            _layoutElement.minHeight = _baseMinimumHeight * scale;
            _layoutElement.preferredHeight = _basePreferredHeight * scale;
        }
    }
}
