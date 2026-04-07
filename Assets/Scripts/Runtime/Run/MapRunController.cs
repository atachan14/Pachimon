using Pachimon.Map;
using Pachimon.UI;

namespace Pachimon.Run
{
    public sealed class MapRunController
    {
        private readonly HeaderView _headerView;
        private readonly MainPaneView _mainPaneView;
        private readonly StartScreen _startScreen;
        private readonly BattleScreen _battleScreen;
        private readonly CityScreen _cityScreen;
        private readonly RestSpotScreen _restSpotScreen;
        private readonly LeagueGateScreen _leagueGateScreen;

        public MapRunController(
            HeaderView headerView,
            MainPaneView mainPaneView,
            StartScreen startScreen,
            BattleScreen battleScreen,
            CityScreen cityScreen,
            RestSpotScreen restSpotScreen,
            LeagueGateScreen leagueGateScreen)
        {
            _headerView = headerView;
            _mainPaneView = mainPaneView;
            _startScreen = startScreen;
            _battleScreen = battleScreen;
            _cityScreen = cityScreen;
            _restSpotScreen = restSpotScreen;
            _leagueGateScreen = leagueGateScreen;
        }

        public RunContext Context { get; private set; }

        public void StartRun(RunContext context)
        {
            Context = context;
            Context.RunState.CurrentNodeId = Context.RunMap.StartNodeId;
            ApplyHeaderState();
            ShowCurrentNode();
        }

        public MapNode GetCurrentNode()
        {
            return Context?.RunMap?.GetNode(Context.RunState.CurrentNodeId);
        }

        public bool TryMoveToNextNode()
        {
            var currentNode = GetCurrentNode();
            if (currentNode == null || currentNode.NextNodeIds.Count == 0)
            {
                return false;
            }

            currentNode.IsResolved = true;
            Context.RunState.ResolvedNodeIds.Add(currentNode.NodeId);
            Context.RunState.CurrentNodeId = currentNode.NextNodeIds[0];
            ApplyHeaderState();
            ShowCurrentNode();
            return true;
        }

        private void ApplyHeaderState()
        {
            if (_headerView.GoldText != null)
            {
                _headerView.GoldText.text = $"Gold: {Context.RunState.Gold}";
            }

            var currentNode = GetCurrentNode();
            if (_headerView.StageText != null)
            {
                var stage = currentNode != null ? currentNode.RowIndex : 0;
                _headerView.StageText.text = $"Stage: {stage}";
            }

            if (_headerView.BadgeText != null)
            {
                _headerView.BadgeText.text = $"Badges: {Context.RunState.BadgeCount}";
            }
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
                    break;
                case NodeType.Battle:
                    _mainPaneView.Show(_battleScreen);
                    break;
                case NodeType.City:
                    _mainPaneView.Show(_cityScreen);
                    break;
                case NodeType.RestSpot:
                    _mainPaneView.Show(_restSpotScreen);
                    break;
                case NodeType.LeagueGate:
                    _mainPaneView.Show(_leagueGateScreen);
                    break;
                default:
                    _mainPaneView.Show(_startScreen);
                    break;
            }
        }
    }
}
