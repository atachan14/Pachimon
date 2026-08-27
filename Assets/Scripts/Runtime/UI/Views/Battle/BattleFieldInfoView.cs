using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Battle;
using Pachimon.Reward;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public sealed class BattleFieldInfoView : MonoBehaviour
    {
        private const float CardWidth = 132f;
        private const float CardHeight = 30f;

        private RectTransform _enemyLane;
        private RectTransform _globalLane;
        private RectTransform _allyLane;
        private readonly List<FieldCardBinding> _enemyCards = new();
        private readonly List<FieldCardBinding> _globalCards = new();
        private readonly List<FieldCardBinding> _allyCards = new();
        private Action<BattleFieldEffectInstance> _detailsRequested;
        private Action<BattleWeatherInstance> _weatherDetailsRequested;

        private sealed class FieldCardBinding
        {
            public FieldCardBinding(GameObject root, Image background, Button button, TMP_Text label)
            {
                Root = root;
                Background = background;
                Button = button;
                Label = label;
            }

            public GameObject Root { get; }
            public Image Background { get; }
            public Button Button { get; }
            public TMP_Text Label { get; }
        }

        public void Initialize(
            Action<BattleFieldEffectInstance> detailsRequested,
            Action<BattleWeatherInstance> weatherDetailsRequested)
        {
            _detailsRequested = detailsRequested;
            _weatherDetailsRequested = weatherDetailsRequested;
            EnsureLayout();
        }

        public void Render(
            IReadOnlyList<BattleFieldEffectInstance> effects,
            IReadOnlyList<BattleWeatherInstance> weather)
        {
            EnsureLayout();
            DeactivateCards(_enemyCards);
            DeactivateCards(_globalCards);
            DeactivateCards(_allyCards);

            var enemyIndex = 0;
            var globalIndex = 0;
            var allyIndex = 0;

            foreach (var effect in effects ?? Array.Empty<BattleFieldEffectInstance>())
            {
                if (effect.EffectId == BattleFieldEffectId.FrozenGround)
                {
                    BindEffectCard(
                        GetOrCreateCard(_globalLane, _globalCards, globalIndex++),
                        effect);
                }
                else if (effect.TargetSide == BattleSide.Player)
                {
                    BindEffectCard(
                        GetOrCreateCard(_allyLane, _allyCards, allyIndex++),
                        effect);
                }
                else
                {
                    BindEffectCard(
                        GetOrCreateCard(_enemyLane, _enemyCards, enemyIndex++),
                        effect);
                }
            }

            foreach (var item in weather ?? Array.Empty<BattleWeatherInstance>())
            {
                BindWeatherCard(
                    GetOrCreateCard(_globalLane, _globalCards, globalIndex++),
                    item);
            }
        }

        private void BindWeatherCard(
            FieldCardBinding binding,
            BattleWeatherInstance weather)
        {
            var color = GetWeatherAccentColor(
                weather.WeatherId,
                weather.IsSnow ? -weather.Value : weather.Value);
            var valueLabel = weather.WeatherId == BattleWeatherId.Temperature
                ? weather.Value.ToString("+#;-#;0")
                : Math.Abs(weather.Value).ToString();
            PrepareCard(
                binding,
                $"{weather.WeatherId}WeatherCard",
                color,
                $"{weather.DisplayName} {valueLabel}");
            binding.Button.onClick.AddListener(
                () => _weatherDetailsRequested?.Invoke(weather));
        }

        private void EnsureLayout()
        {
            if (_enemyLane != null)
            {
                return;
            }

            if (transform is not RectTransform)
            {
                throw new InvalidOperationException(
                    $"{nameof(BattleFieldInfoView)} requires a RectTransform.");
            }

            var layout = GetComponent<VerticalLayoutGroup>()
                ?? gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 0, 0);
            layout.spacing = 0f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            _enemyLane = CreateLane("EnemyFieldLane", TextAnchor.MiddleRight);
            _globalLane = CreateLane("GlobalFieldLane", TextAnchor.MiddleCenter);
            _allyLane = CreateLane("AllyFieldLane", TextAnchor.MiddleLeft);
        }

        private RectTransform CreateLane(string objectName, TextAnchor alignment)
        {
            var scrollObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(ScrollRect),
                typeof(LayoutElement));
            scrollObject.layer = gameObject.layer;
            scrollObject.transform.SetParent(transform, false);

            var laneElement = scrollObject.GetComponent<LayoutElement>();
            laneElement.minHeight = 0f;
            laneElement.preferredHeight = 0f;
            laneElement.flexibleHeight = 1f;

            var viewportObject = new GameObject(
                "Viewport",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(RectMask2D));
            viewportObject.layer = gameObject.layer;
            viewportObject.transform.SetParent(scrollObject.transform, false);
            var viewport = viewportObject.GetComponent<RectTransform>();
            Stretch(viewport);
            viewportObject.GetComponent<Image>().color =
                new Color(0f, 0f, 0f, 0.001f);

            var laneObject = new GameObject(
                "Content",
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup),
                typeof(ContentSizeFitter));
            laneObject.layer = gameObject.layer;
            laneObject.transform.SetParent(viewport, false);
            var lane = laneObject.GetComponent<RectTransform>();
            var anchor = alignment switch
            {
                TextAnchor.MiddleRight => 1f,
                TextAnchor.MiddleCenter => 0.5f,
                _ => 0f,
            };
            lane.anchorMin = new Vector2(anchor, 0f);
            lane.anchorMax = new Vector2(anchor, 1f);
            lane.pivot = new Vector2(anchor, 0.5f);
            lane.sizeDelta = Vector2.zero;

            var layout = laneObject.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 0, 0);
            layout.spacing = 5f;
            layout.childAlignment = alignment;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            var fitter = laneObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            var scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = lane;
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = true;
            scroll.scrollSensitivity = CardWidth * 0.5f;
            return lane;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void BindEffectCard(
            FieldCardBinding binding,
            BattleFieldEffectInstance effect)
        {
            var statusLabel = effect.Statuses.Count == 0
                ? string.Empty
                : "  [" + string.Join(
                    " / ",
                    effect.Statuses.Select(status => status.DisplayName)) + "]";
            var label = effect.EffectId == BattleFieldEffectId.FireBarrier
                ? $"{effect.DisplayName} {effect.Value}{statusLabel}"
                : effect.EffectId == BattleFieldEffectId.IceBlade
                    ? $"{effect.DisplayName} [{effect.RemainingTicks}]"
                    : $"{effect.DisplayName} {effect.Value}";
            PrepareCard(
                binding,
                $"{effect.EffectId}Card",
                GetAccentColor(effect.EffectId),
                label);
            binding.Button.onClick.AddListener(
                () => _detailsRequested?.Invoke(effect));
        }

        private FieldCardBinding GetOrCreateCard(
            RectTransform lane,
            List<FieldCardBinding> cards,
            int index)
        {
            while (cards.Count <= index)
            {
                cards.Add(CreateCardObject(lane, $"FieldCard_{cards.Count + 1}"));
            }

            return cards[index];
        }

        private FieldCardBinding CreateCardObject(
            RectTransform lane,
            string objectName)
        {
            var cardObject = new GameObject(
                objectName,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(Button), typeof(Outline), typeof(LayoutElement));
            cardObject.layer = gameObject.layer;
            cardObject.transform.SetParent(lane, false);
            var background = cardObject.GetComponent<Image>();
            var outline = cardObject.GetComponent<Outline>();
            outline.effectColor = GameUiPalette.Border;
            outline.effectDistance = new Vector2(1f, -1f);

            var element = cardObject.GetComponent<LayoutElement>();
            element.preferredWidth = CardWidth;
            element.preferredHeight = CardHeight;
            element.minWidth = CardWidth;
            // The three field lanes own their height. Cards must not increase
            // the parent field area's minimum height when they appear.
            element.minHeight = 0f;

            var label = CreateLabel(cardObject.transform);
            var button = cardObject.GetComponent<Button>();
            button.targetGraphic = background;
            return new FieldCardBinding(cardObject, background, button, label);
        }

        private void PrepareCard(
            FieldCardBinding binding,
            string objectName,
            Color color,
            string text)
        {
            binding.Root.name = objectName;
            binding.Root.SetActive(true);
            binding.Background.color = color;
            binding.Label.text = text;
            binding.Label.color = AttributeCardPalette.GetReadableTextColor(color);
            binding.Button.onClick.RemoveAllListeners();
        }

        private TMP_Text CreateLabel(Transform parent)
        {
            var labelObject = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            labelObject.layer = gameObject.layer;
            labelObject.transform.SetParent(parent, false);
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(5f, 1f);
            labelRect.offsetMax = new Vector2(-5f, -1f);

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.color = GameUiPalette.PrimaryText;
            label.fontSize = 16f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 10f;
            label.fontSizeMax = 16f;
            label.raycastTarget = false;
            return label;
        }

        private static void DeactivateCards(IReadOnlyList<FieldCardBinding> cards)
        {
            foreach (var binding in cards)
            {
                binding.Root.SetActive(false);
                binding.Button.onClick.RemoveAllListeners();
            }
        }

        public static string GetDisplayName(BattleFieldEffectId effectId)
        {
            return effectId switch
            {
                BattleFieldEffectId.Smog => "スモッグ",
                BattleFieldEffectId.FrozenGround => "氷の大地",
                BattleFieldEffectId.IceBlade => "氷の刃",
                _ => effectId.ToString(),
            };
        }

        public static Color GetAccentColor(BattleFieldEffectId effectId)
        {
            return effectId switch
            {
                BattleFieldEffectId.Smog =>
                    RewardElementPalette.GetAttributeColor(PachimonAttribute.Poison),
                BattleFieldEffectId.FireBarrier =>
                    RewardElementPalette.GetAttributeColor(PachimonAttribute.Fire),
                BattleFieldEffectId.FrozenGround =>
                    RewardElementPalette.GetAttributeColor(PachimonAttribute.Ice),
                BattleFieldEffectId.IceBlade =>
                    RewardElementPalette.GetAttributeColor(PachimonAttribute.Ice),
                BattleFieldEffectId.WaterVeil =>
                    RewardElementPalette.GetAttributeColor(PachimonAttribute.Aqua),
                BattleFieldEffectId.BeatVine =>
                    RewardElementPalette.GetAttributeColor(PachimonAttribute.Leaf),
                BattleFieldEffectId.FireVine =>
                    RewardElementPalette.GetAttributeColor(PachimonAttribute.Fire),
                BattleFieldEffectId.PoisonMist =>
                    RewardElementPalette.GetAttributeColor(PachimonAttribute.Poison),
                _ => GameUiPalette.StatusChip,
            };
        }

        public static Color GetWeatherAccentColor(
            BattleWeatherId weatherId,
            int value = 0)
        {
            return weatherId switch
            {
                BattleWeatherId.Temperature when value < 0 =>
                    RewardElementPalette.GetAttributeColor(PachimonAttribute.Ice),
                BattleWeatherId.Temperature =>
                    RewardElementPalette.GetAttributeColor(PachimonAttribute.Fire),
                BattleWeatherId.Rain when value < 0 =>
                    RewardElementPalette.GetAttributeColor(PachimonAttribute.Ice),
                BattleWeatherId.Rain =>
                    RewardElementPalette.GetAttributeColor(PachimonAttribute.Aqua),
                BattleWeatherId.Thunder =>
                    RewardElementPalette.GetAttributeColor(PachimonAttribute.Electric),
                BattleWeatherId.Wind =>
                    RewardElementPalette.GetAttributeColor(PachimonAttribute.Wind),
                BattleWeatherId.Moisture when value < 0 =>
                    RewardElementPalette.GetAttributeColor(PachimonAttribute.Fire),
                BattleWeatherId.Moisture =>
                    RewardElementPalette.GetAttributeColor(PachimonAttribute.Aqua),
                BattleWeatherId.Plasma when value < 0 =>
                    RewardElementPalette.GetAttributeColor(PachimonAttribute.Leaf),
                BattleWeatherId.Plasma =>
                    RewardElementPalette.GetAttributeColor(PachimonAttribute.Electric),
                _ => GameUiPalette.StatusChip,
            };
        }

    }
}
