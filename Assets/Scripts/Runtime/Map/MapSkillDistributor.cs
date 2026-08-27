using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Data;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;
using Pachimon.Trainer;

namespace Pachimon.Map
{
    public sealed class MapSkillDistributor
    {
        private readonly SkillCatalog _skillCatalog;
        private readonly TrainerStyleCatalog _trainerStyleCatalog;
        private readonly MapGenerationSettings _settings;

        public MapSkillDistributor(
            SkillCatalog skillCatalog,
            TrainerStyleCatalog trainerStyleCatalog,
            MapGenerationSettings settings)
        {
            _skillCatalog = skillCatalog ?? throw new ArgumentNullException(nameof(skillCatalog));
            _trainerStyleCatalog = trainerStyleCatalog
                ?? throw new ArgumentNullException(nameof(trainerStyleCatalog));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public void Distribute(RunMap map, RunPachimonPool pachimonPool, int runSeed)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (pachimonPool == null) throw new ArgumentNullException(nameof(pachimonPool));

            var catalogErrors = _skillCatalog.ValidateContent();
            if (catalogErrors.Count > 0)
            {
                throw new MapGenerationException(
                    "SkillCatalog is invalid:\n" + string.Join("\n", catalogErrors));
            }

            foreach (var instance in pachimonPool.Instances)
            {
                instance.ResetAdditionalSkills();
            }

            var participatingFixedSkillIds = pachimonPool.Instances
                .Select(instance => instance.FixedSkillId)
                .ToHashSet();
            var mapAssignableSkills = _skillCatalog.GetMapAssignableSkills()
                .Where(skill => participatingFixedSkillIds.Contains(skill.SkillId))
                .ToArray();
            var usageCounts = mapAssignableSkills.ToDictionary(skill => skill.SkillId, _ => 0);
            foreach (var instance in pachimonPool.Instances)
            {
                foreach (var skillId in instance.SkillIds)
                {
                    if (usageCounts.ContainsKey(skillId))
                    {
                        usageCounts[skillId]++;
                    }
                }
            }

            var random = new Random(unchecked(runSeed * 16777619) ^ 0x534B494C);
            var nodes = map.Nodes.Values
                .OrderBy(node => node.RowIndex)
                .ThenBy(node => node.ColumnIndex)
                .ToArray();

            ValidateCandidatePool(mapAssignableSkills);
            AssignMatchingSkills(
                nodes,
                pachimonPool,
                mapAssignableSkills,
                usageCounts,
                random);
            AssignRandomSkills(nodes, pachimonPool, mapAssignableSkills, usageCounts, random);
            ValidateDistribution(nodes, pachimonPool);
        }

        private void AssignMatchingSkills(
            IEnumerable<MapNode> nodes,
            RunPachimonPool pachimonPool,
            IReadOnlyList<SkillAsset> mapAssignableSkills,
            IDictionary<int, int> usageCounts,
            Random random)
        {
            foreach (var node in nodes.Where(node => node.NodeType == NodeType.Gym
                         || node.NodeType == NodeType.Elite))
            {
                var allocationType = GetLeagueAllocationType(node);
                var candidates = mapAssignableSkills
                    .Where(skill => skill.AllocationType == allocationType)
                    .ToArray();
                var count = node.NodeType == NodeType.Gym
                    ? _settings.GymMatchingSkillCount
                    : _settings.EliteMatchingSkillCount;

                foreach (var instanceId in GetAssignedInstanceIds(node))
                {
                    var instance = GetRequiredInstance(pachimonPool, instanceId, node.NodeId);
                    AssignLeastUsedSkills(
                        instance,
                        candidates,
                        count,
                        usageCounts,
                        random,
                        node.NodeId);
                }
            }
        }

        private void ValidateCandidatePool(IReadOnlyList<SkillAsset> candidates)
        {
            var requiredPerType = _settings.EliteMatchingSkillCount + 1;
            var errors = new List<string>();
            foreach (AllocationType type in Enum.GetValues(typeof(AllocationType)))
            {
                if (type == AllocationType.Unassigned)
                {
                    continue;
                }

                var count = candidates.Count(skill => skill.AllocationType == type);
                if (count < requiredPerType)
                {
                    errors.Add(
                        $"{type} requires at least {requiredPerType} participating "
                        + $"Map-assignable Skills, but has {count}.");
                }
            }

            if (errors.Count > 0)
            {
                throw new MapGenerationException(
                    "Participating Skill candidates are invalid:\n"
                    + string.Join("\n", errors));
            }
        }

        private void AssignRandomSkills(
            IEnumerable<MapNode> nodes,
            RunPachimonPool pachimonPool,
            IReadOnlyList<SkillAsset> candidates,
            IDictionary<int, int> usageCounts,
            Random random)
        {
            foreach (var node in nodes)
            {
                var count = GetAssignedRandomSkillCount(node);
                foreach (var instanceId in GetAssignedInstanceIds(node))
                {
                    var instance = GetRequiredInstance(pachimonPool, instanceId, node.NodeId);
                    if (node.NodeType is not (NodeType.Gym or NodeType.Elite)
                        && count >= 2)
                    {
                        AssignLeastUsedSkills(
                            instance,
                            candidates
                                .Where(skill => skill.AllocationType
                                    == instance.AllocationType)
                                .ToArray(),
                            1,
                            usageCounts,
                            random,
                            node.NodeId);
                        AssignLeastUsedSkills(
                            instance,
                            candidates
                                .Where(skill => skill.AllocationType
                                    != instance.AllocationType)
                                .ToArray(),
                            1,
                            usageCounts,
                            random,
                            node.NodeId);
                        AssignLeastUsedSkills(
                            instance,
                            candidates,
                            count - 2,
                            usageCounts,
                            random,
                            node.NodeId);
                        continue;
                    }

                    AssignLeastUsedSkills(instance, candidates, count, usageCounts, random, node.NodeId);
                }
            }
        }

        private static void AssignLeastUsedSkills(
            PachimonInstance instance,
            IReadOnlyList<SkillAsset> candidates,
            int count,
            IDictionary<int, int> usageCounts,
            Random random,
            string nodeId)
        {
            for (var index = 0; index < count; index++)
            {
                var available = candidates
                    .Where(skill => !instance.SkillIds.Contains(skill.SkillId))
                    .ToArray();
                if (available.Length == 0)
                {
                    throw new MapGenerationException(
                        $"Node {nodeId} has no remaining Skill candidate for {instance.InstanceId}.");
                }

                var minimumUsage = available.Min(skill => usageCounts[skill.SkillId]);
                var leastUsed = available
                    .Where(skill => usageCounts[skill.SkillId] == minimumUsage)
                    .ToArray();
                var selected = leastUsed[random.Next(leastUsed.Length)];
                if (!instance.AddSkill(selected.SkillId))
                {
                    throw new MapGenerationException(
                        $"Could not assign Skill {selected.SkillId} to {instance.InstanceId}.");
                }

                usageCounts[selected.SkillId]++;
            }
        }

        private int GetRandomSkillCount(int rowIndex)
        {
            if (rowIndex >= _settings.LateRandomSkillStartRow)
            {
                return _settings.LateRandomSkillCount;
            }

            return rowIndex >= _settings.MidRandomSkillStartRow
                ? _settings.MidRandomSkillCount
                : _settings.EarlyRandomSkillCount;
        }

        private int GetAssignedRandomSkillCount(MapNode node)
        {
            var matchingCount = node.NodeType switch
            {
                NodeType.Gym => _settings.GymMatchingSkillCount,
                NodeType.Elite => _settings.EliteMatchingSkillCount,
                _ => 0,
            };
            var remainingSlots = Math.Max(
                0,
                PachimonInstance.MaxSkillSlots - 1 - matchingCount);
            return Math.Min(GetRandomSkillCount(node.RowIndex), remainingSlots);
        }

        private void ValidateDistribution(
            IEnumerable<MapNode> nodes,
            RunPachimonPool pachimonPool)
        {
            foreach (var node in nodes)
            {
                var matchingCount = node.NodeType switch
                {
                    NodeType.Gym => _settings.GymMatchingSkillCount,
                    NodeType.Elite => _settings.EliteMatchingSkillCount,
                    _ => 0,
                };
                var randomSkillCount = GetAssignedRandomSkillCount(node);
                var expectedSkillCount = 1 + matchingCount + randomSkillCount;
                var matchingType = matchingCount > 0
                    ? GetLeagueAllocationType(node)
                    : AllocationType.Unassigned;

                foreach (var instanceId in GetAssignedInstanceIds(node))
                {
                    var instance = GetRequiredInstance(pachimonPool, instanceId, node.NodeId);
                    if (instance.SkillIds.Count != expectedSkillCount
                        || instance.SkillIds.Distinct().Count() != expectedSkillCount)
                    {
                        throw new MapGenerationException(
                            $"{instance.InstanceId} at node {node.NodeId} requires "
                            + $"{expectedSkillCount} unique Skills, but has {instance.SkillIds.Count}.");
                    }

                    foreach (var skillId in instance.SkillIds)
                    {
                        var skill = _skillCatalog.Get(skillId);
                        if (skill == null || !skill.IsMapAssignable)
                        {
                            throw new MapGenerationException(
                                $"{instance.InstanceId} has invalid Map-assigned Skill {skillId}.");
                        }
                    }

                    if (matchingCount == 0)
                    {
                        var ownTypeSkillCount = instance.SkillIds.Count(skillId =>
                            _skillCatalog.Get(skillId)?.AllocationType
                            == instance.AllocationType);
                        var otherTypeSkillCount = instance.SkillIds.Count(skillId =>
                            _skillCatalog.Get(skillId)?.AllocationType
                            != instance.AllocationType);
                        if (randomSkillCount >= 2
                            && (ownTypeSkillCount < 2 || otherTypeSkillCount < 1))
                        {
                            throw new MapGenerationException(
                                $"{instance.InstanceId} at node {node.NodeId} requires "
                                + "one matching and one non-matching additional Skill.");
                        }
                        continue;
                    }

                    var finalMatchingCount = instance.SkillIds.Count(skillId =>
                        _skillCatalog.Get(skillId)?.AllocationType == matchingType);
                    if (finalMatchingCount < matchingCount)
                    {
                        throw new MapGenerationException(
                            $"{instance.InstanceId} at node {node.NodeId} requires at least "
                            + $"{matchingCount} {matchingType} Skills, but has {finalMatchingCount}.");
                    }
                }
            }
        }

        private AllocationType GetLeagueAllocationType(MapNode node)
        {
            if (node.Content is GymNodeContent gym)
            {
                var attribute = gym.NodeReward.BadgeAttribute
                    ?? throw new MapGenerationException($"Gym node {node.NodeId} has no Badge attribute.");
                return FromAttribute(attribute);
            }

            if (node.Content is not EliteNodeContent elite)
            {
                throw new MapGenerationException($"Node {node.NodeId} is not a League battle.");
            }

            var style = _trainerStyleCatalog.Get(elite.TrainerProfile.StyleId);
            if (style == null)
            {
                throw new MapGenerationException(
                    $"Elite node {node.NodeId} has unknown TrainerStyle {elite.TrainerProfile.StyleId}.");
            }

            return FromTheme(style.Theme);
        }

        private static PachimonInstance GetRequiredInstance(
            RunPachimonPool pachimonPool,
            string instanceId,
            string nodeId)
        {
            return pachimonPool.Get(instanceId)
                ?? throw new MapGenerationException(
                    $"Node {nodeId} references missing Pachimon instance {instanceId}.");
        }

        private static IEnumerable<string> GetAssignedInstanceIds(MapNode node)
        {
            return node.Content switch
            {
                StartNodeContent start => start.CandidatePachimonInstanceIds,
                BattleNodeContent battle => battle.EnemyPachimonInstanceIds,
                GymNodeContent gym => gym.EnemyPachimonInstanceIds,
                EliteNodeContent elite => elite.EnemyPachimonInstanceIds,
                _ => Array.Empty<string>(),
            };
        }

        private static AllocationType FromAttribute(PachimonAttribute attribute)
        {
            return (AllocationType)((int)attribute + 1);
        }

        private static AllocationType FromTheme(TrainerTheme theme)
        {
            return theme switch
            {
                TrainerTheme.Fire => AllocationType.Fire,
                TrainerTheme.Aqua => AllocationType.Aqua,
                TrainerTheme.Leaf => AllocationType.Leaf,
                TrainerTheme.Electric => AllocationType.Electric,
                TrainerTheme.Poison => AllocationType.Poison,
                TrainerTheme.Wind => AllocationType.Wind,
                TrainerTheme.Ice => AllocationType.Ice,
                TrainerTheme.Dragon => AllocationType.Dragon,
                _ => throw new MapGenerationException(
                    $"TrainerTheme {theme} cannot be used for matching Skill assignment."),
            };
        }
    }
}
