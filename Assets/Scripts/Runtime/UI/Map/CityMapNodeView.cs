using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public sealed class CityMapNodeView : MonoBehaviour
    {
        private const float BaseVisualScale = 1.1f;
        private const float HighlightedVisualScale = 1.18f;

        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Button _button;
        [SerializeField] private Outline _outline;

        private string _selectableTargetNodeId;
        private Action<string> _onSelected;

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(NotifySelected);
            }
        }

        public void Configure(Image background, TMP_Text label, Button button, Outline outline)
        {
            _background = background;
            _label = label;
            _button = button;
            _outline = outline;
        }

        public void Bind(
            string selectableTargetNodeId,
            bool isCurrent,
            bool isResolved,
            bool isSelectable,
            bool isSelected,
            Action<string> onSelected)
        {
            _selectableTargetNodeId = selectableTargetNodeId;
            _onSelected = onSelected;

            if (_label != null)
            {
                _label.text = string.Empty;
            }

            if (_background != null)
            {
                _background.color = isResolved
                    ? new Color(0.48f, 0.5f, 0.5f, 0.82f)
                    : Color.white;
            }

            if (_outline != null)
            {
                _outline.enabled = isCurrent || isSelectable || isSelected;
                _outline.effectColor = isSelected
                    ? new Color(1f, 0.76f, 0.18f, 1f)
                    : isCurrent
                    ? new Color(1f, 0.95f, 0.68f, 1f)
                    : new Color(0.92f, 1f, 0.82f, 0.9f);
                _outline.effectDistance = isCurrent || isSelected
                    ? new Vector2(4f, -4f)
                    : new Vector2(2f, -2f);
            }

            transform.localScale = Vector3.one * (
                isCurrent || isSelected
                    ? HighlightedVisualScale
                    : BaseVisualScale);

            if (_button == null)
            {
                return;
            }

            _button.onClick.RemoveListener(NotifySelected);
            _button.onClick.AddListener(NotifySelected);
            _button.interactable = _selectableTargetNodeId != null;
        }

        private void NotifySelected()
        {
            if (_selectableTargetNodeId != null)
            {
                _onSelected?.Invoke(_selectableTargetNodeId);
            }
        }
    }
}
