using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Pachimon.App;
using Pachimon.Battle;
using Pachimon.Data;
using Pachimon.Items;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;
using Pachimon.Passives;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public sealed class GameRootView : MonoBehaviour
    {
        [field: SerializeField] public HeaderView HeaderView { get; private set; }
        [field: SerializeField] public LeftPaneView LeftPaneView { get; private set; }
        [field: SerializeField] public MainPaneView MainPaneView { get; private set; }
        [field: SerializeField] public RightPaneView RightPaneView { get; private set; }
        [field: SerializeField] public MapOverlayView MapOverlayView { get; private set; }
        public ItemPanelView ItemPanelView { get; private set; }
        [field: SerializeField] public LayoutMode LayoutMode { get; private set; }

        [SerializeField, Min(0f)] private float _drawerTransitionDuration = 0.25f;
        [SerializeField, Min(0f)] private float _sceneFadeDuration = 0.75f;
        [SerializeField, Min(1f)] private float _compactTextScale = 1.5f;
        [SerializeField, Min(0.05f)] private float _typographyScanInterval = 0.25f;

        private RectTransform _headerRect;
        private RectTransform _contentRect;
        private RectTransform _bodyRect;
        private RectTransform _leftPaneRect;
        private RectTransform _mainPaneRect;
        private RectTransform _rightPaneRect;
        private RectTransform _overlayLayer;
        private RectTransform _leftDrawerViewport;
        private RectTransform _rightDrawerViewport;
        private RectTransform _mapViewport;
        private RectTransform _itemPanelViewport;
        private RectTransform _contentDetailViewport;
        private ContentDetailOverlayView _contentDetailOverlayView;
        private ItemCatalog _itemCatalog;
        private SkillCatalog _skillCatalog;
        private PassiveCatalog _passiveCatalog;
        private CanvasGroup _leftDrawerCanvasGroup;
        private CanvasGroup _rightDrawerCanvasGroup;
        private HorizontalLayoutGroup _contentLayout;
        private Coroutine _drawerRoutine;
        private Coroutine _sceneFadeRoutine;
        private CanvasGroup _sceneFadeCanvasGroup;
        private readonly List<TMP_Text> _typographyTexts = new();
        private readonly List<PachimonTabView> _abilityDetailTabs = new();
        private CompactPane _compactPane = CompactPane.Main;
        private float _leftDrawerProgress;
        private float _rightDrawerProgress;
        private float _compactBreakpoint;
        private float _nextTypographyScanTime;
        private bool _isInitialized;

        public CompactPane CurrentCompactPane => _compactPane;

        public void Initialize(
            HeaderView headerView,
            LeftPaneView leftPaneView,
            MainPaneView mainPaneView,
            RightPaneView rightPaneView,
            MapOverlayView mapOverlayView,
            RectTransform headerRect,
            RectTransform contentRect,
            RectTransform leftPaneRect,
            RectTransform mainPaneRect,
            RectTransform rightPaneRect,
            float compactBreakpoint)
        {
            HeaderView = headerView;
            LeftPaneView = leftPaneView;
            MainPaneView = mainPaneView;
            RightPaneView = rightPaneView;
            MapOverlayView = mapOverlayView;
            _headerRect = headerRect;
            _contentRect = contentRect;
            _bodyRect = contentRect != null ? contentRect.parent as RectTransform : null;
            _leftPaneRect = leftPaneRect;
            _mainPaneRect = mainPaneRect;
            _rightPaneRect = rightPaneRect;
            _compactBreakpoint = compactBreakpoint;

            LogMissingRuntimeReferences();
            InitializeResponsiveHierarchy();
            WireResponsiveEvents();
            _isInitialized = true;
            ApplyLayoutMode(GetRecommendedLayoutMode());
        }

        private void Update()
        {
            if (!_isInitialized)
            {
                return;
            }

            var recommendedMode = GetRecommendedLayoutMode();
            if (recommendedMode != LayoutMode)
            {
                ApplyLayoutMode(recommendedMode);
            }

            if (Time.unscaledTime >= _nextTypographyScanTime)
            {
                RefreshTypographyScale();
                _nextTypographyScanTime = Time.unscaledTime + _typographyScanInterval;
            }
        }

        private void LateUpdate()
        {
            if (!_isInitialized)
            {
                return;
            }

            if (LayoutMode == LayoutMode.Compact)
            {
                RefreshCompactPaneGeometry();
            }

            RefreshMapViewportGeometry();
            RefreshItemPanelGeometry();
            RefreshContentDetailGeometry();
        }

        private void OnDestroy()
        {
            UnwireAbilityDetailTabs();
            if (RightPaneView == null)
            {
                return;
            }

            RightPaneView.ContentShown -= HandleRightPaneContentShown;
            RightPaneView.ContentCleared -= HandleRightPaneContentCleared;
            RightPaneView.MainPaneRequested -= HandleMainPaneRequested;
            if (MapOverlayView != null)
            {
                MapOverlayView.Opening -= HandleMapOpening;
            }

            if (ItemPanelView != null)
            {
                ItemPanelView.DetailsRequested -= HandleItemDetailsRequested;
            }
        }

        public void ToggleMapOverlay()
        {
            if (MapOverlayView == null)
            {
                Debug.LogWarning(
                    $"{nameof(GameRootView)} on '{name}' cannot toggle map because {nameof(MapOverlayView)} is missing.",
                    this);
                return;
            }

            if (!MapOverlayView.IsOpen)
            {
                MapOverlayView.Open();
            }
            else if (IsOverlayTop(_mapViewport))
            {
                MapOverlayView.Close();
            }
            else
            {
                BringOverlayToFront(_mapViewport);
                MapOverlayView.ReplayOpenTransition();
            }
        }

        public void BindItemPanel(ItemInventory inventory, ItemCatalog itemCatalog)
        {
            _itemCatalog = itemCatalog;
            ItemPanelView?.Bind(inventory, itemCatalog);
        }

        public void BindAbilityDetails(
            SkillCatalog skillCatalog,
            PassiveCatalog passiveCatalog)
        {
            _skillCatalog = skillCatalog;
            _passiveCatalog = passiveCatalog;
            WireAbilityDetailTabs();
        }

        public void RefreshItemPanel(bool hideDetails = false)
        {
            ItemPanelView?.Refresh();
            if (hideDetails
                && _contentDetailOverlayView != null
                && _contentDetailOverlayView.ShownKind == ContentDetailKind.Item)
            {
                _contentDetailOverlayView.Close();
            }
        }

        public void ShowItemDetails(int itemId)
        {
            var item = _itemCatalog?.Get(itemId);
            if (item == null || _contentDetailOverlayView == null)
            {
                return;
            }

            BringOverlayToFront(_contentDetailViewport);
            _contentDetailOverlayView.Show(CreateItemDetail(item));
        }

        public void ShowFieldEffectDetails(BattleFieldEffectInstance effect)
        {
            if (effect == null || _contentDetailOverlayView == null)
            {
                return;
            }

            BringOverlayToFront(_contentDetailViewport);
            _contentDetailOverlayView.Show(CreateFieldEffectDetail(effect));
        }

        public void ShowWeatherDetails(BattleWeatherInstance weather)
        {
            if (weather == null || _contentDetailOverlayView == null)
            {
                return;
            }

            BringOverlayToFront(_contentDetailViewport);
            _contentDetailOverlayView.Show(CreateWeatherDetail(weather));
        }

        public void ToggleItemPanel()
        {
            if (ItemPanelView == null || _itemPanelViewport == null)
            {
                Debug.LogWarning(
                    $"{nameof(GameRootView)} on '{name}' has no Item Panel.",
                    this);
                return;
            }

            if (!ItemPanelView.IsOpen)
            {
                BringOverlayToFront(_itemPanelViewport);
                ItemPanelView.Open();
            }
            else if (IsOverlayTop(_itemPanelViewport))
            {
                ItemPanelView.Close();
            }
            else
            {
                BringOverlayToFront(_itemPanelViewport);
                ItemPanelView.ReplayOpenTransition();
            }
        }

        public void ToggleLeftPane()
        {
            ToggleCompactPane(CompactPane.Left);
        }

        public void ToggleRightPane()
        {
            ToggleCompactPane(CompactPane.Right);
        }

        public void FadeToTitleScene()
        {
            if (_sceneFadeRoutine != null)
            {
                return;
            }

            _sceneFadeRoutine = StartCoroutine(FadeToTitleSceneRoutine());
        }

        public void ShowCompactPane(CompactPane pane, bool animate = true)
        {
            var wasAlreadyOpen = _compactPane == pane && pane != CompactPane.Main;
            var targetViewport = GetDrawerViewport(pane);
            if (wasAlreadyOpen && IsOverlayTop(targetViewport))
            {
                return;
            }

            _compactPane = pane;
            HeaderView?.SetCompactPaneSelection(pane);

            if (LayoutMode != LayoutMode.Compact)
            {
                return;
            }

            if (targetViewport != null)
            {
                BringOverlayToFront(targetViewport);
                if (wasAlreadyOpen && animate)
                {
                    if (pane == CompactPane.Left)
                    {
                        ApplyDrawerProgress(0f, _rightDrawerProgress);
                    }
                    else
                    {
                        ApplyDrawerProgress(_leftDrawerProgress, 0f);
                    }
                }
            }

            var targetLeft = pane == CompactPane.Left ? 1f : 0f;
            var targetRight = pane == CompactPane.Right ? 1f : 0f;
            if (_drawerRoutine != null)
            {
                StopCoroutine(_drawerRoutine);
                _drawerRoutine = null;
            }

            if (!animate || _drawerTransitionDuration <= 0f || !isActiveAndEnabled)
            {
                ApplyDrawerProgress(targetLeft, targetRight);
                return;
            }

            _drawerRoutine = StartCoroutine(AnimateDrawers(targetLeft, targetRight));
        }

        public LayoutMode GetRecommendedLayoutMode()
        {
            if (Screen.height > Screen.width)
            {
                return LayoutMode.Compact;
            }

            var width = _bodyRect != null && _bodyRect.rect.width > 0f
                ? _bodyRect.rect.width
                : Screen.width;
            return width < _compactBreakpoint ? LayoutMode.Compact : LayoutMode.Expanded;
        }

        public void ApplyLayoutMode(LayoutMode layoutMode)
        {
            LayoutMode = layoutMode;

            if (_mainPaneRect == null
                || _leftPaneRect == null
                || _rightPaneRect == null
                || _contentRect == null
                || _bodyRect == null)
            {
                Debug.LogWarning(
                    $"{nameof(GameRootView)} on '{name}' is missing layout rect references.",
                    this);
                return;
            }

            if (_drawerRoutine != null)
            {
                StopCoroutine(_drawerRoutine);
                _drawerRoutine = null;
            }

            if (layoutMode == LayoutMode.Compact)
            {
                ApplyCompactLayout();
            }
            else
            {
                ApplyExpandedLayout();
            }

            if (_headerRect != null)
            {
                _headerRect.sizeDelta = new Vector2(
                    0f,
                    layoutMode == LayoutMode.Compact ? 110f : 96f);
            }

            HeaderView?.SetCompactPaneButtonsVisible(layoutMode == LayoutMode.Compact);
            HeaderView?.SetCompactPaneSelection(_compactPane);
            RightPaneView?.ApplyLayoutMode(layoutMode);
            ItemPanelView?.ApplyLayoutMode(layoutMode);
            RefreshTypographyScale();
            Canvas.ForceUpdateCanvases();
            RefreshMapViewportGeometry();
            RefreshItemPanelGeometry();
        }

        private void RefreshTypographyScale()
        {
            _typographyTexts.Clear();
            GetComponentsInChildren(true, _typographyTexts);

            var scale = LayoutMode == LayoutMode.Compact ? _compactTextScale : 1f;
            foreach (var text in _typographyTexts)
            {
                if (text == null)
                {
                    continue;
                }

                var typographySize = text.GetComponent<ResponsiveTypographySize>();
                if (typographySize == null)
                {
                    typographySize = text.gameObject.AddComponent<ResponsiveTypographySize>();
                }

                typographySize.Apply(text, scale);
            }
        }

        private void InitializeResponsiveHierarchy()
        {
            if (_bodyRect == null)
            {
                return;
            }

            _contentLayout = _contentRect.GetComponent<HorizontalLayoutGroup>();
            if (_mainPaneRect.GetComponent<RectMask2D>() == null)
            {
                _mainPaneRect.gameObject.AddComponent<RectMask2D>();
            }
            _overlayLayer = CreateLayer("OverlayLayer", _bodyRect);
            if (_overlayLayer.GetComponent<RectMask2D>() == null)
            {
                _overlayLayer.gameObject.AddComponent<RectMask2D>();
            }
            _leftDrawerViewport = CreateDrawerViewport(
                "LeftDrawerViewport",
                _overlayLayer,
                out _leftDrawerCanvasGroup);
            _rightDrawerViewport = CreateDrawerViewport(
                "RightDrawerViewport",
                _overlayLayer,
                out _rightDrawerCanvasGroup);

            _mapViewport = MapOverlayView != null
                ? MapOverlayView.transform.parent as RectTransform
                : null;
            if (_mapViewport != null)
            {
                _mapViewport.SetParent(_overlayLayer, false);
                _mapViewport.SetAsLastSibling();
            }

            _itemPanelViewport = CreateLayer("ItemPanelViewport", _overlayLayer);
            ItemPanelView = ItemPanelView.CreateRuntime(_itemPanelViewport);
            ItemPanelView.DetailsRequested += HandleItemDetailsRequested;
            ItemPanelView.Close();
            _contentDetailViewport = CreateLayer(
                "ContentDetailViewport",
                _overlayLayer);
            _contentDetailOverlayView =
                ContentDetailOverlayView.CreateRuntime(_contentDetailViewport);
            _contentDetailOverlayView.Close();

            _leftDrawerViewport.gameObject.SetActive(false);
            _rightDrawerViewport.gameObject.SetActive(false);
        }

        private void WireResponsiveEvents()
        {
            HeaderView?.ConfigureCompactPaneButtons(ToggleLeftPane, ToggleRightPane);
            if (RightPaneView == null)
            {
                return;
            }

            RightPaneView.ContentShown -= HandleRightPaneContentShown;
            RightPaneView.ContentCleared -= HandleRightPaneContentCleared;
            RightPaneView.MainPaneRequested -= HandleMainPaneRequested;
            RightPaneView.ContentShown += HandleRightPaneContentShown;
            RightPaneView.ContentCleared += HandleRightPaneContentCleared;
            RightPaneView.MainPaneRequested += HandleMainPaneRequested;
            if (MapOverlayView != null)
            {
                MapOverlayView.Opening -= HandleMapOpening;
                MapOverlayView.Opening += HandleMapOpening;
            }
        }

        private void ApplyCompactLayout()
        {
            _leftDrawerViewport.gameObject.SetActive(true);
            _rightDrawerViewport.gameObject.SetActive(true);
            _leftPaneRect.gameObject.SetActive(true);
            _rightPaneRect.gameObject.SetActive(true);

            _leftPaneRect.SetParent(_leftDrawerViewport, false);
            _rightPaneRect.SetParent(_rightDrawerViewport, false);
            _mainPaneRect.SetParent(_contentRect, false);
            _mainPaneRect.SetSiblingIndex(0);

            SetStretch(_contentRect);
            if (_contentLayout != null)
            {
                _contentLayout.enabled = true;
            }

            RefreshCompactPaneGeometry();
            ApplyDrawerProgress(
                _compactPane == CompactPane.Left ? 1f : 0f,
                _compactPane == CompactPane.Right ? 1f : 0f);
        }

        private void ApplyExpandedLayout()
        {
            _leftPaneRect.gameObject.SetActive(true);
            _rightPaneRect.gameObject.SetActive(true);
            _leftPaneRect.SetParent(_contentRect, false);
            _mainPaneRect.SetParent(_contentRect, false);
            _rightPaneRect.SetParent(_contentRect, false);
            _leftPaneRect.SetSiblingIndex(0);
            _mainPaneRect.SetSiblingIndex(1);
            _rightPaneRect.SetSiblingIndex(2);

            SetStretch(_contentRect);
            if (_contentLayout != null)
            {
                _contentLayout.enabled = true;
            }

            _leftDrawerViewport.gameObject.SetActive(false);
            _rightDrawerViewport.gameObject.SetActive(false);
            _leftDrawerProgress = 0f;
            _rightDrawerProgress = 0f;
        }

        private IEnumerator AnimateDrawers(float targetLeft, float targetRight)
        {
            var startLeft = _leftDrawerProgress;
            var startRight = _rightDrawerProgress;
            var elapsed = 0f;
            while (elapsed < _drawerTransitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / _drawerTransitionDuration);
                var eased = progress * progress * (3f - (2f * progress));
                ApplyDrawerProgress(
                    Mathf.Lerp(startLeft, targetLeft, eased),
                    Mathf.Lerp(startRight, targetRight, eased));
                yield return null;
            }

            ApplyDrawerProgress(targetLeft, targetRight);
            _drawerRoutine = null;
        }

        private IEnumerator FadeToTitleSceneRoutine()
        {
            var canvasGroup = EnsureSceneFadeOverlay();
            canvasGroup.gameObject.SetActive(true);
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.transform.SetAsLastSibling();

            if (_sceneFadeDuration > 0f)
            {
                var elapsed = 0f;
                while (elapsed < _sceneFadeDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    canvasGroup.alpha = Mathf.Clamp01(elapsed / _sceneFadeDuration);
                    yield return null;
                }
            }

            canvasGroup.alpha = 1f;
            SceneLoader.LoadTitleScene();
        }

        private CanvasGroup EnsureSceneFadeOverlay()
        {
            if (_sceneFadeCanvasGroup != null)
            {
                return _sceneFadeCanvasGroup;
            }

            var fadeObject = new GameObject(
                "SceneFadeOverlay",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup),
                typeof(Image),
                typeof(GraphicRaycaster),
                typeof(LayoutElement));
            fadeObject.layer = gameObject.layer;

            var fadeRect = fadeObject.GetComponent<RectTransform>();
            fadeRect.SetParent(transform, false);
            SetStretch(fadeRect);

            var canvas = fadeObject.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = short.MaxValue;

            var image = fadeObject.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = true;

            fadeObject.GetComponent<LayoutElement>().ignoreLayout = true;
            _sceneFadeCanvasGroup = fadeObject.GetComponent<CanvasGroup>();
            return _sceneFadeCanvasGroup;
        }

        private void ApplyDrawerProgress(float leftProgress, float rightProgress)
        {
            _leftDrawerProgress = Mathf.Clamp01(leftProgress);
            _rightDrawerProgress = Mathf.Clamp01(rightProgress);

            _leftDrawerViewport.anchorMin = Vector2.zero;
            _leftDrawerViewport.anchorMax = new Vector2(_leftDrawerProgress, 1f);
            _leftDrawerViewport.offsetMin = Vector2.zero;
            _leftDrawerViewport.offsetMax = Vector2.zero;

            _rightDrawerViewport.anchorMin = new Vector2(1f - _rightDrawerProgress, 0f);
            _rightDrawerViewport.anchorMax = Vector2.one;
            _rightDrawerViewport.offsetMin = Vector2.zero;
            _rightDrawerViewport.offsetMax = Vector2.zero;

            SetDrawerInteraction(_leftDrawerCanvasGroup, _leftDrawerProgress >= 0.999f);
            SetDrawerInteraction(_rightDrawerCanvasGroup, _rightDrawerProgress >= 0.999f);
        }

        private void RefreshCompactPaneGeometry()
        {
            if (_bodyRect == null || _leftDrawerViewport == null || _rightDrawerViewport == null)
            {
                return;
            }

            var width = Mathf.Max(1f, _bodyRect.rect.width);
            ConfigureDrawerPane(_leftPaneRect, width, true);
            ConfigureDrawerPane(_rightPaneRect, width, false);
        }

        private void RefreshMapViewportGeometry()
        {
            if (_mapViewport == null || _overlayLayer == null)
            {
                return;
            }

            if (LayoutMode == LayoutMode.Compact)
            {
                SetStretch(_mapViewport);
                return;
            }

            var corners = new Vector3[4];
            _mainPaneRect.GetWorldCorners(corners);
            var bottomLeft = _overlayLayer.InverseTransformPoint(corners[0]);
            var topRight = _overlayLayer.InverseTransformPoint(corners[2]);
            var center = (bottomLeft + topRight) * 0.5f;
            var size = topRight - bottomLeft;

            _mapViewport.anchorMin = new Vector2(0.5f, 0.5f);
            _mapViewport.anchorMax = new Vector2(0.5f, 0.5f);
            _mapViewport.pivot = new Vector2(0.5f, 0.5f);
            _mapViewport.anchoredPosition = center;
            _mapViewport.sizeDelta = new Vector2(size.x, size.y);
        }

        private void RefreshItemPanelGeometry()
        {
            if (_itemPanelViewport == null
                || _overlayLayer == null
                || MainPaneView?.LogWindowView == null)
            {
                return;
            }

            var logRect = MainPaneView.LogWindowView.transform as RectTransform;
            if (logRect == null)
            {
                return;
            }

            var corners = new Vector3[4];
            logRect.GetWorldCorners(corners);
            var bottomLeft = _overlayLayer.InverseTransformPoint(corners[0]);
            var topRight = _overlayLayer.InverseTransformPoint(corners[2]);
            var center = (bottomLeft + topRight) * 0.5f;
            var size = topRight - bottomLeft;

            _itemPanelViewport.anchorMin = new Vector2(0.5f, 0.5f);
            _itemPanelViewport.anchorMax = new Vector2(0.5f, 0.5f);
            _itemPanelViewport.pivot = new Vector2(0.5f, 0.5f);
            _itemPanelViewport.anchoredPosition = center;
            _itemPanelViewport.sizeDelta = new Vector2(
                Mathf.Max(1f, size.x),
                Mathf.Max(1f, size.y));
            ItemPanelView?.SetSlideDistance(
                Mathf.Max(_bodyRect?.rect.height ?? 0f, size.y));
        }

        private void RefreshContentDetailGeometry()
        {
            if (_contentDetailViewport == null
                || _overlayLayer == null
                || _mainPaneRect == null)
            {
                return;
            }

            if (LayoutMode == LayoutMode.Compact)
            {
                SetStretch(_contentDetailViewport);
            }
            else
            {
                var corners = new Vector3[4];
                _mainPaneRect.GetWorldCorners(corners);
                var bottomLeft = _overlayLayer.InverseTransformPoint(corners[0]);
                var topRight = _overlayLayer.InverseTransformPoint(corners[2]);
                var center = (bottomLeft + topRight) * 0.5f;
                var size = topRight - bottomLeft;

                _contentDetailViewport.anchorMin = new Vector2(0.5f, 0.5f);
                _contentDetailViewport.anchorMax = new Vector2(0.5f, 0.5f);
                _contentDetailViewport.pivot = new Vector2(0.5f, 0.5f);
                _contentDetailViewport.anchoredPosition = center;
                _contentDetailViewport.sizeDelta = new Vector2(
                    Mathf.Max(1f, size.x),
                    Mathf.Max(1f, size.y));
            }

            _contentDetailOverlayView?.SetSlideDistance(
                Mathf.Max(
                    _bodyRect?.rect.height ?? 0f,
                    _contentDetailViewport.rect.height));
        }

        private void WireAbilityDetailTabs()
        {
            UnwireAbilityDetailTabs();
            if (_leftPaneRect == null || _rightPaneRect == null)
            {
                return;
            }

            var tabs = new List<PachimonTabView>();
            tabs.AddRange(_leftPaneRect.GetComponentsInChildren<PachimonTabView>(true));
            tabs.AddRange(_rightPaneRect.GetComponentsInChildren<PachimonTabView>(true));
            foreach (var tab in tabs)
            {
                if (tab == null || _abilityDetailTabs.Contains(tab))
                {
                    continue;
                }

                tab.AbilityDetailsRequested += HandleAbilityDetailsRequested;
                _abilityDetailTabs.Add(tab);
            }
        }

        private void UnwireAbilityDetailTabs()
        {
            foreach (var tab in _abilityDetailTabs)
            {
                if (tab != null)
                {
                    tab.AbilityDetailsRequested -= HandleAbilityDetailsRequested;
                }
            }

            _abilityDetailTabs.Clear();
        }

        private void HandleAbilityDetailsRequested(
            PachimonAbilityPreview ability,
            PachimonPreviewContent owner)
        {
            var content = ability.Kind == PachimonAbilityKind.Skill
                ? CreateSkillDetail(ability, owner)
                : CreatePassiveDetail(ability, owner);
            if (content == null || _contentDetailOverlayView == null)
            {
                return;
            }

            BringOverlayToFront(_contentDetailViewport);
            _contentDetailOverlayView.Show(content);
        }

        private ContentDetailOverlayContent CreateSkillDetail(
            PachimonAbilityPreview ability,
            PachimonPreviewContent owner)
        {
            var skill = _skillCatalog?.Get(ability.Id);
            if (skill == null)
            {
                return new ContentDetailOverlayContent(
                    ContentDetailKind.Skill,
                    ability.DisplayName,
                    $"ID  {ability.Id}",
                    "詳細データが見つかりません。",
                    GameUiPalette.SkillChip);
            }

            var timing = skill.BaseStartupTicks > 0
                ? $"発生  {skill.BaseStartupTicks}    硬直  {skill.BaseRecoveryTicks}"
                : $"硬直  {skill.BaseRecoveryTicks}";
            return new ContentDetailOverlayContent(
                ContentDetailKind.Skill,
                skill.DisplayName,
                $"{timing}    CD  {skill.BaseCooldownTicks}"
                + $"    MN  {skill.BaseManaCost}",
                SkillDetailDescriptionFormatter.Format(skill, owner),
                GameUiPalette.SkillChip);
        }

        private ContentDetailOverlayContent CreatePassiveDetail(
            PachimonAbilityPreview ability,
            PachimonPreviewContent owner)
        {
            var description = "説明未設定";
            if (_passiveCatalog?.Get(ability.Id)
                is DerivedAdditivePassiveAsset statDefinition)
            {
                description = CreateDerivedPassiveDescription(
                    statDefinition,
                    owner?.StatCalculation);
            }
            else if (_passiveCatalog?.Get(ability.Id)
                is FieldValueAmplificationPassiveAsset fieldDefinition)
            {
                var currentMultiplier = owner != null
                    && owner.TryGetStat(
                        PachimonDisplayStat.Poison,
                        out var poison)
                        ? SignedStatMath.AmplificationMultiplier(
                            poison
                            * fieldDefinition.PoisonScalingPercent
                            / 100m)
                        : (decimal?)null;
                var currentText = currentMultiplier.HasValue
                    ? $"現在の増幅率は{currentMultiplier.Value:0.##}倍。"
                    : string.Empty;
                description = "自身が生成物を生成するとき、"
                    + "生成予定ValueをPoisonに応じて増幅する。"
                    + currentText;
            }
            else if (_passiveCatalog?.Get(ability.Id)
                is ToxinGrowthPassiveAsset toxinGrowthDefinition)
            {
                description = "自身が毒素を付与するたび、Battle中のPoisonが"
                    + $"{toxinGrowthDefinition.PoisonPercentPerApplication}%増加する。"
                    + "複数回発動した増加率は加算してから適用する。";
            }
            else if (_passiveCatalog?.Get(ability.Id)
                is PoisonKnightPassiveAsset poisonKnightDefinition)
            {
                decimal? currentSharePercent = null;
                if (owner != null
                    && owner.TryGetStat(PachimonDisplayStat.Poison, out var poison))
                {
                    currentSharePercent = SignedStatMath.ScaleFromBase(
                        poisonKnightDefinition.BaseSharePercent,
                        poison,
                        poisonKnightDefinition.PoisonScalingPercent);
                }

                var currentText = currentSharePercent.HasValue
                    ? $"現在の共有率は{currentSharePercent.Value:0.##}%。"
                    : string.Empty;
                description = "自身が受けたShieldと実際のHP回復量の一部を、"
                    + "生存中の他の味方全員にも与える。"
                    + currentText;
            }
            else if (_passiveCatalog?.Get(ability.Id)
                is FireGrowthOnDamagePassiveAsset fireGrowthDefinition)
            {
                description = "Damageを受けるたび、Battle中のFireが"
                    + $"{fireGrowthDefinition.FireIncreasePerDamage}増加する。"
                    + "HPとShieldのどちらへ適用されたDamageでも発動する。";
            }
            else if (_passiveCatalog?.Get(ability.Id)
                is DarkFlamePassiveAsset darkFlameDefinition)
            {
                decimal? currentConversionPercent = null;
                if (owner != null
                    && owner.TryGetStat(PachimonDisplayStat.Poison, out var poison))
                {
                    currentConversionPercent =
                        darkFlameDefinition.BaseConversionPercent
                        * SignedStatMath.AmplificationMultiplier(
                            poison
                            * darkFlameDefinition.PoisonScalingPercent
                            / 100m);
                }

                var currentText = currentConversionPercent.HasValue
                    ? $"現在の変換率は{currentConversionPercent.Value:0.##}%。"
                    : string.Empty;
                description = "Fire Damageを与えたとき、その軽減前Valueを基に"
                    + "同じ対象へ追加Poison Damageを与える。"
                    + currentText;
            }
            else if (_passiveCatalog?.Get(ability.Id)
                is FireArcherPassiveAsset fireArcherDefinition)
            {
                decimal? currentMissingHpPercent = null;
                if (owner != null
                    && owner.TryGetStat(PachimonDisplayStat.Fire, out var fire))
                {
                    currentMissingHpPercent =
                        fireArcherDefinition.MissingHpPercent
                        * SignedStatMath.AmplificationMultiplier(
                            fire
                            * fireArcherDefinition.FireScalingPercent
                            / 100m);
                }

                var currentText = currentMissingHpPercent.HasValue
                    ? "現在は対象の減少HPの"
                      + $"{currentMissingHpPercent.Value:0.##}%をBaseDamageにする。"
                    : string.Empty;
                description = "Skill Damageを与えたとき、対象の減少HPとFireに"
                    + "応じた追加Fire Damageを同じ対象へ与える。"
                    + currentText;
            }
            else if (_passiveCatalog?.Get(ability.Id)
                is ComboMasterPassiveAsset comboMasterDefinition)
            {
                description = "Battle中に完了した最大追加連鎖回数1回につき、"
                    + "DamageBonusが"
                    + $"{comboMasterDefinition.DamageBonusPerChain}増加する。";
            }
            else if (PassiveLogicRegistry.TryGetPlaceholderAttribute(
                    ability.Id,
                    _passiveCatalog,
                    out var attribute))
            {
                var attributeLabel = GetAttributeLabel(attribute);
                var allocationType = (AllocationType)((int)attribute + 1);
                var icon = AttributeRichText.GetIcon(allocationType);
                description =
                    $"与える{icon}{attributeLabel}ダメージが"
                    + $"{OutgoingAttributeDamagePassiveLogic.DamagePercent - 100}%増加する。";
            }

            return new ContentDetailOverlayContent(
                ContentDetailKind.Passive,
                ability.DisplayName,
                string.Empty,
                description,
                GameUiPalette.PassiveChip);
        }

        private static string CreateDerivedPassiveDescription(
            DerivedAdditivePassiveAsset definition,
            StatCalculationResult calculation)
        {
            var referenceLabel = GetStatLabel(definition.ReferenceStat);
            var targetLabel = GetStatLabel(definition.TargetStat);
            var contribution = calculation?
                .GetContributions(definition.TargetStat)
                .FirstOrDefault(item =>
                    item.Source.SourceId == $"passive:{definition.PassiveId}")?
                .Value;
            var actualValue = contribution.HasValue
                ? $"現在の加算値は{contribution.Value:0.##}。"
                : string.Empty;
            return $"{referenceLabel}の{definition.Percent}%を"
                + $"{targetLabel}へ加算する。{actualValue}";
        }

        private static string GetStatLabel(PachimonStatType statType)
        {
            if (PachimonStatTypeUtility.TryGetAttribute(statType, out var attribute))
            {
                var allocationType = (AllocationType)((int)attribute + 1);
                return AttributeRichText.GetIcon(allocationType)
                    + GetAttributeLabel(attribute);
            }

            return statType.ToString();
        }

        private static ContentDetailOverlayContent CreateItemDetail(ItemAsset item)
        {
            var category = item.Category switch
            {
                ItemCategory.Pharmacy => "薬局",
                ItemCategory.Other => "その他",
                ItemCategory.SkillMachine => "技マシーン",
                _ => "未分類",
            };
            return new ContentDetailOverlayContent(
                ContentDetailKind.Item,
                item.DisplayName,
                $"カテゴリ  {category}    基準価格  {item.BasePrice} Gold",
                item.Description,
                GameUiPalette.ItemChip);
        }

        private static ContentDetailOverlayContent CreateFieldEffectDetail(
            BattleFieldEffectInstance effect)
        {
            var side = effect.TargetSide == BattleSide.Player
                ? "自陣生成物"
                : "敵陣生成物";
            var description = !string.IsNullOrWhiteSpace(effect.Description)
                ? effect.Description
                : effect.EffectId switch
            {
                BattleFieldEffectId.Smog =>
                    "毎tick、現在Valueの1%を対象陣営の生存パチモン全員へ"
                    + "毒素として付与する。\n"
                    + "毎tick、現在Valueの1%ずつ減衰する。",
                _ => "説明未設定",
            };
            var runtimeValues = effect.EffectId
                    == BattleFieldEffectId.FireBarrier
                ? $"Value  {effect.Value}    HP  {effect.CurrentHp}/{effect.MaxHp}"
                    + $"    残り  {effect.RemainingTicks}tick"
                : $"Value  {effect.Value}";
            return new ContentDetailOverlayContent(
                ContentDetailKind.FieldEffect,
                effect.DisplayName,
                $"{side}    {runtimeValues}    生成者  {effect.Source.DisplayName}",
                description,
                BattleFieldInfoView.GetAccentColor(effect.EffectId));
        }

        private static ContentDetailOverlayContent CreateWeatherDetail(
            BattleWeatherInstance weather)
        {
            var runtimeValues = weather.WeatherId == BattleWeatherId.Temperature
                ? $"気温  {weather.Value:+#;-#;0}"
                : $"Value  {weather.Value}";
            return new ContentDetailOverlayContent(
                ContentDetailKind.FieldEffect,
                weather.DisplayName,
                $"全体環境    {runtimeValues}    最終変更者  {weather.Source.DisplayName}",
                string.IsNullOrWhiteSpace(weather.Description)
                    ? "説明未設定"
                    : weather.Description,
                BattleFieldInfoView.GetWeatherAccentColor(
                    weather.WeatherId,
                    weather.IsSnow ? -weather.Value : weather.Value));
        }

        private static string GetAllocationTypeLabel(AllocationType type)
        {
            return type switch
            {
                AllocationType.Fire => "炎",
                AllocationType.Aqua => "水",
                AllocationType.Leaf => "草",
                AllocationType.Electric => "電気",
                AllocationType.Poison => "毒",
                AllocationType.Ice => "氷",
                AllocationType.Wind => "風",
                AllocationType.Dragon => "竜",
                _ => "なし",
            };
        }

        private static string GetAttributeLabel(PachimonAttribute attribute)
        {
            return GetAllocationTypeLabel(
                (AllocationType)((int)attribute + 1));
        }

        private void HandleRightPaneContentShown()
        {
            WireAbilityDetailTabs();
            RefreshTypographyScale();
            if (LayoutMode == LayoutMode.Compact)
            {
                ShowCompactPane(CompactPane.Right);
            }
        }

        private void HandleRightPaneContentCleared()
        {
            if (LayoutMode == LayoutMode.Compact && _compactPane == CompactPane.Right)
            {
                ShowCompactPane(CompactPane.Main);
            }
        }

        private void HandleMainPaneRequested()
        {
            if (LayoutMode == LayoutMode.Compact)
            {
                ShowCompactPane(CompactPane.Main);
            }
        }

        private void HandleMapOpening()
        {
            BringOverlayToFront(_mapViewport);
        }

        private void HandleItemDetailsRequested(ItemInstance itemInstance)
        {
            if (itemInstance == null)
            {
                return;
            }

            ShowItemDetails(itemInstance.ItemId);
        }

        private void ToggleCompactPane(CompactPane pane)
        {
            if (LayoutMode != LayoutMode.Compact)
            {
                return;
            }

            var viewport = GetDrawerViewport(pane);
            if (_compactPane != pane)
            {
                ShowCompactPane(pane);
            }
            else if (IsOverlayTop(viewport))
            {
                ShowCompactPane(CompactPane.Main);
            }
            else
            {
                ShowCompactPane(pane);
            }
        }

        private RectTransform GetDrawerViewport(CompactPane pane)
        {
            return pane switch
            {
                CompactPane.Left => _leftDrawerViewport,
                CompactPane.Right => _rightDrawerViewport,
                _ => null,
            };
        }

        private bool IsOverlayTop(RectTransform candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            var highestSiblingIndex = -1;
            if (LayoutMode == LayoutMode.Compact && _compactPane != CompactPane.Main)
            {
                var drawer = GetDrawerViewport(_compactPane);
                if (drawer != null)
                {
                    highestSiblingIndex = drawer.GetSiblingIndex();
                }
            }

            if (MapOverlayView != null && MapOverlayView.IsOpen && _mapViewport != null)
            {
                highestSiblingIndex = Mathf.Max(
                    highestSiblingIndex,
                    _mapViewport.GetSiblingIndex());
            }

            if (ItemPanelView != null
                && ItemPanelView.IsOpen
                && _itemPanelViewport != null)
            {
                highestSiblingIndex = Mathf.Max(
                    highestSiblingIndex,
                    _itemPanelViewport.GetSiblingIndex());
            }

            if (_contentDetailOverlayView != null
                && _contentDetailOverlayView.IsOpen
                && _contentDetailViewport != null)
            {
                highestSiblingIndex = Mathf.Max(
                    highestSiblingIndex,
                    _contentDetailViewport.GetSiblingIndex());
            }

            return candidate.GetSiblingIndex() == highestSiblingIndex;
        }

        private static void BringOverlayToFront(RectTransform overlay)
        {
            overlay?.SetAsLastSibling();
        }

        private static RectTransform CreateLayer(string objectName, RectTransform parent)
        {
            var layerObject = new GameObject(objectName, typeof(RectTransform));
            layerObject.layer = parent.gameObject.layer;
            var rect = layerObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetStretch(rect);
            rect.SetAsLastSibling();
            return rect;
        }

        private static RectTransform CreateDrawerViewport(
            string objectName,
            RectTransform parent,
            out CanvasGroup canvasGroup)
        {
            var viewportObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(RectMask2D));
            viewportObject.layer = parent.gameObject.layer;
            var rect = viewportObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            canvasGroup = viewportObject.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            SetDrawerInteraction(canvasGroup, false);
            return rect;
        }

        private static void ConfigureDrawerPane(
            RectTransform pane,
            float width,
            bool alignLeft)
        {
            var anchorX = alignLeft ? 0f : 1f;
            pane.anchorMin = new Vector2(anchorX, 0f);
            pane.anchorMax = new Vector2(anchorX, 1f);
            pane.pivot = new Vector2(anchorX, 0.5f);
            pane.anchoredPosition = Vector2.zero;
            pane.sizeDelta = new Vector2(width, 0f);
        }

        private static void SetStretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetDrawerInteraction(CanvasGroup canvasGroup, bool enabled)
        {
            canvasGroup.interactable = enabled;
            canvasGroup.blocksRaycasts = enabled;
        }

        private void LogMissingRuntimeReferences()
        {
            var missing = new List<string>();

            if (HeaderView == null) missing.Add(nameof(HeaderView));
            if (LeftPaneView == null) missing.Add(nameof(LeftPaneView));
            if (MainPaneView == null) missing.Add(nameof(MainPaneView));
            if (RightPaneView == null) missing.Add(nameof(RightPaneView));
            if (MapOverlayView == null) missing.Add(nameof(MapOverlayView));
            if (_headerRect == null) missing.Add(nameof(_headerRect));
            if (_contentRect == null) missing.Add(nameof(_contentRect));
            if (_bodyRect == null) missing.Add(nameof(_bodyRect));
            if (_leftPaneRect == null) missing.Add(nameof(_leftPaneRect));
            if (_mainPaneRect == null) missing.Add(nameof(_mainPaneRect));
            if (_rightPaneRect == null) missing.Add(nameof(_rightPaneRect));

            if (missing.Count == 0)
            {
                return;
            }

            Debug.LogWarning(
                $"{nameof(GameRootView)} on '{name}' is missing references: {string.Join(", ", missing)}",
                this);
        }

    }

    internal sealed class ResponsiveTypographySize : MonoBehaviour
    {
        [SerializeField] private bool _isCaptured;
        [SerializeField] private float _fontSize;
        [SerializeField] private float _fontSizeMin;
        [SerializeField] private float _fontSizeMax;

        public void SetBaseFontSize(TMP_Text text, float fontSize)
        {
            _fontSize = fontSize;
            if (text.enableAutoSizing)
            {
                _fontSizeMin = fontSize;
                _fontSizeMax = fontSize;
            }

            _isCaptured = true;
            text.fontSize = fontSize;
        }

        public void Apply(TMP_Text text, float scale)
        {
            if (!_isCaptured)
            {
                _fontSize = text.fontSize;
                _fontSizeMin = text.fontSizeMin;
                _fontSizeMax = text.fontSizeMax;
                _isCaptured = true;
            }

            text.fontSize = _fontSize * scale;
            if (text.enableAutoSizing)
            {
                text.fontSizeMin = _fontSizeMin * scale;
                text.fontSizeMax = _fontSizeMax * scale;
            }
        }
    }
}
