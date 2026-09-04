using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public readonly struct StartCandidateCardContent
    {
        public StartCandidateCardContent(string instanceId, string displayName, Sprite frontSprite)
        {
            InstanceId = instanceId;
            DisplayName = displayName;
            FrontSprite = frontSprite;
        }

        public string InstanceId { get; }
        public string DisplayName { get; }
        public Sprite FrontSprite { get; }
    }

    public sealed class StartCandidateCardView : MonoBehaviour
    {
        private const float GraphicWidthRatio = 0.84f;
        private const float GraphicHeightRatio = 0.68f;
        private const float MinimumNameHeight = 40f;
        private const float MaximumNameHeight = 80f;
        private const float MinimumGraphicNameGap = 8f;
        private const float MaximumGraphicNameGap = 24f;

        private static readonly Color SelectedGraphicColor =
            new(0.62f, 0.62f, 0.62f, 1f);

        [SerializeField] private Image _frontGraphic;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _selectionOrderText;
        [SerializeField] private Button _button;
        [SerializeField] private Outline _focusOutline;

        private string _instanceId;
        private Action<string> _onClicked;
        private Material _grayscaleMaterial;

        private void OnRectTransformDimensionsChange()
        {
            RefreshContentLayout();
        }

        private void OnDestroy()
        {
            _button?.onClick.RemoveListener(NotifyClicked);
            if (_grayscaleMaterial != null)
            {
                Destroy(_grayscaleMaterial);
            }
        }

        public void Configure(
            Image frontGraphic,
            TMP_Text nameText,
            TMP_Text selectionOrderText,
            Button button,
            Outline focusOutline)
        {
            _frontGraphic = frontGraphic;
            _nameText = nameText;
            _selectionOrderText = selectionOrderText;
            _button = button;
            _focusOutline = focusOutline;
            if (_button != null)
            {
                _button.transition = Selectable.Transition.None;
                _button.targetGraphic = null;
            }

            if (_frontGraphic != null)
            {
                _frontGraphic.canvasRenderer.SetColor(Color.white);
                var shader = Resources.Load<Shader>("Shaders/UIGrayscale")
                    ?? Shader.Find("Pachimon/UI/Grayscale");
                if (shader != null)
                {
                    _grayscaleMaterial = new Material(shader)
                    {
                        name = "RuntimeCandidateGrayscale"
                    };
                    _frontGraphic.material = _grayscaleMaterial;
                }
            }

            SetFocused(false);
            RefreshContentLayout();
        }

        public void Bind(StartCandidateCardContent content, Action<string> onClicked)
        {
            _instanceId = content.InstanceId;
            _onClicked = onClicked;

            if (_nameText != null) _nameText.text = content.DisplayName;
            if (_frontGraphic != null)
            {
                _frontGraphic.sprite = content.FrontSprite;
                _frontGraphic.enabled = content.FrontSprite != null;
                _frontGraphic.preserveAspect = true;
            }

            if (_button != null)
            {
                _button.onClick.RemoveListener(NotifyClicked);
                _button.onClick.AddListener(NotifyClicked);
            }

            SetSelectionOrder(0);
            RefreshContentLayout();
        }

        private void RefreshContentLayout()
        {
            if (transform is not RectTransform cardRect
                || _frontGraphic?.rectTransform.parent is not RectTransform graphicArea
                || _nameText == null)
            {
                return;
            }

            var cardSize = cardRect.rect.size;
            if (cardSize.x <= 1f || cardSize.y <= 1f)
            {
                return;
            }

            var graphicSize = Mathf.Max(
                1f,
                Mathf.Min(
                    cardSize.x * GraphicWidthRatio,
                    cardSize.y * GraphicHeightRatio));
            var nameHeight = Mathf.Clamp(
                cardSize.y * 0.1f,
                MinimumNameHeight,
                MaximumNameHeight);
            var gap = Mathf.Clamp(
                cardSize.y * 0.02f,
                MinimumGraphicNameGap,
                MaximumGraphicNameGap);
            var groupHeight = graphicSize + gap + nameHeight;
            var nameCenterY = (-groupHeight * 0.5f) + (nameHeight * 0.5f);
            var graphicCenterY = nameCenterY
                + (nameHeight * 0.5f)
                + gap
                + (graphicSize * 0.5f);

            SetCenteredRect(
                graphicArea,
                new Vector2(graphicSize, graphicSize),
                new Vector2(0f, graphicCenterY));
            SetCenteredRect(
                _nameText.rectTransform,
                new Vector2(cardSize.x * 0.92f, nameHeight),
                new Vector2(0f, nameCenterY));

            if (_selectionOrderText != null)
            {
                var orderHeight = nameHeight * 0.8f;
                SetCenteredRect(
                    _selectionOrderText.rectTransform,
                    new Vector2(cardSize.x * 0.76f, orderHeight),
                    new Vector2(
                        0f,
                        nameCenterY - (nameHeight * 0.5f) - (orderHeight * 0.5f)));
            }
        }

        private static void SetCenteredRect(
            RectTransform rect,
            Vector2 size,
            Vector2 position)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        public void SetFocused(bool isFocused)
        {
            if (_focusOutline != null)
            {
                _focusOutline.enabled = isFocused;
            }
        }

        public void SetSelectionOrder(int selectionOrder)
        {
            var isSelected = selectionOrder > 0;
            if (_frontGraphic != null)
            {
                _frontGraphic.canvasRenderer.SetColor(Color.white);
                _frontGraphic.color = isSelected
                    ? SelectedGraphicColor
                    : Color.white;
                SetGrayscaleAmount(isSelected ? 1f : 0f);
            }

            if (_selectionOrderText != null)
            {
                _selectionOrderText.gameObject.SetActive(isSelected);
                _selectionOrderText.text = isSelected ? $"{selectionOrder}匹目" : string.Empty;
            }
        }

        public void SetConfirmationProgress(float progress)
        {
            if (_frontGraphic != null)
            {
                _frontGraphic.canvasRenderer.SetColor(Color.white);
                _frontGraphic.color = Color.Lerp(
                    SelectedGraphicColor,
                    Color.white,
                    Mathf.Clamp01(progress));
                SetGrayscaleAmount(1f - Mathf.Clamp01(progress));
            }
        }

        private void SetGrayscaleAmount(float amount)
        {
            if (_grayscaleMaterial != null)
            {
                _grayscaleMaterial.SetFloat("_EffectAmount", Mathf.Clamp01(amount));
            }
        }

        private void NotifyClicked() => _onClicked?.Invoke(_instanceId);
    }
}
