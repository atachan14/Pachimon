using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Pachimon.Battle;
using Pachimon.Data;
using Pachimon.Items;

namespace Pachimon.UI
{
    [Serializable]
    public sealed class BattleUnitSlotView
    {
        private static readonly Color HealthyHpColor =
            new(0.25f, 0.67f, 0.34f, 1f);
        private static readonly Color WarningHpColor =
            new(0.86f, 0.66f, 0.18f, 1f);
        private static readonly Color CriticalHpColor =
            new(0.78f, 0.22f, 0.20f, 1f);
        private static readonly Color EmptyHpColor =
            new(0.32f, 0.32f, 0.32f, 1f);
        private static readonly Color HpTrackColor =
            new(0.12f, 0.15f, 0.16f, 1f);
        private static readonly Color MnColor =
            new(0.20f, 0.52f, 0.86f, 1f);
        private static readonly Color PreviewColor =
            new(0.62f, 0.62f, 0.62f, 0.88f);
        private const string PreviewDamageColorHex = "#E84B3C";
        private const string PreviewRecoveryColorHex = "#2D75C7";

        [SerializeField] private RectTransform _infoRoot;
        [SerializeField] private RectTransform _graphicRoot;

        private RectTransform _hpBarRoot;
        private TMP_Text _nameText;
        private Image _hpFill;
        private Image _hpPreview;
        private TMP_Text _hpValueText;
        private Image _mnFill;
        private Image _mnPreview;
        private TMP_Text _mnValueText;
        private Image _graphic;
        private RectTransform _interactionRoot;
        private BattleUnitState _renderedUnit;

        public void Render(
            BattleUnitState unit,
            string emptyLabel,
            PachimonCatalog pachimonCatalog,
            bool useBackSprite)
        {
            EnsureResourceBars();
            EnsureGraphic();
            RenderResources(unit, emptyLabel);
            RenderGraphic(unit, pachimonCatalog, useBackSprite);
        }

        public void ConfigureItemDrop(Func<ItemInstance, bool> tryUse)
        {
            EnsureInteractionRoot();
            _interactionRoot?.GetComponent<ItemDropTargetView>()?.Configure(tryUse);
        }

        public void ConfigureClick(Action onClicked)
        {
            EnsureInteractionRoot();
            if (_interactionRoot == null)
            {
                return;
            }

            var button = _interactionRoot.GetComponent<Button>()
                ?? _interactionRoot.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = _interactionRoot.GetComponent<Image>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClicked?.Invoke());
        }

        public void ShowResourcePreview(
            BattleUnitState unit,
            int hpDelta,
            int mnDelta)
        {
            EnsureResourceBars();
            if (unit == null)
            {
                ClearResourcePreview();
                return;
            }

            SetPreviewSegment(
                _hpPreview,
                unit.CurrentHp,
                unit.MaxHp,
                hpDelta);
            SetPreviewSegment(
                _mnPreview,
                unit.CurrentMn,
                unit.MaxMn,
                mnDelta);
            RenderResourceValueTexts(unit, hpDelta, mnDelta);
        }

        public void ClearResourcePreview()
        {
            SetPreviewSegment(_hpPreview, 0, 0, 0);
            SetPreviewSegment(_mnPreview, 0, 0, 0);
            if (_renderedUnit != null)
            {
                RenderResourceValueTexts(_renderedUnit, 0, 0);
            }
        }

        private void EnsureResourceBars()
        {
            if (_hpBarRoot != null || _infoRoot == null)
            {
                return;
            }

            _hpBarRoot = _infoRoot.Find("RuntimeBattleHpBar") as RectTransform;
            if (_hpBarRoot == null)
            {
                var rootObject = new GameObject(
                    "RuntimeBattleHpBar",
                    typeof(RectTransform),
                    typeof(LayoutElement));
                rootObject.layer = _infoRoot.gameObject.layer;
                _hpBarRoot = rootObject.GetComponent<RectTransform>();
                _hpBarRoot.SetParent(_infoRoot, false);
            }

            var layout = _hpBarRoot.GetComponent<LayoutElement>()
                ?? _hpBarRoot.gameObject.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            Stretch(_hpBarRoot);

            _nameText = GetOrCreateLabel(
                _hpBarRoot,
                "Name",
                TextAlignmentOptions.Bottom,
                16f);
            _nameText.fontStyle = FontStyles.Bold;
            SetAnchors(
                _nameText.rectTransform,
                new Vector2(0f, 0.66f),
                new Vector2(1f, 0.98f),
                new Vector2(4f, 0f),
                new Vector2(-4f, 0f));

            var track = GetOrCreateImage(_hpBarRoot, "Track", HpTrackColor);
            SetAnchors(
                track.rectTransform,
                new Vector2(0.04f, 0.35f),
                new Vector2(0.96f, 0.65f),
                Vector2.zero,
                Vector2.zero);

            _hpFill = GetOrCreateImage(
                track.rectTransform,
                "Fill",
                HealthyHpColor);
            Stretch(_hpFill.rectTransform);

            _hpPreview = GetOrCreateImage(
                track.rectTransform,
                "Preview",
                PreviewColor);
            _hpPreview.enabled = false;

            _hpValueText = GetOrCreateLabel(
                track.rectTransform,
                "Value",
                TextAlignmentOptions.Center,
                15f);
            _hpValueText.color = Color.white;
            _hpValueText.fontStyle = FontStyles.Bold;
            _hpValueText.overrideColorTags = false;
            Stretch(_hpValueText.rectTransform);

            var mnTrack = GetOrCreateImage(_hpBarRoot, "MnTrack", HpTrackColor);
            SetAnchors(
                mnTrack.rectTransform,
                new Vector2(0.04f, 0.03f),
                new Vector2(0.96f, 0.32f),
                Vector2.zero,
                Vector2.zero);

            _mnFill = GetOrCreateImage(
                mnTrack.rectTransform,
                "Fill",
                MnColor);
            Stretch(_mnFill.rectTransform);

            _mnPreview = GetOrCreateImage(
                mnTrack.rectTransform,
                "Preview",
                PreviewColor);
            _mnPreview.enabled = false;

            _mnValueText = GetOrCreateLabel(
                mnTrack.rectTransform,
                "Value",
                TextAlignmentOptions.Center,
                15f);
            _mnValueText.color = Color.white;
            _mnValueText.fontStyle = FontStyles.Bold;
            _mnValueText.overrideColorTags = false;
            Stretch(_mnValueText.rectTransform);
            _hpValueText.transform.SetAsLastSibling();
            _mnValueText.transform.SetAsLastSibling();
        }

        private void EnsureGraphic()
        {
            if (_graphic != null || _graphicRoot == null)
            {
                return;
            }

            _graphic = _graphicRoot
                .Find("RuntimePachimonGraphic")
                ?.GetComponent<Image>();
            if (_graphic == null)
            {
                var graphicObject = new GameObject(
                    "RuntimePachimonGraphic",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                graphicObject.layer = _graphicRoot.gameObject.layer;
                graphicObject.transform.SetParent(_graphicRoot, false);
                _graphic = graphicObject.GetComponent<Image>();
            }

            _graphic.preserveAspect = true;
            _graphic.raycastTarget = false;
            Stretch(
                _graphic.rectTransform,
                new Vector2(3f, 3f),
                new Vector2(-3f, -3f));
        }

        private void EnsureInteractionRoot()
        {
            if (_interactionRoot != null || _graphicRoot == null)
            {
                return;
            }

            _interactionRoot = _graphicRoot.Find("ItemDropTarget") as RectTransform;
            if (_interactionRoot == null)
            {
                var interactionObject = new GameObject(
                    "ItemDropTarget",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(ItemDropTargetView));
                interactionObject.layer = _graphicRoot.gameObject.layer;
                _interactionRoot = interactionObject.GetComponent<RectTransform>();
                _interactionRoot.SetParent(_graphicRoot, false);
            }
            else if (_interactionRoot.GetComponent<ItemDropTargetView>() == null)
            {
                _interactionRoot.gameObject.AddComponent<ItemDropTargetView>();
            }

            Stretch(_interactionRoot);
            var image = _interactionRoot.GetComponent<Image>()
                ?? _interactionRoot.gameObject.AddComponent<Image>();
            image.color = GameUiPalette.Transparent;
            image.raycastTarget = true;
            _interactionRoot.SetAsLastSibling();
        }

        private void RenderResources(BattleUnitState unit, string emptyLabel)
        {
            if (_nameText == null
                || _hpValueText == null
                || _hpFill == null
                || _mnValueText == null
                || _mnFill == null)
            {
                return;
            }

            _renderedUnit = unit;
            if (unit == null)
            {
                _nameText.text = emptyLabel;
                _nameText.color = EmptyHpColor;
                _hpValueText.text = "---";
                _mnValueText.text = "---";
                SetHpFill(0f, EmptyHpColor);
                SetMnFill(0f);
                return;
            }

            var ratio = unit.MaxHp > 0
                ? Mathf.Clamp01((float)unit.CurrentHp / unit.MaxHp)
                : 0f;
            _nameText.text = unit.IsDefeated
                ? $"{unit.DisplayName}  DOWN"
                : unit.DisplayName;
            _nameText.color = unit.IsDefeated ? EmptyHpColor : Color.black;
            RenderResourceValueTexts(unit, 0, 0);
            SetHpFill(ratio, GetHpColor(ratio, unit.IsDefeated));
            var mnRatio = unit.MaxMn > 0
                ? Mathf.Clamp01((float)unit.CurrentMn / unit.MaxMn)
                : 0f;
            SetMnFill(mnRatio);
        }

        private void RenderResourceValueTexts(
            BattleUnitState unit,
            int hpDelta,
            int mnDelta)
        {
            if (unit == null || _hpValueText == null || _mnValueText == null)
            {
                return;
            }

            _hpValueText.text =
                $"HP  {unit.CurrentHp}{FormatPreviewDelta(hpDelta)} / {unit.MaxHp}";
            _mnValueText.text =
                $"MN  {unit.CurrentMn}{FormatPreviewDelta(mnDelta)} / {unit.MaxMn}";
        }

        private static string FormatPreviewDelta(int delta)
        {
            if (delta == 0)
            {
                return string.Empty;
            }

            var color = delta < 0
                ? PreviewDamageColorHex
                : PreviewRecoveryColorHex;
            var sign = delta > 0 ? "+" : string.Empty;
            return $" <color={color}>{sign}{delta}</color>";
        }

        private void RenderGraphic(
            BattleUnitState unit,
            PachimonCatalog pachimonCatalog,
            bool useBackSprite)
        {
            if (_graphic == null)
            {
                return;
            }

            var definition = unit == null
                ? null
                : pachimonCatalog?.Get(unit.SpeciesId);
            _graphic.sprite = useBackSprite
                ? definition?.BackSprite
                : definition?.FrontSprite;
            _graphic.enabled = _graphic.sprite != null;
            _graphic.color = unit != null && unit.IsAlive
                ? Color.white
                : new Color(0.42f, 0.42f, 0.42f, 0.7f);
        }

        private void SetHpFill(float ratio, Color color)
        {
            _hpFill.color = color;
            _hpFill.rectTransform.anchorMax = new Vector2(
                Mathf.Clamp01(ratio),
                1f);
        }

        private void SetMnFill(float ratio)
        {
            _mnFill.color = ratio <= 0f ? EmptyHpColor : MnColor;
            _mnFill.rectTransform.anchorMax = new Vector2(
                Mathf.Clamp01(ratio),
                1f);
        }

        private static void SetPreviewSegment(
            Image preview,
            int currentValue,
            int maxValue,
            int delta)
        {
            if (preview == null || maxValue <= 0 || delta == 0)
            {
                if (preview != null)
                {
                    preview.enabled = false;
                }

                return;
            }

            var predictedValue = Math.Max(
                0,
                Math.Min(maxValue, currentValue + delta));
            var currentRatio = Mathf.Clamp01((float)currentValue / maxValue);
            var predictedRatio = Mathf.Clamp01((float)predictedValue / maxValue);
            if (Mathf.Approximately(currentRatio, predictedRatio))
            {
                preview.enabled = false;
                return;
            }

            preview.enabled = true;
            preview.color = PreviewColor;
            preview.rectTransform.anchorMin =
                new Vector2(Mathf.Min(currentRatio, predictedRatio), 0f);
            preview.rectTransform.anchorMax =
                new Vector2(Mathf.Max(currentRatio, predictedRatio), 1f);
            preview.rectTransform.offsetMin = Vector2.zero;
            preview.rectTransform.offsetMax = Vector2.zero;
        }

        private static Color GetHpColor(float ratio, bool isDefeated)
        {
            if (isDefeated || ratio <= 0f)
            {
                return EmptyHpColor;
            }

            if (ratio <= 0.25f)
            {
                return CriticalHpColor;
            }

            return ratio <= 0.5f ? WarningHpColor : HealthyHpColor;
        }

        private static Image GetOrCreateImage(
            RectTransform parent,
            string objectName,
            Color color)
        {
            var image = parent.Find(objectName)?.GetComponent<Image>();
            if (image == null)
            {
                var imageObject = new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                imageObject.layer = parent.gameObject.layer;
                imageObject.transform.SetParent(parent, false);
                image = imageObject.GetComponent<Image>();
            }

            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI GetOrCreateLabel(
            RectTransform parent,
            string objectName,
            TextAlignmentOptions alignment,
            float fontSize)
        {
            var label = parent.Find(objectName)?.GetComponent<TextMeshProUGUI>();
            if (label == null)
            {
                var labelObject = new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI));
                labelObject.layer = parent.gameObject.layer;
                labelObject.transform.SetParent(parent, false);
                label = labelObject.GetComponent<TextMeshProUGUI>();
            }

            if (TMP_Settings.defaultFontAsset != null)
            {
                label.font = TMP_Settings.defaultFontAsset;
            }

            label.alignment = alignment;
            label.fontSize = fontSize;
            label.color = Color.black;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            return label;
        }

        private static void Stretch(
            RectTransform rect,
            Vector2? offsetMin = null,
            Vector2? offsetMax = null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin ?? Vector2.zero;
            rect.offsetMax = offsetMax ?? Vector2.zero;
        }

        private static void SetAnchors(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
