using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public sealed class TextChipView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;
        private bool _hasDefaultBackgroundColor;
        private Color _defaultBackgroundColor;

        public void Configure(TMP_Text label) => _label = label;

        public void Bind(string label)
        {
            Bind(label, null);
        }

        public void Bind(string label, Action onClicked)
        {
            if (_label != null) _label.text = label;
            ClearAttributeColors();

            var button = GetComponent<Button>();
            if (onClicked == null)
            {
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    button.interactable = false;
                }

                return;
            }

            button ??= gameObject.AddComponent<Button>();
            button.targetGraphic = GetComponent<Graphic>();
            button.transition = Selectable.Transition.ColorTint;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClicked());
            button.interactable = true;
        }

        public void SetAttributeColors(IReadOnlyList<Color> colors)
        {
            if (colors == null || colors.Count == 0)
            {
                return;
            }

            CacheDefaultBackgroundColor();
            var textColor = AttributeCardPalette.Apply(gameObject, colors);
            if (_label != null)
            {
                _label.color = textColor;
                _label.overrideColorTags = true;
            }
        }

        private void ClearAttributeColors()
        {
            CacheDefaultBackgroundColor();
            var image = GetComponent<Image>();
            if (image != null)
            {
                image.color = _defaultBackgroundColor;
            }

            var gradient = transform.Find("AttributeGradient")
                ?.GetComponent<AttributeGradientGraphic>();
            if (gradient != null)
            {
                gradient.enabled = false;
            }

            if (_label != null)
            {
                _label.color = AttributeCardPalette.GetReadableTextColor(
                    _defaultBackgroundColor);
                _label.overrideColorTags = true;
            }
        }

        private void CacheDefaultBackgroundColor()
        {
            if (_hasDefaultBackgroundColor)
            {
                return;
            }

            _defaultBackgroundColor = GetComponent<Image>()?.color
                ?? Color.clear;
            _hasDefaultBackgroundColor = true;
        }
    }
}
