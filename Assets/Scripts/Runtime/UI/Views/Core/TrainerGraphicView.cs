using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public sealed class TrainerGraphicView : MonoBehaviour
    {
        [SerializeField] private Image _graphic;

        public void Configure(Image graphic) => _graphic = graphic;

        public void Render(Sprite graphic)
        {
            if (_graphic == null) return;
            _graphic.sprite = graphic;
            _graphic.color = Color.white;
            _graphic.enabled = graphic != null;
            _graphic.preserveAspect = true;
            _graphic.raycastTarget = false;
        }
    }
}
