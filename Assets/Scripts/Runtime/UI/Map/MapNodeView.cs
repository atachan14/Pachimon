using System;
using Pachimon.Map;
using Pachimon.Trainer;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public sealed class MapNodeView : MonoBehaviour
    {
        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Button _button;
        [SerializeField] private Outline _outline;
        [SerializeField] private TrainerMapIconView _trainerIcon;
        [SerializeField] private Image[] _trainerSelectionFrame;
        [SerializeField] private Image[] _gymRoleFrame;
        [FormerlySerializedAs("_eventRing")]
        [SerializeField] private Image _symbolRing;
        [FormerlySerializedAs("_eventOutline")]
        [SerializeField] private Outline _symbolOutline;
        [SerializeField] private Sprite _eventRingSprite;
        [SerializeField] private Sprite _restSpotRingSprite;

        private string _nodeId;
        private Action<string> _onSelected;

        public string NodeId => _nodeId;

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(NotifySelected);
            }
        }

        public void Configure(
            Image background,
            TMP_Text label,
            Button button,
            Outline outline,
            TrainerMapIconView trainerIcon = null,
            Image[] trainerSelectionFrame = null,
            Image[] gymRoleFrame = null,
            Image symbolRing = null,
            Outline symbolOutline = null,
            Sprite eventRingSprite = null,
            Sprite restSpotRingSprite = null)
        {
            _background = background;
            _label = label;
            _button = button;
            _outline = outline;
            _trainerIcon = trainerIcon;
            _trainerSelectionFrame = trainerSelectionFrame;
            _gymRoleFrame = gymRoleFrame;
            _symbolRing = symbolRing;
            _symbolOutline = symbolOutline;
            _eventRingSprite = eventRingSprite;
            _restSpotRingSprite = restSpotRingSprite;
        }

        public void Bind(
            MapNode node,
            bool isCurrent,
            bool isResolved,
            bool isSelectable,
            bool isSelected,
            TrainerMapIconSet trainerIconSet,
            TrainerColorScheme? trainerColors,
            Action<string> onSelected)
        {
            _nodeId = node.NodeId;
            _onSelected = onSelected;

            var showTrainerIcon = _trainerIcon != null
                && trainerIconSet != null
                && trainerColors.HasValue;
            var showEventIcon = _symbolRing != null && node.NodeType == NodeType.Event;
            var showRestSpotIcon = _symbolRing != null && node.NodeType == NodeType.RestSpot;
            var showSymbolIcon = showEventIcon || showRestSpotIcon;

            if (_trainerIcon != null)
            {
                _trainerIcon.gameObject.SetActive(showTrainerIcon);
                _trainerIcon.transform.localScale = node.NodeType == NodeType.Gym
                    ? Vector3.one * 1.05f
                    : Vector3.one;
                if (showTrainerIcon)
                {
                    _trainerIcon.Render(trainerIconSet, trainerColors.Value);
                }
            }

            if (_gymRoleFrame != null)
            {
                var showGymRoleFrame = showTrainerIcon && node.NodeType == NodeType.Gym;
                foreach (var framePart in _gymRoleFrame)
                {
                    if (framePart != null)
                    {
                        framePart.enabled = showGymRoleFrame;
                    }
                }
            }

            if (_symbolRing != null)
            {
                _symbolRing.gameObject.SetActive(showSymbolIcon);
                if (showSymbolIcon)
                {
                    _symbolRing.sprite = showEventIcon
                        ? _eventRingSprite
                        : _restSpotRingSprite;
                }
            }

            if (_label != null)
            {
                _label.gameObject.SetActive(!showTrainerIcon);
                _label.text = GetNodeLabel(node.NodeType);
                _label.color = showSymbolIcon
                    ? GetNodeTypeColor(node.NodeType)
                    : new Color(1f, 0.96f, 0.84f, 1f);
                _label.fontSize = showSymbolIcon ? 28f : 22f;
            }

            if (_background != null)
            {
                if (showTrainerIcon || showSymbolIcon)
                {
                    _background.color = Color.clear;
                }
                else
                {
                    var typeColor = GetNodeTypeColor(node.NodeType);
                    _background.color = isResolved
                        ? Color.Lerp(typeColor, new Color(0.12f, 0.14f, 0.13f), 0.58f)
                        : typeColor;
                }
            }

            var showSelection = isCurrent || isSelectable || isSelected;
            var selectionColor = isSelected
                ? new Color(1f, 0.76f, 0.18f, 1f)
                : isCurrent
                    ? new Color(1f, 0.95f, 0.68f, 1f)
                    : new Color(0.92f, 1f, 0.82f, 0.9f);

            if (_outline != null)
            {
                _outline.enabled = !showTrainerIcon && !showSymbolIcon && showSelection;
                _outline.effectColor = selectionColor;
                _outline.effectDistance = isCurrent || isSelected
                    ? new Vector2(4f, -4f)
                    : new Vector2(2f, -2f);
            }

            if (_symbolOutline != null)
            {
                _symbolOutline.enabled = showSymbolIcon && showSelection;
                _symbolOutline.effectColor = selectionColor;
                _symbolOutline.effectDistance = isCurrent || isSelected
                    ? new Vector2(3f, -3f)
                    : new Vector2(2f, -2f);
            }

            if (_trainerSelectionFrame != null)
            {
                foreach (var framePart in _trainerSelectionFrame)
                {
                    if (framePart == null)
                    {
                        continue;
                    }

                    framePart.enabled = showTrainerIcon && showSelection;
                    framePart.color = selectionColor;
                }
            }

            transform.localScale = isCurrent || isSelected ? Vector3.one * 1.14f : Vector3.one;

            if (_button == null)
            {
                return;
            }

            _button.onClick.RemoveListener(NotifySelected);
            _button.onClick.AddListener(NotifySelected);
            _button.interactable = true;
        }

        private void NotifySelected()
        {
            _onSelected?.Invoke(_nodeId);
        }

        private static string GetNodeLabel(NodeType nodeType)
        {
            return nodeType switch
            {
                NodeType.Start => "S",
                NodeType.Battle => "B",
                NodeType.Gym => "G",
                NodeType.RestSpot => "+",
                NodeType.City => "C",
                NodeType.Event => "?",
                NodeType.LeagueGate => "L",
                NodeType.Elite => "E",
                NodeType.Ghost => "Gh",
                _ => "-",
            };
        }

        private static Color GetNodeTypeColor(NodeType nodeType)
        {
            return nodeType switch
            {
                NodeType.Start => new Color(0.16f, 0.55f, 0.50f, 1f),
                NodeType.Battle => new Color(0.72f, 0.25f, 0.16f, 1f),
                NodeType.Gym => new Color(0.88f, 0.61f, 0.12f, 1f),
                NodeType.RestSpot => new Color(0.30f, 0.63f, 0.30f, 1f),
                NodeType.City => new Color(0.12f, 0.51f, 0.68f, 1f),
                NodeType.Event => new Color(0.88f, 0.43f, 0.12f, 1f),
                NodeType.LeagueGate => new Color(0.21f, 0.23f, 0.22f, 1f),
                NodeType.Elite => new Color(0.52f, 0.10f, 0.12f, 1f),
                NodeType.Ghost => new Color(0.40f, 0.45f, 0.50f, 1f),
                _ => Color.gray,
            };
        }
    }
}
