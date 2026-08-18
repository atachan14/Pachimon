using TMPro;
using UnityEngine;

namespace Pachimon.UI
{
    internal sealed class ResponsiveTypographySize : MonoBehaviour
    {
        [SerializeField] private bool _isCaptured;
        [SerializeField] private float _fontSize;
        [SerializeField] private float _fontSizeMin;
        [SerializeField] private float _fontSizeMax;
        [SerializeField] private bool _isLayoutControlled;
        [SerializeField] private float _lastAppliedScale = -1f;

        public void SetBaseFontSize(TMP_Text text, float fontSize)
        {
            _isLayoutControlled = false;
            _fontSize = fontSize;
            if (text.enableAutoSizing)
            {
                _fontSizeMin = fontSize;
                _fontSizeMax = fontSize;
            }

            _isCaptured = true;
            _lastAppliedScale = -1f;
            text.fontSize = fontSize;
        }

        public void SetLayoutControlledFontSize(TMP_Text text, float fontSize)
        {
            _isLayoutControlled = true;
            _isCaptured = true;
            _fontSize = fontSize;
            _lastAppliedScale = 1f;
            text.fontSize = fontSize;
        }

        public void Apply(TMP_Text text, float scale)
        {
            if (_isLayoutControlled
                || Mathf.Approximately(_lastAppliedScale, scale))
            {
                return;
            }

            if (!_isCaptured)
            {
                _fontSize = text.fontSize;
                _fontSizeMin = text.fontSizeMin;
                _fontSizeMax = text.fontSizeMax;
                _isCaptured = true;
            }

            text.fontSize = _fontSize * scale;
            if (text.enableAutoSizing)
            {
                text.fontSizeMin = _fontSizeMin * scale;
                text.fontSizeMax = _fontSizeMax * scale;
            }

            _lastAppliedScale = scale;
        }
    }
}
