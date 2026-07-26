using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public sealed class MapEdgeView : MonoBehaviour
    {
        [SerializeField] private Image _line;
        [SerializeField, Min(1f)] private float _lineWidth = 5f;

        public void Configure(Image line)
        {
            _line = line;
        }

        public void Bind(Vector2 from, Vector2 to, bool isResolved, bool isSelectable)
        {
            var rectTransform = (RectTransform)transform;
            var direction = to - from;
            rectTransform.anchoredPosition = from;
            rectTransform.sizeDelta = new Vector2(direction.magnitude, _lineWidth);
            rectTransform.localEulerAngles = new Vector3(
                0f,
                0f,
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

            if (_line == null)
            {
                return;
            }

            _line.color = isSelectable
                ? new Color(0.94f, 0.78f, 0.25f, 0.95f)
                : isResolved
                    ? new Color(0.36f, 0.58f, 0.42f, 0.8f)
                    : new Color(0.18f, 0.22f, 0.21f, 0.48f);
        }
    }
}
