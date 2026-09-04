using System.Collections;
using System.Collections.Generic;
using Pachimon.App;
using Pachimon.Battle;
using Pachimon.Items;
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
        private const string LayoutPreferenceKey = "Pachimon.LayoutMode";
        private const string ResponsiveUiMetricsResourcePath =
            "UI/ResponsiveUiMetrics";

        [field: SerializeField] public HeaderView HeaderView { get; private set; }
        [field: SerializeField] public LeftPaneView LeftPaneView { get; private set; }
        [field: SerializeField] public MainPaneView MainPaneView { get; private set; }
        [field: SerializeField] public RightPaneView RightPaneView { get; private set; }
        [field: SerializeField] public MapOverlayView MapOverlayView { get; private set; }
        public ItemPanelView ItemPanelView { get; private set; }
        public SettingsOverlayView SettingsOverlayView { get; private set; }
        public RuntimeErrorOverlayView RuntimeErrorOverlayView { get; private set; }
        [field: SerializeField] public LayoutMode LayoutMode { get; private set; }

        [SerializeField, Min(0f)] private float _drawerTransitionDuration = 0.25f;
        [SerializeField, Min(0f)] private float _sceneFadeDuration = 0.75f;
        [SerializeField] private ResponsiveUiMetrics _responsiveUiMetrics;
        [SerializeField, Min(0.05f)] private float _typographyScanInterval = 0.25f;

        private RectTransform _rootRect;
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
        private RectTransform _settingsOverlayViewport;
        private RectTransform _contentDetailViewport;
        private ContentDetailOverlayView _contentDetailOverlayView;
        private ItemCatalog _itemCatalog;
        private CanvasGroup _leftDrawerCanvasGroup;
        private CanvasGroup _rightDrawerCanvasGroup;
        private HorizontalLayoutGroup _contentLayout;
        private Coroutine _drawerRoutine;
        private Coroutine _sceneFadeRoutine;
        private CanvasGroup _sceneFadeCanvasGroup;
        private readonly List<TMP_Text> _typographyTexts = new();
        private readonly List<PachimonTabView> _abilityDetailTabs = new();
        private readonly ContentDetailFactory _contentDetailFactory = new();
        private readonly OverlayLayerCoordinator _overlayCoordinator = new();
        private ResponsiveUiGeometry _responsiveGeometry;
        private CompactPane _compactPane = CompactPane.Main;
        private float _leftDrawerProgress;
        private float _rightDrawerProgress;
        private float _compactBreakpoint;
        private float _nextTypographyScanTime;
        private float _currentUiScale = 1f;
        private Vector2 _lastRootViewportSize = new(float.NaN, float.NaN);
        private bool _isInitialized;

        private ResponsiveUiMetrics UiMetrics
        {
            get
            {
                if (_responsiveUiMetrics != null)
                {
                    return _responsiveUiMetrics;
                }

                _responsiveUiMetrics = Resources.Load<ResponsiveUiMetrics>(
                    ResponsiveUiMetricsResourcePath);
                return _responsiveUiMetrics != null
                    ? _responsiveUiMetrics
                    : _responsiveUiMetrics = ResponsiveUiMetrics.CreateRuntimeDefaults();
            }
        }

        public CompactPane CurrentCompactPane => _compactPane;
        public LayoutMode PreferredLayoutMode { get; private set; }
            = LayoutMode.Compact;
        public float CurrentUiScale => _currentUiScale;

        public void BindErrorDiagnostics(
            System.Func<RuntimeErrorDiagnosticContext> contextProvider)
        {
            RuntimeErrorOverlayView?.ConfigureDiagnostics(contextProvider);
        }

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
            _rootRect = transform as RectTransform;
            _headerRect = headerRect;
            _contentRect = contentRect;
            _bodyRect = contentRect != null ? contentRect.parent as RectTransform : null;
            _leftPaneRect = leftPaneRect;
            _mainPaneRect = mainPaneRect;
            _rightPaneRect = rightPaneRect;
            _compactBreakpoint = compactBreakpoint;
            PreferredLayoutMode = LoadPreferredLayoutMode();

            LogMissingRuntimeReferences();
            InitializeResponsiveHierarchy();
            WireResponsiveEvents();
            _isInitialized = true;
            ApplyLayoutMode(GetEffectiveLayoutMode());
        }

        private void Update()
        {
            if (!_isInitialized)
            {
                return;
            }

            var effectiveMode = GetEffectiveLayoutMode();
            if (effectiveMode != LayoutMode)
            {
                ApplyLayoutMode(effectiveMode);
            }
            else if (ApplyRootWidthConstraint(LayoutMode, false))
            {
                Canvas.ForceUpdateCanvases();
                _responsiveGeometry?.Invalidate();
                RefreshTypographyScale();
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

            _responsiveGeometry?.RefreshIfChanged(LayoutMode);
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
            _contentDetailFactory.Configure(skillCatalog, passiveCatalog);
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

        public void ShowItemDetails(
            int itemId,
            GeneratedItemData generatedData = null)
        {
            var item = _itemCatalog?.Get(itemId);
            if (item == null || _contentDetailOverlayView == null)
            {
                return;
            }

            BringOverlayToFront(_contentDetailViewport);
            _contentDetailOverlayView.Show(
                _contentDetailFactory.CreateItem(item, generatedData));
        }

        public void CloseItemPanel()
        {
            ItemPanelView?.Close();
            if (_contentDetailOverlayView != null
                && _contentDetailOverlayView.ShownKind == ContentDetailKind.Item)
            {
                _contentDetailOverlayView.Close();
            }
        }

        public void ShowAbilityDetails(
            PachimonAbilityPreview ability,
            PachimonPreviewContent owner)
        {
            HandleAbilityDetailsRequested(ability, owner);
        }

        public void ShowFieldEffectDetails(BattleFieldEffectInstance effect)
        {
            if (effect == null || _contentDetailOverlayView == null)
            {
                return;
            }

            BringOverlayToFront(_contentDetailViewport);
            _contentDetailOverlayView.Show(
                _contentDetailFactory.CreateFieldEffect(effect));
        }

        public void ShowWeatherDetails(BattleWeatherInstance weather)
        {
            if (weather == null || _contentDetailOverlayView == null)
            {
                return;
            }

            BringOverlayToFront(_contentDetailViewport);
            _contentDetailOverlayView.Show(
                _contentDetailFactory.CreateWeather(weather));
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

        public void ToggleSettingsOverlay()
        {
            if (SettingsOverlayView == null || _settingsOverlayViewport == null)
            {
                Debug.LogWarning(
                    $"{nameof(GameRootView)} on '{name}' has no Settings Overlay.",
                    this);
                return;
            }

            if (!SettingsOverlayView.IsOpen)
            {
                BringOverlayToFront(_settingsOverlayViewport);
                SettingsOverlayView.Open();
            }
            else if (IsOverlayTop(_settingsOverlayViewport))
            {
                SettingsOverlayView.Close();
            }
            else
            {
                BringOverlayToFront(_settingsOverlayViewport);
                SettingsOverlayView.ReplayOpenTransition();
            }
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

            var viewport = _rootRect != null
                ? _rootRect.parent as RectTransform
                : null;
            var width = viewport != null && viewport.rect.width > 0f
                ? viewport.rect.width
                : Screen.width;
            return width < _compactBreakpoint ? LayoutMode.Compact : LayoutMode.Expanded;
        }

        public void SetPreferredLayoutMode(LayoutMode layoutMode)
        {
            PreferredLayoutMode = layoutMode;
            PlayerPrefs.SetInt(LayoutPreferenceKey, (int)layoutMode);
            PlayerPrefs.Save();
            ApplyLayoutMode(GetEffectiveLayoutMode());
        }

        private LayoutMode GetEffectiveLayoutMode()
        {
            return LayoutModePolicy.Resolve(
                PreferredLayoutMode,
                GetRecommendedLayoutMode() == LayoutMode.Expanded);
        }

        private static LayoutMode LoadPreferredLayoutMode()
        {
            var stored = PlayerPrefs.GetInt(
                LayoutPreferenceKey,
                (int)LayoutMode.Compact);
            return System.Enum.IsDefined(typeof(LayoutMode), stored)
                ? (LayoutMode)stored
                : LayoutMode.Compact;
        }

        public void ApplyLayoutMode(LayoutMode layoutMode)
        {
            LayoutMode = layoutMode;
            ApplyRootWidthConstraint(layoutMode, true);

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
                const float headerHeight = 100f;
                _headerRect.sizeDelta = new Vector2(
                    0f,
                    headerHeight);

                var headerLayout = _headerRect.GetComponent<LayoutElement>();
                if (headerLayout != null)
                {
                    headerLayout.minHeight = headerHeight;
                    headerLayout.preferredHeight = headerHeight;
                    headerLayout.flexibleHeight = 0f;
                }
            }

            HeaderView?.SetCompactPaneButtonsVisible(layoutMode == LayoutMode.Compact);
            HeaderView?.SetCompactPaneSelection(_compactPane);
            HeaderView?.ApplyLayoutMode(layoutMode);
            RightPaneView?.ApplyLayoutMode(layoutMode);
            ItemPanelView?.ApplyLayoutMode(layoutMode);
            MapOverlayView?.ApplyLayoutMode(layoutMode);
            Canvas.ForceUpdateCanvases();
            RefreshTypographyScale();
            Canvas.ForceUpdateCanvases();
            _responsiveGeometry?.Invalidate();
            _responsiveGeometry?.RefreshIfChanged(LayoutMode);
            SettingsOverlayView?.SetLayoutModes(
                PreferredLayoutMode,
                LayoutMode);
        }

        private bool ApplyRootWidthConstraint(
            LayoutMode layoutMode,
            bool force)
        {
            if (_rootRect == null
                || _rootRect.parent is not RectTransform viewport)
            {
                return false;
            }

            var viewportSize = viewport.rect.size;
            if (viewportSize.x <= 0f || viewportSize.y <= 0f)
            {
                return false;
            }

            if (!force
                && (viewportSize - _lastRootViewportSize).sqrMagnitude < 0.01f)
            {
                return false;
            }

            _lastRootViewportSize = viewportSize;
            var maxCompactWidth = viewportSize.y * UiMetrics.CompactMaxWidthToHeight;
            var shouldConstrain = layoutMode == LayoutMode.Compact
                && viewportSize.x > maxCompactWidth;

            _rootRect.pivot = new Vector2(0.5f, 0.5f);
            _rootRect.anchoredPosition = Vector2.zero;
            if (shouldConstrain)
            {
                _rootRect.anchorMin = new Vector2(0.5f, 0f);
                _rootRect.anchorMax = new Vector2(0.5f, 1f);
                _rootRect.sizeDelta = new Vector2(maxCompactWidth, 0f);
            }
            else
            {
                _rootRect.anchorMin = Vector2.zero;
                _rootRect.anchorMax = Vector2.one;
                _rootRect.sizeDelta = Vector2.zero;
            }

            LayoutRebuilder.MarkLayoutForRebuild(_rootRect);
            return true;
        }

        private void RefreshTypographyScale()
        {
            var mainLayout = ResolveResponsiveLayout(_mainPaneRect);
            if (MainPaneView != null)
            {
                foreach (var city in MainPaneView.GetComponentsInChildren<CityScreen>(true))
                {
                    city?.ApplyResponsiveLayout(mainLayout);
                }

                foreach (var reward in MainPaneView.GetComponentsInChildren<RewardOverlayView>(true))
                {
                    reward?.ApplyResponsiveLayout(mainLayout);
                }
            }

            _typographyTexts.Clear();
            GetComponentsInChildren(true, _typographyTexts);

            var scale = mainLayout.TypographyScale;
            _currentUiScale = scale;
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

            MainPaneView?.LogWindowView?.ApplyUiScale(scale);
            RightPaneView?.NodeSelectionWindow?.ApplyUiScale(scale);
            ApplyPachimonTabLayout(_leftPaneRect);
            ApplyPachimonTabLayout(_rightPaneRect);
            ApplyTrainerTabLayout(_leftPaneRect);
            ApplyTrainerTabLayout(_rightPaneRect);
        }

        private ResponsiveUiLayout ResolveResponsiveLayout(RectTransform pane)
        {
            var paneSize = pane != null ? pane.rect.size : Vector2.zero;
            var rootSize = _rootRect != null ? _rootRect.rect.size : Vector2.zero;
            var width = paneSize.x > 0f ? paneSize.x : rootSize.x;
            var height = paneSize.y > 0f ? paneSize.y : rootSize.y;
            return UiMetrics.Resolve(LayoutMode, width, height);
        }

        private void ApplyPachimonTabLayout(RectTransform pane)
        {
            if (pane == null)
            {
                return;
            }

            var layout = ResolveResponsiveLayout(pane);
            foreach (var tab in pane.GetComponentsInChildren<PachimonTabView>(true))
            {
                tab?.ApplyResponsiveLayout(layout);
            }
        }

        private void ApplyTrainerTabLayout(RectTransform pane)
        {
            if (pane == null)
            {
                return;
            }

            var layout = ResolveResponsiveLayout(pane);
            foreach (var tab in pane.GetComponentsInChildren<TrainerTabView>(true))
            {
                tab?.ApplyResponsiveLayout(layout);
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
            RuntimeErrorOverlayView =
                Pachimon.UI.RuntimeErrorOverlayView.CreateRuntime(
                    transform as RectTransform ?? _bodyRect);
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
            MainPaneView?.LogWindowView?.SetInputBlockedProvider(
                () => ItemPanelView != null && ItemPanelView.IsOpen);
            _settingsOverlayViewport = CreateLayer(
                "SettingsOverlayViewport",
                _overlayLayer);
            SettingsOverlayView =
                SettingsOverlayView.CreateRuntime(_settingsOverlayViewport);
            SettingsOverlayView.ConfigureLayoutMode(SetPreferredLayoutMode);
            SettingsOverlayView.SetLayoutModes(
                PreferredLayoutMode,
                GetEffectiveLayoutMode());
            SettingsOverlayView.Close();
            _contentDetailViewport = CreateLayer(
                "ContentDetailViewport",
                _overlayLayer);
            _contentDetailOverlayView =
                ContentDetailOverlayView.CreateRuntime(_contentDetailViewport);
            _contentDetailOverlayView.Close();

            _responsiveGeometry = new ResponsiveUiGeometry(
                _bodyRect,
                _mainPaneRect,
                _leftPaneRect,
                _rightPaneRect,
                _overlayLayer,
                _leftDrawerViewport,
                _rightDrawerViewport,
                _mapViewport,
                _itemPanelViewport,
                _settingsOverlayViewport,
                _contentDetailViewport,
                MainPaneView,
                ItemPanelView,
                SettingsOverlayView,
                _contentDetailOverlayView);

            RegisterOverlayLayers();

            _leftDrawerViewport.gameObject.SetActive(false);
            _rightDrawerViewport.gameObject.SetActive(false);
        }

        private void RegisterOverlayLayers()
        {
            _overlayCoordinator.Clear();
            _overlayCoordinator.Register(
                _leftDrawerViewport,
                () => LayoutMode == LayoutMode.Compact
                    && _compactPane == CompactPane.Left);
            _overlayCoordinator.Register(
                _rightDrawerViewport,
                () => LayoutMode == LayoutMode.Compact
                    && _compactPane == CompactPane.Right);
            _overlayCoordinator.Register(
                _mapViewport,
                () => MapOverlayView != null && MapOverlayView.IsOpen);
            _overlayCoordinator.Register(
                _itemPanelViewport,
                () => ItemPanelView != null && ItemPanelView.IsOpen);
            _overlayCoordinator.Register(
                _settingsOverlayViewport,
                () => SettingsOverlayView != null && SettingsOverlayView.IsOpen);
            _overlayCoordinator.Register(
                _contentDetailViewport,
                () => _contentDetailOverlayView != null
                    && _contentDetailOverlayView.IsOpen);
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

            _responsiveGeometry?.RefreshCompactDrawers();
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
                tab.StatusDetailsRequested += HandleStatusDetailsRequested;
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
                    tab.StatusDetailsRequested -= HandleStatusDetailsRequested;
                }
            }

            _abilityDetailTabs.Clear();
        }

        private void HandleAbilityDetailsRequested(
            PachimonAbilityPreview ability,
            PachimonPreviewContent owner)
        {
            var content = ability.Kind == PachimonAbilityKind.Skill
                ? _contentDetailFactory.CreateSkill(ability, owner)
                : _contentDetailFactory.CreatePassive(ability, owner);
            if (content == null || _contentDetailOverlayView == null)
            {
                return;
            }

            BringOverlayToFront(_contentDetailViewport);
            _contentDetailOverlayView.Show(content);
        }

        private void HandleStatusDetailsRequested(PachimonStatusPreview preview)
        {
            var status = preview.Instance;
            if (status == null || _contentDetailOverlayView == null)
            {
                return;
            }

            BringOverlayToFront(_contentDetailViewport);
            _contentDetailOverlayView.Show(
                _contentDetailFactory.CreateStatus(status));
        }

        private void HandleRightPaneContentShown(bool requestCompactPane)
        {
            WireAbilityDetailTabs();
            RefreshTypographyScale();
            if (requestCompactPane && LayoutMode == LayoutMode.Compact)
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

            ShowItemDetails(
                itemInstance.ItemId,
                itemInstance.GeneratedData);
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
            return _overlayCoordinator.IsTop(candidate);
        }

        private void BringOverlayToFront(RectTransform overlay)
        {
            _overlayCoordinator.BringToFront(overlay);
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
}
