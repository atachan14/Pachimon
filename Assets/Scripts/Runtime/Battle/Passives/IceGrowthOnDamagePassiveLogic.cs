using System;
using Pachimon.Passives;
using Pachimon.Reward;

namespace Pachimon.Battle
{
    public sealed class IceGrowthOnDamagePassiveLogic : IPassiveLogic
    {
        private readonly IceGrowthOnDamagePassiveAsset _definition;

        public IceGrowthOnDamagePassiveLogic(
            BattleUnitState owner,
            IceGrowthOnDamagePassiveAsset definition)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _definition = definition
                ?? throw new ArgumentNullException(nameof(definition));
        }

        public BattleUnitState Owner { get; }

        public void Handle(IBattleEvent battleEvent)
        {
            if (battleEvent is not DamageAppliedEvent damageEvent
                || damageEvent.Attribute != PachimonAttribute.Ice
                || damageEvent.ReceivedDamage <= 0
                || (damageEvent.Tags & DamageTag.DamageOverTime) != 0
                || !Owner.IsAlive
                || _definition.IceIncreasePerDamage == 0)
            {
                return;
            }

            Owner.AddStatusStacks(
                BattleStatusId.IceGrowth,
                BattleStatusCategory.None,
                Owner,
                _definition.IceIncreasePerDamage,
                stackCount: 1);
        }
    }
}
