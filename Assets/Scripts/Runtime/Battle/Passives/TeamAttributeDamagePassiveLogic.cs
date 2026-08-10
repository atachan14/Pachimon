using System;
using Pachimon.Passives;

namespace Pachimon.Battle
{
    public sealed class TeamAttributeDamagePassiveLogic : IPassiveLogic
    {
        private readonly TeamAttributeDamagePassiveAsset _definition;

        public TeamAttributeDamagePassiveLogic(
            BattleUnitState owner,
            TeamAttributeDamagePassiveAsset definition)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _definition = definition
                ?? throw new ArgumentNullException(nameof(definition));
        }

        public BattleUnitState Owner { get; }

        public void Handle(IBattleEvent battleEvent)
        {
            if (battleEvent is not BeforeAttributeDamageEvent damageEvent
                || !Owner.IsAlive
                || damageEvent.Source == null
                || damageEvent.Source.Side != Owner.Side
                || damageEvent.Attribute != _definition.Attribute
                || damageEvent.Calculation?.Context.ApplyOutgoingModifiers
                    == false)
            {
                return;
            }

            damageEvent.MultiplyDamage(_definition.DamagePercent);
            damageEvent.State.AddLog(
                $"{Owner.DisplayName}\u306E{_definition.DisplayName}\uFF01");
        }
    }
}
