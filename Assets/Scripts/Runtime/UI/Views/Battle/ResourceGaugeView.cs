using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public sealed class ResourceGaugeView : MonoBehaviour
    {
        private const float MinimumDuration = 0.15f;
        private const float MaximumDuration = 0.65f;
        private const string PreviewDamageColorHex = "#E84B3C";
        private const string PreviewRecoveryColorHex = "#2D75C7";

        private Image _fill;
        private TMP_Text _value;
        private string _label;
        private string _ownerId;
        private int _displayedValue;
        private int _displayedMaximum;
        private int _startValue;
        private int _targetValue;
        private int _targetMaximum;
        private int _previewDelta;
        private float _startRatio;
        private float _targetRatio;
        private Color _startColor;
        private Color _targetColor;
        private float _animationDuration;
        private float _animationElapsed;
        private bool _isInitialized;

        public void Configure(
            string label,
            Image fill,
            TMP_Text value)
        {
            _label = label;
            _fill = fill;
            _value = value;
        }

        public void Present(
            string ownerId,
            int currentValue,
            int maximumValue,
            Color fillColor)
        {
            var safeMaximum = Mathf.Max(0, maximumValue);
            var safeValue = Mathf.Clamp(currentValue, 0, safeMaximum);
            var ratio = safeMaximum > 0
                ? (float)safeValue / safeMaximum
                : 0f;
            if (!_isInitialized || _ownerId != ownerId)
            {
                _isInitialized = true;
                _ownerId = ownerId;
                _displayedValue = safeValue;
                _displayedMaximum = safeMaximum;
                _targetValue = safeValue;
                _targetMaximum = safeMaximum;
                _startValue = safeValue;
                _startRatio = ratio;
                _targetRatio = ratio;
                _startColor = fillColor;
                _targetColor = fillColor;
                _animationDuration = 0f;
                Render(ratio, fillColor);
                return;
            }

            if (_targetValue == safeValue
                && _targetMaximum == safeMaximum
                && _targetColor == fillColor)
            {
                return;
            }

            _startRatio = GetDisplayedRatio();
            _startValue = _displayedValue;
            _targetRatio = ratio;
            _startColor = _fill != null ? _fill.color : fillColor;
            _targetColor = fillColor;
            _targetValue = safeValue;
            _targetMaximum = safeMaximum;
            _animationElapsed = 0f;
            _animationDuration = Mathf.Lerp(
                MinimumDuration,
                MaximumDuration,
                Mathf.Clamp01(Mathf.Abs(_targetRatio - _startRatio)));
        }

        public void SetPreviewDelta(int delta)
        {
            _previewDelta = delta;
            RenderValue();
        }

        public void Clear()
        {
            _ownerId = null;
            _isInitialized = false;
            _animationDuration = 0f;
            _previewDelta = 0;
            _displayedValue = 0;
            _displayedMaximum = 0;
            if (_fill != null)
            {
                _fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            }

            if (_value != null)
            {
                _value.text = "---";
            }
        }

        private void Update()
        {
            if (_animationDuration <= 0f)
            {
                return;
            }

            _animationElapsed += Time.unscaledDeltaTime;
            var progress = Mathf.Clamp01(
                _animationElapsed / _animationDuration);
            var eased = 1f - Mathf.Pow(1f - progress, 3f);
            var ratio = Mathf.Lerp(_startRatio, _targetRatio, eased);
            var color = Color.Lerp(_startColor, _targetColor, eased);
            _displayedValue = Mathf.RoundToInt(Mathf.Lerp(
                _startValue,
                _targetValue,
                eased));
            _displayedMaximum = _targetMaximum;
            Render(ratio, color);

            if (progress < 1f)
            {
                return;
            }

            _animationDuration = 0f;
            _displayedValue = _targetValue;
            _displayedMaximum = _targetMaximum;
            Render(_targetRatio, _targetColor);
        }

        private void Render(float ratio, Color color)
        {
            if (_fill != null)
            {
                _fill.color = color;
                _fill.rectTransform.anchorMax = new Vector2(
                    Mathf.Clamp01(ratio),
                    1f);
            }

            RenderValue();
        }

        private void RenderValue()
        {
            if (_value == null)
            {
                return;
            }

            _value.text =
                $"{_label}  {_displayedValue}"
                + $"{FormatPreviewDelta(_previewDelta)}"
                + $" / {_displayedMaximum}";
        }

        private float GetDisplayedRatio()
        {
            return _displayedMaximum > 0
                ? Mathf.Clamp01(
                    (float)_displayedValue / _displayedMaximum)
                : 0f;
        }

        private static string FormatPreviewDelta(int delta)
        {
            if (delta == 0)
            {
                return string.Empty;
            }

            var color = delta < 0
                ? PreviewDamageColorHex
                : PreviewRecoveryColorHex;
            var sign = delta > 0 ? "+" : string.Empty;
            return $" <color={color}>{sign}{delta}</color>";
        }
    }
}
