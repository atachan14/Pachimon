using System;
using Pachimon.Data;
using Pachimon.Reward;

namespace Pachimon.Battle
{
    public sealed class OutgoingAttributeDamagePassiveLogic : IPassiveLogic
    {
        public const int DefaultDamagePercent = 130;

        private readonly int _passiveId;
        private readonly PachimonAttribute _attribute;
        private readonly int _damagePercent;

        public OutgoingAttributeDamagePassiveLogic(
            int passiveId,
            BattleUnitState owner,
            PachimonAttribute attribute)
        {
            if (passiveId <= 0) throw new ArgumentOutOfRangeException(nameof(passiveId));
            _passiveId = passiveId;
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _attribute = attribute;
            _damagePercent = DefaultDamagePercent;
        }

        public OutgoingAttributeDamagePassiveLogic(
            BattleUnitState owner,
            OutgoingAttributeDamagePassiveAsset definition)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            _passiveId = definition.PassiveId;
            _attribute = definition.Attribute;
            _damagePercent = definition.DamagePercent;
        }

        public BattleUnitState Owner { get; }

        public void Handle(IBattleEvent battleEvent)
        {
            if (battleEvent is not BeforeAttributeDamageEvent damageEvent
                || !ReferenceEquals(damageEvent.Source, Owner)
                || damageEvent.Attribute != _attribute
                || damageEvent.Calculation?.Context.ApplyOutgoingModifiers
                    == false)
            {
                return;
            }

            damageEvent.MultiplyDamage(_damagePercent);
            battleEvent.State.AddLog(
                $"{AttributePlaceholderName.FromCyclicId(_passiveId)} increased "
                + $"{damageEvent.Attribute} damage by {_damagePercent - 100}%.");
        }
    }
}
