using System;
using System.Collections.Generic;
using Pachimon.Run;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(
        fileName = "FrozenBreakStatus",
        menuName = "Pachimon/Battle/Status/Frozen Break")]
    public sealed class FrozenBreakStatusAsset : BattleStatusAsset
    {
        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (StatusId != BattleStatusId.FrozenBreakSelf)
            {
                errors?.Add("Frozen Break Status must use Frozen Break Self ID.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string displayName,
            string description,
            Sprite icon = null)
        {
            ConfigureDefinitionForEditor(
                BattleStatusId.FrozenBreakSelf,
                displayName,
                description,
                icon);
        }
#endif
    }

    public sealed class FrozenBreakRuntimeState
    {
        public FrozenBreakRuntimeState(int totalDurationTicks, decimal healPerTick)
        {
            if (totalDurationTicks <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalDurationTicks));
            }
            if (healPerTick < 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(healPerTick));
            }

            TotalDurationTicks = totalDurationTicks;
            HealPerTick = healPerTick;
        }

        private FrozenBreakRuntimeState(
            int totalDurationTicks,
            decimal healPerTick,
            decimal healWork)
            : this(totalDurationTicks, healPerTick)
        {
            HealWork = healWork;
        }

        public int TotalDurationTicks { get; }
        public decimal HealPerTick { get; }
        public decimal HealWork { get; private set; }

        public int AccumulateHealing(int ticks)
        {
            if (ticks < 0) throw new ArgumentOutOfRangeException(nameof(ticks));
            HealWork += HealPerTick * ticks;
            var healing = SignedStatMath.FloorNonNegative(HealWork);
            HealWork -= healing;
            return healing;
        }

        public FrozenBreakRuntimeState CreateSimulationClone()
        {
            return new FrozenBreakRuntimeState(
                TotalDurationTicks,
                HealPerTick,
                HealWork);
        }
    }
}
