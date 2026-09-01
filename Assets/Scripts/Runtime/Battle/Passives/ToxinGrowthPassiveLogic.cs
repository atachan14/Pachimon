using System;
using Pachimon.Passives;

namespace Pachimon.Battle
{
    public sealed class ToxinGrowthPassiveLogic : IPassiveLogic
    {
        private readonly ToxinGrowthPassiveAsset _definition;

        public ToxinGrowthPassiveLogic(
            BattleUnitState owner,
            ToxinGrowthPassiveAsset definition)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _definition = definition
                ?? throw new ArgumentNullException(nameof(definition));
        }

        public BattleUnitState Owner { get; }

        public void Handle(IBattleEvent battleEvent)
        {
            if (battleEvent is not ToxinAppliedEvent toxinEvent
                || !ReferenceEquals(toxinEvent.Source, Owner)
                || (toxinEvent.Tags & StatusApplicationTag.OverTime) != 0
                || _definition.PoisonPercentPerApplication == 0)
            {
                return;
            }

            Owner.AddStatusStacks(
                BattleStatusId.ToxinGrowth,
                BattleStatusCategory.None,
                Owner,
                _definition.PoisonPercentPerApplication,
                stackCount: 1);
        }
    }
}
