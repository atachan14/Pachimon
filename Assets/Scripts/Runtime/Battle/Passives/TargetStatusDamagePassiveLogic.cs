using System;
using Pachimon.Passives;

namespace Pachimon.Battle
{
    public sealed class TargetStatusDamagePassiveLogic : IPassiveLogic
    {
        private readonly TargetStatusDamagePassiveAsset _definition;

        public TargetStatusDamagePassiveLogic(
            BattleUnitState owner,
            TargetStatusDamagePassiveAsset definition)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _definition = definition
                ?? throw new ArgumentNullException(nameof(definition));
        }

        public BattleUnitState Owner { get; }

        public void Handle(IBattleEvent battleEvent)
        {
            if (battleEvent is not BeforeAttributeDamageEvent damageEvent
                || !ReferenceEquals(damageEvent.Source, Owner)
                || damageEvent.Calculation?.Context.ApplyOutgoingModifiers
                    == false
                || !damageEvent.Target.HasStatusCategory(
                    _definition.TargetCategory))
            {
                return;
            }

            damageEvent.MultiplyDamage(_definition.DamagePercent);
        }
    }
}
