using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Data;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class BattleStateFactory
    {
        private readonly RunPachimonPool _pachimonPool;
        private readonly PachimonCatalog _pachimonCatalog;

        public BattleStateFactory(RunPachimonPool pachimonPool, PachimonCatalog pachimonCatalog)
        {
            _pachimonPool = pachimonPool
                ?? throw new ArgumentNullException(nameof(pachimonPool));
            _pachimonCatalog = pachimonCatalog
                ?? throw new ArgumentNullException(nameof(pachimonCatalog));
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
                CreateSide(BattleSide.Enemy, enemyIds, enemyModifiers));
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
            var effectiveStats = new EffectivePachimonStats(instance.Stats, modifiers);
            var startingHp = Math.Min(instance.CurrentHp, effectiveStats.MaxHp);
            var startingMn = Math.Min(instance.CurrentMn, effectiveStats.MaxMn);
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
