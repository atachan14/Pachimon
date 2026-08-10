using System;
using Pachimon.Passives;
using Pachimon.Reward;

namespace Pachimon.Battle
{
    public sealed class DragonBoxerPassiveLogic : IPassiveLogic
    {
        private readonly DragonBoxerPassiveAsset _definition;

        public DragonBoxerPassiveLogic(
            BattleUnitState owner,
            DragonBoxerPassiveAsset definition)
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
                case BeforeAttributeDamageEvent before:
                    ApplyDragonBonus(before);
                    break;
                case AttributeDamageAppliedEvent applied:
                    UpdateStacks(applied);
                    break;
            }
        }

        private void ApplyDragonBonus(BeforeAttributeDamageEvent damageEvent)
        {
            if (!ReferenceEquals(damageEvent.Source, Owner)
                || damageEvent.Attribute != PachimonAttribute.Dragon
                || damageEvent.Calculation?.Context.ApplyOutgoingModifiers == false)
            {
                return;
            }

            var stacks = Owner.GetStatus(BattleStatusId.DragonBoxer)?.StackCount ?? 0;
            if (stacks > 0)
            {
                damageEvent.MultiplyDamage(checked(
                    100 + stacks * _definition.DamagePercentPerStack));
            }
        }

        private void UpdateStacks(AttributeDamageAppliedEvent damageEvent)
        {
            if (!ReferenceEquals(damageEvent.Source, Owner)
                || damageEvent.FinalDamage <= 0)
            {
                return;
            }

            if (damageEvent.Attribute == PachimonAttribute.Dragon)
            {
                var current = Owner.GetStatus(BattleStatusId.DragonBoxer);
                Owner.ApplyOrReplaceStatus(new BattleStatusInstance(
                    BattleStatusId.DragonBoxer,
                    BattleStatusCategory.None,
                    Owner,
                    value: 0,
                    stackCount: checked(
                        (current?.StackCount ?? 0) + _definition.StackGain),
                    definition: _definition.StackStatus));
                return;
            }

            var existing = Owner.GetStatus(BattleStatusId.DragonBoxer);
            if (existing == null)
            {
                return;
            }

            var remaining = existing.StackCount / 2;
            Owner.TryConsumeStatus(BattleStatusId.DragonBoxer, out _);
            if (remaining > 0)
            {
                Owner.ApplyOrReplaceStatus(new BattleStatusInstance(
                    BattleStatusId.DragonBoxer,
                    BattleStatusCategory.None,
                    Owner,
                    value: 0,
                    stackCount: remaining,
                    definition: _definition.StackStatus));
            }
        }
    }
}
