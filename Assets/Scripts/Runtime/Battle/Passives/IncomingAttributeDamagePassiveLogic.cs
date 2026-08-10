using System;
using Pachimon.Passives;

namespace Pachimon.Battle
{
    public sealed class IncomingAttributeDamagePassiveLogic : IPassiveLogic
    {
        private readonly IncomingAttributeDamagePassiveAsset _definition;

        public IncomingAttributeDamagePassiveLogic(
            BattleUnitState owner,
            IncomingAttributeDamagePassiveAsset definition)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _definition = definition
                ?? throw new ArgumentNullException(nameof(definition));
        }

        public BattleUnitState Owner { get; }

        public void Handle(IBattleEvent battleEvent)
        {
            if (battleEvent is not BeforeAttributeDamageEvent damageEvent
                || !ReferenceEquals(damageEvent.Target, Owner)
                || damageEvent.Attribute != _definition.Attribute)
            {
                return;
            }

            damageEvent.MultiplyDamage(_definition.DamagePercent);
        }
    }
}
