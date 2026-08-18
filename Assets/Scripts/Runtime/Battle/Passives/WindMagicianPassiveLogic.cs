using System;
using Pachimon.Passives;
using Pachimon.Reward;

namespace Pachimon.Battle
{
    public sealed class WindMagicianPassiveLogic : IPassiveLogic
    {
        private readonly WindMagicianPassiveAsset _definition;
        public WindMagicianPassiveLogic(BattleUnitState owner,
            WindMagicianPassiveAsset definition)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }
        public BattleUnitState Owner { get; }
        public void Handle(IBattleEvent battleEvent)
        {
            if (battleEvent is not AttributeDamageAppliedEvent damage
                || !ReferenceEquals(damage.Source, Owner)
                || damage.Attribute == PachimonAttribute.Wind
                || damage.AppliedDamage + damage.ShieldAbsorbedDamage <= 0
                || !Owner.IsAlive || _definition.WindGainPerHit <= 0) return;
            Owner.AddStatusStacks(BattleStatusId.WindMagicianGrowth,
                BattleStatusCategory.None, Owner, _definition.WindGainPerHit,
                1, _definition.GrowthStatus);
        }
    }
}
