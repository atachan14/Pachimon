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
            string playerName,
            RunStartupProfile startupProfile)
        {
            if (startupProfile == null)
            {
                throw new System.ArgumentNullException(nameof(startupProfile));
            }
            var profileErrors = startupProfile.ValidateContent(itemCatalog);
            if (profileErrors.Count > 0)
            {
                throw new System.InvalidOperationException(
                    $"Run Startup Profile '{startupProfile.name}' is invalid: "
                    + string.Join(" ", profileErrors));
            }

            var runSeed = Random.Range(100000, 999999);
            var passiveStatModifierRegistry =
                new PassiveStatModifierRegistry(passiveCatalog);

            var runState = new RunState(runSeed, playerName)
            {
                Gold = startupProfile.StartingGold,
            };
            AddStartingItems(runState, startupProfile);

            var pachimonPoolGenerator = new RunPachimonPoolGenerator(pachimonCatalog, skillCatalog);
            var pachimonPool = pachimonPoolGenerator.Generate(runSeed);

            var mapGenerator = new MapGenerator(
                skillCatalog,
                itemCatalog,
                trainerStyleCatalog,
                trainerNameCatalog,
                passiveStatModifierRegistry);
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

        private static void AddStartingItems(
            RunState runState,
            RunStartupProfile startupProfile)
        {
            for (var slotIndex = 0;
                 slotIndex < startupProfile.StartingItems.Count;
                 slotIndex++)
            {
                var item = startupProfile.StartingItems[slotIndex];
                if (item == null)
                {
                    continue;
                }
                if (!runState.ItemInventory.TryAddAt(
                        slotIndex,
                        item.ItemId,
                        out _))
                {
                    throw new System.InvalidOperationException(
                        $"Could not add Starting Item "
                        + $"'{item.DisplayName}' to Slot {slotIndex + 1}.");
                }
            }
        }
    }
}
