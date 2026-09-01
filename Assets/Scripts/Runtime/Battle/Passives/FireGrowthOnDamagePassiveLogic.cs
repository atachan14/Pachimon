using System;
using Pachimon.Passives;

namespace Pachimon.Battle
{
    public sealed class FireGrowthOnDamagePassiveLogic : IPassiveLogic
    {
        private readonly FireGrowthOnDamagePassiveAsset _definition;

        public FireGrowthOnDamagePassiveLogic(
            BattleUnitState owner,
            FireGrowthOnDamagePassiveAsset definition)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _definition = definition
                ?? throw new ArgumentNullException(nameof(definition));
        }

        public BattleUnitState Owner { get; }

        public void Handle(IBattleEvent battleEvent)
        {
            if (battleEvent is not DamageAppliedEvent damageEvent
                || !ReferenceEquals(damageEvent.Target, Owner)
                || damageEvent.ReceivedDamage <= 0
                || (damageEvent.Tags & DamageTag.DamageOverTime) != 0
                || !Owner.IsAlive
                || _definition.FireIncreasePerDamage == 0)
            {
                return;
            }

            Owner.AddStatusStacks(
                BattleStatusId.FireGrowth,
                BattleStatusCategory.None,
                Owner,
                _definition.FireIncreasePerDamage,
                stackCount: 1);
        }
    }
}
