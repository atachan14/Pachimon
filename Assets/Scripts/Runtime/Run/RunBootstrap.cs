using Pachimon.Map;
using Pachimon.UI;
using Pachimon.Data;
using Pachimon.Trainer;
using Pachimon.Skills;
using Pachimon.Items;
using Pachimon.Passives;
using UnityEngine;

namespace Pachimon.Run
{
    public sealed class RunBootstrap
    {
        public RunContext CreateContext(
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
            PachimonCatalog pachimonCatalog,
            SkillCatalog skillCatalog,
            PassiveCatalog passiveCatalog,
            ItemCatalog itemCatalog,
            TrainerStyleCatalog trainerStyleCatalog,
            TrainerNameCatalog trainerNameCatalog,
            string playerName)
        {
            var runSeed = Random.Range(100000, 999999);
            var passiveStatModifierRegistry =
                new PassiveStatModifierRegistry(passiveCatalog);

            var runState = new RunState(runSeed, playerName)
            {
                Gold = 100,
            };

            var pachimonPoolGenerator = new RunPachimonPoolGenerator(pachimonCatalog, skillCatalog);
            var pachimonPool = pachimonPoolGenerator.Generate(runSeed);

            var mapGenerator = new MapGenerator(
                skillCatalog,
                itemCatalog,
                trainerStyleCatalog,
                trainerNameCatalog);
            var runMap = mapGenerator.Generate(runSeed, pachimonPool);

            var mapRunController = new MapRunController(
                gameRootView,
                headerView,
                leftPaneView,
                mainPaneView,
                rightPaneView,
                mapOverlayView,
                startScreen,
                battleScreen,
                cityScreen,
                restSpotScreen,
                leagueGateScreen);

            var context = new RunContext(
                pachimonPool,
                runState,
                runMap,
                pachimonCatalog,
                skillCatalog,
                passiveCatalog,
                passiveStatModifierRegistry,
                itemCatalog,
                trainerStyleCatalog,
                trainerNameCatalog,
                mapRunController);
            mapRunController.StartRun(context);
            return context;
        }
    }
}
