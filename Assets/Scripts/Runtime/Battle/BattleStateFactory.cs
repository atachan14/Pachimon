using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Data;
using Pachimon.Run;
using Pachimon.Passives;

namespace Pachimon.Battle
{
    public sealed class BattleStateFactory
    {
        private readonly RunPachimonPool _pachimonPool;
        private readonly PachimonCatalog _pachimonCatalog;
        private readonly PassiveStatModifierRegistry _passiveStatModifierRegistry;
        private readonly PassiveCatalog _passiveCatalog;

        public BattleStateFactory(
            RunPachimonPool pachimonPool,
            PachimonCatalog pachimonCatalog,
            PassiveCatalog passiveCatalog,
            PassiveStatModifierRegistry passiveStatModifierRegistry)
        {
            _pachimonPool = pachimonPool
                ?? throw new ArgumentNullException(nameof(pachimonPool));
            _pachimonCatalog = pachimonCatalog
                ?? throw new ArgumentNullException(nameof(pachimonCatalog));
            _passiveStatModifierRegistry = passiveStatModifierRegistry
                ?? throw new ArgumentNullException(nameof(passiveStatModifierRegistry));
            _passiveCatalog = passiveCatalog
                ?? throw new ArgumentNullException(nameof(passiveCatalog));
        }

        public BattleState Create(
            int battleSeed,
            IEnumerable<string> playerInstanceIds,
            IEnumerable<string> enemyInstanceIds,
            TrainerModifierSet playerModifiers,
            TrainerModifierSet enemyModifiers)
        {
            var playerIds = ValidatePartyIds(playerInstanceIds, nameof(playerInstanceIds));
            var enemyIds = ValidatePartyIds(enemyInstanceIds, nameof(enemyInstanceIds));
            if (playerIds.Intersect(enemyIds, StringComparer.Ordinal).Any())
            {
                throw new ArgumentException(
                    "The same Pachimon Instance cannot join both Battle Sides.");
            }

            return new BattleState(
                battleSeed,
                CreateSide(BattleSide.Player, playerIds, playerModifiers),
                CreateSide(BattleSide.Enemy, enemyIds, enemyModifiers),
                new PassiveLogicRegistry(_passiveCatalog));
        }

        private BattleSideState CreateSide(
            BattleSide side,
            IReadOnlyList<string> instanceIds,
            TrainerModifierSet modifiers)
        {
            return new BattleSideState(
                side,
                instanceIds.Select((instanceId, slotIndex) =>
                    CreateUnit(instanceId, side, slotIndex, modifiers)));
        }

        private BattleUnitState CreateUnit(
            string instanceId,
            BattleSide side,
            int slotIndex,
            TrainerModifierSet modifiers)
        {
            var instance = _pachimonPool.Get(instanceId)
                ?? throw new InvalidOperationException(
                    $"Pachimon Instance '{instanceId}' was not found.");
            var definition = _pachimonCatalog.Get(instance.SpeciesId)
                ?? throw new InvalidOperationException(
                    $"Pachimon Species '{instance.SpeciesId}' was not found.");
            var effectiveStats = PachimonStatService.Calculate(
                instance,
                modifiers,
                _passiveStatModifierRegistry);
            var startingHp = Math.Min(instance.CurrentHp, effectiveStats.MaxHp);
            var startingMn = Math.Min(instance.CurrentMn, effectiveStats.MaxMn);
            if (side == BattleSide.Enemy)
            {
                var unmodifiedStats = PachimonStatService.Calculate(
                    instance,
                    null,
                    _passiveStatModifierRegistry);
                startingHp = EnemyTrainerScalingService.PreserveMissingResource(
                    instance.CurrentHp,
                    unmodifiedStats.MaxHp,
                    effectiveStats.MaxHp);
                startingMn = EnemyTrainerScalingService.PreserveMissingResource(
                    instance.CurrentMn,
                    unmodifiedStats.MaxMn,
                    effectiveStats.MaxMn);
            }
            return new BattleUnitState(
                instance.InstanceId,
                instance.SpeciesId,
                definition.DisplayName,
                side,
                slotIndex,
                effectiveStats,
                startingHp,
                startingMn,
                instance.SkillSlots,
                instance.PassiveIds);
        }

        private static string[] ValidatePartyIds(
            IEnumerable<string> instanceIds,
            string parameterName)
        {
            if (instanceIds == null) throw new ArgumentNullException(parameterName);
            var ids = instanceIds.ToArray();
            if (ids.Length != BattleSideState.PartySize
                || ids.Any(string.IsNullOrWhiteSpace)
                || ids.Distinct(StringComparer.Ordinal).Count() != BattleSideState.PartySize)
            {
                throw new ArgumentException(
                    $"A Battle party requires {BattleSideState.PartySize} unique Instance IDs.",
                    parameterName);
            }

            return ids;
        }
    }
}
