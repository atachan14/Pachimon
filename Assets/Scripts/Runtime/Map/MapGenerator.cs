using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Data;
using Pachimon.Items;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;
using Pachimon.Trainer;

namespace Pachimon.Map
{
    public sealed class MapGenerator
    {
        private const int StartCandidateCount = 9;
        private const int PartySize = 3;
        private const int MinimumCityEdgeCount = 2;

        private readonly MapGenerationSettings _settings;
        private readonly SkillCatalog _skillCatalog;
        private readonly ItemCatalog _itemCatalog;
        private readonly TrainerStyleCatalog _trainerStyleCatalog;
        private readonly TrainerNameCatalog _trainerNameCatalog;

        public MapGenerator(
            SkillCatalog skillCatalog,
            ItemCatalog itemCatalog,
            TrainerStyleCatalog trainerStyleCatalog,
            TrainerNameCatalog trainerNameCatalog,
            MapGenerationSettings settings = null)
        {
            _skillCatalog = skillCatalog ?? throw new ArgumentNullException(nameof(skillCatalog));
            _itemCatalog = itemCatalog ?? throw new ArgumentNullException(nameof(itemCatalog));
            _trainerStyleCatalog = trainerStyleCatalog
                ?? throw new ArgumentNullException(nameof(trainerStyleCatalog));
            _trainerNameCatalog = trainerNameCatalog
                ?? throw new ArgumentNullException(nameof(trainerNameCatalog));
            _settings = settings ?? new MapGenerationSettings();
        }

        public RunMap Generate(int runSeed, RunPachimonPool pachimonPool)
        {
            if (pachimonPool == null || pachimonPool.Instances.Count != RunPachimonPoolGenerator.PoolSize)
            {
                throw new MapGenerationException("Map generation requires a 300-instance RunPachimonPool.");
            }

            var random = new Random(unchecked(runSeed * 486187739) ^ 0x4D415000);
            var rows = CreateNodeRows(random);
            var nodes = rows.SelectMany(row => row).ToDictionary(node => node.NodeId);

            ConnectRows(rows, random);
            PlaceFixedNodes(rows, random);
            PlaceGyms(rows, nodes, random);
            PlaceNonAdjacentNodes(rows, nodes, random, NodeType.RestSpot, _settings.RestSpotNodeCount);
            PlaceNonAdjacentNodes(rows, nodes, random, NodeType.Event, _settings.EventNodeCount);
            FillBattleNodes(rows);
            AssignRewardsAndTrainers(rows, random);
            AssignPachimon(rows, nodes, pachimonPool, random);

            var map = BuildRunMap(rows);
            var skillDistributor = new MapSkillDistributor(
                _skillCatalog,
                _trainerStyleCatalog,
                _settings);
            skillDistributor.Distribute(map, pachimonPool, runSeed);
            ValidateMap(map, pachimonPool);
            return map;
        }

        private List<List<NodeBuilder>> CreateNodeRows(Random random)
        {
            var rowCounts = new int[_settings.EliteRowEnd + 1];
            rowCounts[0] = 1;

            for (var rowIndex = _settings.MainRowStart; rowIndex <= _settings.MainRowEnd; rowIndex++)
            {
                rowCounts[rowIndex] = _settings.BaseNodesPerRow;
            }

            var baseMainNodeCount = (_settings.MainRowEnd - _settings.MainRowStart + 1)
                * _settings.BaseNodesPerRow;
            var nodesToAdd = _settings.MainNodeCount - baseMainNodeCount;

            for (var i = 0; i < nodesToAdd; i++)
            {
                var candidateRows = Enumerable.Range(2, _settings.MainRowEnd - 1)
                    .Where(rowIndex => rowCounts[rowIndex] < _settings.MaxNodesPerRow)
                    .ToArray();

                if (candidateRows.Length == 0)
                {
                    throw new MapGenerationException("No row has room for the required additional nodes.");
                }

                rowCounts[candidateRows[random.Next(candidateRows.Length)]]++;
            }

            for (var rowIndex = _settings.LeagueGateRow; rowIndex <= _settings.EliteRowEnd; rowIndex++)
            {
                rowCounts[rowIndex] = 1;
            }

            var rows = new List<List<NodeBuilder>>(rowCounts.Length);
            for (var rowIndex = 0; rowIndex < rowCounts.Length; rowIndex++)
            {
                var row = new List<NodeBuilder>(rowCounts[rowIndex]);
                for (var columnIndex = 0; columnIndex < rowCounts[rowIndex]; columnIndex++)
                {
                    row.Add(new NodeBuilder(
                        $"node_{rowIndex:D2}_{columnIndex:D2}",
                        rowIndex,
                        columnIndex));
                }

                rows.Add(row);
            }

            return rows;
        }

        private void ConnectRows(IReadOnlyList<List<NodeBuilder>> rows, Random random)
        {
            for (var rowIndex = 0; rowIndex < rows.Count - 1; rowIndex++)
            {
                var sources = rows[rowIndex];
                var targets = rows[rowIndex + 1];

                if (targets.Count >= sources.Count)
                {
                    for (var targetIndex = 0; targetIndex < targets.Count; targetIndex++)
                    {
                        var sourceIndex = targetIndex * sources.Count / targets.Count;
                        sources[sourceIndex].NextNodeIds.Add(targets[targetIndex].NodeId);
                    }
                }
                else
                {
                    for (var sourceIndex = 0; sourceIndex < sources.Count; sourceIndex++)
                    {
                        var targetIndex = sourceIndex * targets.Count / sources.Count;
                        sources[sourceIndex].NextNodeIds.Add(targets[targetIndex].NodeId);
                    }
                }

                AddBranchEdges(sources, targets, random);
            }
        }

        private void AddBranchEdges(
            IReadOnlyList<NodeBuilder> sources,
            IReadOnlyList<NodeBuilder> targets,
            Random random)
        {
            if (targets.Count <= 1)
            {
                return;
            }

            var sourceIndices = Enumerable.Range(0, sources.Count).ToList();
            Shuffle(sourceIndices, random);

            foreach (var sourceIndex in sourceIndices)
            {
                var source = sources[sourceIndex];
                if (source.NextNodeIds.Count >= 2
                    || random.NextDouble() > _settings.AdditionalEdgeChance)
                {
                    continue;
                }

                var sourcePosition = (sourceIndex + 0.5) / sources.Count;
                var candidates = Enumerable.Range(0, targets.Count)
                    .Where(targetIndex => !source.NextNodeIds.Contains(targets[targetIndex].NodeId))
                    .Select(targetIndex => new
                    {
                        TargetIndex = targetIndex,
                        Distance = Math.Abs(
                            sourcePosition - ((targetIndex + 0.5) / targets.Count)),
                        TieBreaker = random.Next(),
                    })
                    .Where(candidate => candidate.Distance <= _settings.MaximumAdditionalEdgeDistance)
                    .OrderBy(candidate => candidate.Distance)
                    .ThenBy(candidate => candidate.TieBreaker)
                    .ToArray();

                foreach (var candidate in candidates)
                {
                    if (!CanAddEdgeWithoutCrossing(
                            sources,
                            sourceIndex,
                            candidate.TargetIndex))
                    {
                        continue;
                    }

                    source.NextNodeIds.Add(targets[candidate.TargetIndex].NodeId);
                    break;
                }
            }
        }

        private static bool CanAddEdgeWithoutCrossing(
            IReadOnlyList<NodeBuilder> sources,
            int sourceIndex,
            int targetIndex)
        {
            for (var existingSourceIndex = 0;
                 existingSourceIndex < sources.Count;
                 existingSourceIndex++)
            {
                foreach (var existingTargetNodeId in sources[existingSourceIndex].NextNodeIds)
                {
                    var existingTargetIndex = GetColumnIndex(existingTargetNodeId);
                    var crossesFromLeft = existingSourceIndex < sourceIndex
                        && existingTargetIndex > targetIndex;
                    var crossesFromRight = existingSourceIndex > sourceIndex
                        && existingTargetIndex < targetIndex;

                    if (crossesFromLeft || crossesFromRight)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void PlaceFixedNodes(IReadOnlyList<List<NodeBuilder>> rows, Random random)
        {
            rows[0][0].NodeType = NodeType.Start;

            var leagueGate = rows[_settings.LeagueGateRow][0];
            leagueGate.NodeType = NodeType.LeagueGate;
            leagueGate.Content = new LeagueGateNodeContent(
                _settings.RequiredBadgeCount,
                LeagueGateFailureMode.SpecialDefeat);

            for (var rowIndex = _settings.EliteRowStart; rowIndex <= _settings.EliteRowEnd; rowIndex++)
            {
                rows[rowIndex][0].NodeType = NodeType.Elite;
            }

            var cityPlacements = new List<CityPlacement>(_settings.CityRows.Length);
            for (var cityIndex = 0; cityIndex < _settings.CityRows.Length; cityIndex++)
            {
                var rowIndex = _settings.CityRows[cityIndex];
                var cityRow = rows[rowIndex];
                var candidateLeftColumns = Enumerable.Range(0, cityRow.Count - 1)
                    .Where(leftColumn => HasEnoughCityEdges(rows, rowIndex, leftColumn))
                    .ToArray();
                if (candidateLeftColumns.Length == 0)
                {
                    throw new MapGenerationException(
                        $"City row {rowIndex} has no adjacent pair with at least "
                        + $"{MinimumCityEdgeCount} incoming and outgoing edges.");
                }

                var leftColumn = candidateLeftColumns[random.Next(candidateLeftColumns.Length)];
                var cityGroupId = $"city_{cityIndex + 1:D2}";
                var shopSeed = random.Next();
                cityPlacements.Add(new CityPlacement(
                    cityGroupId,
                    shopSeed,
                    cityRow[leftColumn],
                    cityRow[leftColumn + 1]));
            }

            var stockRequests = cityPlacements
                .Select(placement => new CityStockRequest(
                    placement.CityGroupId,
                    placement.ShopSeed))
                .ToArray();
            var stockByCity = new CityStockGenerator().Generate(
                stockRequests,
                _itemCatalog);
            foreach (var placement in cityPlacements)
            {
                var cityContent = new CityNodeContent(
                    placement.CityGroupId,
                    placement.ShopSeed,
                    stockByCity[placement.CityGroupId]);

                foreach (var cityNode in placement.Nodes)
                {
                    cityNode.NodeType = NodeType.City;
                    cityNode.Content = cityContent;
                }
            }
        }

        private static bool HasEnoughCityEdges(
            IReadOnlyList<List<NodeBuilder>> rows,
            int rowIndex,
            int leftColumn)
        {
            var cityNodes = new[]
            {
                rows[rowIndex][leftColumn],
                rows[rowIndex][leftColumn + 1],
            };
            var cityNodeIds = cityNodes.Select(node => node.NodeId).ToHashSet();
            var incomingCount = rows[rowIndex - 1]
                .Count(source => source.NextNodeIds.Any(cityNodeIds.Contains));
            var outgoingCount = cityNodes
                .SelectMany(node => node.NextNodeIds)
                .Distinct()
                .Count();
            return incomingCount >= MinimumCityEdgeCount
                && outgoingCount >= MinimumCityEdgeCount;
        }

        private void PlaceGyms(
            IReadOnlyList<List<NodeBuilder>> rows,
            IReadOnlyDictionary<string, NodeBuilder> nodes,
            Random random)
        {
            for (var attempt = 0; attempt < _settings.PlacementAttemptLimit; attempt++)
            {
                ClearNodeType(rows, NodeType.Gym);

                if (!TryPlaceNonAdjacentNodes(
                        rows,
                        nodes,
                        random,
                        NodeType.Gym,
                        _settings.GymNodeCount))
                {
                    continue;
                }

                if (GetMaximumGymCount(rows, nodes) >= _settings.RequiredBadgeCount)
                {
                    return;
                }
            }

            throw new MapGenerationException(
                $"Could not place {_settings.GymNodeCount} Gym nodes with an "
                + $"{_settings.RequiredBadgeCount}-badge route.");
        }

        private void PlaceNonAdjacentNodes(
            IReadOnlyList<List<NodeBuilder>> rows,
            IReadOnlyDictionary<string, NodeBuilder> nodes,
            Random random,
            NodeType nodeType,
            int count)
        {
            for (var attempt = 0; attempt < _settings.PlacementAttemptLimit; attempt++)
            {
                ClearNodeType(rows, nodeType);
                if (TryPlaceNonAdjacentNodes(rows, nodes, random, nodeType, count))
                {
                    ApplySimpleNodeContent(rows, random, nodeType);
                    return;
                }
            }

            throw new MapGenerationException($"Could not place {count} {nodeType} nodes.");
        }

        private bool TryPlaceNonAdjacentNodes(
            IReadOnlyList<List<NodeBuilder>> rows,
            IReadOnlyDictionary<string, NodeBuilder> nodes,
            Random random,
            NodeType nodeType,
            int count)
        {
            for (var placed = 0; placed < count; placed++)
            {
                var candidates = GetAdventureNodes(rows)
                    .Where(node => node.NodeType == null)
                    .Where(node => !HasAdjacentNodeOfType(node, nodes, nodeType))
                    .ToArray();

                if (candidates.Length == 0)
                {
                    return false;
                }

                candidates[random.Next(candidates.Length)].NodeType = nodeType;
            }

            return true;
        }

        private void ApplySimpleNodeContent(
            IReadOnlyList<List<NodeBuilder>> rows,
            Random random,
            NodeType nodeType)
        {
            foreach (var node in GetAdventureNodes(rows).Where(node => node.NodeType == nodeType))
            {
                node.Content = nodeType switch
                {
                    NodeType.RestSpot => new RestSpotNodeContent(
                        _settings.RestSpotHealPercent),
                    NodeType.Event => new EventNodeContent(random.Next()),
                    _ => node.Content,
                };
            }
        }

        private void FillBattleNodes(IReadOnlyList<List<NodeBuilder>> rows)
        {
            for (var rowIndex = _settings.MainRowStart; rowIndex <= _settings.MainRowEnd; rowIndex++)
            {
                foreach (var node in rows[rowIndex])
                {
                    if (node.NodeType == null)
                    {
                        node.NodeType = NodeType.Battle;
                    }
                }
            }
        }

        private void AssignRewardsAndTrainers(
            IReadOnlyList<List<NodeBuilder>> rows,
            Random random)
        {
            var rewardGenerator = new NodeRewardGenerator(random);
            var profileFactory = new TrainerProfileFactory(
                _trainerStyleCatalog,
                _trainerNameCatalog,
                random);

            var battleNodes = rows.SelectMany(row => row)
                .Where(node => node.NodeType == NodeType.Battle)
                .OrderBy(node => node.RowIndex)
                .ThenBy(node => node.ColumnIndex)
                .ToArray();
            var battleRewards = rewardGenerator.CreateBattleRewards();
            if (battleNodes.Length != battleRewards.Count)
            {
                throw new MapGenerationException(
                    $"Expected {battleRewards.Count} Battle nodes, but found {battleNodes.Length}.");
            }

            for (var index = 0; index < battleNodes.Length; index++)
            {
                var reward = battleRewards[index];
                battleNodes[index].NodeReward = reward;
                battleNodes[index].TrainerProfile = profileFactory.Create(
                    TrainerRole.Normal,
                    TrainerThemeResolver.FromBattleReward(reward));
            }

            var gymNodes = rows.SelectMany(row => row)
                .Where(node => node.NodeType == NodeType.Gym)
                .OrderBy(node => node.RowIndex)
                .ThenBy(node => node.ColumnIndex)
                .ToArray();
            var gymRewards = rewardGenerator.CreateGymRewards();
            if (gymNodes.Length != gymRewards.Count)
            {
                throw new MapGenerationException(
                    $"Expected {gymRewards.Count} Gym nodes, but found {gymNodes.Length}.");
            }

            var gymFavoredAttributes = gymRewards
                .Select(reward => reward.BadgeAttribute
                    ?? throw new MapGenerationException("Gym Reward has no Badge attribute."))
                .ToArray();
            var gymWeakAttributes = CreateBalancedWeaknesses(
                gymFavoredAttributes,
                random);

            for (var index = 0; index < gymNodes.Length; index++)
            {
                var reward = gymRewards[index];
                var badgeAttribute = gymFavoredAttributes[index];
                gymNodes[index].NodeReward = reward;
                gymNodes[index].TrainerProfile = profileFactory.Create(
                    TrainerRole.GymLeader,
                    TrainerThemeResolver.FromAttribute(badgeAttribute),
                    badgeAttribute,
                    gymWeakAttributes[index]);
            }

            var eliteAttributes = Enum.GetValues(typeof(PachimonAttribute))
                .Cast<PachimonAttribute>()
                .ToList();
            Shuffle(eliteAttributes, random);
            var eliteNodes = rows.SelectMany(row => row)
                .Where(node => node.NodeType == NodeType.Elite)
                .OrderBy(node => node.RowIndex)
                .ToArray();
            var eliteWeakAttributes = eliteAttributes
                .Skip(eliteNodes.Length)
                .ToList();
            Shuffle(eliteWeakAttributes, random);
            for (var index = 0; index < eliteNodes.Length; index++)
            {
                var favoredAttribute = eliteAttributes[index];
                eliteNodes[index].TrainerProfile = profileFactory.Create(
                    TrainerRole.Elite,
                    TrainerThemeResolver.FromAttribute(favoredAttribute),
                    favoredAttribute,
                    eliteWeakAttributes[index]);
            }
        }

        private void AssignPachimon(
            IReadOnlyList<List<NodeBuilder>> rows,
            IReadOnlyDictionary<string, NodeBuilder> nodes,
            RunPachimonPool pachimonPool,
            Random random)
        {
            var slots = CreatePachimonSlots(rows);
            if (slots.Count != pachimonPool.Instances.Count)
            {
                throw new MapGenerationException(
                    $"Map has {slots.Count} Pachimon slots, but the pool contains "
                    + $"{pachimonPool.Instances.Count} instances.");
            }

            Dictionary<PlacementSlot, string> assignments = null;
            for (var attempt = 0; attempt < _settings.PlacementAttemptLimit; attempt++)
            {
                if (TryAssignPachimon(slots, nodes, pachimonPool, random, out assignments))
                {
                    break;
                }
            }

            if (assignments == null)
            {
                throw new MapGenerationException("Could not assign Pachimon instances to map nodes.");
            }

            var assignmentsByNode = assignments
                .GroupBy(pair => pair.Key.Node.NodeId)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderBy(pair => pair.Key.Index).Select(pair => pair.Value).ToArray());

            foreach (var node in rows.SelectMany(row => row))
            {
                switch (node.NodeType)
                {
                    case NodeType.Start:
                        node.Content = new StartNodeContent(
                            OrderPachimonByMaxHp(
                                assignmentsByNode[node.NodeId],
                                pachimonPool),
                            PartySize);
                        break;
                    case NodeType.Battle:
                        node.Content = new BattleNodeContent(
                            OrderPachimonByMaxHp(
                                assignmentsByNode[node.NodeId],
                                pachimonPool),
                            node.NodeReward,
                            node.TrainerProfile);
                        break;
                    case NodeType.Gym:
                        node.Content = new GymNodeContent(
                            OrderPachimonByMaxHp(
                                assignmentsByNode[node.NodeId],
                                pachimonPool),
                            node.NodeReward,
                            node.TrainerProfile);
                        break;
                    case NodeType.Elite:
                        node.Content = new EliteNodeContent(
                            OrderPachimonByMaxHp(
                                assignmentsByNode[node.NodeId],
                                pachimonPool),
                            node.TrainerProfile);
                        break;
                }
            }
        }

        private static string[] OrderPachimonByMaxHp(
            IEnumerable<string> instanceIds,
            RunPachimonPool pachimonPool)
        {
            return instanceIds
                .OrderByDescending(instanceId =>
                    pachimonPool.Get(instanceId)?.MaxHp
                    ?? throw new MapGenerationException(
                        $"Node content references missing Pachimon {instanceId}."))
                .ToArray();
        }

        private bool TryAssignPachimon(
            IReadOnlyList<PlacementSlot> slots,
            IReadOnlyDictionary<string, NodeBuilder> nodes,
            RunPachimonPool pachimonPool,
            Random random,
            out Dictionary<PlacementSlot, string> assignments)
        {
            var workingAssignments = new Dictionary<PlacementSlot, string>();
            assignments = null;
            var remainingSlots = slots.ToList();
            var remainingInstances = pachimonPool.Instances.ToList();

            var leagueNodes = slots.Select(slot => slot.Node)
                .Where(node => node.NodeType == NodeType.Gym || node.NodeType == NodeType.Elite)
                .Distinct()
                .OrderByDescending(node => node.RowIndex)
                .ThenBy(node => node.ColumnIndex)
                .ToArray();

            // The first League slot is the strongest remaining Power specialist.
            foreach (var node in leagueNodes)
            {
                var type = GetLeagueAllocationType(node);
                var aceSlot = remainingSlots.Single(slot => slot.Node == node && slot.Index == 0);
                var ace = SelectAce(
                    remainingInstances,
                    aceSlot,
                    type,
                    workingAssignments,
                    pachimonPool,
                    nodes,
                    random);
                if (ace == null
                    || !CommitAssignment(aceSlot, ace, workingAssignments, remainingSlots, remainingInstances))
                {
                    assignments = null;
                    return false;
                }
            }

            // Gym and Elite parties each receive two additional matching-type members.
            foreach (var node in leagueNodes)
            {
                var type = GetLeagueAllocationType(node);
                for (var slotIndex = 1; slotIndex < PartySize; slotIndex++)
                {
                    var slot = remainingSlots.Single(item => item.Node == node && item.Index == slotIndex);
                    if (!TryAssignMatchingType(
                            slot,
                            type,
                            workingAssignments,
                            remainingSlots,
                            remainingInstances,
                            pachimonPool,
                            nodes,
                            random))
                    {
                        assignments = null;
                        return false;
                    }
                }
            }

            // First-slot Attribute trainers receive one matching-type member.
            var attributeBattles = slots.Select(slot => slot.Node)
                .Where(node => node.NodeType == NodeType.Battle
                    && node.NodeReward?.FirstElement?.Kind == RewardElementKind.Attribute)
                .Distinct()
                .OrderByDescending(node => node.RowIndex)
                .ThenBy(node => node.ColumnIndex)
                .ToArray();
            foreach (var node in attributeBattles)
            {
                var attribute = node.NodeReward.FirstElement.Attribute
                    ?? throw new MapGenerationException("Attribute Reward has no attribute.");
                var slot = remainingSlots.Single(item => item.Node == node && item.Index == 0);
                if (!TryAssignMatchingType(
                        slot,
                        FromAttribute(attribute),
                        workingAssignments,
                        remainingSlots,
                        remainingInstances,
                        pachimonPool,
                        nodes,
                        random))
                {
                    assignments = null;
                    return false;
                }
            }

            // Start candidates are random, followed by every unfilled enemy slot.
            var startSlots = remainingSlots
                .Where(slot => slot.Node.NodeType == NodeType.Start)
                .OrderBy(slot => slot.Index)
                .ToArray();
            if (!TryAssignRandomSlots(
                    startSlots,
                    workingAssignments,
                    remainingSlots,
                    remainingInstances,
                    pachimonPool,
                    nodes,
                    random)
                || !TryAssignRandomSlots(
                    remainingSlots.ToArray(),
                    workingAssignments,
                    remainingSlots,
                    remainingInstances,
                    pachimonPool,
                    nodes,
                    random))
            {
                assignments = null;
                return false;
            }

            if (workingAssignments.Count != slots.Count || remainingInstances.Count != 0)
            {
                return false;
            }

            assignments = workingAssignments;
            return true;
        }

        private static PachimonInstance SelectAce(
            IEnumerable<PachimonInstance> instances,
            PlacementSlot slot,
            AllocationType type,
            IEnumerable<KeyValuePair<PlacementSlot, string>> assignments,
            RunPachimonPool pachimonPool,
            IReadOnlyDictionary<string, NodeBuilder> nodes,
            Random random)
        {
            foreach (var rule in SpeciesPlacementRules)
            {
                var selected = instances
                    .Where(instance => CanAssign(
                        instance,
                        slot,
                        assignments,
                        pachimonPool,
                        nodes,
                        rule))
                    .OrderByDescending(instance =>
                        instance.Stats.GetValueUnits(GetAttributeStatType(type)))
                    .ThenBy(_ => random.Next())
                    .FirstOrDefault();
                if (selected != null)
                {
                    return selected;
                }
            }

            return null;
        }

        private static bool TryAssignMatchingType(
            PlacementSlot slot,
            AllocationType type,
            IDictionary<PlacementSlot, string> assignments,
            ICollection<PlacementSlot> remainingSlots,
            ICollection<PachimonInstance> remainingInstances,
            RunPachimonPool pachimonPool,
            IReadOnlyDictionary<string, NodeBuilder> nodes,
            Random random)
        {
            foreach (var rule in SpeciesPlacementRules)
            {
                var selected = remainingInstances
                    .Where(instance => instance.AllocationType == type)
                    .Where(instance => CanAssign(
                        instance,
                        slot,
                        assignments,
                        pachimonPool,
                        nodes,
                        rule))
                    .OrderBy(_ => random.Next())
                    .FirstOrDefault();
                if (selected != null)
                {
                    return CommitAssignment(
                        slot,
                        selected,
                        assignments,
                        remainingSlots,
                        remainingInstances);
                }
            }

            return false;
        }

        private static bool TryAssignRandomSlots(
            IReadOnlyList<PlacementSlot> requestedSlots,
            IDictionary<PlacementSlot, string> assignments,
            ICollection<PlacementSlot> remainingSlots,
            ICollection<PachimonInstance> remainingInstances,
            RunPachimonPool pachimonPool,
            IReadOnlyDictionary<string, NodeBuilder> nodes,
            Random random)
        {
            var slots = requestedSlots.Where(remainingSlots.Contains).ToList();
            Shuffle(slots, random);
            foreach (var slot in slots)
            {
                PachimonInstance selected = null;
                foreach (var rule in SpeciesPlacementRules)
                {
                    selected = remainingInstances
                        .Where(instance => CanAssign(
                            instance,
                            slot,
                            assignments,
                            pachimonPool,
                            nodes,
                            rule))
                        .OrderByDescending(instance => assignments.Values.Any(
                            id => pachimonPool.Get(id).SpeciesId == instance.SpeciesId))
                        .ThenBy(_ => random.Next())
                        .FirstOrDefault();
                    if (selected != null)
                    {
                        break;
                    }
                }

                if (selected == null
                    || !CommitAssignment(slot, selected, assignments, remainingSlots, remainingInstances))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool CanAssign(
            PachimonInstance instance,
            PlacementSlot slot,
            IEnumerable<KeyValuePair<PlacementSlot, string>> assignments,
            RunPachimonPool pachimonPool,
            IReadOnlyDictionary<string, NodeBuilder> nodes,
            SpeciesPlacementRule rule)
        {
            foreach (var assignment in assignments)
            {
                var assigned = pachimonPool.Get(assignment.Value);
                if (assigned.SpeciesId != instance.SpeciesId)
                {
                    continue;
                }

                if (assignment.Key.Node == slot.Node)
                {
                    return false;
                }

                if (rule != SpeciesPlacementRule.AvoidSameNode
                    && assignment.Key.Node.RowIndex == slot.Node.RowIndex)
                {
                    return false;
                }

                if (rule == SpeciesPlacementRule.AvoidAdjacentNodes
                    && AreAdjacent(assignment.Key.Node, slot.Node, nodes))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool CommitAssignment(
            PlacementSlot slot,
            PachimonInstance instance,
            IDictionary<PlacementSlot, string> assignments,
            ICollection<PlacementSlot> remainingSlots,
            ICollection<PachimonInstance> remainingInstances)
        {
            if (!remainingSlots.Remove(slot) || !remainingInstances.Remove(instance))
            {
                return false;
            }

            assignments.Add(slot, instance.InstanceId);
            return true;
        }

        private AllocationType GetLeagueAllocationType(NodeBuilder node)
        {
            if (node.TrainerProfile == null)
            {
                throw new MapGenerationException($"League node {node.NodeId} has no TrainerProfile.");
            }

            var style = _trainerStyleCatalog.Get(node.TrainerProfile.StyleId);
            if (style == null)
            {
                throw new MapGenerationException(
                    $"League node {node.NodeId} has unknown TrainerStyle {node.TrainerProfile.StyleId}.");
            }

            return FromTheme(style.Theme);
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
                    $"TrainerTheme {theme} cannot be used as a League Allocation Type."),
            };
        }

        private static AllocationType FromAttribute(PachimonAttribute attribute)
        {
            return (AllocationType)((int)attribute + 1);
        }

        private static PachimonStatType GetAttributeStatType(AllocationType type)
        {
            if (type < AllocationType.Fire || type > AllocationType.Dragon)
            {
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            return PachimonStatTypeUtility.FromAttribute(
                (PachimonAttribute)((int)type - 1));
        }

        private List<PlacementSlot> CreatePachimonSlots(IReadOnlyList<List<NodeBuilder>> rows)
        {
            var slots = new List<PlacementSlot>(RunPachimonPoolGenerator.PoolSize);
            AddSlots(slots, rows[0][0], StartCandidateCount);

            foreach (var node in rows.SelectMany(row => row))
            {
                if (node.NodeType == NodeType.Battle
                    || node.NodeType == NodeType.Gym
                    || node.NodeType == NodeType.Elite)
                {
                    AddSlots(slots, node, PartySize);
                }
            }

            return slots;
        }

        private static void AddSlots(ICollection<PlacementSlot> slots, NodeBuilder node, int count)
        {
            for (var index = 0; index < count; index++)
            {
                slots.Add(new PlacementSlot(node, index));
            }
        }

        private RunMap BuildRunMap(IReadOnlyList<List<NodeBuilder>> rows)
        {
            var map = new RunMap();

            foreach (var sourceRow in rows)
            {
                var mapRow = new MapRow(sourceRow[0].RowIndex);
                foreach (var sourceNode in sourceRow)
                {
                    if (sourceNode.NodeType == null || sourceNode.Content == null)
                    {
                        throw new MapGenerationException(
                            $"Node {sourceNode.NodeId} was not fully configured.");
                    }

                    var mapNode = new MapNode(
                        sourceNode.NodeId,
                        sourceNode.RowIndex,
                        sourceNode.ColumnIndex,
                        sourceNode.NodeType.Value,
                        sourceNode.Content);

                    foreach (var nextNodeId in sourceNode.NextNodeIds
                                 .OrderBy(id => GetColumnIndex(id)))
                    {
                        mapNode.NextNodeIds.Add(nextNodeId);
                    }

                    map.Nodes.Add(mapNode.NodeId, mapNode);
                    mapRow.NodeIds.Add(mapNode.NodeId);
                }

                map.Rows.Add(mapRow);
            }

            foreach (var cityGroup in map.Nodes.Values
                         .Where(node => node.Content is CityNodeContent)
                         .GroupBy(node => ((CityNodeContent)node.Content).CityGroupId))
            {
                map.AddNodeGroup(new MapNodeGroup(
                    cityGroup.Key,
                    NodeType.City,
                    cityGroup.OrderBy(node => node.ColumnIndex).Select(node => node.NodeId)));
            }

            map.StartNodeId = rows[0][0].NodeId;
            return map;
        }

        private void ValidateMap(RunMap map, RunPachimonPool pachimonPool)
        {
            var mainNodes = map.Rows
                .Where(row => row.RowIndex >= _settings.MainRowStart
                    && row.RowIndex <= _settings.MainRowEnd)
                .SelectMany(row => row.NodeIds)
                .Select(map.GetNode)
                .ToArray();

            var cityNodeCount = _settings.CityRows.Length * 2;
            var battleNodeCount = _settings.MainNodeCount
                - cityNodeCount
                - _settings.GymNodeCount
                - _settings.RestSpotNodeCount
                - _settings.EventNodeCount;

            AssertCount(mainNodes, NodeType.City, cityNodeCount);
            AssertCount(mainNodes, NodeType.Gym, _settings.GymNodeCount);
            AssertCount(mainNodes, NodeType.RestSpot, _settings.RestSpotNodeCount);
            AssertCount(mainNodes, NodeType.Event, _settings.EventNodeCount);
            AssertCount(mainNodes, NodeType.Battle, battleNodeCount);
            ValidateRewardsAndTrainers(map);

            if (map.NodeGroups.Count != _settings.CityRows.Length
                || map.NodeGroups.Values.Any(group => group.NodeType != NodeType.City
                    || group.NodeIds.Count != 2))
            {
                throw new MapGenerationException(
                    $"Expected {_settings.CityRows.Length} two-node City groups, but generated "
                    + $"{map.NodeGroups.Count} groups.");
            }

            foreach (var cityGroup in map.NodeGroups.Values)
            {
                var members = cityGroup.NodeIds.Select(map.GetNode).ToArray();
                if (members.Any(node => node == null)
                    || members[0].RowIndex != members[1].RowIndex
                    || Math.Abs(members[0].ColumnIndex - members[1].ColumnIndex) != 1)
                {
                    throw new MapGenerationException(
                        $"City group {cityGroup.GroupId} must contain two adjacent nodes in one row.");
                }

                var memberIds = cityGroup.NodeIds.ToHashSet();
                var incomingCount = map.Nodes.Values.Count(node =>
                    !memberIds.Contains(node.NodeId)
                    && node.NextNodeIds.Any(memberIds.Contains));
                var outgoingCount = members
                    .SelectMany(node => node.NextNodeIds)
                    .Where(nodeId => !memberIds.Contains(nodeId))
                    .Distinct()
                    .Count();
                if (incomingCount < MinimumCityEdgeCount
                    || outgoingCount < MinimumCityEdgeCount)
                {
                    throw new MapGenerationException(
                        $"City group {cityGroup.GroupId} requires at least "
                        + $"{MinimumCityEdgeCount} distinct incoming and outgoing edges, but has "
                        + $"{incomingCount} incoming and {outgoingCount} outgoing.");
                }

                ValidateCityStock(cityGroup.GroupId, members);
            }

            ValidateCityStockDistribution(map);

            var reachable = TraverseForward(map, map.StartNodeId);
            if (reachable.Count != map.Nodes.Count)
            {
                throw new MapGenerationException(
                    $"Only {reachable.Count} of {map.Nodes.Count} nodes are reachable from Start.");
            }

            var leagueGateId = map.Rows[_settings.LeagueGateRow].NodeIds[0];
            var nodesReachingLeagueGate = TraverseBackward(map, leagueGateId);
            var expectedPreGateNodeCount = map.Rows
                .Where(row => row.RowIndex <= _settings.LeagueGateRow)
                .Sum(row => row.NodeIds.Count);
            if (nodesReachingLeagueGate.Count != expectedPreGateNodeCount)
            {
                throw new MapGenerationException(
                    $"Only {nodesReachingLeagueGate.Count} of {expectedPreGateNodeCount} pre-gate nodes "
                    + "can reach LeagueGate.");
            }

            foreach (var node in map.Nodes.Values)
            {
                var maxEdges = node.NodeType == NodeType.Start ? 3 : 2;
                if (node.NextNodeIds.Count > maxEdges)
                {
                    throw new MapGenerationException(
                        $"Node {node.NodeId} has {node.NextNodeIds.Count} outgoing edges; max is {maxEdges}.");
                }
            }

            var assignedIds = GetAssignedPachimonIds(map).ToArray();
            if (assignedIds.Length != pachimonPool.Instances.Count
                || assignedIds.Distinct().Count() != pachimonPool.Instances.Count)
            {
                throw new MapGenerationException("Pachimon assignment is incomplete or contains duplicates.");
            }

            ValidateSpeciesNodeSeparation(map, pachimonPool);
            ValidatePachimonAllocation(map, pachimonPool);
            ValidateEnemyPartyOrder(map, pachimonPool);
        }

        private void ValidateCityStock(string cityGroupId, IReadOnlyList<MapNode> members)
        {
            var contents = members
                .Select(node => node.Content as CityNodeContent)
                .ToArray();
            if (contents.Any(content => content == null)
                || contents.Any(content => !ReferenceEquals(content, contents[0])))
            {
                throw new MapGenerationException(
                    $"City group {cityGroupId} must share one CityNodeContent.");
            }

            var stock = contents[0].StockEntries;
            if (stock == null
                || stock.Count == 0
                || stock.Select(entry => entry.StockId).Distinct().Count() != stock.Count
                || stock.Any(entry => entry.IsPurchased))
            {
                throw new MapGenerationException(
                    $"City group {cityGroupId} stock entries are invalid.");
            }

            foreach (var entry in stock)
            {
                var item = _itemCatalog.Get(entry.ItemId);
                if (item == null
                    || entry.GeneratedData == null
                    || entry.GeneratedData.ItemId != entry.ItemId
                    || entry.BasePrice != item.BasePrice
                    || entry.Price < CityStockGenerator.GetMinimumPrice(entry.BasePrice)
                    || entry.Price > CityStockGenerator.GetMaximumPrice(entry.BasePrice))
                {
                    throw new MapGenerationException(
                        $"City group {cityGroupId} contains an invalid stock price.");
                }
            }

            if (stock.Sum(entry => entry.Price) != stock.Sum(entry => entry.BasePrice))
            {
                throw new MapGenerationException(
                    $"City group {cityGroupId} stock price total is invalid.");
            }

            foreach (var itemGroup in stock
                         .GroupBy(entry => entry.ItemId))
            {
                var groupedItem = _itemCatalog.Get(itemGroup.Key);
                if (groupedItem is EngravingItemAsset engraving)
                {
                    ValidateEngravingStock(cityGroupId, engraving, itemGroup.ToArray());
                    continue;
                }

                if (groupedItem is EquipmentItemAsset equipment)
                {
                    ValidateEquipmentStock(cityGroupId, equipment, itemGroup.ToArray());
                    continue;
                }

                if (groupedItem is not HealingItemAsset healingItem)
                {
                    continue;
                }

                var entries = itemGroup.ToArray();
                var minimumEffect = checked(
                    ((healingItem.RecoveryPercent
                        * CityStockGenerator.MinimumEffectPercent) + 99) / 100);
                var maximumEffect = checked(
                    (healingItem.RecoveryPercent
                        * CityStockGenerator.MaximumEffectPercent) / 100);
                if (entries.Any(entry =>
                        !entry.GeneratedData.PrimaryEffectValue.HasValue
                        || entry.GeneratedData.PrimaryEffectValue.Value < minimumEffect
                        || entry.GeneratedData.PrimaryEffectValue.Value > maximumEffect)
                    || entries.Sum(entry =>
                        entry.GeneratedData.PrimaryEffectValue.Value)
                        != healingItem.RecoveryPercent * entries.Length)
                {
                    throw new MapGenerationException(
                        $"City group {cityGroupId} Item {itemGroup.Key} effect values are invalid.");
                }

                foreach (var cheaper in entries)
                {
                    if (entries.Any(expensive => expensive.Price > cheaper.Price
                        && expensive.GeneratedData.PrimaryEffectValue.Value
                            < cheaper.GeneratedData.PrimaryEffectValue.Value))
                    {
                        throw new MapGenerationException(
                            $"City group {cityGroupId} Item {itemGroup.Key} price and effect ranks differ.");
                    }
                }
            }

            var equipmentEntries = stock
                .Where(entry => _itemCatalog.Get(entry.ItemId)
                    is EquipmentItemAsset)
                .ToArray();
            foreach (var cheaper in equipmentEntries)
            {
                var cheaperValue = GetRankedEquipmentValue(cheaper);
                if (equipmentEntries.Any(expensive =>
                        expensive.Price > cheaper.Price
                        && GetRankedEquipmentValue(expensive) < cheaperValue))
                {
                    throw new MapGenerationException(
                        $"City group {cityGroupId} Equipment price and effect ranks differ.");
                }
            }
        }

        private int GetRankedEquipmentValue(CityStockEntry entry)
        {
            var equipment = _itemCatalog.Get(entry.ItemId) as EquipmentItemAsset;
            if (equipment == null)
            {
                throw new MapGenerationException(
                    $"Item {entry.ItemId} is not Equipment.");
            }

            var mainStat = PachimonStatTypeUtility.FromAttribute(
                equipment.MainAttribute);
            var mainValue = entry.GeneratedData.StatChanges
                .Single(change => change.StatType == mainStat)
                .Amount;
            return equipment.Slot == EquipmentSlot.Head
                ? mainValue / 2
                : mainValue;
        }

        private void ValidateEngravingStock(
            string cityGroupId,
            EngravingItemAsset engraving,
            IReadOnlyList<CityStockEntry> entries)
        {
            var minimumEffect = checked(
                ((engraving.BaseEffectValue
                    * CityStockGenerator.MinimumEffectPercent) + 99) / 100);
            var maximumEffect = checked(
                (engraving.BaseEffectValue
                    * CityStockGenerator.MaximumEffectPercent) / 100);
            foreach (var entry in entries)
            {
                var changes = entry.GeneratedData.StatChanges;
                var main = changes.SingleOrDefault(change => change.Amount > 0);
                var downside = changes.SingleOrDefault(change => change.Amount < 0);
                if (changes.Count != 2
                    || main == null
                    || downside == null
                    || main.StatType != engraving.TargetStat
                    || main.Amount < minimumEffect
                    || main.Amount > maximumEffect
                    || downside.StatType == main.StatType)
                {
                    throw new MapGenerationException(
                        $"City group {cityGroupId} Engraving {engraving.ItemId} effects are invalid.");
                }
            }

            if (entries.Sum(entry => entry.GeneratedData.StatChanges
                    .Single(change => change.Amount > 0).Amount)
                != engraving.BaseEffectValue * entries.Count)
            {
                throw new MapGenerationException(
                    $"City group {cityGroupId} Engraving {engraving.ItemId} total is invalid.");
            }

            foreach (var cheaper in entries)
            {
                if (entries.Any(expensive => expensive.Price > cheaper.Price
                    && expensive.GeneratedData.StatChanges
                        .Single(change => change.Amount > 0).Amount
                    < cheaper.GeneratedData.StatChanges
                        .Single(change => change.Amount > 0).Amount))
                {
                    throw new MapGenerationException(
                        $"City group {cityGroupId} Engraving price and effect ranks differ.");
                }
            }
        }

        private static void ValidateEquipmentStock(
            string cityGroupId,
            EquipmentItemAsset equipment,
            IReadOnlyList<CityStockEntry> entries)
        {
            var mainStat = PachimonStatTypeUtility.FromAttribute(
                equipment.MainAttribute);
            var mainUnits = equipment.Slot == EquipmentSlot.Head ? 6 : 3;
            var mainBase = checked(StatUnitValue.Get(mainStat) * mainUnits);
            var minimumMain = checked(
                mainBase * CityStockGenerator.MinimumEquipmentEffectPercent / 100);
            var maximumMain = checked(
                mainBase * CityStockGenerator.MaximumEquipmentEffectPercent / 100);

            foreach (var entry in entries)
            {
                var changes = entry.GeneratedData.StatChanges;
                var expectedCount = equipment.Slot == EquipmentSlot.Head ? 2 : 3;
                var main = changes.FirstOrDefault(change => change.StatType == mainStat);
                var additional = changes.FirstOrDefault(change =>
                    change.StatType != mainStat
                    && PachimonStatTypeUtility.TryGetAttribute(
                        change.StatType,
                        out _));
                var fixedStat = equipment.Slot switch
                {
                    EquipmentSlot.Body => PachimonStatType.Haste,
                    EquipmentSlot.Feet => PachimonStatType.Speed,
                    _ => PachimonStatType.Count,
                };
                var fixedChange = fixedStat == PachimonStatType.Count
                    ? null
                    : changes.FirstOrDefault(change => change.StatType == fixedStat);
                if (entry.GeneratedData.EquipmentSlot != equipment.Slot
                    || changes.Count != expectedCount
                    || main == null
                    || main.Amount < minimumMain
                    || main.Amount > maximumMain
                    || additional == null
                    || additional.Amount != Math.Max(1, main.Amount / 3)
                    || (fixedStat != PachimonStatType.Count
                        && (fixedChange == null
                            || fixedChange.Amount != StatUnitValue.Get(fixedStat) * 4)))
                {
                    throw new MapGenerationException(
                        $"City group {cityGroupId} Equipment {equipment.ItemId} effects are invalid.");
                }
            }
        }

        private void ValidateCityStockDistribution(RunMap map)
        {
            var cityStocks = map.NodeGroups.Values
                .Select(group => ((CityNodeContent)map.GetNode(group.NodeIds[0]).Content)
                    .StockEntries)
                .ToArray();
            var allEntries = cityStocks.SelectMany(stock => stock).ToArray();
            if (allEntries.Count(entry => entry.ItemId == ItemIds.Potion)
                    != CityStockGenerator.PotionTotalCopies
                || allEntries.Count(entry => entry.ItemId == ItemIds.MnPotion)
                    != CityStockGenerator.MnPotionTotalCopies)
            {
                throw new MapGenerationException(
                    "City recovery Item distribution is invalid.");
            }

            foreach (var stock in cityStocks)
            {
                var equipmentCount = stock.Count(entry =>
                    _itemCatalog.Get(entry.ItemId) is EquipmentItemAsset);
                if (equipmentCount != CityStockGenerator.EquipmentPerCity)
                {
                    throw new MapGenerationException(
                        "Each City requires exactly six Equipment entries.");
                }

                var engravingStats = stock
                    .Select(entry => _itemCatalog.Get(entry.ItemId))
                    .OfType<EngravingItemAsset>()
                    .Select(item => item.TargetStat)
                    .Distinct()
                    .Count();
                if (engravingStats != (int)PachimonStatType.Count)
                {
                    throw new MapGenerationException(
                        "Each City requires at least one Engraving for every Stat.");
                }

                var machines = stock
                    .Select(entry => _itemCatalog.Get(entry.ItemId))
                    .OfType<SkillMachineItemAsset>()
                    .Where(item => item.SkillId >= SkillIdRanges.FirstMachineExclusiveId
                        && item.SkillId <= SkillIdRanges.LastMachineExclusiveId)
                    .ToArray();
                if (machines.Count(item =>
                        item.Skill.AllocationType == AllocationType.Unassigned)
                        != CityStockGenerator.MachineCopiesPerPoolPerCity
                    || machines.Count(item =>
                        item.Skill.AllocationType != AllocationType.Unassigned)
                        != CityStockGenerator.MachineCopiesPerPoolPerCity)
                {
                    throw new MapGenerationException(
                        "Each City requires one neutral and one Attribute machine-exclusive Skill.");
                }
            }

            foreach (var engraving in _itemCatalog.Items.OfType<EngravingItemAsset>())
            {
                if (allEntries.Count(entry => entry.ItemId == engraving.ItemId)
                    != CityStockGenerator.EngravingCopiesPerStat)
                {
                    throw new MapGenerationException(
                        $"Engraving {engraving.ItemId} distribution is invalid.");
                }
            }

            foreach (var equipment in _itemCatalog.Items.OfType<EquipmentItemAsset>())
            {
                if (allEntries.Count(entry => entry.ItemId == equipment.ItemId)
                    != CityStockGenerator.EquipmentCopiesPerDefinition)
                {
                    throw new MapGenerationException(
                        $"Equipment {equipment.ItemId} distribution is invalid.");
                }
            }

            var distributedMachineIds = allEntries
                .Select(entry => _itemCatalog.Get(entry.ItemId))
                .OfType<SkillMachineItemAsset>()
                .Where(item => item.SkillId >= SkillIdRanges.FirstMachineExclusiveId
                    && item.SkillId <= SkillIdRanges.LastMachineExclusiveId)
                .Select(item => item.ItemId)
                .ToArray();
            var expectedMachineCount = _settings.CityRows.Length
                * CityStockGenerator.MachineCopiesPerPoolPerCity
                * 2;
            if (distributedMachineIds.Length != expectedMachineCount
                || distributedMachineIds.Distinct().Count()
                    != distributedMachineIds.Length)
            {
                throw new MapGenerationException(
                    "Machine-exclusive City stock contains missing or duplicate Items.");
            }
        }

        private static void ValidateEnemyPartyOrder(
            RunMap map,
            RunPachimonPool pachimonPool)
        {
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

                var previousMaxHp = int.MaxValue;
                foreach (var instanceId in enemyIds)
                {
                    var instance = pachimonPool.Get(instanceId)
                        ?? throw new MapGenerationException(
                            $"Node {node.NodeId} references missing Pachimon {instanceId}.");
                    if (instance.MaxHp > previousMaxHp)
                    {
                        throw new MapGenerationException(
                            $"Node {node.NodeId} Enemy party is not ordered by MaxHP.");
                    }

                    previousMaxHp = instance.MaxHp;
                }
            }
        }

        private void ValidatePachimonAllocation(RunMap map, RunPachimonPool pachimonPool)
        {
            var matchingBattleCount = 0;
            foreach (var node in map.Nodes.Values)
            {
                switch (node.Content)
                {
                    case BattleNodeContent battle
                        when battle.NodeReward.FirstElement?.Kind == RewardElementKind.Attribute:
                    {
                        var attribute = battle.NodeReward.FirstElement.Attribute
                            ?? throw new MapGenerationException("Attribute Reward has no attribute.");
                        var expectedType = FromAttribute(attribute);
                        if (!battle.EnemyPachimonInstanceIds.Any(instanceId =>
                                pachimonPool.Get(instanceId)?.AllocationType == expectedType))
                        {
                            throw new MapGenerationException(
                                $"Node {node.NodeId} requires one {expectedType} Pachimon.");
                        }
                        matchingBattleCount++;
                        break;
                    }
                    case GymNodeContent gym:
                    {
                        var attribute = gym.NodeReward.BadgeAttribute
                            ?? throw new MapGenerationException("Gym Reward has no Badge attribute.");
                        AssertLeagueMatchingMembers(
                            pachimonPool,
                            gym.EnemyPachimonInstanceIds,
                            FromAttribute(attribute),
                            node.NodeId);
                        break;
                    }
                    case EliteNodeContent elite:
                    {
                        var style = _trainerStyleCatalog.Get(elite.TrainerProfile.StyleId)
                            ?? throw new MapGenerationException(
                                $"Elite node {node.NodeId} has an unknown TrainerStyle.");
                        AssertLeagueMatchingMembers(
                            pachimonPool,
                            elite.EnemyPachimonInstanceIds,
                            FromTheme(style.Theme),
                            node.NodeId);
                        break;
                    }
                }
            }

            var expectedMatchingBattleCount = Enum.GetValues(typeof(PachimonAttribute)).Length
                * NodeRewardGenerator.AttributeCopiesPerSlot;
            if (matchingBattleCount != expectedMatchingBattleCount)
            {
                throw new MapGenerationException(
                    $"Expected {expectedMatchingBattleCount} Attribute matching parties, "
                    + $"but found {matchingBattleCount}.");
            }
        }

        private static void AssertLeagueMatchingMembers(
            RunPachimonPool pachimonPool,
            IReadOnlyList<string> instanceIds,
            AllocationType expectedType,
            string nodeId)
        {
            var matchingCount = instanceIds.Count(instanceId =>
            {
                var instance = pachimonPool.Get(instanceId);
                if (instance == null)
                {
                    throw new MapGenerationException(
                        $"Node {nodeId} references missing Pachimon {instanceId}.");
                }

                return instance.AllocationType == expectedType;
            });
            var requiredCount = PartySize - 1;
            if (matchingCount < requiredCount)
            {
                throw new MapGenerationException(
                    $"Node {nodeId} requires at least {requiredCount} {expectedType} Pachimon, "
                    + $"but received {matchingCount}.");
            }
        }

        private void ValidateRewardsAndTrainers(RunMap map)
        {
            var battles = map.Nodes.Values
                .Where(node => node.NodeType == NodeType.Battle)
                .Select(node => (BattleNodeContent)node.Content)
                .ToArray();
            if (battles.Length != NodeRewardGenerator.BattleRewardCount
                || battles.Sum(content => content.NodeReward.Gold)
                    != NodeRewardGenerator.TotalBattleGold
                || battles.Any(content => content.NodeReward.Gold < NodeRewardGenerator.MinimumGold
                    || content.NodeReward.Gold > NodeRewardGenerator.MaximumGold))
            {
                throw new MapGenerationException("Battle Gold distribution is invalid.");
            }

            var firstElements = battles.Select(content => content.NodeReward.FirstElement).ToArray();
            var secondElements = battles.Select(content => content.NodeReward.SecondElement).ToArray();
            if (firstElements.Any(element => element == null)
                || secondElements.Any(element => element == null))
            {
                throw new MapGenerationException("Every Battle Reward requires two elements.");
            }

            foreach (PachimonAttribute attribute in Enum.GetValues(typeof(PachimonAttribute)))
            {
                var firstCount = firstElements.Count(element =>
                    element.Kind == RewardElementKind.Attribute
                    && element.Attribute == attribute);
                var secondCount = secondElements.Count(element =>
                    element.Kind == RewardElementKind.Attribute
                    && element.Attribute == attribute);
                if (firstCount != NodeRewardGenerator.AttributeCopiesPerSlot
                    || secondCount != NodeRewardGenerator.AttributeCopiesPerSlot)
                {
                    throw new MapGenerationException(
                        $"Attribute {attribute} must appear "
                        + $"{NodeRewardGenerator.AttributeCopiesPerSlot} times in each Reward role.");
                }
            }

            foreach (var kind in new[]
                     {
                         RewardElementKind.MaxHp,
                         RewardElementKind.MaxMn,
                         RewardElementKind.Speed,
                         RewardElementKind.DamageBonus,
                         RewardElementKind.ResistBonus,
                     })
            {
                if (firstElements.Count(element => element.Kind == kind)
                        != NodeRewardGenerator.NonAttributeCopiesPerSlot
                    || secondElements.Count(element => element.Kind == kind)
                        != NodeRewardGenerator.NonAttributeCopiesPerSlot)
                {
                    throw new MapGenerationException(
                        $"Reward {kind} must appear "
                        + $"{NodeRewardGenerator.NonAttributeCopiesPerSlot} times in each slot.");
                }
            }

            if (firstElements.Count(element => element.Kind == RewardElementKind.BonusGold)
                    != NodeRewardGenerator.BonusGoldCopiesPerSlot
                || secondElements.Count(element => element.Kind == RewardElementKind.BonusGold)
                    != NodeRewardGenerator.BonusGoldCopiesPerSlot)
            {
                throw new MapGenerationException("Bonus Gold Reward element counts are invalid.");
            }

            if (battles.Any(content => IsSameRewardElement(
                    content.NodeReward.FirstElement,
                    content.NodeReward.SecondElement)))
            {
                throw new MapGenerationException("A Battle Reward contains duplicate elements.");
            }

            if (battles.Count(content => content.NodeReward.IsBonusGold)
                    != NodeRewardGenerator.BonusGoldCopiesPerSlot * 2)
            {
                throw new MapGenerationException("Bonus Gold Reward distribution is invalid.");
            }

            foreach (var battle in battles)
            {
                ValidateTrainerProfile(
                    battle.TrainerProfile,
                    TrainerRole.Normal,
                    TrainerThemeResolver.FromBattleReward(battle.NodeReward));
            }

            var gyms = map.Nodes.Values
                .Where(node => node.NodeType == NodeType.Gym)
                .Select(node => (GymNodeContent)node.Content)
                .ToArray();
            if (gyms.Length != NodeRewardGenerator.GymRewardCount
                || gyms.Sum(content => content.NodeReward.Gold)
                    != NodeRewardGenerator.TotalGymGold
                || gyms.Any(content => content.NodeReward.Gold
                        < NodeRewardGenerator.MinimumGold
                    || content.NodeReward.Gold
                        > NodeRewardGenerator.MaximumGold))
            {
                throw new MapGenerationException("Gym Gold distribution is invalid.");
            }

            foreach (PachimonAttribute attribute in Enum.GetValues(typeof(PachimonAttribute)))
            {
                if (gyms.Count(content => content.NodeReward.BadgeAttribute == attribute) != 3)
                {
                    throw new MapGenerationException($"Badge {attribute} must appear 3 times.");
                }
            }

            foreach (var gym in gyms)
            {
                var attribute = gym.NodeReward.BadgeAttribute
                    ?? throw new MapGenerationException("Gym has no Badge attribute.");
                ValidateTrainerProfile(
                    gym.TrainerProfile,
                    TrainerRole.GymLeader,
                    TrainerThemeResolver.FromAttribute(attribute));
                if (gym.TrainerProfile.FavoredAttribute != attribute
                    || !gym.TrainerProfile.WeakAttribute.HasValue
                    || gym.TrainerProfile.WeakAttribute == attribute)
                {
                    throw new MapGenerationException(
                        "Gym favored/weak attributes are missing or invalid.");
                }
            }

            foreach (PachimonAttribute attribute in Enum.GetValues(typeof(PachimonAttribute)))
            {
                if (gyms.Count(content =>
                        content.TrainerProfile.WeakAttribute == attribute) != 3)
                {
                    throw new MapGenerationException(
                        $"Gym weakness {attribute} must appear 3 times.");
                }
            }

            var elites = map.Nodes.Values
                .Where(node => node.NodeType == NodeType.Elite)
                .Select(node => (EliteNodeContent)node.Content)
                .ToArray();
            var eliteThemes = elites
                .Select(content =>
                {
                    var favoredAttribute = content.TrainerProfile.FavoredAttribute
                        ?? throw new MapGenerationException(
                            "Elite favored attribute is missing.");
                    return ValidateTrainerProfile(
                        content.TrainerProfile,
                        TrainerRole.Elite,
                        TrainerThemeResolver.FromAttribute(favoredAttribute)).Theme;
                })
                .ToArray();
            if (eliteThemes.Distinct().Count() != elites.Length)
            {
                throw new MapGenerationException("Elite themes must be unique within a Run.");
            }

            var eliteFavoredAttributes = elites
                .Select(content => content.TrainerProfile.FavoredAttribute)
                .ToArray();
            var eliteWeakAttributes = elites
                .Select(content => content.TrainerProfile.WeakAttribute)
                .ToArray();
            if (eliteFavoredAttributes.Any(attribute => !attribute.HasValue)
                || eliteWeakAttributes.Any(attribute => !attribute.HasValue)
                || eliteFavoredAttributes.Distinct().Count() != elites.Length
                || eliteWeakAttributes.Distinct().Count() != elites.Length
                || eliteFavoredAttributes.Intersect(eliteWeakAttributes).Any())
            {
                throw new MapGenerationException(
                    "Elite favored and weak attributes must partition eight attributes.");
            }

            var leagueStyleIds = gyms.Select(content => content.TrainerProfile.StyleId)
                .Concat(elites.Select(content => content.TrainerProfile.StyleId))
                .ToArray();
            if (leagueStyleIds.Length != 28
                || leagueStyleIds.Distinct().Count() != leagueStyleIds.Length)
            {
                throw new MapGenerationException("League TrainerStyles must not repeat within a Run.");
            }
        }

        private static bool IsSameRewardElement(RewardElement first, RewardElement second)
        {
            return first.Kind == second.Kind && first.Attribute == second.Attribute;
        }

        private TrainerStyleDefinition ValidateTrainerProfile(
            TrainerProfile profile,
            TrainerRole expectedRole,
            TrainerTheme? expectedTheme)
        {
            if (profile == null || profile.Role != expectedRole)
            {
                throw new MapGenerationException($"Trainer role {expectedRole} is missing or invalid.");
            }

            var style = _trainerStyleCatalog.Get(profile.StyleId);
            var expectedCategory = expectedRole == TrainerRole.Normal
                ? TrainerStyleCategory.Normal
                : TrainerStyleCategory.League;
            if (style == null
                || style.StyleCategory != expectedCategory
                || expectedTheme.HasValue && style.Theme != expectedTheme.Value)
            {
                throw new MapGenerationException(
                    $"TrainerStyle {profile.StyleId} does not match {expectedRole}/{expectedTheme}.");
            }

            var name = _trainerNameCatalog.Get(profile.NameId);
            if (name == null || name.Gender != style.Gender)
            {
                throw new MapGenerationException(
                    $"Trainer name {profile.NameId} does not match style gender {style.Gender}.");
            }

            return style;
        }

        private static void ValidateSpeciesNodeSeparation(
            RunMap map,
            RunPachimonPool pachimonPool)
        {
            foreach (var node in map.Nodes.Values)
            {
                var speciesIds = new HashSet<int>();
                foreach (var instanceId in GetAssignedPachimonIds(node))
                {
                    var instance = pachimonPool.Get(instanceId);
                    if (!speciesIds.Add(instance.SpeciesId))
                    {
                        throw new MapGenerationException(
                            $"Species {instance.SpeciesId} was assigned twice "
                            + $"to node {node.NodeId}.");
                    }
                }
            }
        }

        private static IEnumerable<string> GetAssignedPachimonIds(RunMap map)
        {
            foreach (var node in map.Nodes.Values)
            {
                foreach (var instanceId in GetAssignedPachimonIds(node)) yield return instanceId;
            }
        }

        private static IEnumerable<string> GetAssignedPachimonIds(MapNode node)
        {
            switch (node.Content)
            {
                case StartNodeContent start:
                    foreach (var id in start.CandidatePachimonInstanceIds) yield return id;
                    break;
                case BattleNodeContent battle:
                    foreach (var id in battle.EnemyPachimonInstanceIds) yield return id;
                    break;
                case GymNodeContent gym:
                    foreach (var id in gym.EnemyPachimonInstanceIds) yield return id;
                    break;
                case EliteNodeContent elite:
                    foreach (var id in elite.EnemyPachimonInstanceIds) yield return id;
                    break;
            }
        }

        private static HashSet<string> TraverseForward(RunMap map, string startNodeId)
        {
            var visited = new HashSet<string>();
            var pending = new Stack<string>();
            pending.Push(startNodeId);

            while (pending.Count > 0)
            {
                var nodeId = pending.Pop();
                if (!visited.Add(nodeId))
                {
                    continue;
                }

                var node = map.GetNode(nodeId);
                foreach (var nextNodeId in node.NextNodeIds)
                {
                    pending.Push(nextNodeId);
                }
            }

            return visited;
        }

        private static HashSet<string> TraverseBackward(RunMap map, string targetNodeId)
        {
            var predecessors = map.Nodes.Keys.ToDictionary(nodeId => nodeId, _ => new List<string>());
            foreach (var node in map.Nodes.Values)
            {
                foreach (var nextNodeId in node.NextNodeIds)
                {
                    predecessors[nextNodeId].Add(node.NodeId);
                }
            }

            var visited = new HashSet<string>();
            var pending = new Stack<string>();
            pending.Push(targetNodeId);

            while (pending.Count > 0)
            {
                var nodeId = pending.Pop();
                if (!visited.Add(nodeId))
                {
                    continue;
                }

                foreach (var previousNodeId in predecessors[nodeId])
                {
                    pending.Push(previousNodeId);
                }
            }

            return visited;
        }

        private static void AssertCount(IEnumerable<MapNode> nodes, NodeType nodeType, int expected)
        {
            var actual = nodes.Count(node => node.NodeType == nodeType);
            if (actual != expected)
            {
                throw new MapGenerationException(
                    $"Expected {expected} {nodeType} nodes, but generated {actual}.");
            }
        }

        private int GetMaximumGymCount(
            IReadOnlyList<List<NodeBuilder>> rows,
            IReadOnlyDictionary<string, NodeBuilder> nodes)
        {
            var unreachable = int.MinValue;
            var maximumByNodeId = nodes.Keys.ToDictionary(nodeId => nodeId, _ => unreachable);
            maximumByNodeId[rows[0][0].NodeId] = 0;

            for (var rowIndex = 0; rowIndex < _settings.LeagueGateRow; rowIndex++)
            {
                foreach (var node in rows[rowIndex])
                {
                    var current = maximumByNodeId[node.NodeId];
                    if (current == unreachable)
                    {
                        continue;
                    }

                    foreach (var nextNodeId in node.NextNodeIds)
                    {
                        var nextNode = nodes[nextNodeId];
                        var candidate = current + (nextNode.NodeType == NodeType.Gym ? 1 : 0);
                        maximumByNodeId[nextNodeId] = Math.Max(maximumByNodeId[nextNodeId], candidate);
                    }
                }
            }

            return maximumByNodeId[rows[_settings.LeagueGateRow][0].NodeId];
        }

        private IEnumerable<NodeBuilder> GetAdventureNodes(IReadOnlyList<List<NodeBuilder>> rows)
        {
            for (var rowIndex = _settings.AdventureRowStart;
                 rowIndex <= _settings.MainRowEnd;
                 rowIndex++)
            {
                foreach (var node in rows[rowIndex])
                {
                    yield return node;
                }
            }
        }

        private static bool HasAdjacentNodeOfType(
            NodeBuilder node,
            IReadOnlyDictionary<string, NodeBuilder> nodes,
            NodeType nodeType)
        {
            return node.NextNodeIds.Any(id => nodes[id].NodeType == nodeType)
                || nodes.Values.Any(other => other.NodeType == nodeType
                    && other.NextNodeIds.Contains(node.NodeId));
        }

        private static bool AreAdjacent(
            NodeBuilder first,
            NodeBuilder second,
            IReadOnlyDictionary<string, NodeBuilder> nodes)
        {
            return first.NextNodeIds.Contains(second.NodeId)
                || second.NextNodeIds.Contains(first.NodeId)
                || nodes[first.NodeId].NextNodeIds.Contains(second.NodeId)
                || nodes[second.NodeId].NextNodeIds.Contains(first.NodeId);
        }

        private static void ClearNodeType(IReadOnlyList<List<NodeBuilder>> rows, NodeType nodeType)
        {
            foreach (var node in rows.SelectMany(row => row))
            {
                if (node.NodeType == nodeType)
                {
                    node.NodeType = null;
                    node.Content = null;
                }
            }
        }

        private static void Shuffle<T>(IList<T> values, Random random)
        {
            for (var index = values.Count - 1; index > 0; index--)
            {
                var swapIndex = random.Next(index + 1);
                (values[index], values[swapIndex]) = (values[swapIndex], values[index]);
            }
        }

        private static PachimonAttribute[] CreateBalancedWeaknesses(
            IReadOnlyList<PachimonAttribute> favoredAttributes,
            Random random)
        {
            const int maximumAttempts = 1000;
            for (var attempt = 0; attempt < maximumAttempts; attempt++)
            {
                var weaknesses = favoredAttributes.ToList();
                Shuffle(weaknesses, random);
                if (weaknesses
                    .Select((attribute, index) =>
                        attribute != favoredAttributes[index])
                    .All(isValid => isValid))
                {
                    return weaknesses.ToArray();
                }
            }

            throw new MapGenerationException(
                "Could not assign balanced Gym weaknesses without matching favored attributes.");
        }

        private static int GetColumnIndex(string nodeId)
        {
            var separatorIndex = nodeId.LastIndexOf('_');
            return int.Parse(nodeId.Substring(separatorIndex + 1));
        }

        private sealed class NodeBuilder
        {
            public NodeBuilder(string nodeId, int rowIndex, int columnIndex)
            {
                NodeId = nodeId;
                RowIndex = rowIndex;
                ColumnIndex = columnIndex;
            }

            public string NodeId { get; }

            public int RowIndex { get; }

            public int ColumnIndex { get; }

            public NodeType? NodeType { get; set; }

            public NodeContent Content { get; set; }

            public NodeReward NodeReward { get; set; }

            public TrainerProfile TrainerProfile { get; set; }

            public HashSet<string> NextNodeIds { get; } = new();
        }

        private sealed class CityPlacement
        {
            public CityPlacement(
                string cityGroupId,
                int shopSeed,
                NodeBuilder leftNode,
                NodeBuilder rightNode)
            {
                CityGroupId = cityGroupId;
                ShopSeed = shopSeed;
                Nodes = new[] { leftNode, rightNode };
            }

            public string CityGroupId { get; }

            public int ShopSeed { get; }

            public IReadOnlyList<NodeBuilder> Nodes { get; }
        }

        private sealed class PlacementSlot
        {
            public PlacementSlot(NodeBuilder node, int index)
            {
                Node = node;
                Index = index;
            }

            public NodeBuilder Node { get; }

            public int Index { get; }
        }

        private enum SpeciesPlacementRule
        {
            AvoidAdjacentNodes = 0,
            AvoidSameRow = 1,
            AvoidSameNode = 2,
        }

        private static readonly SpeciesPlacementRule[] SpeciesPlacementRules =
        {
            SpeciesPlacementRule.AvoidAdjacentNodes,
            SpeciesPlacementRule.AvoidSameRow,
            SpeciesPlacementRule.AvoidSameNode,
        };
    }
}
