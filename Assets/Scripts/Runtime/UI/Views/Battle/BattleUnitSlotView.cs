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
        private static readonly Color ShieldColor =
            new(0.58f, 0.61f, 0.64f, 1f);
        private static readonly Color MnColor =
            new(0.20f, 0.52f, 0.86f, 1f);
        private static readonly Color PreviewColor =
            new(0.62f, 0.62f, 0.62f, 0.88f);
        private static readonly Color ToxinPreviewColor =
            new(0.56f, 0.24f, 0.72f, 0.94f);
        private static readonly Color InitialElapsedColor =
            new(0.94f, 0.49f, 0.12f, 1f);
        private static readonly Color InitialRemainingColor =
            new(0.08f, 0.08f, 0.08f, 1f);
        private static readonly Color StartupElapsedColor =
            new(0.96f, 0.78f, 0.18f, 1f);
        private static readonly Color StartupRemainingColor =
            new(0.48f, 0.24f, 0.72f, 1f);
        private static readonly Color RecoveryElapsedColor =
            new(0.94f, 0.49f, 0.12f, 1f);
        private static readonly Color RecoveryRemainingColor =
            new(0.96f, 0.78f, 0.18f, 1f);
        private static readonly Color TurnColor =
            new(0.84f, 0.18f, 0.18f, 1f);
        private static readonly Color ActionLockElapsedColor =
            new(0.28f, 0.72f, 0.92f, 1f);
        private static readonly Color ActionLockRemainingColor =
            new(0.10f, 0.28f, 0.52f, 1f);
        [SerializeField] private RectTransform _infoRoot;
        [SerializeField] private RectTransform _graphicRoot;

        private RectTransform _gaugeRoot;
        private TMP_Text _nameText;
        private Image _hpFill;
        private Image _hpShield;
        private Image _hpPreview;
        private TMP_Text _hpValueText;
        private ResourceGaugeView _hpGaugeView;
        private Image _mnFill;
        private Image _mnPreview;
        private TMP_Text _mnValueText;
        private ResourceGaugeView _mnGaugeView;
        private Image _actionElapsed;
        private Image _actionRemaining;
        private TMP_Text _actionValueText;
        private ActionGaugeView _actionGaugeView;
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
                unit.MaxHp + unit.TotalShield,
                hpDelta);
            SetPreviewSegment(
                _mnPreview,
                unit.CurrentMn,
                unit.MaxMn,
                mnDelta);
            _hpGaugeView?.SetPreviewDelta(hpDelta);
            _mnGaugeView?.SetPreviewDelta(mnDelta);
        }

        public void ClearResourcePreview()
        {
            SetPreviewSegment(_hpPreview, 0, 0, 0);
            SetPreviewSegment(_mnPreview, 0, 0, 0);
            _hpGaugeView?.SetPreviewDelta(0);
            _mnGaugeView?.SetPreviewDelta(0);
            if (_renderedUnit != null)
            {
                RenderActionGauge(_renderedUnit);
            }
        }

        public void PresentResourceSnapshot(
            BattleUnitState unit,
            int currentHp,
            int currentMn)
        {
            EnsureResourceBars();
            if (unit == null || _hpGaugeView == null || _mnGaugeView == null)
            {
                return;
            }

            _renderedUnit = unit;
            var safeHp = Mathf.Clamp(currentHp, 0, unit.MaxHp);
            var hpRatio = unit.MaxHp > 0
                ? Mathf.Clamp01((float)safeHp / unit.MaxHp)
                : 0f;
            var isDefeated = safeHp == 0;
            _nameText.text = isDefeated
                ? $"{unit.DisplayName}  DOWN"
                : unit.DisplayName;
            _nameText.color = isDefeated ? EmptyHpColor : Color.black;
            _hpGaugeView.Present(
                unit.InstanceId,
                safeHp,
                unit.MaxHp,
                GetHpColor(hpRatio, isDefeated),
                unit.TotalShield);

            var safeMn = Mathf.Clamp(currentMn, 0, unit.MaxMn);
            var mnRatio = unit.MaxMn > 0
                ? Mathf.Clamp01((float)safeMn / unit.MaxMn)
                : 0f;
            _mnGaugeView.Present(
                unit.InstanceId,
                safeMn,
                unit.MaxMn,
                mnRatio <= 0f ? EmptyHpColor : MnColor);
        }

        public void ShowPendingToxinDamage(
            BattleUnitState unit,
            int hpBefore,
            int hpAfter,
            int currentMn)
        {
            if (unit == null)
            {
                return;
            }

            PresentResourceSnapshot(unit, hpBefore, currentMn);
            SetPreviewSegment(
                _hpPreview,
                hpBefore,
                unit.MaxHp + unit.TotalShield,
                hpAfter - hpBefore,
                ToxinPreviewColor);
        }

        public void CommitToxinDamage(
            BattleUnitState unit,
            int hpAfter,
            int currentMn)
        {
            if (_hpPreview != null)
            {
                _hpPreview.enabled = false;
            }

            PresentResourceSnapshot(unit, hpAfter, currentMn);
        }

        private void EnsureResourceBars()
        {
            if (_gaugeRoot != null || _infoRoot == null)
            {
                return;
            }

            _gaugeRoot = _infoRoot.Find("RuntimeBattleGauges") as RectTransform;
            if (_gaugeRoot == null)
            {
                var legacyRoot =
                    _infoRoot.Find("RuntimeBattleHpBar") as RectTransform;
                if (legacyRoot != null)
                {
                    legacyRoot.name = "RuntimeBattleGauges";
                    _gaugeRoot = legacyRoot;
                }
            }

            if (_gaugeRoot == null)
            {
                var rootObject = new GameObject(
                    "RuntimeBattleGauges",
                    typeof(RectTransform),
                    typeof(LayoutElement));
                rootObject.layer = _infoRoot.gameObject.layer;
                _gaugeRoot = rootObject.GetComponent<RectTransform>();
                _gaugeRoot.SetParent(_infoRoot, false);
            }

            var layout = _gaugeRoot.GetComponent<LayoutElement>()
                ?? _gaugeRoot.gameObject.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            Stretch(_gaugeRoot);

            _nameText = GetOrCreateLabel(
                _gaugeRoot,
                "Name",
                TextAlignmentOptions.Bottom,
                16f);
            _nameText.fontStyle = FontStyles.Bold;
            SetAnchors(
                _nameText.rectTransform,
                new Vector2(0f, 0.77f),
                new Vector2(1f, 0.98f),
                new Vector2(4f, 0f),
                new Vector2(-4f, 0f));

            var track = GetOrCreateImage(_gaugeRoot, "HpGauge", HpTrackColor);
            SetAnchors(
                track.rectTransform,
                new Vector2(0.04f, 0.53f),
                new Vector2(0.96f, 0.75f),
                Vector2.zero,
                Vector2.zero);

            _hpFill = GetOrCreateImage(
                track.rectTransform,
                "Fill",
                HealthyHpColor);
            Stretch(_hpFill.rectTransform);

            _hpShield = GetOrCreateImage(
                track.rectTransform,
                "Shield",
                ShieldColor);
            _hpShield.enabled = false;

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
            _hpGaugeView = track.GetComponent<ResourceGaugeView>()
                ?? track.gameObject.AddComponent<ResourceGaugeView>();
            _hpGaugeView.Configure(
                "HP",
                _hpFill,
                _hpValueText,
                _hpShield);

            var mnTrack = GetOrCreateImage(_gaugeRoot, "MnGauge", HpTrackColor);
            SetAnchors(
                mnTrack.rectTransform,
                new Vector2(0.04f, 0.28f),
                new Vector2(0.96f, 0.50f),
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
            _mnGaugeView = mnTrack.GetComponent<ResourceGaugeView>()
                ?? mnTrack.gameObject.AddComponent<ResourceGaugeView>();
            _mnGaugeView.Configure("MN", _mnFill, _mnValueText);

            var actionTrack = GetOrCreateImage(
                _gaugeRoot,
                "ActionGauge",
                HpTrackColor);
            SetAnchors(
                actionTrack.rectTransform,
                new Vector2(0.04f, 0.10f),
                new Vector2(0.72f, 0.17f),
                Vector2.zero,
                Vector2.zero);

            _actionElapsed = GetOrCreateActionSegment(
                actionTrack.rectTransform,
                "Elapsed",
                "Fill",
                InitialElapsedColor);
            _actionRemaining = GetOrCreateActionSegment(
                actionTrack.rectTransform,
                "Remaining",
                "Preview",
                InitialRemainingColor);

            _actionValueText = GetOrCreateLabel(
                _gaugeRoot,
                "ActionGaugeValue",
                TextAlignmentOptions.Left,
                13f);
            _actionValueText.color = InitialElapsedColor;
            _actionValueText.fontStyle = FontStyles.Bold;
            SetAnchors(
                _actionValueText.rectTransform,
                new Vector2(0.75f, 0.03f),
                new Vector2(0.99f, 0.24f),
                Vector2.zero,
                Vector2.zero);
            _hpValueText.transform.SetAsLastSibling();
            _mnValueText.transform.SetAsLastSibling();
            _actionValueText.transform.SetAsLastSibling();
            _actionGaugeView = actionTrack.GetComponent<ActionGaugeView>()
                ?? actionTrack.gameObject.AddComponent<ActionGaugeView>();
            _actionGaugeView.Configure(
                _actionElapsed,
                _actionRemaining,
                _actionValueText);
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
                _hpGaugeView?.Clear();
                _mnGaugeView?.Clear();
                RenderActionGauge(null);
                return;
            }

            PresentResourceSnapshot(unit, unit.CurrentHp, unit.CurrentMn);
            RenderActionGauge(unit);
        }

        private void RenderActionGauge(BattleUnitState unit)
        {
            if (_actionElapsed == null
                || _actionRemaining == null
                || _actionValueText == null)
            {
                return;
            }

            if (unit == null || unit.IsDefeated)
            {
                PresentActionGauge(
                    BattleActionPhase.Defeated,
                    0f,
                    0,
                    0,
                    EmptyHpColor,
                    InitialRemainingColor,
                    EmptyHpColor,
                    unit == null ? "---" : "DOWN",
                    showRemaining: true);
                return;
            }

            var timing = unit.Timing;
            var ratio = Mathf.Clamp01(timing.Progress);
            if (timing.Phase == BattleActionPhase.Ready)
            {
                PresentActionGauge(
                    timing.Phase,
                    1f,
                    0,
                    0,
                    TurnColor,
                    TurnColor,
                    TurnColor,
                    "Turn",
                    showRemaining: false);
                return;
            }

            var actionLock = unit.GetStatus(BattleStatusId.FrozenBreakSelf);
            if (actionLock?.RemainingTicks is int lockRemaining
                && actionLock.RuntimeData is FrozenBreakRuntimeState lockRuntime)
            {
                var lockTotal = lockRuntime.TotalDurationTicks;
                var lockRatio = lockTotal > 0
                    ? Mathf.Clamp01(1f - (float)lockRemaining / lockTotal)
                    : 1f;
                PresentActionGauge(
                    timing.Phase,
                    lockRatio,
                    lockTotal,
                    lockRemaining,
                    ActionLockElapsedColor,
                    ActionLockRemainingColor,
                    ActionLockElapsedColor,
                    $"対象外 {lockRemaining}",
                    showRemaining: true,
                    useValueText: true);
                return;
            }

            var elapsedColor = InitialElapsedColor;
            var remainingColor = InitialRemainingColor;
            var valueColor = InitialElapsedColor;
            if (timing.Phase == BattleActionPhase.Startup)
            {
                elapsedColor = StartupElapsedColor;
                remainingColor = StartupRemainingColor;
                valueColor = StartupElapsedColor;
            }
            else if (timing.Phase == BattleActionPhase.Recovery)
            {
                elapsedColor = RecoveryElapsedColor;
                remainingColor = RecoveryRemainingColor;
                valueColor = RecoveryElapsedColor;
            }

            if (timing.IsPaused)
            {
                elapsedColor = PreviewColor;
                remainingColor = EmptyHpColor;
                valueColor = PreviewColor;
            }

            var remainingTicks = timing.IsPaused
                ? timing.RemainingTicks
                : unit.GetActionRemainingTicks();
            PresentActionGauge(
                timing.Phase,
                ratio,
                timing.TotalTicks,
                remainingTicks,
                elapsedColor,
                remainingColor,
                valueColor,
                timing.IsPaused
                ? $"{timing.RemainingTicks} 停止"
                : remainingTicks.ToString(),
                showRemaining: true);
        }

        private void PresentActionGauge(
            BattleActionPhase phase,
            float ratio,
            int totalTicks,
            int remainingTicks,
            Color elapsedColor,
            Color remainingColor,
            Color valueColor,
            string valueText,
            bool showRemaining,
            bool useValueText = false)
        {
            _actionGaugeView?.Present(
                phase,
                ratio,
                totalTicks,
                remainingTicks,
                elapsedColor,
                remainingColor,
                valueColor,
                valueText,
                showRemaining,
                useValueText);
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

        private static void SetPreviewSegment(
            Image preview,
            int currentValue,
            int maxValue,
            int delta,
            Color? segmentColor = null)
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
            preview.color = segmentColor ?? PreviewColor;
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

        private static Image GetOrCreateActionSegment(
            RectTransform parent,
            string objectName,
            string legacyObjectName,
            Color color)
        {
            var image = parent.Find(objectName)?.GetComponent<Image>();
            if (image == null)
            {
                image = parent.Find(legacyObjectName)?.GetComponent<Image>();
                if (image != null)
                {
                    image.name = objectName;
                }
            }

            return image ?? GetOrCreateImage(parent, objectName, color);
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
