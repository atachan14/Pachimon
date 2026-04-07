namespace Pachimon.Map
{
    public sealed class MapGenerator
    {
        public RunMap Generate(int runSeed)
        {
            var map = new RunMap();

            var row0 = new MapRow(0);
            var row1 = new MapRow(1);
            var row2 = new MapRow(2);
            var row3 = new MapRow(3);
            var row36 = new MapRow(36);

            var startNode = new MapNode(
                "start_0",
                0,
                0,
                NodeType.Start,
                new StartNodeContent(
                    new[]
                    {
                        "pachi_fire_001",
                        "pachi_water_001",
                        "pachi_leaf_001",
                    },
                    3));

            var battleNode = new MapNode(
                "battle_1_0",
                1,
                0,
                NodeType.Battle,
                new BattleNodeContent(runSeed + 101, 25));

            var restSpotNode = new MapNode(
                "rest_2_0",
                2,
                0,
                NodeType.RestSpot,
                new RestSpotNodeContent(20));

            var cityNode = new MapNode(
                "city_3_0",
                3,
                0,
                NodeType.City,
                new CityNodeContent(runSeed + 301));

            var leagueGateNode = new MapNode(
                "league_36_0",
                36,
                0,
                NodeType.LeagueGate,
                new LeagueGateNodeContent(8, "special_defeat"));

            startNode.NextNodeIds.Add(battleNode.NodeId);
            battleNode.NextNodeIds.Add(restSpotNode.NodeId);
            restSpotNode.NextNodeIds.Add(cityNode.NodeId);
            cityNode.NextNodeIds.Add(leagueGateNode.NodeId);

            row0.NodeIds.Add(startNode.NodeId);
            row1.NodeIds.Add(battleNode.NodeId);
            row2.NodeIds.Add(restSpotNode.NodeId);
            row3.NodeIds.Add(cityNode.NodeId);
            row36.NodeIds.Add(leagueGateNode.NodeId);

            map.Rows.Add(row0);
            map.Rows.Add(row1);
            map.Rows.Add(row2);
            map.Rows.Add(row3);
            map.Rows.Add(row36);

            map.Nodes.Add(startNode.NodeId, startNode);
            map.Nodes.Add(battleNode.NodeId, battleNode);
            map.Nodes.Add(restSpotNode.NodeId, restSpotNode);
            map.Nodes.Add(cityNode.NodeId, cityNode);
            map.Nodes.Add(leagueGateNode.NodeId, leagueGateNode);
            map.StartNodeId = startNode.NodeId;

            return map;
        }
    }
}
