using Pachimon.Map;
using Pachimon.UI;
using UnityEngine;

namespace Pachimon.Run
{
    public sealed class RunBootstrap
    {
        public RunContext CreateContext(
            HeaderView headerView,
            MainPaneView mainPaneView,
            StartScreen startScreen,
            BattleScreen battleScreen,
            CityScreen cityScreen,
            RestSpotScreen restSpotScreen,
            LeagueGateScreen leagueGateScreen)
        {
            var runSeed = Random.Range(100000, 999999);

            var runState = new RunState(runSeed)
            {
                Gold = 100,
                BadgeCount = 0,
            };

            var mapGenerator = new MapGenerator();
            var runMap = mapGenerator.Generate(runSeed);

            var mapRunController = new MapRunController(
                headerView,
                mainPaneView,
                startScreen,
                battleScreen,
                cityScreen,
                restSpotScreen,
                leagueGateScreen);

            var context = new RunContext(runState, runMap, mapRunController);
            mapRunController.StartRun(context);
            return context;
        }
    }
}
