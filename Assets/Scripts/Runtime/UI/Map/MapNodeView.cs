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
        private Button _partyCandidateButton;
        private Image _partyCandidateGraphic;
        private Action<string> _onPartyCandidatesSelected;
        private float _nodeVisualScale = 1f;
        private Vector2 _trainerBasePosition;
        private bool _trainerBasePositionCaptured;
        private Vector2[] _trainerSelectionFrameBasePositions;

        public string NodeId => _nodeId;

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(NotifySelected);
            }

            if (_partyCandidateButton != null)
            {
                _partyCandidateButton.onClick.RemoveListener(NotifyPartyCandidatesSelected);
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
            CaptureTrainerBasePosition();
            _trainerSelectionFrame = trainerSelectionFrame;
            CaptureTrainerSelectionFrameBasePositions();
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
            Action<string> onSelected,
            Action<string> onPartyCandidatesSelected = null,
            float partyCandidateHorizontalOffset = 0f)
        {
            _nodeId = node.NodeId;
            _onSelected = onSelected;
            _onPartyCandidatesSelected = onPartyCandidatesSelected;

            var showTrainerIcon = _trainerIcon != null
                && trainerIconSet != null
                && trainerColors.HasValue;
            var showEventIcon = _symbolRing != null && node.NodeType == NodeType.Event;
            var showRestSpotIcon = _symbolRing != null && node.NodeType == NodeType.RestSpot;
            var showSymbolIcon = showEventIcon || showRestSpotIcon;

            if (_trainerIcon != null)
            {
                _trainerIcon.gameObject.SetActive(showTrainerIcon);
                var trainerScale = node.NodeType == NodeType.Gym ? 1.05f : 1f;
                if (node.NodeType == NodeType.PartyEncounter
                    && (isCurrent || isSelected))
                {
                    trainerScale *= 1.14f;
                }
                _trainerIcon.transform.localScale = Vector3.one * trainerScale;
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
                _label.fontSize = (showSymbolIcon ? 28f : 22f) * _nodeVisualScale;
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

            ConfigurePartyCandidateButton(
                node.NodeType == NodeType.PartyEncounter,
                partyCandidateHorizontalOffset);

            transform.localScale = node.NodeType != NodeType.PartyEncounter
                && (isCurrent || isSelected)
                    ? Vector3.one * 1.14f
                    : Vector3.one;

            if (_button == null)
            {
                return;
            }

            _button.onClick.RemoveListener(NotifySelected);
            _button.onClick.AddListener(NotifySelected);
            _button.interactable = true;
        }

        public void ApplyNodeSize(float nodeSize)
        {
            _nodeVisualScale = Mathf.Max(1f, nodeSize / 56f);
            if (_partyCandidateButton != null)
            {
                ((RectTransform)_partyCandidateButton.transform).sizeDelta =
                    new Vector2(64f, 76f) * _nodeVisualScale;
            }
        }

        public void SetTrainerApproachOffset(Vector2 offset)
        {
            CaptureTrainerBasePosition();
            CaptureTrainerSelectionFrameBasePositions();
            if (_trainerIcon?.transform is RectTransform trainerRect)
            {
                trainerRect.anchoredPosition = _trainerBasePosition + offset;
            }

            if (_trainerSelectionFrame == null
                || _trainerSelectionFrameBasePositions == null)
            {
                return;
            }

            for (var index = 0; index < _trainerSelectionFrame.Length; index++)
            {
                if (_trainerSelectionFrame[index]?.transform is RectTransform frameRect)
                {
                    frameRect.anchoredPosition =
                        _trainerSelectionFrameBasePositions[index] + offset;
                }
            }
        }

        private void CaptureTrainerBasePosition()
        {
            if (_trainerBasePositionCaptured
                || _trainerIcon?.transform is not RectTransform trainerRect)
            {
                return;
            }

            _trainerBasePosition = trainerRect.anchoredPosition;
            _trainerBasePositionCaptured = true;
        }

        private void CaptureTrainerSelectionFrameBasePositions()
        {
            if (_trainerSelectionFrameBasePositions != null
                || _trainerSelectionFrame == null)
            {
                return;
            }

            _trainerSelectionFrameBasePositions = new Vector2[_trainerSelectionFrame.Length];
            for (var index = 0; index < _trainerSelectionFrame.Length; index++)
            {
                if (_trainerSelectionFrame[index]?.transform is RectTransform frameRect)
                {
                    _trainerSelectionFrameBasePositions[index] = frameRect.anchoredPosition;
                }
            }
        }

        private void NotifySelected()
        {
            _onSelected?.Invoke(_nodeId);
        }

        private void NotifyPartyCandidatesSelected()
        {
            _onPartyCandidatesSelected?.Invoke(_nodeId);
        }

        private void ConfigurePartyCandidateButton(bool isVisible, float horizontalOffset)
        {
            if (!isVisible)
            {
                _partyCandidateButton?.gameObject.SetActive(false);
                return;
            }

            EnsurePartyCandidateButton();
            if (_partyCandidateButton == null)
            {
                return;
            }

            _partyCandidateButton.gameObject.SetActive(true);
            ((RectTransform)_partyCandidateButton.transform).anchoredPosition =
                new Vector2(horizontalOffset, 0f);
            _partyCandidateButton.onClick.RemoveListener(NotifyPartyCandidatesSelected);
            _partyCandidateButton.onClick.AddListener(NotifyPartyCandidatesSelected);
        }

        private void EnsurePartyCandidateButton()
        {
            if (_partyCandidateButton != null)
            {
                return;
            }

            var buttonObject = new GameObject(
                "PartyCandidateProfessor",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(Outline));
            buttonObject.layer = gameObject.layer;
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.SetParent(transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(64f, 76f);
            rect.sizeDelta *= _nodeVisualScale;

            _partyCandidateGraphic = buttonObject.GetComponent<Image>();
            _partyCandidateGraphic.sprite = Resources.Load<Sprite>("Professor/professor");
            _partyCandidateGraphic.preserveAspect = true;
            _partyCandidateGraphic.color = Color.white;

            var outline = buttonObject.GetComponent<Outline>();
            outline.effectColor = new Color(0.14f, 0.18f, 0.17f, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);

            _partyCandidateButton = buttonObject.GetComponent<Button>();
            _partyCandidateButton.targetGraphic = _partyCandidateGraphic;
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
                NodeType.HallOfFame => "H",
                NodeType.PartyEncounter => "P",
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
                NodeType.HallOfFame => new Color(0.90f, 0.68f, 0.18f, 1f),
                NodeType.PartyEncounter => new Color(0.86f, 0.32f, 0.28f, 1f),
                _ => Color.gray,
            };
        }
    }
}
