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
