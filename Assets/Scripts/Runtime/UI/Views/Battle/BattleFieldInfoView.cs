using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Battle;
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
        private Action<BattleFieldEffectInstance> _detailsRequested;
        private Action<BattleWeatherInstance> _weatherDetailsRequested;

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
            ClearLane(_enemyLane);
            ClearLane(_globalLane);
            ClearLane(_allyLane);

            foreach (var effect in effects ?? Array.Empty<BattleFieldEffectInstance>())
            {
                var lane = effect.EffectId == BattleFieldEffectId.FrozenGround
                    ? _globalLane
                    : effect.TargetSide == BattleSide.Player
                        ? _allyLane
                        : _enemyLane;
                CreateCard(lane, effect);
            }

            foreach (var item in weather ?? Array.Empty<BattleWeatherInstance>())
            {
                CreateWeatherCard(_globalLane, item);
            }
        }

        private void CreateWeatherCard(
            RectTransform lane,
            BattleWeatherInstance weather)
        {
            var cardObject = CreateCardObject(
                lane,
                $"{weather.WeatherId}WeatherCard",
                GetWeatherAccentColor(
                    weather.WeatherId,
                    weather.IsSnow ? -weather.Value : weather.Value));
            var valueLabel = weather.WeatherId == BattleWeatherId.Temperature
                ? weather.Value.ToString("+#;-#;0")
                : weather.Value.ToString();
            CreateLabel(cardObject.transform, $"{weather.DisplayName} {valueLabel}");
            cardObject.GetComponent<Button>().onClick.AddListener(
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
            layout.padding = new RectOffset(6, 6, 2, 2);
            layout.spacing = 2f;
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
            var laneObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup),
                typeof(LayoutElement));
            laneObject.layer = gameObject.layer;
            laneObject.transform.SetParent(transform, false);

            var layout = laneObject.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 0, 0);
            layout.spacing = 5f;
            layout.childAlignment = alignment;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            laneObject.GetComponent<LayoutElement>().flexibleHeight = 1f;
            return laneObject.GetComponent<RectTransform>();
        }

        private void CreateCard(RectTransform lane, BattleFieldEffectInstance effect)
        {
            var cardObject = CreateCardObject(
                lane,
                $"{effect.EffectId}Card",
                GetAccentColor(effect.EffectId));
            CreateLabel(
                cardObject.transform,
                effect.EffectId == BattleFieldEffectId.FireBarrier
                    ? $"{effect.DisplayName} {effect.CurrentHp}/{effect.MaxHp}"
                    : effect.EffectId == BattleFieldEffectId.IceBlade
                        ? $"{effect.DisplayName} [{effect.RemainingTicks}]"
                    : $"{effect.DisplayName} {effect.Value}");
            cardObject.GetComponent<Button>().onClick.AddListener(
                () => _detailsRequested?.Invoke(effect));
        }

        private GameObject CreateCardObject(
            RectTransform lane,
            string objectName,
            Color color)
        {
            var cardObject = new GameObject(
                objectName,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(Button), typeof(Outline), typeof(LayoutElement));
            cardObject.layer = gameObject.layer;
            cardObject.transform.SetParent(lane, false);
            cardObject.GetComponent<Image>().color = color;
            var outline = cardObject.GetComponent<Outline>();
            outline.effectColor = GameUiPalette.Border;
            outline.effectDistance = new Vector2(1f, -1f);

            var element = cardObject.GetComponent<LayoutElement>();
            element.preferredWidth = CardWidth;
            element.preferredHeight = CardHeight;
            element.minWidth = CardWidth;
            element.minHeight = CardHeight;

            return cardObject;
        }

        private void CreateLabel(Transform parent, string text)
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
            label.text = text;
            label.color = GameUiPalette.OnAccentText;
            label.fontSize = 16f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 10f;
            label.fontSizeMax = 16f;
            label.raycastTarget = false;
        }

        private static void ClearLane(RectTransform lane)
        {
            if (lane == null)
            {
                return;
            }

            foreach (Transform child in lane.Cast<Transform>().ToArray())
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
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
                BattleFieldEffectId.Smog => new Color32(0x77, 0x56, 0x8A, 0xFF),
                BattleFieldEffectId.FireBarrier =>
                    new Color32(0xC9, 0x4F, 0x3D, 0xFF),
                BattleFieldEffectId.FrozenGround =>
                    new Color32(0x6E, 0xB9, 0xD7, 0xFF),
                BattleFieldEffectId.IceBlade =>
                    new Color32(0x88, 0xCE, 0xE8, 0xFF),
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
                    new Color32(0x55, 0xA6, 0xC8, 0xFF),
                BattleWeatherId.Temperature =>
                    new Color32(0xE8, 0xA8, 0x35, 0xFF),
                BattleWeatherId.Rain when value < 0 =>
                    new Color32(0x88, 0xC8, 0xE0, 0xFF),
                BattleWeatherId.Rain =>
                    new Color32(0x4E, 0x8E, 0xC7, 0xFF),
                _ => GameUiPalette.StatusChip,
            };
        }
    }
}
