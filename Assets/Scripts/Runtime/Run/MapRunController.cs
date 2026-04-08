using System.Text;
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
                    ApplyStartNode(currentNode);
                    break;
                case NodeType.Battle:
                    _mainPaneView.Show(_battleScreen);
                    _battleScreen.ConfigureLogWindow(_mainPaneView.LogWindowView, TryAdvanceRun);
                    break;
                case NodeType.City:
                    _mainPaneView.Show(_cityScreen);
                    ApplyCityNode(currentNode);
                    break;
                case NodeType.RestSpot:
                    _mainPaneView.Show(_restSpotScreen);
                    ApplyRestSpotNode(currentNode);
                    break;
                case NodeType.LeagueGate:
                    _mainPaneView.Show(_leagueGateScreen);
                    ApplyLeagueGateNode(currentNode);
                    break;
                default:
                    _mainPaneView.Show(_startScreen);
                    ApplyFallbackNode(currentNode);
                    break;
            }
        }

        private void ApplyStartNode(MapNode node)
        {
            if (node.Content is not StartNodeContent content)
            {
                _mainPaneView.LogWindowView?.SetLogText("row:0 の開始ノード");
                _mainPaneView.LogWindowView?.ShowSingleOption("次へ進む", TryAdvanceRun);
                return;
            }

            var builder = new StringBuilder();
            builder.AppendLine("初期パチモン候補");

            foreach (var candidate in content.CandidatePachimonIds)
            {
                builder.Append("- ").AppendLine(candidate);
            }

            builder.AppendLine();
            builder.Append("この run では ")
                .Append(content.SelectionCount)
                .Append(" 体を初期 skill 付きで選ぶ想定");

            _mainPaneView.LogWindowView?.SetLogText(builder.ToString().TrimEnd());
            _mainPaneView.LogWindowView?.ShowSingleOption("次へ進む", TryAdvanceRun);
        }

        private void ApplyCityNode(MapNode node)
        {
            if (node.Content is not CityNodeContent content)
            {
                _mainPaneView.LogWindowView?.SetLogText("シティノード");
                _mainPaneView.LogWindowView?.ShowSingleOption("次へ進む", TryAdvanceRun);
                return;
            }

            _mainPaneView.LogWindowView?.SetLogText(
                $"City Node\nショップ seed: {content.ShopSeed}\nここでは今後、shop inventory を表示する。");
            _mainPaneView.LogWindowView?.ShowSingleOption("次へ進む", TryAdvanceRun);
        }

        private void ApplyRestSpotNode(MapNode node)
        {
            if (node.Content is not RestSpotNodeContent content)
            {
                _mainPaneView.LogWindowView?.SetLogText("回復ノード");
                _mainPaneView.LogWindowView?.ShowSingleOption("次へ進む", TryAdvanceRun);
                return;
            }

            _mainPaneView.LogWindowView?.SetLogText(
                $"Rest Spot\n最大体力の {content.HealValue}% 回復する仮仕様。");
            _mainPaneView.LogWindowView?.ShowSingleOption("回復して進む", TryAdvanceRun);
        }

        private void ApplyLeagueGateNode(MapNode node)
        {
            if (node.Content is not LeagueGateNodeContent content)
            {
                _mainPaneView.LogWindowView?.SetLogText("リーグゲート");
                _mainPaneView.LogWindowView?.ShowSingleOption("挑戦する", TryAdvanceRun);
                return;
            }

            _mainPaneView.LogWindowView?.SetLogText(
                $"League Gate\n必要 badge 数: {content.RequiredBadgeCount}\n未達時: {content.FailureMode}");
            _mainPaneView.LogWindowView?.ShowSingleOption("挑戦する", TryAdvanceRun);
        }

        private void ApplyFallbackNode(MapNode node)
        {
            _mainPaneView.LogWindowView?.SetLogText($"未定義ノード: {node.NodeType}");
            _mainPaneView.LogWindowView?.ShowSingleOption("次へ進む", TryAdvanceRun);
        }

        private void TryAdvanceRun()
        {
            TryMoveToNextNode();
        }
    }
}
