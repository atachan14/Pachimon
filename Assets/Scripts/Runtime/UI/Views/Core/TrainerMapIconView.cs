using Pachimon.Trainer;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace Pachimon.UI
{
    public sealed class TrainerMapIconView : MonoBehaviour
    {
        [SerializeField] private Image _base;
        [FormerlySerializedAs("_tops")]
        [SerializeField] private Image _primary;
        [FormerlySerializedAs("_bottoms")]
        [SerializeField] private Image _secondary;
        [SerializeField] private Image _detail;

        public void Configure(Image baseImage, Image primary, Image secondary, Image detail)
        {
            _base = baseImage;
            _primary = primary;
            _secondary = secondary;
            _detail = detail;
        }

        public void Render(TrainerMapIconSet iconSet, TrainerColorScheme colors)
        {
            var layers = iconSet?.Layers;
            ApplyLayer(_base, layers?.Base, Color.white);
            ApplyLayer(_secondary, layers?.Secondary, colors.SecondaryColor);
            ApplyLayer(_primary, layers?.Primary, colors.PrimaryColor);
            ApplyLayer(_detail, layers?.Detail, Color.white);
        }

        private static void ApplyLayer(Image image, Sprite sprite, Color color)
        {
            if (image == null) return;
            image.sprite = sprite;
            image.color = color;
            image.enabled = sprite != null;
            image.raycastTarget = false;
        }
    }
}
