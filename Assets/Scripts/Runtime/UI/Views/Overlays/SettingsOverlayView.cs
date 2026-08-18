using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public sealed class SettingsOverlayView : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _transitionDuration = 0.25f;

        private RectTransform _rectTransform;
        private VerticalSlideTransition _slideTransition;
        private bool _initialized;

        public bool IsOpen { get; private set; }

        public static SettingsOverlayView CreateRuntime(RectTransform parent)
        {
            var overlayObject = new GameObject(
                "SettingsOverlayView",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup),
                typeof(SettingsOverlayView));
            overlayObject.layer = parent.gameObject.layer;

            var rect = overlayObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Stretch(rect);
            return overlayObject.GetComponent<SettingsOverlayView>();
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!IsOpen && _rectTransform != null)
            {
                _slideTransition?.Snap(0f);
            }
        }

        public void SetSlideDistance(float distance)
        {
            _slideTransition?.SetSlideDistance(distance);
            if (!IsOpen && _slideTransition?.IsRunning != true)
            {
                _slideTransition?.Snap(0f);
            }
        }

        public void Open()
        {
            EnsureInitialized();
            IsOpen = true;
            gameObject.SetActive(true);
            _slideTransition.Play(1f, _transitionDuration);
        }

        public void ReplayOpenTransition()
        {
            EnsureInitialized();
            IsOpen = true;
            gameObject.SetActive(true);
            _slideTransition.Snap(0f);
            _slideTransition.Play(1f, _transitionDuration);
        }

        public void Close()
        {
            EnsureInitialized();
            if (!IsOpen && _slideTransition?.IsRunning != true)
            {
                _slideTransition?.Snap(0f);
                return;
            }

            IsOpen = false;
            _slideTransition.Play(0f, _transitionDuration);
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _rectTransform = GetComponent<RectTransform>();
            _slideTransition = new VerticalSlideTransition(
                this,
                _rectTransform,
                GetComponent<CanvasGroup>(),
                () => IsOpen);

            var background = GetComponent<Image>();
            background.color = new Color32(247, 244, 238, 252);
            background.raycastTarget = true;

            var labelObject = new GameObject(
                "Message",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            labelObject.layer = gameObject.layer;
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.SetParent(transform, false);
            Stretch(labelRect);

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null)
            {
                label.font = TMP_Settings.defaultFontAsset;
            }

            label.text = "未実装";
            label.fontSize = 30f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = GameUiPalette.PrimaryText;
            label.raycastTarget = false;

            _initialized = true;
            _slideTransition.Snap(0f);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
