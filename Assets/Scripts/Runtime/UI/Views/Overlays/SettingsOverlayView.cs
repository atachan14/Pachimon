using System;
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
        private Action<LayoutMode> _layoutModeSelected;
        private Image _compactButtonImage;
        private Image _expandedButtonImage;
        private TMP_Text _layoutStatusText;
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

        public void ConfigureLayoutMode(Action<LayoutMode> onSelected)
        {
            EnsureInitialized();
            _layoutModeSelected = onSelected;
        }

        public void SetLayoutModes(
            LayoutMode preferredMode,
            LayoutMode effectiveMode)
        {
            EnsureInitialized();
            SetButtonSelected(
                _compactButtonImage,
                preferredMode == LayoutMode.Compact);
            SetButtonSelected(
                _expandedButtonImage,
                preferredMode == LayoutMode.Expanded);

            if (_layoutStatusText == null)
            {
                return;
            }

            _layoutStatusText.text = preferredMode == LayoutMode.Expanded
                                     && effectiveMode == LayoutMode.Compact
                ? "\u753b\u9762\u5e45\u304c\u72ed\u3044\u305f\u3081\u3001\u73fe\u5728\u306f\u30b3\u30f3\u30d1\u30af\u30c8\u8868\u793a\u3067\u3059\u3002"
                : $"\u73fe\u5728\u306e\u8868\u793a: {GetModeLabel(effectiveMode)}";
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

            CreateContent();

            _initialized = true;
            _slideTransition.Snap(0f);
        }

        private void CreateContent()
        {
            var contentObject = new GameObject(
                "Content",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup));
            contentObject.layer = gameObject.layer;
            var contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.SetParent(transform, false);
            contentRect.anchorMin = new Vector2(0.12f, 0.12f);
            contentRect.anchorMax = new Vector2(0.88f, 0.88f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            var layout = contentObject.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CreateLabel(
                contentRect,
                "Title",
                "\u8a2d\u5b9a",
                34f,
                FontStyles.Bold,
                54f);
            CreateLabel(
                contentRect,
                "LayoutModeLabel",
                "\u8868\u793a\u30e2\u30fc\u30c9",
                25f,
                FontStyles.Bold,
                44f);

            var buttonRowObject = new GameObject(
                "LayoutModeButtons",
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup),
                typeof(LayoutElement));
            buttonRowObject.layer = gameObject.layer;
            var buttonRowRect = buttonRowObject.GetComponent<RectTransform>();
            buttonRowRect.SetParent(contentRect, false);
            var buttonRowLayout = buttonRowObject.GetComponent<HorizontalLayoutGroup>();
            buttonRowLayout.spacing = 16f;
            buttonRowLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonRowLayout.childControlWidth = true;
            buttonRowLayout.childControlHeight = true;
            buttonRowLayout.childForceExpandWidth = true;
            buttonRowLayout.childForceExpandHeight = true;
            buttonRowObject.GetComponent<LayoutElement>().preferredHeight = 68f;

            _compactButtonImage = CreateModeButton(
                buttonRowRect,
                "CompactButton",
                "\u30b3\u30f3\u30d1\u30af\u30c8",
                () => _layoutModeSelected?.Invoke(LayoutMode.Compact));
            _expandedButtonImage = CreateModeButton(
                buttonRowRect,
                "ExpandedButton",
                "\u62e1\u5f35",
                () => _layoutModeSelected?.Invoke(LayoutMode.Expanded));
            _layoutStatusText = CreateLabel(
                contentRect,
                "LayoutStatus",
                "\u73fe\u5728\u306e\u8868\u793a: \u30b3\u30f3\u30d1\u30af\u30c8",
                20f,
                FontStyles.Normal,
                58f);
        }

        private Image CreateModeButton(
            RectTransform parent,
            string objectName,
            string label,
            Action onClick)
        {
            var buttonObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            buttonObject.layer = gameObject.layer;
            buttonObject.transform.SetParent(parent, false);
            var image = buttonObject.GetComponent<Image>();
            image.color = Color.white;

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick?.Invoke());

            var text = CreateLabel(
                buttonObject.GetComponent<RectTransform>(),
                "Label",
                label,
                22f,
                FontStyles.Bold,
                -1f);
            Stretch(text.rectTransform);
            return image;
        }

        private TMP_Text CreateLabel(
            RectTransform parent,
            string objectName,
            string value,
            float fontSize,
            FontStyles fontStyle,
            float preferredHeight)
        {
            var labelObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            labelObject.layer = gameObject.layer;
            labelObject.transform.SetParent(parent, false);
            var label = labelObject.GetComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null)
            {
                label.font = TMP_Settings.defaultFontAsset;
            }

            label.text = value;
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.alignment = TextAlignmentOptions.Center;
            label.color = GameUiPalette.PrimaryText;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            if (preferredHeight >= 0f)
            {
                labelObject.AddComponent<LayoutElement>().preferredHeight =
                    preferredHeight;
            }

            return label;
        }

        private static void SetButtonSelected(Image image, bool selected)
        {
            if (image == null)
            {
                return;
            }

            image.color = selected
                ? GameUiPalette.ButtonAccent
                : Color.white;
            var text = image.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                text.color = selected
                    ? GameUiPalette.OnAccentText
                    : GameUiPalette.PrimaryText;
            }
        }

        private static string GetModeLabel(LayoutMode mode)
        {
            return mode == LayoutMode.Compact
                ? "\u30b3\u30f3\u30d1\u30af\u30c8"
                : "\u62e1\u5f35";
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
