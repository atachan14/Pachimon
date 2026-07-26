using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class BattleUnitResult
    {
        public BattleUnitResult(
            string instanceId,
            int currentHp,
            int effectiveMaxHp,
            int currentMn,
            int effectiveMaxMn)
        {
            InstanceId = instanceId;
            CurrentHp = currentHp;
            EffectiveMaxHp = effectiveMaxHp;
            CurrentMn = currentMn;
            EffectiveMaxMn = effectiveMaxMn;
        }

        public string InstanceId { get; }
        public int CurrentHp { get; }
        public int EffectiveMaxHp { get; }
        public int CurrentMn { get; }
        public int EffectiveMaxMn { get; }
    }

    public sealed class BattleResult
    {
        private BattleResult(
            BattleOutcome outcome,
            IReadOnlyList<BattleUnitResult> playerUnits)
        {
            Outcome = outcome;
            PlayerUnits = playerUnits;
        }

        public BattleOutcome Outcome { get; }
        public IReadOnlyList<BattleUnitResult> PlayerUnits { get; }

        public static BattleResult From(BattleState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var outcome = state.EvaluateOutcome();
            if (outcome == BattleOutcome.Undecided)
            {
                throw new InvalidOperationException(
                    "A Battle Result cannot be created before the Battle is resolved.");
            }

            return new BattleResult(
                outcome,
                state.Player.Units
                    .Select(unit => new BattleUnitResult(
                        unit.InstanceId,
                        unit.CurrentHp,
                        unit.MaxHp,
                        unit.CurrentMn,
                        unit.MaxMn))
                    .ToArray());
        }
    }

    public static class BattleResultCommitter
    {
        public static void CommitPlayerResources(
            BattleResult result,
            RunPachimonPool pachimonPool)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (pachimonPool == null) throw new ArgumentNullException(nameof(pachimonPool));
            if (result.Outcome != BattleOutcome.PlayerVictory)
            {
                throw new InvalidOperationException(
                    "Player resources are committed only after a Player victory.");
            }

            var updates = result.PlayerUnits
                .Select(unitResult => new
                {
                    Result = unitResult,
                    Instance = pachimonPool.Get(unitResult.InstanceId)
                        ?? throw new InvalidOperationException(
                            $"Pachimon Instance '{unitResult.InstanceId}' was not found."),
                })
                .ToArray();

            foreach (var update in updates)
            {
                update.Instance.SetCurrentHp(
                    update.Result.CurrentHp,
                    update.Result.EffectiveMaxHp);
                update.Instance.SetCurrentMn(
                    update.Result.CurrentMn,
                    update.Result.EffectiveMaxMn);
            }
        }
    }
}
