using System;
using System.Collections.Generic;
using System.Linq;

namespace Pachimon.Battle
{
    public enum ChargePhase
    {
        Charging = 0,
        Charged = 1,
    }

    public sealed class ChargeStatusRuntimeState
    {
        public ChargeStatusRuntimeState(ChargePhase phase)
        {
            Phase = phase;
        }

        public ChargePhase Phase { get; }
    }

    public static class ChargeStatusQuery
    {
        public static IReadOnlyList<BattleStatusInstance> GetChargeStatuses(
            this BattleUnitState unit,
            ChargePhase phase)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            return unit.GetStatuses(BattleStatusId.Charge)
                .Where(status =>
                    status.RuntimeData is ChargeStatusRuntimeState state
                    && state.Phase == phase)
                .ToArray();
        }
    }
}
