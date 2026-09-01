using System;
using Pachimon.Passives;
using Pachimon.Reward;

namespace Pachimon.Battle
{
    public sealed class BurningFlowerPassiveLogic : IPassiveLogic
    {
        private readonly BurningFlowerPassiveAsset _definition;

        public BurningFlowerPassiveLogic(
            BattleUnitState owner,
            BurningFlowerPassiveAsset definition)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        public BattleUnitState Owner { get; }

        public void Handle(IBattleEvent battleEvent)
        {
            if (battleEvent is not AttributeDamageAppliedEvent damage
                || !Owner.IsAlive
                || (damage.Tags & DamageTag.DamageOverTime) != 0
                || damage.AppliedDamage + damage.ShieldAbsorbedDamage <= 0
                || _definition.StatGainPerDamage <= 0)
            {
                return;
            }

            var statusId = damage.Attribute switch
            {
                PachimonAttribute.Fire => BattleStatusId.BurningFlowerLeaf,
                PachimonAttribute.Leaf => BattleStatusId.BurningFlowerFire,
                _ => (BattleStatusId?)null,
            };
            if (!statusId.HasValue) return;
            var definition = statusId == BattleStatusId.BurningFlowerLeaf
                ? _definition.LeafGrowthStatus
                : _definition.FireGrowthStatus;
            var existing = Owner.GetStatus(statusId.Value);
            Owner.ApplyOrReplaceStatus(new BattleStatusInstance(
                statusId.Value,
                BattleStatusCategory.None,
                Owner,
                _definition.StatGainPerDamage,
                stackCount: checked((existing?.StackCount ?? 0) + 1),
                definition: definition));
        }
    }
}
