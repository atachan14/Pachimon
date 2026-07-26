using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public readonly struct TrainerRewardIconContent
    {
        public TrainerRewardIconContent(string label, string colorHex, Sprite sprite = null)
        {
            Label = label;
            ColorHex = colorHex;
            Sprite = sprite;
        }

        public string Label { get; }
        public string ColorHex { get; }
        public Sprite Sprite { get; }
    }

    public sealed class TrainerPreviewContent
    {
        public TrainerPreviewContent(
            Sprite graphic,
            string displayName,
            IEnumerable<TrainerRewardIconContent> rewardIcons,
            int? gold)
        {
            Graphic = graphic;
            DisplayName = displayName;
            RewardIcons = rewardIcons?.ToArray() ?? Array.Empty<TrainerRewardIconContent>();
            Gold = gold;
        }

        public Sprite Graphic { get; }
        public string DisplayName { get; }
        public IReadOnlyList<TrainerRewardIconContent> RewardIcons { get; }
        public int? Gold { get; }
    }

    public sealed class TrainerTabView : MonoBehaviour
    {
        [SerializeField] private Image _graphic;
        [SerializeField] private TMP_Text _displayName;
        [SerializeField] private Transform _rewardIconContainer;
        [SerializeField] private TrainerRewardIconView _rewardIconTemplate;
        [SerializeField] private TMP_Text _emptyRewardText;
        [SerializeField] private TMP_Text _goldText;
        public RectTransform GraphicRect => _graphic?.rectTransform;

        public void Configure(
            Image graphic,
            TMP_Text displayName,
            Transform rewardIconContainer,
            TrainerRewardIconView rewardIconTemplate,
            TMP_Text emptyRewardText,
            TMP_Text goldText)
        {
            _graphic = graphic;
            _displayName = displayName;
            _rewardIconContainer = rewardIconContainer;
            _rewardIconTemplate = rewardIconTemplate;
            _emptyRewardText = emptyRewardText;
            _goldText = goldText;
        }

        public void Bind(TrainerPreviewContent content)
        {
            if (content == null) return;

            if (_graphic != null)
            {
                _graphic.sprite = content.Graphic;
                _graphic.enabled = content.Graphic != null;
                _graphic.color = Color.white;
                _graphic.preserveAspect = true;
            }

            if (_displayName != null) _displayName.text = content.DisplayName;
            if (_goldText != null) _goldText.text = content.Gold?.ToString() ?? "---";
            RebuildRewardIcons(content.RewardIcons);
        }

        private void RebuildRewardIcons(IReadOnlyList<TrainerRewardIconContent> icons)
        {
            if (_rewardIconContainer == null || _rewardIconTemplate == null) return;

            for (var index = _rewardIconContainer.childCount - 1; index >= 0; index--)
            {
                var child = _rewardIconContainer.GetChild(index);
                if (child != _rewardIconTemplate.transform
                    && (_emptyRewardText == null || child != _emptyRewardText.transform))
                {
                    Destroy(child.gameObject);
                }
            }

            var hasIcons = icons != null && icons.Count > 0;
            if (_emptyRewardText != null) _emptyRewardText.gameObject.SetActive(!hasIcons);
            if (hasIcons)
            {
                foreach (var icon in icons)
                {
                    var view = Instantiate(_rewardIconTemplate, _rewardIconContainer, false);
                    view.gameObject.SetActive(true);
                    view.Bind(icon);
                }
            }

            _rewardIconTemplate.gameObject.SetActive(false);
        }
    }
}
