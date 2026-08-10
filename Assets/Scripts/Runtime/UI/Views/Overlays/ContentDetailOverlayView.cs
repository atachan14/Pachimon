using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public enum ContentDetailKind
    {
        Skill,
        Passive,
        Item,
        FieldEffect,
    }

    public sealed class ContentDetailOverlayContent
    {
        public ContentDetailOverlayContent(
            ContentDetailKind kind,
            string title,
            string metadata,
            string description,
            Color accentColor)
        {
            Kind = kind;
            Title = title ?? string.Empty;
            Metadata = metadata ?? string.Empty;
            Description = description ?? string.Empty;
            AccentColor = accentColor;
        }

        public ContentDetailKind Kind { get; }
        public string Title { get; }
        public string Metadata { get; }
        public string Description { get; }
        public Color AccentColor { get; }
    }

    public sealed class ContentDetailOverlayView : MonoBehaviour
    {
        private const float TransitionDuration = 0.25f;
        private const float PanelInset = 36f;

        private RectTransform _rect;
        private CanvasGroup _canvasGroup;
        private TMP_Text _kind;
        private TMP_Text _title;
        private TMP_Text _metadata;
        private TMP_Text _description;
        private Image _accent;
        private Coroutine _transition;
        private float _slideDistance = 1f;

        public bool IsOpen { get; private set; }
        public ContentDetailKind? ShownKind { get; private set; }

        public static ContentDetailOverlayView CreateRuntime(RectTransform parent)
        {
            var rootObject = new GameObject(
                "ContentDetailOverlayView",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup),
                typeof(ContentDetailOverlayView));
            rootObject.layer = parent.gameObject.layer;
            var rect = rootObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Stretch(rect);
            rect.offsetMin = new Vector2(PanelInset, PanelInset);
            rect.offsetMax = new Vector2(-PanelInset, -PanelInset);
            var view = rootObject.GetComponent<ContentDetailOverlayView>();
            view.Build();
            view.ApplyProgress(0f);
            return view;
        }

        public void SetSlideDistance(float distance)
        {
            _slideDistance = Mathf.Max(1f, distance);
            if (!IsOpen && _transition == null)
            {
                ApplyProgress(0f);
            }
        }

        public void Show(ContentDetailOverlayContent content)
        {
            if (content == null)
            {
                Close();
                return;
            }

            _kind.text = GetKindLabel(content.Kind);
            _title.text = content.Title;
            _metadata.text = content.Metadata;
            var hasMetadata = !string.IsNullOrWhiteSpace(content.Metadata);
            _metadata.gameObject.SetActive(hasMetadata);
            SetAnchors(
                _description.rectTransform,
                new Vector2(0.08f, 0.18f),
                new Vector2(0.92f, hasMetadata ? 0.54f : 0.68f));
            _description.text = content.Description;
            _accent.color = content.AccentColor;
            ShownKind = content.Kind;
            IsOpen = true;
            gameObject.SetActive(true);
            StartTransition(1f);
        }

        public void Close()
        {
            if (!IsOpen && _transition == null)
            {
                ApplyProgress(0f);
                return;
            }

            IsOpen = false;
            StartTransition(0f);
        }

        private void Build()
        {
            _rect = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
            GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.98f);

            _accent = CreateImage("Accent", transform, GameUiPalette.SkillChip);
            SetAnchors(_accent.rectTransform, new Vector2(0f, 0.94f), Vector2.one);

            _kind = CreateText(
                "Kind",
                transform,
                18f,
                FontStyles.Bold,
                TextAlignmentOptions.Left);
            _kind.color = GameUiPalette.SecondaryText;
            SetAnchors(
                _kind.rectTransform,
                new Vector2(0.08f, 0.84f),
                new Vector2(0.92f, 0.92f));

            _title = CreateText(
                "Title",
                transform,
                32f,
                FontStyles.Bold,
                TextAlignmentOptions.Left);
            SetAnchors(
                _title.rectTransform,
                new Vector2(0.08f, 0.70f),
                new Vector2(0.92f, 0.84f));

            _metadata = CreateText(
                "Metadata",
                transform,
                20f,
                FontStyles.Bold,
                TextAlignmentOptions.TopLeft);
            _metadata.color = GameUiPalette.SecondaryText;
            _metadata.textWrappingMode = TextWrappingModes.Normal;
            SetAnchors(
                _metadata.rectTransform,
                new Vector2(0.08f, 0.56f),
                new Vector2(0.92f, 0.70f));

            _description = CreateText(
                "Description",
                transform,
                22f,
                FontStyles.Normal,
                TextAlignmentOptions.TopLeft);
            _description.textWrappingMode = TextWrappingModes.Normal;
            SetAnchors(
                _description.rectTransform,
                new Vector2(0.08f, 0.18f),
                new Vector2(0.92f, 0.54f));

            var closeButton = CreateButton("CloseButton", transform, "閉じる", Close);
            SetAnchors(
                closeButton.GetComponent<RectTransform>(),
                new Vector2(0.32f, 0.05f),
                new Vector2(0.68f, 0.14f));
        }

        private static string GetKindLabel(ContentDetailKind kind)
        {
            return kind switch
            {
                ContentDetailKind.Skill => "SKILL",
                ContentDetailKind.Passive => "PASSIVE",
                ContentDetailKind.Item => "ITEM",
                ContentDetailKind.FieldEffect => "FIELD",
                _ => string.Empty,
            };
        }

        private void StartTransition(float target)
        {
            if (_transition != null)
            {
                StopCoroutine(_transition);
            }

            _transition = StartCoroutine(Animate(target));
        }

        private IEnumerator Animate(float target)
        {
            var start = GetProgress();
            var elapsed = 0f;
            while (elapsed < TransitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / TransitionDuration);
                var eased = t * t * (3f - (2f * t));
                ApplyProgress(Mathf.Lerp(start, target, eased));
                yield return null;
            }

            ApplyProgress(target);
            _transition = null;
            if (!IsOpen)
            {
                gameObject.SetActive(false);
            }
        }

        private float GetProgress()
        {
            return Mathf.Clamp01(1f - (_rect.anchoredPosition.y / _slideDistance));
        }

        private void ApplyProgress(float progress)
        {
            if (_rect == null || _canvasGroup == null)
            {
                return;
            }

            _rect.anchoredPosition = new Vector2(
                0f,
                Mathf.Lerp(_slideDistance, 0f, progress));
            _canvasGroup.alpha = progress;
            _canvasGroup.interactable = progress >= 0.999f;
            _canvasGroup.blocksRaycasts = progress > 0.01f;
        }

        private static Image CreateImage(string objectName, Transform parent, Color color)
        {
            var target = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            target.layer = parent.gameObject.layer;
            target.transform.SetParent(parent, false);
            var image = target.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text CreateText(
            string objectName,
            Transform parent,
            float fontSize,
            FontStyles style,
            TextAlignmentOptions alignment)
        {
            var target = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            target.layer = parent.gameObject.layer;
            target.transform.SetParent(parent, false);
            var text = target.GetComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null)
            {
                text.font = TMP_Settings.defaultFontAsset;
            }

            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.richText = true;
            text.color = GameUiPalette.PrimaryText;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(
            string objectName,
            Transform parent,
            string label,
            Action onClicked)
        {
            var target = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            target.layer = parent.gameObject.layer;
            target.transform.SetParent(parent, false);
            var image = target.GetComponent<Image>();
            image.color = GameUiPalette.ButtonNeutral;
            var button = target.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClicked?.Invoke());

            var text = CreateText(
                "Label",
                target.transform,
                20f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);
            text.color = GameUiPalette.OnAccentText;
            Stretch(text.rectTransform);
            text.text = label;
            return button;
        }

        private static void SetAnchors(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
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
