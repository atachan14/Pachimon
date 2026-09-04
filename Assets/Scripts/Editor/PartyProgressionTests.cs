using System.Linq;
using NUnit.Framework;
using Pachimon.Data;
using Pachimon.Items;
using Pachimon.Map;
using Pachimon.Passives;
using Pachimon.Run;
using Pachimon.Skills;
using Pachimon.Trainer;
using UnityEditor;

namespace Pachimon.Editor.Tests
{
    public sealed class PartyProgressionTests
    {
        [TestCase(0, 1)]
        [TestCase(10, 1)]
        [TestCase(11, 2)]
        [TestCase(20, 2)]
        [TestCase(21, 3)]
        [TestCase(41, 3)]
        public void PartySizeMatchesMapProgression(int rowIndex, int expected)
        {
            Assert.That(
                PartyProgressionRules.GetPartySizeForRow(rowIndex),
                Is.EqualTo(expected));
        }

        [Test]
        public void RunStateBuildsPartyOneMemberAtATime()
        {
            var runState = new RunState(123, "test");

            Assert.That(runState.TrySetInitialParty(new[] { "first" }), Is.True);
            Assert.That(runState.IsPartyInitialized, Is.True);
            Assert.That(runState.IsPartyFull, Is.False);
            Assert.That(runState.TryAddPartyMember("second"), Is.True);
            Assert.That(runState.TryAddPartyMember("third"), Is.True);
            Assert.That(runState.IsPartyFull, Is.True);
            Assert.That(runState.PlayerPachimonIds,
                Is.EqualTo(new[] { "first", "second", "third" }));
            Assert.That(runState.TryAddPartyMember("fourth"), Is.False);
        }

        [Test]
        public void RunStateRejectsLegacyThreeMemberInitialParty()
        {
            var runState = new RunState(123, "test");

            Assert.That(
                runState.TrySetInitialParty(new[] { "first", "second", "third" }),
                Is.False);
            Assert.That(runState.IsPartyInitialized, Is.False);
        }

        [Test]
        public void StartControllerCommitsOneSelectedCandidate()
        {
            string committedId = null;
            var completed = false;
            var controller = new StartNodeController(
                new[] { "a", "b", "c" },
                1,
                new StartDialogueData("g", "s", "c", "f"),
                ids =>
                {
                    committedId = ids[0];
                    return true;
                },
                () => completed = true);

            Assert.That(controller.AdvanceIntro(), Is.True);
            Assert.That(controller.ToggleCandidate("b"), Is.True);
            Assert.That(controller.State,
                Is.EqualTo(StartNodeProgressState.SelectionConfirmation));
            Assert.That(controller.ConfirmSelection(), Is.True);
            Assert.That(committedId, Is.EqualTo("b"));
            Assert.That(controller.Complete(), Is.True);
            Assert.That(completed, Is.True);
        }

        [TestCase(123456)]
        [TestCase(456789)]
        [TestCase(987654)]
        public void GeneratedMapFollowsPartyProgression(int runSeed)
        {
            var pachimonCatalog = LoadAsset<PachimonCatalog>(
                "Assets/GameData/Pachimon/PachimonCatalog.asset");
            var skillCatalog = LoadAsset<SkillCatalog>(
                "Assets/GameData/Skill/SkillCatalog.asset");
            var passiveCatalog = LoadAsset<PassiveCatalog>(
                "Assets/GameData/Passive/PassiveCatalog.asset");
            var itemCatalog = LoadAsset<ItemCatalog>(
                "Assets/GameData/Item/ItemCatalog.asset");
            var trainerStyleCatalog = LoadAsset<TrainerStyleCatalog>(
                "Assets/GameData/Trainer/TrainerStyleCatalog.asset");
            var trainerNameCatalog = LoadAsset<TrainerNameCatalog>(
                "Assets/GameData/Trainer/TrainerNameCatalog.asset");
            var pool = new RunPachimonPoolGenerator(
                pachimonCatalog,
                skillCatalog).Generate(runSeed);
            var map = new MapGenerator(
                skillCatalog,
                itemCatalog,
                trainerStyleCatalog,
                trainerNameCatalog,
                new PassiveStatModifierRegistry(passiveCatalog)).Generate(runSeed, pool);

            var start = (StartNodeContent)map.GetNode(map.StartNodeId).Content;
            Assert.That(start.SelectionCount, Is.EqualTo(1));
            Assert.That(start.CandidatePachimonInstanceIds,
                Has.Length.EqualTo(PartyProgressionRules.StartCandidateCount));

            var encounters = map.Nodes.Values
                .Where(node => node.Content is PartyEncounterNodeContent)
                .ToArray();
            Assert.That(encounters, Has.Length.EqualTo(2));
            var rival = encounters.Single(node =>
                ((PartyEncounterNodeContent)node.Content).Kind
                == PartyEncounterKind.Rival);
            var gang = encounters.Single(node =>
                ((PartyEncounterNodeContent)node.Content).Kind
                == PartyEncounterKind.PachipachiGang);
            AssertEncounter(rival, 1, PartyProgressionRules.RivalCandidateCount);
            AssertEncounter(gang, 2, PartyProgressionRules.GangCandidateCount);
            AssertTransitionEncounter(map, rival, PartyProgressionRules.FirstExpansionAfterRow);
            AssertTransitionEncounter(map, gang, PartyProgressionRules.SecondExpansionAfterRow);

            var candidateIds = start.CandidatePachimonInstanceIds
                .Concat(((PartyEncounterNodeContent)rival.Content).CandidatePachimonInstanceIds)
                .Concat(((PartyEncounterNodeContent)gang.Content).CandidatePachimonInstanceIds)
                .ToArray();
            Assert.That(candidateIds, Has.Length.EqualTo(18));
            Assert.That(candidateIds.Distinct().Count(), Is.EqualTo(18));
            Assert.That(
                candidateIds.Select(id => pool.Get(id).SpeciesId).Distinct().Count(),
                Is.EqualTo(18));

            foreach (var node in map.Nodes.Values)
            {
                var enemyIds = node.Content switch
                {
                    BattleNodeContent battle => battle.EnemyPachimonInstanceIds,
                    GymNodeContent gym => gym.EnemyPachimonInstanceIds,
                    EliteNodeContent elite => elite.EnemyPachimonInstanceIds,
                    _ => null,
                };
                if (enemyIds == null)
                {
                    continue;
                }

                Assert.That(enemyIds, Has.Length.EqualTo(
                    PartyProgressionRules.GetPartySizeForRow(node.RowIndex)),
                    node.NodeId);

                if (node.Content is GymNodeContent gymContent)
                {
                    var badgeAttribute = gymContent.NodeReward.BadgeAttribute
                        ?? throw new AssertionException(
                            $"Gym {node.NodeId} has no Badge attribute.");
                    var expectedType = (AllocationType)((int)badgeAttribute + 1);
                    var matchingCount = enemyIds.Count(instanceId =>
                        pool.Get(instanceId).AllocationType == expectedType);
                    var requiredCount = enemyIds.Length == 1
                        ? 1
                        : enemyIds.Length - 1;
                    Assert.That(
                        matchingCount,
                        Is.GreaterThanOrEqualTo(requiredCount),
                        node.NodeId);
                }
            }

            foreach (var node in map.Nodes.Values.Where(node => node.RowIndex <= 10))
            {
                foreach (var instanceId in GetAssignedIds(node))
                {
                    var instance = pool.Get(instanceId);
                    Assert.That(instance.MinimumPartySize, Is.EqualTo(1), instanceId);
                    Assert.That(instance.SkillIds, Has.Count.EqualTo(2), instanceId);
                    Assert.That(
                        skillCatalog.Get(instance.SkillIds[1]).AllocationType,
                        Is.EqualTo(instance.AllocationType),
                        instanceId);
                }
            }
        }

        private static void AssertEncounter(
            MapNode node,
            int enemyCount,
            int candidateCount)
        {
            var content = (PartyEncounterNodeContent)node.Content;
            Assert.That(content.EnemyPachimonInstanceIds, Has.Length.EqualTo(enemyCount));
            Assert.That(content.CandidatePachimonInstanceIds,
                Has.Length.EqualTo(candidateCount));
        }

        private static void AssertTransitionEncounter(
            RunMap map,
            MapNode encounter,
            int precedingRow)
        {
            var sourceNodes = map.Rows.Single(row => row.RowIndex == precedingRow)
                .NodeIds.Select(map.GetNode).ToArray();
            Assert.That(sourceNodes.SelectMany(node => node.NextNodeIds), Is.Not.Empty);
            Assert.That(sourceNodes.SelectMany(node => node.NextNodeIds).All(nodeId =>
                map.GetNode(nodeId)?.RowIndex == precedingRow + 1), Is.True);
            Assert.That(sourceNodes.Any(node =>
                node.NextNodeIds.Count < map.Rows[precedingRow + 1].NodeIds.Count), Is.True);
            Assert.That(sourceNodes.All(node =>
                !node.NextNodeIds.Contains(encounter.NodeId)), Is.True);
            Assert.That(encounter.NextNodeIds, Is.Empty);
        }

        private static string[] GetAssignedIds(MapNode node)
        {
            return node.Content switch
            {
                StartNodeContent start => start.CandidatePachimonInstanceIds,
                BattleNodeContent battle => battle.EnemyPachimonInstanceIds,
                GymNodeContent gym => gym.EnemyPachimonInstanceIds,
                EliteNodeContent elite => elite.EnemyPachimonInstanceIds,
                PartyEncounterNodeContent encounter => encounter.EnemyPachimonInstanceIds
                    .Concat(encounter.CandidatePachimonInstanceIds)
                    .ToArray(),
                _ => System.Array.Empty<string>(),
            };
        }

        private static T LoadAsset<T>(string path) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path)
                ?? throw new AssertionException($"Required test Asset was not found: {path}");
        }
    }
}
