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
                    var backgroundColor = _background.color;
                    var luminance = (0.299f * backgroundColor.r)
                        + (0.587f * backgroundColor.g)
                        + (0.114f * backgroundColor.b);
                    _label.color = luminance > 0.62f
                        ? new Color(0.08f, 0.09f, 0.09f, 1f)
                        : Color.white;
                }
                _label.gameObject.SetActive(content.Sprite == null);
            }
        }
    }
}
