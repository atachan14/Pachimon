using System;
using Pachimon.Passives;
using Pachimon.Reward;

namespace Pachimon.Battle
{
    public sealed class WindRiderPassiveLogic : IPassiveLogic
    {
        private readonly WindRiderPassiveAsset _definition;
        public WindRiderPassiveLogic(BattleUnitState owner,
            WindRiderPassiveAsset definition)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }
        public BattleUnitState Owner { get; }
        public void Handle(IBattleEvent battleEvent)
        {
            if (battleEvent is not AttributeDamageAppliedEvent damage
                || !ReferenceEquals(damage.Source, Owner)
                || damage.Attribute != PachimonAttribute.Wind
                || damage.AppliedDamage + damage.ShieldAbsorbedDamage <= 0
                || !Owner.IsAlive || _definition.SpeedGainPerHit <= 0) return;
            Owner.AddStatusStacks(BattleStatusId.WindRiderGrowth,
                BattleStatusCategory.None, Owner, _definition.SpeedGainPerHit,
                1, _definition.GrowthStatus);
        }
    }
}
