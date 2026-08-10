using System;
using Pachimon.Passives;

namespace Pachimon.Battle
{
    public sealed class BurnPursuitPassiveLogic : IPassiveLogic
    {
        private readonly BurnPursuitPassiveAsset _definition;

        public BurnPursuitPassiveLogic(
            BattleUnitState owner,
            BurnPursuitPassiveAsset definition)
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
                    != true
                || damageEvent.Target.GetStatus(BattleStatusId.Burn) == null)
            {
                return;
            }

            damageEvent.MultiplyDamage(_definition.DamagePercent);
        }
    }
}
