using System;
using Pachimon.Passives;
using Pachimon.Reward;

namespace Pachimon.Battle
{
    public sealed class PoisonMagicianPassiveLogic : IPassiveLogic
    {
        private readonly PoisonMagicianPassiveAsset _definition;

        public PoisonMagicianPassiveLogic(
            BattleUnitState owner,
            PoisonMagicianPassiveAsset definition)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        public BattleUnitState Owner { get; }

        public void Handle(IBattleEvent battleEvent)
        {
            if (battleEvent is not AttributeDamageAppliedEvent damage
                || !ReferenceEquals(damage.Source, Owner)
                || damage.Calculation.Context.OriginKind != DamageOriginKind.Skill
                || damage.Attribute == PachimonAttribute.Poison
                || damage.AppliedDamage + damage.ShieldAbsorbedDamage <= 0
                || !Owner.IsAlive
                || _definition.PoisonGainPerHit <= 0)
            {
                return;
            }

            Owner.AddStatusStacks(
                BattleStatusId.PoisonMagicianGrowth,
                BattleStatusCategory.None,
                Owner,
                _definition.PoisonGainPerHit,
                stackCount: 1,
                definition: _definition.GrowthStatus);
        }
    }
}
