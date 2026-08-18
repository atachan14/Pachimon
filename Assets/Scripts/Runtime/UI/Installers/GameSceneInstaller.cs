using System.Collections.Generic;
using Pachimon.App;
using Pachimon.Battle;
using Pachimon.Data;
using Pachimon.Items;
using Pachimon.Run;
using Pachimon.Skills;
using Pachimon.Trainer;
using Pachimon.Passives;
using UnityEngine;

namespace Pachimon.UI
{
    public sealed class GameSceneInstaller : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private GameRootView _gameRootView;
        [SerializeField] private HeaderView _headerView;
        [SerializeField] private LeftPaneView _leftPaneView;
        [SerializeField] private MainPaneView _mainPaneView;
        [SerializeField] private RightPaneView _rightPaneView;
        [SerializeField] private MapOverlayView _mapOverlayView;

        [Header("Main Screens")]
        [SerializeField] private StartScreen _startScreen;
        [SerializeField] private BattleScreen _battleScreen;
        [SerializeField] private CityScreen _cityScreen;
        [SerializeField] private RestSpotScreen _restSpotScreen;
        [SerializeField] private LeagueGateScreen _leagueGateScreen;
        [SerializeField] private DefeatScreen _defeatScreen;
        [SerializeField] private HallOfFameScreen _hallOfFameScreen;

        [Header("Data")]
        [SerializeField] private PachimonCatalog _pachimonCatalog;
        [SerializeField] private SkillCatalog _skillCatalog;
        [SerializeField] private PassiveCatalog _passiveCatalog;
        [SerializeField] private ItemCatalog _itemCatalog;
        [SerializeField] private TrainerStyleCatalog _trainerStyleCatalog;
        [SerializeField] private TrainerNameCatalog _trainerNameCatalog;
        [SerializeField] private RunProfileSettings _runProfileSettings;

        [Header("Layout")]
        [SerializeField] private float _compactBreakpoint = 1100f;
        [SerializeField] private NodeScreen _initialScreen;

        [Header("Debug")]
        [SerializeField] private bool _initializeRun = true;
        [SerializeField] private bool _initializeDemoBattle = true;

        public RunContext CurrentRunContext { get; private set; }

        private void Awake()
        {
            if (!CanUseSceneReferences())
            {
                LogMissingInstallerReferences();
                enabled = false;
                return;
            }

            InitializeSceneHierarchy();
        }

        private bool CanUseSceneReferences()
        {
            return _gameRootView != null
                && _headerView != null
                && _leftPaneView != null
                && _mainPaneView != null
                && _mainPaneView.LogWindowView != null
                && _rightPaneView != null
                && _mapOverlayView != null
                && _startScreen != null
                && _battleScreen != null
                && _cityScreen != null
                && _restSpotScreen != null
                && _leagueGateScreen != null
                && _pachimonCatalog != null
                && _skillCatalog != null
                && _passiveCatalog != null
                && _itemCatalog != null
                && _trainerStyleCatalog != null
                && _trainerNameCatalog != null
                && _runProfileSettings != null
                && _runProfileSettings.Resolve() != null;
        }

        private void InitializeSceneHierarchy()
        {
            _leftPaneView.EnsurePartyWindow(_rightPaneView.NodeSelectionWindow?.BattleWindow);

            var headerRect = _headerView.GetComponent<RectTransform>();
            var contentRect = _mainPaneView.transform.parent as RectTransform;
            var leftPaneRect = _leftPaneView.GetComponent<RectTransform>();
            var mainPaneRect = _mainPaneView.GetComponent<RectTransform>();
            var rightPaneRect = _rightPaneView.GetComponent<RectTransform>();

            _gameRootView.Initialize(
                _headerView,
                _leftPaneView,
                _mainPaneView,
                _rightPaneView,
                _mapOverlayView,
                headerRect,
                contentRect,
                leftPaneRect,
                mainPaneRect,
                rightPaneRect,
                _compactBreakpoint);
            _gameRootView.BindAbilityDetails(_skillCatalog, _passiveCatalog);

            RegisterScreens();
            WireButtons();

            if (_mapOverlayView.IsOpen)
            {
                _mapOverlayView.Close();
            }

            if (_battleScreen.RewardOverlayView != null)
            {
                _battleScreen.RewardOverlayView.Close();
            }

            if (_initializeDemoBattle)
            {
                InitializeDemoBattle();
            }

            if (_initializeRun)
            {
                InitializeRun();
                return;
            }

            _mainPaneView.Show(_initialScreen != null ? _initialScreen : _battleScreen);
        }

        private void RegisterScreens()
        {
            _mainPaneView.RegisterScreen(_startScreen);
            _mainPaneView.RegisterScreen(_battleScreen);
            _mainPaneView.RegisterScreen(_cityScreen);
            _mainPaneView.RegisterScreen(_restSpotScreen);
            _mainPaneView.RegisterScreen(_leagueGateScreen);
            _mainPaneView.RegisterScreen(_defeatScreen);
            _mainPaneView.RegisterScreen(_hallOfFameScreen);
        }

        private void WireButtons()
        {
            if (_headerView.MapButton != null)
            {
                _headerView.MapButton.onClick.RemoveAllListeners();
                _headerView.MapButton.onClick.AddListener(_gameRootView.ToggleMapOverlay);
            }
            else
            {
                Debug.LogWarning($"{nameof(GameSceneInstaller)} on '{name}' is missing HeaderView.MapButton.", this);
            }

            if (_headerView.ItemButton == null)
            {
                Debug.LogWarning($"{nameof(GameSceneInstaller)} on '{name}' is missing HeaderView.ItemButton.", this);
            }
            else
            {
                _headerView.ItemButton.onClick.RemoveAllListeners();
                _headerView.ItemButton.onClick.AddListener(_gameRootView.ToggleItemPanel);
            }

            if (_headerView.SettingsButton == null)
            {
                Debug.LogWarning($"{nameof(GameSceneInstaller)} on '{name}' is missing HeaderView.SettingsButton.", this);
            }
            else
            {
                _headerView.SettingsButton.onClick.RemoveAllListeners();
                _headerView.SettingsButton.onClick.AddListener(
                    _gameRootView.ToggleSettingsOverlay);
            }
        }

        private void InitializeDemoBattle()
        {
            var controller = new BattleController();
            var state = controller.CreateDemoState();
            controller.RunDemoOpeningExchange(state);
            _battleScreen.Render(state);
        }

        private void InitializeRun()
        {
            var runBootstrap = new RunBootstrap();
            CurrentRunContext = runBootstrap.CreateContext(
                _gameRootView,
                _headerView,
                _leftPaneView,
                _mainPaneView,
                _rightPaneView,
                _mapOverlayView,
                _startScreen,
                _battleScreen,
                _cityScreen,
                _restSpotScreen,
                _leagueGateScreen,
                _pachimonCatalog,
                _skillCatalog,
                _passiveCatalog,
                _itemCatalog,
                _trainerStyleCatalog,
                _trainerNameCatalog,
                NewGameRequest.ConsumePlayerName(),
                _runProfileSettings.Resolve());

            _gameRootView.BindItemPanel(
                CurrentRunContext.RunState.ItemInventory,
                _itemCatalog);
            _leftPaneView.ConfigureItemDrop(
                CurrentRunContext.MapRunController.CanUseItemOnPlayer,
                CurrentRunContext.MapRunController.TryUseItemOnPlayer);
            _rightPaneView.ConfigureItemDrop(
                CurrentRunContext.MapRunController.CanUseItemOnEnemy,
                CurrentRunContext.MapRunController.TryUseItemOnEnemy);
            _battleScreen.BattleMainView?.ConfigureItemDrops(
                CurrentRunContext.MapRunController.CanUseItemOnPlayer,
                CurrentRunContext.MapRunController.TryUseItemOnPlayer,
                CurrentRunContext.MapRunController.CanUseItemOnBattleEnemy,
                CurrentRunContext.MapRunController.TryUseItemOnBattleEnemy);
            _battleScreen.BattleMainView?.ConfigureUnitClicks(
                CurrentRunContext.MapRunController.FocusPlayerBattleUnit,
                CurrentRunContext.MapRunController.FocusEnemyBattleUnit);
        }

        public bool ConfigureTrainerCatalogs(
            TrainerStyleCatalog trainerStyleCatalog,
            TrainerNameCatalog trainerNameCatalog)
        {
            if (_trainerStyleCatalog == trainerStyleCatalog
                && _trainerNameCatalog == trainerNameCatalog)
            {
                return false;
            }

            _trainerStyleCatalog = trainerStyleCatalog;
            _trainerNameCatalog = trainerNameCatalog;
            return true;
        }

        public bool ConfigurePachimonCatalog(PachimonCatalog pachimonCatalog)
        {
            if (_pachimonCatalog == pachimonCatalog)
            {
                return false;
            }

            _pachimonCatalog = pachimonCatalog;
            return true;
        }

        public bool ConfigureSkillCatalog(SkillCatalog skillCatalog)
        {
            if (_skillCatalog == skillCatalog)
            {
                return false;
            }

            _skillCatalog = skillCatalog;
            return true;
        }

        public bool ConfigurePassiveCatalog(PassiveCatalog passiveCatalog)
        {
            if (_passiveCatalog == passiveCatalog)
            {
                return false;
            }

            _passiveCatalog = passiveCatalog;
            return true;
        }

        public bool ConfigureItemCatalog(ItemCatalog itemCatalog)
        {
            if (_itemCatalog == itemCatalog)
            {
                return false;
            }

            _itemCatalog = itemCatalog;
            return true;
        }

        private void LogMissingInstallerReferences()
        {
            var missing = new List<string>();

            if (_gameRootView == null) missing.Add(nameof(_gameRootView));
            if (_headerView == null) missing.Add(nameof(_headerView));
            if (_leftPaneView == null) missing.Add(nameof(_leftPaneView));
            if (_mainPaneView == null) missing.Add(nameof(_mainPaneView));
            if (_mainPaneView != null && _mainPaneView.LogWindowView == null) missing.Add("MainPaneView.LogWindowView");
            if (_rightPaneView == null) missing.Add(nameof(_rightPaneView));
            if (_mapOverlayView == null) missing.Add(nameof(_mapOverlayView));
            if (_startScreen == null) missing.Add(nameof(_startScreen));
            if (_battleScreen == null) missing.Add(nameof(_battleScreen));
            if (_cityScreen == null) missing.Add(nameof(_cityScreen));
            if (_restSpotScreen == null) missing.Add(nameof(_restSpotScreen));
            if (_leagueGateScreen == null) missing.Add(nameof(_leagueGateScreen));
            if (_pachimonCatalog == null) missing.Add(nameof(_pachimonCatalog));
            if (_skillCatalog == null) missing.Add(nameof(_skillCatalog));
            if (_passiveCatalog == null) missing.Add(nameof(_passiveCatalog));
            if (_itemCatalog == null) missing.Add(nameof(_itemCatalog));
            if (_trainerStyleCatalog == null) missing.Add(nameof(_trainerStyleCatalog));
            if (_trainerNameCatalog == null) missing.Add(nameof(_trainerNameCatalog));
            if (_runProfileSettings == null)
            {
                missing.Add(nameof(_runProfileSettings));
            }
            else if (_runProfileSettings.Resolve() == null)
            {
                missing.Add("active RunStartupProfile");
            }

            if (missing.Count == 0)
            {
                return;
            }

            Debug.LogError($"{nameof(GameSceneInstaller)} on '{name}' is missing scene references: {string.Join(", ", missing)}. GameScene now requires scene-installed UI and will not fall back to generated UI.", this);
        }
    }
}
