using System;
using Pachimon.Passives;
using Pachimon.Reward;

namespace Pachimon.Battle
{
    public sealed class StoredChargePassiveLogic : IPassiveLogic
    {
        private readonly StoredChargePassiveAsset _definition;

        public StoredChargePassiveLogic(
            BattleUnitState owner,
            StoredChargePassiveAsset definition)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _definition = definition
                ?? throw new ArgumentNullException(nameof(definition));
        }

        public BattleUnitState Owner { get; }

        public void Handle(IBattleEvent battleEvent)
        {
            switch (battleEvent)
            {
                case BeforeAttributeDamageEvent beforeDamage:
                    HandleBeforeDamage(beforeDamage);
                    break;
                case AttributeDamageAppliedEvent appliedDamage:
                    HandleAppliedDamage(appliedDamage);
                    break;
            }
        }

        private void HandleBeforeDamage(BeforeAttributeDamageEvent damageEvent)
        {
            if (!ReferenceEquals(damageEvent.Source, Owner)
                || damageEvent.Attribute != PachimonAttribute.Electric
                || damageEvent.Calculation?.Context.ApplyOutgoingModifiers
                    == false
                || !Owner.TryConsumeStatus(
                    BattleStatusId.StoredCharge,
                    out var storedCharge))
            {
                return;
            }

            damageEvent.MultiplyDamage(checked(
                100
                + storedCharge.StackCount
                * _definition.DamagePercentPerStack));
        }

        private void HandleAppliedDamage(AttributeDamageAppliedEvent damageEvent)
        {
            if (!Owner.IsAlive
                || damageEvent.Attribute != PachimonAttribute.Electric)
            {
                return;
            }

            Owner.AddStatusStacks(
                BattleStatusId.StoredCharge,
                BattleStatusCategory.Charge,
                Owner,
                value: 0,
                stackCount: 1);
        }
    }
}
