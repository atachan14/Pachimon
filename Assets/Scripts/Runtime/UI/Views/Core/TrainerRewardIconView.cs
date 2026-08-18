using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public sealed class TrainerRewardIconView : MonoBehaviour
    {
        [SerializeField] private Image _background;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _label;

        public void Configure(Image background, Image icon, TMP_Text label)
        {
            _background = background;
            _icon = icon;
            _label = label;
        }

        public void Bind(TrainerRewardIconContent content)
        {
            if (_background != null
                && ColorUtility.TryParseHtmlString(content.ColorHex, out var color))
            {
                _background.color = color;
            }

            if (_icon != null)
            {
                _icon.sprite = content.Sprite;
                _icon.enabled = content.Sprite != null;
                _icon.preserveAspect = true;
            }

            if (_label != null)
            {
                _label.text = content.Label;
                if (_background != null)
                {
                    _label.color = AttributeCardPalette.GetReadableTextColor(
                        _background.color);
                }
                _label.gameObject.SetActive(content.Sprite == null);
            }
        }
    }
}
