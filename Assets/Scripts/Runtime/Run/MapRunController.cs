using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Battle;
using Pachimon.Data;
using Pachimon.Map;
using Pachimon.Items;
using Pachimon.Reward;
using Pachimon.Trainer;
using Pachimon.UI;

namespace Pachimon.Run
{
    public sealed class MapRunController
    {
        private readonly GameRootView _gameRootView;
        private readonly HeaderView _headerView;
        private readonly LeftPaneView _leftPaneView;
        private readonly MainPaneView _mainPaneView;
        private readonly RightPaneView _rightPaneView;
        private readonly MapOverlayView _mapOverlayView;
        private readonly StartScreen _startScreen;
        private readonly BattleScreen _battleScreen;
        private readonly CityScreen _cityScreen;
        private readonly RestSpotScreen _restSpotScreen;
        private readonly LeagueGateScreen _leagueGateScreen;
        private readonly HallOfFameScreen _hallOfFameScreen;
        private bool _canMoveToNextNode;
        private string _pendingNodeId;
        private string _startNodeId;
        private string _startPreviewCandidateId;
        private StartNodeController _startNodeController;
        private StartNodeProgressState? _renderedStartNodeState;
        private BattleState _activeBattleState;
        private TrainerPreviewContent _activeEnemyTrainerPreview;
        private string _inspectedNodeId;
        private IReadOnlyList<string> _inspectedEnemyIds = Array.Empty<string>();
        private bool _rightPaneShowsActiveBattle;
        private ItemUseService _itemUseService;

        public MapRunController(
            GameRootView gameRootView,
            HeaderView headerView,
            LeftPaneView leftPaneView,
            MainPaneView mainPaneView,
            RightPaneView rightPaneView,
            MapOverlayView mapOverlayView,
            StartScreen startScreen,
            BattleScreen battleScreen,
            CityScreen cityScreen,
            RestSpotScreen restSpotScreen,
            LeagueGateScreen leagueGateScreen,
            HallOfFameScreen hallOfFameScreen)
        {
            _gameRootView = gameRootView;
            _headerView = headerView;
            _leftPaneView = leftPaneView;
            _mainPaneView = mainPaneView;
            _rightPaneView = rightPaneView;
            _mapOverlayView = mapOverlayView;
            _startScreen = startScreen;
            _battleScreen = battleScreen;
            _cityScreen = cityScreen;
            _restSpotScreen = restSpotScreen;
            _leagueGateScreen = leagueGateScreen;
            _hallOfFameScreen = hallOfFameScreen;

            if (_mapOverlayView != null)
            {
                _mapOverlayView.NodeSelected += SelectNextNode;
                _mapOverlayView.Closed += HandleMapClosed;
            }
        }

        public RunContext Context { get; private set; }

        public long? ActiveBattleTick => _activeBattleState?.CurrentTick;
        public bool CanOpenItemPanel =>
            _activeBattleState == null || _battleScreen?.CanUseItems == true;

        public void StartRun(RunContext context)
        {
            Context = context;
            _itemUseService = new ItemUseService(Context.ItemCatalog);
            _headerView?.BindItemInventory(Context.RunState.ItemInventory);
            Context.RunState.CurrentNodeId = Context.RunMap.StartNodeId;
            _startNodeId = null;
            _startPreviewCandidateId = null;
            _startNodeController = null;
            _canMoveToNextNode = false;
            CancelNodeSelection();
            ApplyHeaderState();
            RefreshPlayerPartyPane();
            ApplyMapOverlayState();
            ShowCurrentNode();
        }

        public void RefreshPlayerPartyPane()
        {
            if (Context == null || _leftPaneView == null)
            {
                return;
            }

            var partyPreviews = Enumerable.Range(0, RunState.PartySize)
                .Select(index => index < Context.RunState.PlayerPachimonIds.Count
                    ? BuildPachimonPreview(
                        Context.RunState.PlayerPachimonIds[index],
                        true,
                        Context.RunState.PlayerModifiers)
                    : PachimonPreviewContent.Hidden)
                .ToArray();
            _leftPaneView.ShowPlayerParty(BuildPlayerTrainerPreview(), partyPreviews);
        }

        public void FocusPlayerBattleUnit(int slotIndex)
        {
            if (_activeBattleState == null
                || slotIndex < 0
                || slotIndex >= _activeBattleState.Player.Units.Count)
            {
                return;
            }

            FocusBattlePaneUnit(_activeBattleState.Player.GetUnitAt(slotIndex));
        }

        public void FocusEnemyBattleUnit(int slotIndex)
        {
            if (_activeBattleState == null
                || slotIndex < 0
                || slotIndex >= _activeBattleState.Enemy.Units.Count)
            {
                return;
            }

            FocusBattlePaneUnit(_activeBattleState.Enemy.GetUnitAt(slotIndex));
        }

        public bool TrySetInitialParty(IEnumerable<string> pachimonIds)
        {
            if (Context == null || pachimonIds == null)
            {
                return false;
            }

            var ids = pachimonIds.ToArray();
            if (ids.Any(instanceId => Context.PachimonPool.Get(instanceId) == null)
                || !Context.RunState.TrySetInitialParty(ids))
            {
                return false;
            }

            RefreshPlayerPartyPane();
            return true;
        }

        public bool TryUseItemOnPlayer(ItemInstance item, int partyIndex)
        {
            if (Context == null
                || item == null
                || partyIndex < 0
                || partyIndex >= RunState.PartySize)
            {
                return false;
            }

            ItemUseContext useContext;
            if (_activeBattleState != null)
            {
                if (!_battleScreen.CanUseItems)
                {
                    return false;
                }

                var battleTarget = _activeBattleState.Player.GetUnitAt(partyIndex);
                if (battleTarget == null)
                {
                    return false;
                }

                var runTarget = Context.PachimonPool.Get(
                    battleTarget.InstanceId);
                useContext = ItemUseContext.ForBattle(
                    battleTarget,
                    ItemTargetAffiliation.Ally,
                    runTarget,
                    _activeBattleState);
            }
            else
            {
                if (partyIndex >= Context.RunState.PlayerPachimonIds.Count)
                {
                    return false;
                }

                var runTarget = Context.PachimonPool.Get(
                    Context.RunState.PlayerPachimonIds[partyIndex]);
                if (runTarget == null)
                {
                    return false;
                }

                var effectiveStats = PachimonStatService.Calculate(
                    runTarget,
                    Context.RunState.PlayerModifiers,
                    Context.PassiveStatModifierRegistry);
                useContext = ItemUseContext.ForRun(
                    runTarget,
                    effectiveStats.MaxHp,
                    effectiveStats.MaxMn,
                    ItemTargetAffiliation.Ally,
                    () => PachimonStatService.Calculate(
                        runTarget,
                        Context.RunState.PlayerModifiers,
                        Context.PassiveStatModifierRegistry));
            }

            var result = _itemUseService.TryUse(
                Context.RunState.ItemInventory,
                item.InstanceId,
                useContext);
            if (!result.Succeeded)
            {
                return false;
            }

            var selectedTab = _leftPaneView?.PartyWindow?.SelectedTabIndex ?? 0;
            var rightWindow = _rightPaneView?.NodeSelectionWindow?.BattleWindow;
            var rightTab = rightWindow?.SelectedTabIndex ?? 0;
            if (_activeBattleState != null)
            {
                _battleScreen.ApplyExternalBattleStateChange();
                RefreshBattlePanes(
                    _activeBattleState,
                    _activeEnemyTrainerPreview);
            }
            else
            {
                RefreshPlayerPartyPane();
            }

            _leftPaneView?.PartyWindow?.ShowTab(selectedTab);
            rightWindow?.ShowTab(rightTab);
            _gameRootView?.RefreshItemPanel(true);
            return true;
        }

        public bool CanUseItemOnPlayer(ItemInstance item, int partyIndex)
        {
            if (Context == null
                || item == null
                || partyIndex < 0
                || partyIndex >= RunState.PartySize)
            {
                return false;
            }

            ItemUseContext useContext;
            if (_activeBattleState != null)
            {
                if (!_battleScreen.CanUseItems)
                {
                    return false;
                }

                var battleTarget = _activeBattleState.Player.GetUnitAt(partyIndex);
                if (battleTarget == null)
                {
                    return false;
                }

                useContext = ItemUseContext.ForBattle(
                    battleTarget,
                    ItemTargetAffiliation.Ally,
                    Context.PachimonPool.Get(battleTarget.InstanceId),
                    _activeBattleState);
            }
            else
            {
                if (partyIndex >= Context.RunState.PlayerPachimonIds.Count)
                {
                    return false;
                }

                var runTarget = Context.PachimonPool.Get(
                    Context.RunState.PlayerPachimonIds[partyIndex]);
                if (runTarget == null)
                {
                    return false;
                }

                var effectiveStats = PachimonStatService.Calculate(
                    runTarget,
                    Context.RunState.PlayerModifiers,
                    Context.PassiveStatModifierRegistry);
                useContext = ItemUseContext.ForRun(
                    runTarget,
                    effectiveStats.MaxHp,
                    effectiveStats.MaxMn,
                    ItemTargetAffiliation.Ally);
            }

            return _itemUseService.CanUse(
                Context.RunState.ItemInventory,
                item.InstanceId,
                useContext) == ItemUseFailureReason.None;
        }

        public bool TryUseItemOnEnemy(ItemInstance item, int enemyIndex)
        {
            if (Context == null || item == null || enemyIndex < 0)
            {
                return false;
            }

            ItemUseContext useContext;
            if (_activeBattleState != null && !_battleScreen.CanUseItems)
            {
                return false;
            }

            if (_activeBattleState != null && _rightPaneShowsActiveBattle)
            {
                if (enemyIndex >= _activeBattleState.Enemy.Units.Count)
                {
                    return false;
                }

                useContext = ItemUseContext.ForBattle(
                    _activeBattleState.Enemy.GetUnitAt(enemyIndex),
                    ItemTargetAffiliation.Enemy,
                    Context.PachimonPool.Get(
                        _activeBattleState.Enemy
                            .GetUnitAt(enemyIndex)
                            .InstanceId),
                    _activeBattleState);
            }
            else
            {
                if (enemyIndex >= _inspectedEnemyIds.Count)
                {
                    return false;
                }

                var runTarget = Context.PachimonPool.Get(
                    _inspectedEnemyIds[enemyIndex]);
                if (runTarget == null)
                {
                    return false;
                }

                useContext = ItemUseContext.ForRun(
                    runTarget,
                    runTarget.MaxHp,
                    ItemTargetAffiliation.Enemy);
            }

            var result = _itemUseService.TryUse(
                Context.RunState.ItemInventory,
                item.InstanceId,
                useContext);
            if (!result.Succeeded)
            {
                return false;
            }

            var leftTab = _leftPaneView?.PartyWindow?.SelectedTabIndex ?? 0;
            var rightWindow = _rightPaneView?.NodeSelectionWindow?.BattleWindow;
            var rightTab = rightWindow?.SelectedTabIndex ?? 0;
            if (_activeBattleState != null && _rightPaneShowsActiveBattle)
            {
                _battleScreen.ApplyExternalBattleStateChange();
                RefreshBattlePanes(
                    _activeBattleState,
                    _activeEnemyTrainerPreview);
            }
            else if (!string.IsNullOrWhiteSpace(_inspectedNodeId))
            {
                SelectNextNode(_inspectedNodeId);
            }

            _leftPaneView?.PartyWindow?.ShowTab(leftTab);
            rightWindow?.ShowTab(rightTab);
            _gameRootView?.RefreshItemPanel(true);
            return true;
        }

        public bool CanUseItemOnEnemy(ItemInstance item, int enemyIndex)
        {
            if (Context == null || item == null || enemyIndex < 0)
            {
                return false;
            }

            ItemUseContext useContext;
            if (_activeBattleState != null && !_battleScreen.CanUseItems)
            {
                return false;
            }

            if (_activeBattleState != null && _rightPaneShowsActiveBattle)
            {
                if (enemyIndex >= _activeBattleState.Enemy.Units.Count)
                {
                    return false;
                }

                var battleTarget = _activeBattleState.Enemy.GetUnitAt(enemyIndex);
                useContext = ItemUseContext.ForBattle(
                    battleTarget,
                    ItemTargetAffiliation.Enemy,
                    Context.PachimonPool.Get(battleTarget.InstanceId),
                    _activeBattleState);
            }
            else
            {
                if (enemyIndex >= _inspectedEnemyIds.Count)
                {
                    return false;
                }

                var runTarget = Context.PachimonPool.Get(_inspectedEnemyIds[enemyIndex]);
                if (runTarget == null)
                {
                    return false;
                }

                useContext = ItemUseContext.ForRun(
                    runTarget,
                    runTarget.MaxHp,
                    ItemTargetAffiliation.Enemy);
            }

            return _itemUseService.CanUse(
                Context.RunState.ItemInventory,
                item.InstanceId,
                useContext) == ItemUseFailureReason.None;
        }

        public bool TryUseItemOnBattleEnemy(ItemInstance item, int enemyIndex)
        {
            if (_activeBattleState == null
                || !_battleScreen.CanUseItems
                || item == null
                || enemyIndex < 0
                || enemyIndex >= _activeBattleState.Enemy.Units.Count)
            {
                return false;
            }

            var battleTarget = _activeBattleState.Enemy.GetUnitAt(enemyIndex);
            var result = _itemUseService.TryUse(
                Context.RunState.ItemInventory,
                item.InstanceId,
                ItemUseContext.ForBattle(
                    battleTarget,
                    ItemTargetAffiliation.Enemy,
                    Context.PachimonPool.Get(battleTarget.InstanceId),
                    _activeBattleState));
            if (!result.Succeeded)
            {
                return false;
            }

            var leftTab = _leftPaneView?.PartyWindow?.SelectedTabIndex ?? 0;
            var rightWindow = _rightPaneView?.NodeSelectionWindow?.BattleWindow;
            var rightTab = rightWindow?.SelectedTabIndex ?? 0;
            _battleScreen.ApplyExternalBattleStateChange();
            RefreshBattlePanes(
                _activeBattleState,
                _activeEnemyTrainerPreview);
            _leftPaneView?.PartyWindow?.ShowTab(leftTab);
            rightWindow?.ShowTab(rightTab);
            _gameRootView?.RefreshItemPanel(true);
            return true;
        }

        public bool CanUseItemOnBattleEnemy(ItemInstance item, int enemyIndex)
        {
            if (_activeBattleState == null
                || !_battleScreen.CanUseItems
                || item == null
                || enemyIndex < 0
                || enemyIndex >= _activeBattleState.Enemy.Units.Count)
            {
                return false;
            }

            var battleTarget = _activeBattleState.Enemy.GetUnitAt(enemyIndex);
            return _itemUseService.CanUse(
                Context.RunState.ItemInventory,
                item.InstanceId,
                ItemUseContext.ForBattle(
                    battleTarget,
                    ItemTargetAffiliation.Enemy,
                    Context.PachimonPool.Get(battleTarget.InstanceId),
                    _activeBattleState)) == ItemUseFailureReason.None;
        }

        public MapNode GetCurrentNode()
        {
            return Context?.RunMap?.GetNode(Context.RunState.CurrentNodeId);
        }

        public bool TryMoveToNextNode()
        {
            var nextNodeIds = GetCurrentOutgoingNodeIds();
            if (nextNodeIds.Count == 0)
            {
                return false;
            }

            return TryMoveToNode(nextNodeIds[0]);
        }

        public bool TryMoveToNode(string targetNodeId)
        {
            var currentNode = GetCurrentNode();
            var outgoingNodeIds = GetCurrentOutgoingNodeIds();
            if (!_canMoveToNextNode
                || currentNode == null
                || !outgoingNodeIds.Contains(targetNodeId))
            {
                return false;
            }

            _gameRootView?.CloseItemPanel();
            Context.RunState.CurrentNodeId = targetNodeId;
            _canMoveToNextNode = false;
            _pendingNodeId = null;
            ApplyHeaderState();
            ApplyMapOverlayState();
            _mapOverlayView?.Close();
            ShowCurrentNode();
            return true;
        }

        private void SelectNextNode(string nodeId)
        {
            var node = Context.RunMap.GetNode(nodeId);
            if (node == null)
            {
                CancelNodeSelection();
                return;
            }

            _inspectedNodeId = nodeId;
            _inspectedEnemyIds = Array.Empty<string>();
            _rightPaneShowsActiveBattle = false;
            var canMoveToNode = _canMoveToNextNode
                && GetCurrentOutgoingNodeIds().Contains(nodeId);
            _pendingNodeId = canMoveToNode ? nodeId : null;
            _mapOverlayView?.SetSelectedNode(nodeId);
            ShowNodeSelectionDetails(node, canMoveToNode);
        }

        private void ShowNodeSelectionDetails(
            MapNode node,
            bool canMoveToNode)
        {
            switch (node.Content)
            {
                case BattleNodeContent battle:
                    var battleModifiers = EnemyTrainerModifierFactory.Create(node);
                    ShowBattleNodeDetails(
                        BuildTrainerPreview(
                            battle.TrainerProfile,
                            battle.NodeReward,
                            node.RowIndex,
                            battleModifiers),
                        battle.EnemyPachimonInstanceIds,
                        canMoveToNode,
                        battleModifiers);
                    break;
                case GymNodeContent gym:
                    var gymModifiers = EnemyTrainerModifierFactory.Create(node);
                    ShowBattleNodeDetails(
                        BuildTrainerPreview(
                            gym.TrainerProfile,
                            gym.NodeReward,
                            node.RowIndex,
                            gymModifiers),
                        gym.EnemyPachimonInstanceIds,
                        canMoveToNode,
                        gymModifiers);
                    break;
                case EliteNodeContent elite:
                    var eliteModifiers = EnemyTrainerModifierFactory.Create(node);
                    ShowBattleNodeDetails(
                        BuildTrainerPreview(
                            elite.TrainerProfile,
                            null,
                            node.RowIndex,
                            eliteModifiers),
                        elite.EnemyPachimonInstanceIds,
                        canMoveToNode,
                        eliteModifiers);
                    break;
                case CityNodeContent city:
                    ShowCityNodeDetails(node, city, canMoveToNode);
                    break;
                default:
                    if (canMoveToNode)
                    {
                        _rightPaneView?.ShowSimpleNodeSelection(
                            GetNodeTitle(node),
                            BuildNodeDetails(node),
                            ConfirmNodeSelection,
                            CancelNodeSelection);
                    }
                    else
                    {
                        _rightPaneView?.ShowSimpleNodePreview(
                            GetNodeTitle(node),
                            BuildNodeDetails(node));
                    }
                    break;
            }
        }

        private void ShowBattleNodeDetails(
            TrainerPreviewContent trainerPreview,
            IEnumerable<string> enemyIds,
            bool canMoveToNode,
            TrainerModifierSet modifiers)
        {
            _inspectedEnemyIds = enemyIds.ToArray();
            var previews = BuildPachimonPreviews(_inspectedEnemyIds, modifiers);
            if (canMoveToNode)
            {
                _rightPaneView?.ShowBattleNodeSelection(
                    trainerPreview,
                    previews,
                    ConfirmNodeSelection,
                    CancelNodeSelection);
                return;
            }

            _rightPaneView?.ShowBattleNodePreview(trainerPreview, previews);
        }

        private void ShowCityNodeDetails(
            MapNode node,
            CityNodeContent city,
            bool canMoveToNode)
        {
            if (IsCurrentLocation(node))
            {
                _rightPaneView?.ShowCityShop(
                    city,
                    Context.ItemCatalog,
                    Context.RunState,
                    ShowCityItemDetails);
                return;
            }

            if (canMoveToNode)
            {
                _rightPaneView?.ShowCityNodeSelection(
                    city,
                    Context.ItemCatalog,
                    Context.RunState,
                    ShowCityItemDetails,
                    ConfirmNodeSelection,
                    CancelNodeSelection);
                return;
            }

            _rightPaneView?.ShowCityNodePreview(
                city,
                Context.ItemCatalog,
                Context.RunState,
                ShowCityItemDetails);
        }

        private bool IsCurrentLocation(MapNode node)
        {
            var currentNode = GetCurrentNode();
            if (node == null || currentNode == null)
            {
                return false;
            }

            if (node.NodeId == currentNode.NodeId)
            {
                return true;
            }

            var nodeGroup = Context.RunMap.GetNodeGroupForNode(node.NodeId);
            var currentGroup = Context.RunMap.GetNodeGroupForNode(currentNode.NodeId);
            return nodeGroup != null
                && currentGroup != null
                && nodeGroup.GroupId == currentGroup.GroupId;
        }

        private TrainerPreviewContent BuildTrainerPreview(
            TrainerProfile trainerProfile,
            NodeReward reward,
            int rowIndex,
            TrainerModifierSet modifiers)
        {
            var style = Context.TrainerStyleCatalog.Get(trainerProfile.StyleId);
            var trainerName = Context.TrainerNameCatalog.Get(trainerProfile.NameId);
            var title = trainerProfile.Role switch
            {
                TrainerRole.Normal => style?.NormalTitle ?? "Trainer",
                TrainerRole.GymLeader => "ジムリーダー",
                TrainerRole.Elite => "四天王",
                _ => "Trainer",
            };
            var displayName = $"{title}の{trainerName?.DisplayName ?? trainerProfile.NameId}";

            return new TrainerPreviewContent(
                style?.BattleGraphic,
                displayName,
                rowIndex,
                BuildTrainerStatPreviews(modifiers),
                BuildTrainerBadgePreviews(modifiers, false),
                BuildTrainerRewardIcons(reward),
                _headerView?.GoldIconSprite,
                GetDisplayedRewardGold(reward),
                reward != null);
        }

        private TrainerPreviewContent BuildPlayerTrainerPreview()
        {
            return new TrainerPreviewContent(
                Context.TrainerStyleCatalog.PlayerBattleGraphic,
                $"トレーナーの{Context.RunState.PlayerName}",
                GetCurrentNode()?.RowIndex ?? 0,
                BuildTrainerStatPreviews(Context.RunState.PlayerModifiers),
                BuildTrainerBadgePreviews(Context.RunState.PlayerModifiers, true),
                System.Array.Empty<TrainerRewardIconContent>(),
                _headerView?.GoldIconSprite,
                null,
                false);
        }

        private static IReadOnlyList<TrainerStatPreview> BuildTrainerStatPreviews(
            TrainerModifierSet modifiers)
        {
            modifiers ??= new TrainerModifierSet();
            return Enumerable.Range(0, (int)PachimonStatType.Count)
                .Select(index => (PachimonStatType)index)
                .Where(PachimonStatTypeUtility.IsGeneratedStat)
                .Select(statType => new TrainerStatPreview(
                    statType,
                    modifiers.GetStatAddition(statType)))
                .ToArray();
        }

        private static IReadOnlyList<TrainerBadgePreview> BuildTrainerBadgePreviews(
            TrainerModifierSet modifiers,
            bool includeEmptySlots)
        {
            modifiers ??= new TrainerModifierSet();
            return Enum.GetValues(typeof(PachimonAttribute))
                .Cast<PachimonAttribute>()
                .Select(attribute => new TrainerBadgePreview(
                    attribute,
                    modifiers.GetBadgeCount(attribute)))
                .Where(preview => includeEmptySlots || preview.Count > 0)
                .ToArray();
        }

        private static IReadOnlyList<TrainerRewardIconContent> BuildTrainerRewardIcons(
            NodeReward reward)
        {
            if (reward?.BadgeAttribute is PachimonAttribute badgeAttribute)
            {
                return new[]
                {
                    new TrainerRewardIconContent(
                        $"{badgeAttribute}Badge",
                        GetAttributeColor(badgeAttribute),
                        amount: 1,
                        useColoredBackground: true),
                };
            }

            return reward?.Elements
                .Select((element, index) => new { Element = element, Index = index })
                .Where(item => item.Element.Kind != RewardElementKind.BonusGold)
                .Select(item => CreateTrainerRewardContent(
                    item.Element,
                    ModValueSettings.RuntimeDefault.GetAmount(
                        item.Element.Kind,
                        item.Index == 1)))
                .ToArray()
                ?? System.Array.Empty<TrainerRewardIconContent>();
        }

        private static int? GetDisplayedRewardGold(NodeReward reward)
        {
            if (reward == null)
            {
                return null;
            }

            var total = reward.Gold;
            for (var index = 0; index < reward.Elements.Count; index++)
            {
                var element = reward.Elements[index];
                if (element.Kind == RewardElementKind.BonusGold)
                {
                    total = checked(total + ModValueSettings.RuntimeDefault.GetAmount(
                        element.Kind,
                        index == 1));
                }
            }

            return total;
        }

        private static TrainerRewardIconContent CreateTrainerRewardContent(
            RewardElement element,
            int amount)
        {
            if (element.Attribute is PachimonAttribute attribute)
            {
                var icon = AttributeRichText.GetIcon(
                    (AllocationType)((int)attribute + 1));
                return new TrainerRewardIconContent(
                    icon,
                    GetAttributeColor(attribute),
                    amount: amount);
            }

            var label = element.Kind switch
            {
                RewardElementKind.Speed => "SPD",
                RewardElementKind.MaxHp => "HP",
                RewardElementKind.MaxMn => "MN",
                RewardElementKind.DamageBonus => "DB",
                RewardElementKind.ResistBonus => "RB",
                _ => GetRewardElementLabel(element),
            };
            return new TrainerRewardIconContent(
                label,
                GetRewardElementColor(element),
                amount: amount,
                useColoredBackground: true);
        }

        private static string GetAttributeInitial(PachimonAttribute attribute)
        {
            return attribute switch
            {
                PachimonAttribute.Fire => "F",
                PachimonAttribute.Aqua => "A",
                PachimonAttribute.Leaf => "L",
                PachimonAttribute.Electric => "E",
                PachimonAttribute.Poison => "P",
                PachimonAttribute.Ice => "I",
                PachimonAttribute.Wind => "W",
                PachimonAttribute.Dragon => "D",
                _ => "?",
            };
        }

        private static string GetAttributeColor(PachimonAttribute attribute)
        {
            return RewardElementPalette.GetAttributeColorHex(attribute);
        }

        private static string GetRewardElementLabel(RewardElement element)
        {
            return element.Kind switch
            {
                RewardElementKind.Attribute =>
                    GetAttributeInitial(element.Attribute.Value).ToString(),
                RewardElementKind.Speed => "SPEED",
                RewardElementKind.MaxHp => "MAX\nHP",
                RewardElementKind.MaxMn => "MAX\nMN",
                RewardElementKind.BonusGold => "BONUS\nGOLD",
                RewardElementKind.DamageBonus => "DAMAGE\nBONUS",
                RewardElementKind.ResistBonus => "RESIST\nBONUS",
                _ => "MOD",
            };
        }

        private static string GetRewardElementColor(RewardElement element)
        {
            return RewardElementPalette.GetColorHex(element);
        }

        private IReadOnlyList<PachimonPreviewContent> BuildPachimonPreviews(
            IEnumerable<string> enemyIds,
            TrainerModifierSet modifiers)
        {
            return enemyIds
                .Select(instanceId => BuildPachimonPreview(
                    instanceId,
                    true,
                    modifiers,
                    true))
                .ToArray();
        }

        private PachimonPreviewContent BuildPachimonPreview(
            string instanceId,
            bool isRevealed,
            TrainerModifierSet modifiers = null,
            bool includeTrainerResourceIncrease = false)
        {
            if (!isRevealed)
            {
                return PachimonPreviewContent.Hidden;
            }

            return PachimonPreviewFactory.FromRunInstance(
                Context.PachimonPool.Get(instanceId),
                modifiers,
                Context.PachimonCatalog,
                Context.SkillCatalog,
                Context.PassiveCatalog,
                Context.PassiveStatModifierRegistry,
                includeTrainerResourceIncrease);
        }

        private void ConfirmNodeSelection()
        {
            var targetNodeId = _pendingNodeId;
            if (targetNodeId == null || !TryMoveToNode(targetNodeId))
            {
                CancelNodeSelection();
            }
        }

        private void CancelNodeSelection()
        {
            _pendingNodeId = null;
            _inspectedNodeId = null;
            _inspectedEnemyIds = Array.Empty<string>();
            _rightPaneShowsActiveBattle = false;
            _mapOverlayView?.SetSelectedNode(null);
            _rightPaneView?.ClearNodeSelection();
        }

        private void HandleMapClosed()
        {
            if (_activeBattleState == null)
            {
                var currentNode = GetCurrentNode();
                if (currentNode?.Content is CityNodeContent)
                {
                    _pendingNodeId = null;
                    _inspectedNodeId = null;
                    _inspectedEnemyIds = Array.Empty<string>();
                    _mapOverlayView?.SetSelectedNode(null);
                    ShowCurrentCityShop();
                    return;
                }

                CancelNodeSelection();
                return;
            }

            _pendingNodeId = null;
            _inspectedNodeId = null;
            _inspectedEnemyIds = Array.Empty<string>();
            _mapOverlayView?.SetSelectedNode(null);
            RefreshBattlePanes(
                _activeBattleState,
                _activeEnemyTrainerPreview);
        }

        private void ApplyHeaderState()
        {
            _headerView?.SetRunSummary(
                Context.RunState.Gold,
                Context.RunState.BadgeCount);
        }

        private void ApplyMapOverlayState()
        {
            var selectableNodeIds = _canMoveToNextNode
                ? GetCurrentOutgoingNodeIds()
                : null;
            _mapOverlayView?.Render(
                Context?.RunMap,
                Context?.RunState,
                selectableNodeIds,
                Context?.TrainerStyleCatalog);
        }

        private void ShowCurrentNode()
        {
            var currentNode = GetCurrentNode();
            if (currentNode == null)
            {
                return;
            }

            switch (currentNode.NodeType)
            {
                case NodeType.Start:
                    _mainPaneView.Show(_startScreen);
                    ApplyStartNode(currentNode);
                    break;
                case NodeType.Battle:
                    _mainPaneView.Show(_battleScreen);
                    ApplyBattleNode(currentNode);
                    break;
                case NodeType.City:
                    _mainPaneView.Show(_cityScreen);
                    ApplyCityNode(currentNode);
                    break;
                case NodeType.RestSpot:
                    _mainPaneView.Show(_restSpotScreen);
                    ApplyRestSpotNode(currentNode);
                    break;
                case NodeType.Gym:
                    _mainPaneView.Show(_battleScreen);
                    ApplyGymNode(currentNode);
                    break;
                case NodeType.Event:
                    _mainPaneView.Show(_startScreen);
                    ApplyPlaceholderNode(currentNode, "Event Node");
                    break;
                case NodeType.LeagueGate:
                    _mainPaneView.Show(_leagueGateScreen);
                    ApplyLeagueGateNode(currentNode);
                    break;
                case NodeType.Elite:
                    _mainPaneView.Show(_battleScreen);
                    ApplyEliteNode(currentNode);
                    break;
                case NodeType.HallOfFame:
                    _mainPaneView.Show(_hallOfFameScreen);
                    ApplyHallOfFameNode(currentNode);
                    break;
                default:
                    _mainPaneView.Show(_startScreen);
                    ApplyFallbackNode(currentNode);
                    break;
            }
        }

        private static string GetNodeTitle(MapNode node)
        {
            return node.NodeType switch
            {
                NodeType.Battle => "バトル",
                NodeType.Gym => "ジム",
                NodeType.RestSpot => "休憩所",
                NodeType.City => "シティ",
                NodeType.Event => "イベント",
                NodeType.LeagueGate => "リーグゲート",
                NodeType.Elite => "四天王",
                NodeType.Ghost => "ゴースト",
                NodeType.HallOfFame => "殿堂入り",
                _ => node.NodeType.ToString(),
            };
        }

        private string BuildNodeDetails(MapNode node)
        {
            var builder = new StringBuilder();
            builder.Append("Stage: ").AppendLine(node.RowIndex.ToString());

            switch (node.Content)
            {
                case BattleNodeContent battle:
                    builder.AppendLine(FormatTrainer(battle.TrainerProfile));
                    builder.Append("Gold: ").AppendLine(battle.NodeReward.Gold.ToString());
                    builder.Append("Reward: ")
                        .AppendLine(FormatRewardElements(battle.NodeReward));
                    AppendEnemies(builder, battle.EnemyPachimonInstanceIds);
                    break;
                case GymNodeContent gym:
                    builder.AppendLine(FormatTrainer(gym.TrainerProfile));
                    builder.Append("Badge: ").AppendLine(gym.NodeReward.BadgeAttribute.ToString());
                    AppendEnemies(builder, gym.EnemyPachimonInstanceIds);
                    break;
                case EliteNodeContent elite:
                    builder.AppendLine(FormatTrainer(elite.TrainerProfile));
                    AppendEnemies(builder, elite.EnemyPachimonInstanceIds);
                    break;
                case RestSpotNodeContent restSpot:
                    builder.Append("最大体力の ").Append(restSpot.HealPercent).AppendLine("% 回復");
                    break;
                case CityNodeContent:
                    builder.AppendLine("ショップを利用できる");
                    break;
                case EventNodeContent:
                    builder.AppendLine("ランダムイベントが発生する");
                    break;
                case LeagueGateNodeContent leagueGate:
                    builder.Append("必要Badge数: ").AppendLine(leagueGate.RequiredBadgeCount.ToString());
                    break;
                case HallOfFameNodeContent:
                    builder.AppendLine("殿堂入り（仮実装）");
                    break;
                default:
                    builder.AppendLine("詳細は未実装");
                    break;
            }

            return builder.ToString().TrimEnd();
        }

        private static void AppendEnemies(StringBuilder builder, IEnumerable<string> enemyIds)
        {
            builder.Append("Enemy: ").Append(string.Join(", ", enemyIds));
        }

        private void ApplyStartNode(MapNode node)
        {
            if (node.Content is not StartNodeContent content)
            {
                _mainPaneView.LogWindowView?.SetLogText($"よく来たね、{Context.RunState.PlayerName}\nrow:0 の開始ノード");
                _mainPaneView.LogWindowView?.ShowSingleOption("次へ進む", CompleteCurrentNode);
                return;
            }

            if (_startNodeController == null || _startNodeId != node.NodeId)
            {
                _startNodeId = node.NodeId;
                _startNodeController = new StartNodeController(
                    content.CandidatePachimonInstanceIds,
                    content.SelectionCount,
                    StartDialogueData.CreateDefault(Context.RunState.PlayerName),
                    TrySetInitialParty,
                    CompleteCurrentNode);
                _renderedStartNodeState = null;
                _startNodeController.Changed += RenderStartNode;
                _startScreen.ShowCandidates(
                    content.CandidatePachimonInstanceIds
                        .Select(BuildStartCandidateCard)
                        .ToArray(),
                    ShowStartCandidateDetails);
            }

            RenderStartNode();
        }

        private void RenderStartNode()
        {
            var logWindow = _mainPaneView.LogWindowView;
            if (_startNodeController == null || logWindow == null)
            {
                return;
            }

            var enteredState = _renderedStartNodeState
                != _startNodeController.State;
            _renderedStartNodeState = _startNodeController.State;

            switch (_startNodeController.State)
            {
                case StartNodeProgressState.IntroDialogue:
                    if (enteredState)
                    {
                        CloseStartCandidateDetails();
                        _startScreen.HideCandidatePanel();
                        logWindow.SetLogText(_startNodeController.Dialogue.Greeting);
                        logWindow.ShowSingleOption(
                            "おう",
                            () => _startNodeController.AdvanceIntro());
                    }
                    break;
                case StartNodeProgressState.Selecting:
                    if (enteredState)
                    {
                        _startScreen.ShowCandidatePanel();
                        _startScreen.ShowCandidateSelection();
                        RenderStartSelection(logWindow);
                    }
                    _startScreen.SetCandidateSelections(_startNodeController.SelectedIds);
                    RefreshStartCandidateDetails();
                    break;
                case StartNodeProgressState.SelectionConfirmation:
                    if (enteredState)
                    {
                        CloseStartCandidateDetails();
                        _startScreen.ShowCandidatePanel();
                        _startScreen.SetCandidateSelections(_startNodeController.SelectedIds);
                        _startScreen.ShowCandidateConfirmation(
                            _startNodeController.SelectedIds);
                        RenderStartConfirmation(logWindow);
                    }
                    break;
                case StartNodeProgressState.FinalDialogue:
                    if (enteredState)
                    {
                        _startScreen.ShowCandidatePanel();
                        _startScreen.SetCandidateSelections(
                            _startNodeController.SelectedIds);
                        _startScreen.ShowCandidateConfirmation(
                            _startNodeController.SelectedIds);
                        logWindow.SetLogText(
                            _startNodeController.Dialogue.FinalMessage);
                        logWindow.ShowSingleOption(
                            "おう",
                            () => _startNodeController.Complete());
                    }
                    break;
                case StartNodeProgressState.Completed:
                    if (enteredState)
                    {
                        logWindow.ClearOptions();
                    }
                    break;
            }
        }

        private void RenderStartSelection(LogWindowView logWindow)
        {
            logWindow.SetLogText(_startNodeController.Dialogue.SelectionPrompt);
            logWindow.ClearOptions();
        }

        private void RenderStartConfirmation(LogWindowView logWindow)
        {
            logWindow.SetLogText(_startNodeController.Dialogue.ConfirmationPrompt);
            logWindow.ShowOptions(
                new LogWindowOption("はい", () => _startNodeController.ConfirmSelection()),
                new LogWindowOption("いいえ", () => _startNodeController.RestartSelection()));
        }

        private StartCandidateCardContent BuildStartCandidateCard(string instanceId)
        {
            var instance = Context.PachimonPool.Get(instanceId);
            var definition = instance == null
                ? null
                : Context.PachimonCatalog.Get(instance.SpeciesId);
            return new StartCandidateCardContent(
                instanceId,
                definition?.DisplayName ?? instanceId,
                definition?.FrontSprite);
        }

        private void ShowStartCandidateDetails(string instanceId)
        {
            if (_startNodeController?.State == StartNodeProgressState.Selecting
                && _startPreviewCandidateId == instanceId)
            {
                ToggleStartPreviewCandidate();
                return;
            }

            _startPreviewCandidateId = instanceId;
            _startScreen.SetFocusedCandidate(instanceId);
            RefreshStartCandidateDetails();
        }

        private void RefreshStartCandidateDetails()
        {
            if (_startNodeController == null
                || _startNodeController.State != StartNodeProgressState.Selecting
                || string.IsNullOrEmpty(_startPreviewCandidateId))
            {
                return;
            }

            var selectedIndex = _startNodeController.CandidateIds
                .ToList()
                .IndexOf(_startPreviewCandidateId);
            if (selectedIndex < 0)
            {
                CloseStartCandidateDetails();
                return;
            }

            var selectionOrder = _startNodeController.GetSelectionOrder(_startPreviewCandidateId);
            var confirmLabel = selectionOrder > 0
                ? $"{selectionOrder}匹目を取り消す"
                : $"{_startNodeController.SelectedIds.Count + 1}匹目にする";
            _rightPaneView?.ShowStartCandidateSelection(
                _startNodeController.CandidateIds
                    .Select(candidateId => BuildPachimonPreview(candidateId, true))
                    .ToArray(),
                _startNodeController.CandidateIds
                    .Select(candidateId => _startNodeController.GetSelectionOrder(candidateId) > 0)
                    .ToArray(),
                selectedIndex,
                SelectStartCandidateTab,
                confirmLabel,
                ToggleStartPreviewCandidate,
                CloseStartCandidateDetails);
        }

        private void SelectStartCandidateTab(int candidateIndex)
        {
            if (_startNodeController == null
                || candidateIndex < 0
                || candidateIndex >= _startNodeController.CandidateIds.Count)
            {
                return;
            }

            _startPreviewCandidateId = _startNodeController.CandidateIds[candidateIndex];
            _startScreen.SetFocusedCandidate(_startPreviewCandidateId);
            RefreshStartCandidateDetails();
        }

        private void ToggleStartPreviewCandidate()
        {
            if (string.IsNullOrEmpty(_startPreviewCandidateId) || _startNodeController == null)
            {
                return;
            }

            if (_startNodeController.ToggleCandidate(_startPreviewCandidateId))
            {
                _rightPaneView?.RequestMainPane();
            }
        }

        private void CloseStartCandidateDetails()
        {
            _startPreviewCandidateId = null;
            _startScreen.SetFocusedCandidate(null);
            _rightPaneView?.ClearNodeSelection();
        }

        private void ApplyCityNode(MapNode node)
        {
            if (node.Content is not CityNodeContent content)
            {
                _mainPaneView.LogWindowView?.SetLogText("シティノード");
                _mainPaneView.LogWindowView?.ShowSingleOption("次へ進む", CompleteCurrentNode);
                return;
            }

            ShowCurrentCityShop();
        }

        private void ShowCurrentCityShop(string statusMessage = null)
        {
            var currentNode = GetCurrentNode();
            if (currentNode?.Content is LeagueGateNodeContent leagueGate)
            {
                ShowCurrentLeagueGateShop(leagueGate, statusMessage);
                return;
            }
            if (currentNode?.Content is not CityNodeContent content)
            {
                return;
            }

            var message = "シティへようこそ。\nMainPaneのショップを選んでください。";
            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                message += $"\n\n{statusMessage}";
            }

            _mainPaneView.LogWindowView?.SetLogText(message);
            _mainPaneView.LogWindowView?.ClearOptions();
            _cityScreen?.Bind(
                content,
                Context.ItemCatalog,
                Context.RunState,
                BuildCityPachimonOptions(),
                statusMessage,
                ShowCityItemDetails,
                TryPurchaseCityItems,
                TryApplyCityEngravings,
                TryTeachCitySkill,
                TryForgetCitySkill,
                ShowCitySkillDetails,
                TryEquipCityItem,
                CompleteCurrentNode);
            _rightPaneView?.ShowCityShop(
                content,
                Context.ItemCatalog,
                Context.RunState,
                ShowCityItemDetails);
        }

        private IReadOnlyList<CityPachimonOption> BuildCityPachimonOptions()
        {
            return Context.RunState.PlayerPachimonIds
                .Select(instanceId => Context.PachimonPool.Get(instanceId))
                .Where(instance => instance != null)
                .Select(instance =>
                {
                    var definition = Context.PachimonCatalog.Get(instance.SpeciesId);
                    var owner = BuildPachimonPreview(instance.InstanceId, true);
                    var skills = instance.SkillSlots.Select(slot =>
                    {
                        var skill = Context.SkillCatalog.Get(slot.SkillId);
                        return new CitySkillOption(
                            slot.SlotId,
                            new PachimonAbilityPreview(
                                PachimonAbilityKind.Skill,
                                slot.SkillId,
                                SkillUpgradeMath.FormatDisplayName(
                                    skill?.DisplayName ?? $"Skill #{slot.SkillId}",
                                    slot.UpgradeLevel),
                                skill,
                                slot.UpgradeLevel),
                            owner);
                    });
                    return new CityPachimonOption(
                        instance.InstanceId,
                        definition?.DisplayName ?? $"Pachimon {instance.SpeciesId}",
                        definition?.FrontSprite,
                        skills,
                        instance.Equipment.Keys,
                        instance.Engravings.Count);
                })
                .ToArray();
        }

        private void TryPurchaseCityItems(IReadOnlyList<CityStockEntry> entries)
        {
            if (!TryValidateCityTransaction(entries, out var totalPrice, out var error))
            {
                ShowCurrentCityShop(error);
                return;
            }

            if (entries.Any(entry => Context.ItemCatalog.Get(entry.ItemId)
                    is not HealingItemAsset))
            {
                ShowCurrentCityShop("薬局の商品ではありません。");
                return;
            }

            var freeSlots = ItemInventory.Capacity - Context.RunState.ItemInventory.Count;
            if (entries.Count > freeSlots)
            {
                ShowCurrentCityShop("バッグがいっぱいだ！");
                return;
            }

            var addedItems = new List<ItemInstance>();
            foreach (var entry in entries)
            {
                if (!Context.RunState.ItemInventory.TryAdd(
                        entry.GeneratedData,
                        out var item,
                        out _))
                {
                    foreach (var added in addedItems)
                        Context.RunState.ItemInventory.TryRemove(added.InstanceId, out _);
                    ShowCurrentCityShop("Itemを追加できませんでした。");
                    return;
                }
                addedItems.Add(item);
            }

            CommitCityStock(entries, totalPrice);
            _gameRootView?.RefreshItemPanel(true);
            ShowCurrentCityShop($"{entries.Count}個の商品を購入しました。");
        }

        private void TryApplyCityEngravings(
            IReadOnlyList<CityStockEntry> entries,
            string targetInstanceId)
        {
            if (!TryValidateCityTransaction(entries, out var totalPrice, out var error)
                || entries.Any(entry => Context.ItemCatalog.Get(entry.ItemId)
                    is not EngravingItemAsset))
            {
                ShowCurrentCityShop(error ?? "刻印を適用できませんでした。");
                return;
            }

            var target = Context.PachimonPool.Get(targetInstanceId);
            if (target == null)
            {
                ShowCurrentCityShop("対象のパチモンが見つかりません。");
                return;
            }

            if (!target.CanAddEngravings(entries.Count))
            {
                ShowCurrentCityShop(
                    $"刻印は1体につき最大{PachimonInstance.MaxEngravings}個です。");
                return;
            }

            foreach (var entry in entries)
            {
                var engraving = (EngravingItemAsset)Context.ItemCatalog.Get(entry.ItemId);
                ApplyPermanentCityChanges(
                    target,
                    entry.GeneratedData.StatChanges,
                    $"city:{entry.StockId}",
                    engraving.DisplayName);
                target.RecordAppliedEngraving(
                    engraving.ItemId,
                    engraving.DisplayName,
                    entry.GeneratedData);
            }
            CommitCityStock(entries, totalPrice);
            RefreshPlayerPartyPane();
            ShowCurrentCityShop($"{entries.Count}個の刻印を適用しました。");
        }

        private void TryTeachCitySkill(CityStockEntry entry, string targetInstanceId)
        {
            if (!TryValidateCityTransaction(new[] { entry }, out var totalPrice, out var error)
                || Context.ItemCatalog.Get(entry.ItemId) is not SkillMachineItemAsset machine)
            {
                ShowCurrentCityShop(error ?? "Skillを習得できませんでした。");
                return;
            }

            var target = Context.PachimonPool.Get(targetInstanceId);
            var previousLevel = target?.SkillSlots
                .FirstOrDefault(slot => slot.SkillId == machine.SkillId)
                ?.UpgradeLevel;
            if (target == null || !target.AddSkill(machine.SkillId))
            {
                ShowCurrentCityShop("これ以上おぼえられない。");
                return;
            }

            CommitCityStock(new[] { entry }, totalPrice);
            RefreshPlayerPartyPane();
            ShowCurrentCityShop(previousLevel.HasValue
                ? $"{machine.Skill.DisplayName}を+{previousLevel.Value + 1}に強化しました。"
                : $"{machine.Skill.DisplayName}を習得しました。");
        }

        private void ShowCitySkillDetails(CitySkillOption option)
        {
            if (option == null)
            {
                return;
            }

            _gameRootView?.ShowAbilityDetails(option.Ability, option.Owner);
        }

        private void TryForgetCitySkill(
            CityStockEntry entry,
            string targetInstanceId,
            int slotId)
        {
            if (!TryValidateCityTransaction(new[] { entry }, out var totalPrice, out var error)
                || Context.ItemCatalog.Get(entry.ItemId) is not SkillForgetItemAsset)
            {
                ShowCurrentCityShop(error ?? "Skillを忘れられませんでした。");
                return;
            }

            var target = Context.PachimonPool.Get(targetInstanceId);
            var slot = target?.SkillSlots.FirstOrDefault(candidate => candidate.SlotId == slotId);
            var skill = slot == null ? null : Context.SkillCatalog.Get(slot.SkillId);
            if (target == null
                || slot == null
                || !target.TryForgetSkillSlot(slotId, out _))
            {
                ShowCurrentCityShop("忘れるSkillが見つかりませんでした。");
                return;
            }

            CommitCityStock(new[] { entry }, totalPrice);
            RefreshPlayerPartyPane();
            ShowCurrentCityShop($"{skill?.DisplayName ?? $"Skill #{slot.SkillId}"}を忘れました。");
        }

        private void TryEquipCityItem(CityStockEntry entry, string targetInstanceId)
        {
            if (!TryValidateCityTransaction(new[] { entry }, out var totalPrice, out var error)
                || Context.ItemCatalog.Get(entry.ItemId) is not EquipmentItemAsset equipment)
            {
                ShowCurrentCityShop(error ?? "装備できませんでした。");
                return;
            }

            var target = Context.PachimonPool.Get(targetInstanceId);
            if (target == null
                || !target.TryEquip(
                    equipment,
                    entry.GeneratedData,
                    $"equipment:{entry.StockId}"))
            {
                ShowCurrentCityShop("この部位はすでに装備済みです。");
                return;
            }

            CommitCityStock(new[] { entry }, totalPrice);
            RefreshPlayerPartyPane();
            ShowCurrentCityShop($"{equipment.DisplayName}を装備しました。");
        }

        private bool TryValidateCityTransaction(
            IReadOnlyList<CityStockEntry> entries,
            out int totalPrice,
            out string error)
        {
            totalPrice = 0;
            error = null;
            var availableStock = GetCurrentShopStockEntries();
            if (availableStock == null
                || entries == null
                || entries.Count == 0
                || entries.Any(entry => entry == null
                    || entry.IsPurchased
                    || !availableStock.Contains(entry)))
            {
                error = "商品が見つからないか、すでに売り切れです。";
                return false;
            }

            totalPrice = entries.Sum(entry => entry.Price);
            if (Context.RunState.Gold < totalPrice)
            {
                error = "Goldが足りません。";
                return false;
            }
            return true;
        }

        private IReadOnlyList<CityStockEntry> GetCurrentShopStockEntries()
        {
            return GetCurrentNode()?.Content switch
            {
                CityNodeContent city => city.StockEntries,
                LeagueGateNodeContent leagueGate => leagueGate.StockEntries,
                _ => null,
            };
        }

        private void CommitCityStock(
            IEnumerable<CityStockEntry> entries,
            int totalPrice)
        {
            foreach (var entry in entries) entry.TryMarkPurchased();
            Context.RunState.Gold -= totalPrice;
            ApplyHeaderState();
        }

        private void ApplyPermanentCityChanges(
            PachimonInstance target,
            IReadOnlyList<GeneratedStatChange> changes,
            string sourceId,
            string displayName)
        {
            var current = PachimonStatService.Calculate(
                target,
                Context.RunState.PlayerModifiers,
                Context.PassiveStatModifierRegistry);
            var useContext = ItemUseContext.ForRun(
                target,
                current.MaxHp,
                current.MaxMn,
                ItemTargetAffiliation.Ally,
                () => PachimonStatService.Calculate(
                    target,
                    Context.RunState.PlayerModifiers,
                    Context.PassiveStatModifierRegistry));
            useContext.ApplyPermanentStatChanges(
                changes,
                sourceId,
                displayName);
        }

        private void ShowCityItemDetails(CityStockEntry entry)
        {
            if (entry != null)
            {
                _gameRootView?.ShowItemDetails(
                    entry.ItemId,
                    entry.GeneratedData);
            }
        }

        private void ApplyBattleNode(MapNode node)
        {
            if (node.Content is not BattleNodeContent content)
            {
                return;
            }

            StartBattle(
                node,
                content.EnemyPachimonInstanceIds,
                content.TrainerProfile,
                content.NodeReward);
        }

        private void ApplyGymNode(MapNode node)
        {
            if (node.Content is not GymNodeContent content)
            {
                return;
            }

            StartBattle(
                node,
                content.EnemyPachimonInstanceIds,
                content.TrainerProfile,
                content.NodeReward);
        }

        private void ApplyEliteNode(MapNode node)
        {
            if (node.Content is not EliteNodeContent content)
            {
                return;
            }

            StartBattle(
                node,
                content.EnemyPachimonInstanceIds,
                content.TrainerProfile,
                null);
        }

        private void ApplyHallOfFameNode(MapNode node)
        {
            if (node.Content is not HallOfFameNodeContent)
            {
                return;
            }

            _mainPaneView.LogWindowView?.SetLogText(
                "殿堂入り演出後、チャンピオンズロードに進む（未実装）");
            _mainPaneView.LogWindowView?.ClearOptions();
            _hallOfFameScreen?.Present(ReturnToTitleFromHallOfFame);
        }

        private void ReturnToTitleFromHallOfFame()
        {
            Context.RunState.IsRunFinished = true;
            _gameRootView?.FadeToTitleScene();
        }

        private void StartBattle(
            MapNode node,
            IReadOnlyList<string> enemyPachimonIds,
            TrainerProfile enemyTrainerProfile,
            NodeReward nodeReward)
        {
            if (!Context.RunState.IsPartyConfirmed)
            {
                _mainPaneView.LogWindowView?.SetLogText(
                    "Player Partyが確定していないためBattleを開始できません。");
                _mainPaneView.LogWindowView?.ClearOptions();
                return;
            }

            var stateFactory = new BattleStateFactory(
                Context.PachimonPool,
                Context.PachimonCatalog,
                Context.SkillCatalog,
                Context.PassiveCatalog,
                Context.PassiveStatModifierRegistry);
            var enemyModifiers = EnemyTrainerModifierFactory.Create(node);
            var battleState = stateFactory.Create(
                CreateBattleSeed(node),
                Context.RunState.PlayerPachimonIds,
                enemyPachimonIds,
                Context.RunState.PlayerModifiers,
                enemyModifiers);
            var enemyTrainerPreview = BuildTrainerPreview(
                enemyTrainerProfile,
                nodeReward,
                node.RowIndex,
                enemyModifiers);
            _activeBattleState = battleState;
            _activeEnemyTrainerPreview = enemyTrainerPreview;
            RefreshBattlePanes(battleState, enemyTrainerPreview);
            _battleScreen.BeginBattle(
                battleState,
                new BattleSkillRuntime(
                    Context.SkillCatalog,
                    Context.PassiveCatalog),
                _mainPaneView.LogWindowView,
                Context.PachimonCatalog,
                BuildPlayerTrainerPreview().Graphic,
                enemyTrainerPreview.Graphic,
                $"{enemyTrainerPreview.DisplayName}が勝負をしかけてきた",
                state => RefreshBattlePanes(state, enemyTrainerPreview),
                FocusBattlePaneUnit,
                _gameRootView.ShowFieldEffectDetails,
                _gameRootView.ShowWeatherDetails,
                outcome => CompleteBattle(outcome, battleState, nodeReward));
        }

        private void RefreshBattlePanes(
            BattleState battleState,
            TrainerPreviewContent enemyTrainerPreview)
        {
            if (battleState == null)
            {
                return;
            }

            var leftSelectedTab = _leftPaneView?.PartyWindow?.SelectedTabIndex ?? 0;
            var rightWindow = _rightPaneView?.NodeSelectionWindow?.BattleWindow;
            var rightSelectedTab = rightWindow?.SelectedTabIndex ?? 0;
            _rightPaneShowsActiveBattle = true;
            _leftPaneView?.ShowPlayerParty(
                BuildPlayerTrainerPreview(),
                battleState.Player.Units
                    .Select(unit => PachimonPreviewFactory.FromBattleUnit(
                        unit,
                        Context.PachimonCatalog,
                        Context.SkillCatalog,
                        Context.PassiveCatalog,
                        Context.PachimonPool.Get(unit.InstanceId)))
                    .ToArray());
            _rightPaneView?.ShowBattleStatus(
                enemyTrainerPreview,
                battleState.Enemy.Units
                    .Select(unit => PachimonPreviewFactory.FromBattleUnit(
                        unit,
                        Context.PachimonCatalog,
                        Context.SkillCatalog,
                        Context.PassiveCatalog,
                        Context.PachimonPool.Get(unit.InstanceId)))
                    .ToArray());
            _leftPaneView?.PartyWindow?.ShowTab(leftSelectedTab);
            rightWindow?.ShowTab(rightSelectedTab);
        }

        private void FocusBattlePaneUnit(BattleUnitState unit)
        {
            if (unit == null)
            {
                return;
            }

            var tabIndex = unit.SlotIndex + 1;
            if (unit.Side == BattleSide.Player)
            {
                _leftPaneView?.PartyWindow?.ShowTab(tabIndex);
                return;
            }

            _rightPaneView?.NodeSelectionWindow?.BattleWindow?.ShowTab(tabIndex);
        }

        private void CompleteBattle(
            BattleOutcome outcome,
            BattleState battleState,
            NodeReward nodeReward)
        {
            if (outcome != BattleOutcome.PlayerVictory)
            {
                _activeBattleState = null;
                _activeEnemyTrainerPreview = null;
                _rightPaneShowsActiveBattle = false;
                Context.RunState.IsRunFinished = true;
                _gameRootView?.FadeToTitleScene();
                return;
            }

            BattleResultCommitter.CommitPlayerResources(
                BattleResult.From(battleState),
                Context.PachimonPool);
            _activeBattleState = null;
            _activeEnemyTrainerPreview = null;
            _rightPaneShowsActiveBattle = false;
            RefreshPlayerPartyPane();

            if (nodeReward == null || _battleScreen.RewardOverlayView == null)
            {
                CompleteCurrentNode();
                return;
            }

            BeginBattleReward(battleState, nodeReward);
        }

        private void BeginBattleReward(
            BattleState battleState,
            NodeReward nodeReward)
        {
            var session = new BattleRewardSession(
                Context.RunState,
                Context.PachimonPool,
                nodeReward,
                Context.PassiveStatModifierRegistry);
            var sources = battleState.Enemy.Units
                .Select(BuildRewardSource)
                .ToArray();
            var targets = Context.RunState.PlayerPachimonIds
                .Select(Context.PachimonPool.Get)
                .Where(instance => instance != null)
                .Select(instance =>
                {
                    var definition = Context.PachimonCatalog.Get(instance.SpeciesId);
                    return new RewardTargetPachimonContent(
                        instance.InstanceId,
                        definition?.DisplayName ?? $"Pachimon #{instance.SpeciesId}",
                        definition?.FrontSprite);
                })
                .ToArray();
            var itemChoices = new[]
            {
                BuildRewardItemChoice(ItemIds.Potion),
                BuildRewardItemChoice(ItemIds.MnPotion),
            };

            _mainPaneView.LogWindowView?.ClearOptions();
            _battleScreen.RewardOverlayView.Present(
                new RewardOverlayContent(
                    nodeReward.Gold,
                    session.UsesBadge,
                    sources,
                    targets,
                    itemChoices,
                    slot => ClaimImmediateReward(session, slot),
                    (kind, rewardId, targetId) =>
                        CanGrantPachimonReward(session, kind, rewardId, targetId),
                    (kind, rewardId, targetId) =>
                        GrantPachimonReward(session, kind, rewardId, targetId),
                    session.CanClaimItem,
                    itemId => ClaimRewardItem(session, itemId),
                    choice => _gameRootView?.ShowAbilityDetails(
                        choice.Ability,
                        choice.Owner),
                    session.AbandonRemaining,
                    () =>
                    {
                        if (!session.IsComplete)
                        {
                            return;
                        }

                        ApplyHeaderState();
                        RefreshPlayerPartyPane();
                        CompleteCurrentNode();
                    }));
        }

        private RewardItemChoiceContent BuildRewardItemChoice(int itemId)
        {
            var item = Context.ItemCatalog.Get(itemId);
            return new RewardItemChoiceContent(
                itemId,
                item?.DisplayName ?? $"Item #{itemId}",
                BattleRewardSession.RewardItemRecoveryAmount);
        }

        private RewardSourcePachimonContent BuildRewardSource(BattleUnitState unit)
        {
            var definition = Context.PachimonCatalog.Get(unit.SpeciesId);
            var owner = PachimonPreviewFactory.FromBattleUnit(
                unit,
                Context.PachimonCatalog,
                Context.SkillCatalog,
                Context.PassiveCatalog,
                Context.PachimonPool.Get(unit.InstanceId));
            return new RewardSourcePachimonContent(
                unit.DisplayName,
                definition?.FrontSprite,
                unit.SkillIds.Select(skillId =>
                {
                    var skill = Context.SkillCatalog.Get(skillId);
                    return new RewardChoiceContent(
                        skillId,
                        skill?.DisplayName ?? $"Skill #{skillId}",
                        new PachimonAbilityPreview(
                            PachimonAbilityKind.Skill,
                            skillId,
                            skill?.DisplayName ?? $"Skill #{skillId}",
                            skill),
                        owner);
                }),
                unit.PassiveIds.Select(passiveId =>
                {
                    var displayName = Context.PassiveCatalog.Get(passiveId)
                        is { } statDefinition
                            ? statDefinition.DisplayName
                            : AttributePlaceholderName.FromCyclicId(passiveId);
                    return new RewardChoiceContent(
                        passiveId,
                        displayName,
                        new PachimonAbilityPreview(
                            PachimonAbilityKind.Passive,
                            passiveId,
                            displayName),
                        owner);
                }));
        }

        private bool ClaimImmediateReward(
            BattleRewardSession session,
            BattleRewardSlot slot)
        {
            var claimed = slot switch
            {
                BattleRewardSlot.Gold => session.ClaimGold(),
                BattleRewardSlot.Secondary => session.ClaimSecondary(),
                _ => false,
            };
            if (!claimed)
            {
                return false;
            }

            ApplyHeaderState();
            RefreshPlayerPartyPane();
            return true;
        }

        private static bool CanGrantPachimonReward(
            BattleRewardSession session,
            RewardSelectionKind kind,
            int rewardId,
            string targetInstanceId)
        {
            return kind == RewardSelectionKind.Skill
                ? session.CanGrantSkill(rewardId, targetInstanceId)
                : session.CanGrantPassive(rewardId, targetInstanceId);
        }

        private bool GrantPachimonReward(
            BattleRewardSession session,
            RewardSelectionKind kind,
            int rewardId,
            string targetInstanceId)
        {
            var granted = kind == RewardSelectionKind.Skill
                ? session.GrantSkill(rewardId, targetInstanceId)
                : session.GrantPassive(rewardId, targetInstanceId);
            if (granted)
            {
                RefreshPlayerPartyPane();
            }

            return granted;
        }

        private bool ClaimRewardItem(
            BattleRewardSession session,
            int itemId)
        {
            if (!session.ClaimItem(itemId))
            {
                return false;
            }

            _gameRootView?.RefreshItemPanel(true);
            return true;
        }

        private int CreateBattleSeed(MapNode node)
        {
            unchecked
            {
                var hash = Context.RunState.RunSeed * 397;
                var nodeId = node?.NodeId ?? string.Empty;
                foreach (var character in nodeId)
                {
                    hash = hash * 31 + character;
                }

                return hash;
            }
        }

        private string FormatTrainer(TrainerProfile profile)
        {
            var style = Context.TrainerStyleCatalog.Get(profile.StyleId);
            var trainerName = Context.TrainerNameCatalog.Get(profile.NameId);
            var title = profile.Role switch
            {
                TrainerRole.Normal => style?.NormalTitle ?? "Trainer",
                TrainerRole.GymLeader => "Gym Leader",
                TrainerRole.Elite => "Elite Four",
                _ => "Trainer",
            };
            return $"{title}: {trainerName?.DisplayName ?? profile.NameId} "
                + $"[{style?.Theme} / {style?.Gender}]";
        }

        private static string FormatRewardElements(NodeReward reward)
        {
            return reward?.Elements == null || reward.Elements.Count == 0
                ? "None"
                : string.Join(" / ", reward.Elements.Select(FormatRewardElement));
        }

        private static string FormatRewardElement(RewardElement element)
        {
            return element.Kind switch
            {
                RewardElementKind.Attribute => element.Attribute.ToString(),
                _ => element.Kind.ToString(),
            };
        }

        private void ApplyRestSpotNode(MapNode node)
        {
            if (node.Content is not RestSpotNodeContent content)
            {
                _mainPaneView.LogWindowView?.SetLogText(
                    "RestSpotの回復情報を読み込めませんでした。");
                _mainPaneView.LogWindowView?.ClearOptions();
                return;
            }

            _mainPaneView.LogWindowView?.SetLogText(
                $"パチモンを休ませますか？\n最大HP・MNの{content.HealPercent}%を回復します。");
            _mainPaneView.LogWindowView?.ShowSingleOption(
                "休む",
                () => RecoverAtRestSpot(content));
        }

        private void RecoverAtRestSpot(RestSpotNodeContent content)
        {
            _mainPaneView.LogWindowView?.ClearOptions();
            var result = RestSpotRecoveryService.RecoverPlayerParty(
                Context.RunState,
                Context.PachimonPool,
                Context.PassiveStatModifierRegistry,
                content.HealPercent);
            RefreshPlayerPartyPane();

            var resultMessage = result.RecoveredPachimonCount == 0
                ? "パチモンたちは元気いっぱいだ！"
                : result.RevivedPachimonCount > 0
                    ? $"パチモンたちのHP・MNが回復した！\n"
                        + $"{result.RevivedPachimonCount}匹が戦闘に戻れるようになった！"
                    : "パチモンたちのHP・MNが回復した！";
            _mainPaneView.LogWindowView?.SetLogText(resultMessage);
            _mainPaneView.LogWindowView?.ShowSingleOption(
                "おう",
                CompleteCurrentNode);
        }

        private void ApplyLeagueGateNode(MapNode node)
        {
            if (node.Content is not LeagueGateNodeContent content)
            {
                _mainPaneView.LogWindowView?.SetLogText("リーグゲート");
                _mainPaneView.LogWindowView?.ShowSingleOption("挑戦する", CompleteCurrentNode);
                return;
            }

            if (Context.RunState.BadgeCount >= content.RequiredBadgeCount)
            {
                _leagueGateScreen?.HideProfessor();
                _mainPaneView.Show(_cityScreen);
                ShowCurrentLeagueGateShop(content);
                return;
            }

            _mainPaneView.Show(_leagueGateScreen);
            _leagueGateScreen?.ShowProfessor();
            _rightPaneView?.ClearNodeSelection();
            _mainPaneView.LogWindowView?.ClearOptions();
            _mainPaneView.LogWindowView?.PlayDialoguePage(
                new DialoguePage(new[]
                {
                    new DialogueBlock(new[]
                    {
                        new DialogueLine(
                            $"集めたバッジの数は・・・\n・・・{Context.RunState.BadgeCount}個じゃな。"),
                    }),
                    new DialogueBlock(new[]
                    {
                        new DialogueLine("もう、家に帰りなさい。"),
                    }),
                    new DialogueBlock(new[]
                    {
                        new DialogueLine(
                            $"{Context.RunState.PlayerName}は目の前が真っ暗になった"),
                    }),
                }),
                CompleteLeagueGateDefeat);
        }

        private void ShowCurrentLeagueGateShop(
            LeagueGateNodeContent content,
            string statusMessage = null)
        {
            if (content == null) return;

            var shopContent = new CityNodeContent(
                "league_gate",
                content.ShopSeed,
                content.StockEntries);
            var message = "リーグゲートへようこそ。\n準備を整えて四天王に挑んでください。";
            if (!string.IsNullOrWhiteSpace(statusMessage))
                message += $"\n\n{statusMessage}";
            _mainPaneView.LogWindowView?.SetLogText(message);
            _mainPaneView.LogWindowView?.ClearOptions();
            _cityScreen?.Bind(
                shopContent,
                Context.ItemCatalog,
                Context.RunState,
                BuildCityPachimonOptions(),
                statusMessage,
                ShowCityItemDetails,
                TryPurchaseCityItems,
                TryApplyCityEngravings,
                TryTeachCitySkill,
                TryForgetCitySkill,
                ShowCitySkillDetails,
                TryEquipCityItem,
                CompleteCurrentNode,
                CityShopConfiguration.LeagueGate);
            _rightPaneView?.ShowCityShop(
                shopContent,
                Context.ItemCatalog,
                Context.RunState,
                ShowCityItemDetails);
        }

        private void CompleteLeagueGateDefeat()
        {
            Context.RunState.IsRunFinished = true;
            _mainPaneView.LogWindowView?.ClearOptions();
            _gameRootView?.FadeToTitleScene();
        }

        private void ApplyFallbackNode(MapNode node)
        {
            _mainPaneView.LogWindowView?.SetLogText($"未定義ノード: {node.NodeType}");
            _mainPaneView.LogWindowView?.ShowSingleOption("次へ進む", CompleteCurrentNode);
        }

        private void ApplyPlaceholderNode(MapNode node, string label)
        {
            _mainPaneView.LogWindowView?.SetLogText(
                $"{label}\nrow: {node.RowIndex}, column: {node.ColumnIndex}\n本処理は後のバージョンで実装する。");
            _mainPaneView.LogWindowView?.ShowSingleOption("次へ進む", CompleteCurrentNode);
        }

        private void CompleteCurrentNode()
        {
            var currentNode = GetCurrentNode();
            if (currentNode == null)
            {
                return;
            }

            ResolveCurrentLocation(currentNode);
            var outgoingNodeIds = GetCurrentOutgoingNodeIds();
            _canMoveToNextNode = outgoingNodeIds.Count > 0;
            ApplyMapOverlayState();

            if (_canMoveToNextNode)
            {
                _mapOverlayView?.Open();
            }
        }

        private IReadOnlyList<string> GetCurrentOutgoingNodeIds()
        {
            var currentNode = GetCurrentNode();
            if (currentNode == null)
            {
                return System.Array.Empty<string>();
            }

            var group = Context.RunMap.GetNodeGroupForNode(currentNode.NodeId);
            var sourceNodeIds = group?.NodeIds ?? new[] { currentNode.NodeId };
            return sourceNodeIds
                .Select(Context.RunMap.GetNode)
                .Where(node => node != null)
                .SelectMany(node => node.NextNodeIds)
                .Distinct()
                .OrderBy(nodeId => Context.RunMap.GetNode(nodeId)?.ColumnIndex ?? int.MaxValue)
                .ToArray();
        }

        private void ResolveCurrentLocation(MapNode currentNode)
        {
            var group = Context.RunMap.GetNodeGroupForNode(currentNode.NodeId);
            if (group == null)
            {
                Context.RunState.ResolvedNodeIds.Add(currentNode.NodeId);
                return;
            }

            foreach (var nodeId in group.NodeIds)
            {
                Context.RunState.ResolvedNodeIds.Add(nodeId);
            }
        }

    }
}
