using System;
using Pachimon.Data;
using Pachimon.Reward;

namespace Pachimon.Battle
{
    public sealed class OutgoingAttributeDamagePassiveLogic : IPassiveLogic
    {
        public const int DamagePercent = 130;

        private readonly int _passiveId;
        private readonly PachimonAttribute _attribute;

        public OutgoingAttributeDamagePassiveLogic(
            int passiveId,
            BattleUnitState owner,
            PachimonAttribute attribute)
        {
            if (passiveId <= 0) throw new ArgumentOutOfRangeException(nameof(passiveId));
            _passiveId = passiveId;
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _attribute = attribute;
        }

        public BattleUnitState Owner { get; }

        public void Handle(IBattleEvent battleEvent)
        {
            if (battleEvent is not BeforeAttributeDamageEvent damageEvent
                || !ReferenceEquals(damageEvent.Source, Owner)
                || damageEvent.Attribute != _attribute)
            {
                return;
            }

            damageEvent.MultiplyDamage(DamagePercent);
            battleEvent.State.AddLog(
                $"{AttributePlaceholderName.FromCyclicId(_passiveId)} increased "
                + $"{damageEvent.Attribute} damage by 30%.");
        }
    }
}
