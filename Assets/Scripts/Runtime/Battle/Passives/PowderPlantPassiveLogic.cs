using System;
using Pachimon.Passives;

namespace Pachimon.Battle
{
    public sealed class PowderPlantPassiveLogic : IPassiveLogic
    {
        private readonly PowderPlantPassiveAsset _definition;
        public PowderPlantPassiveLogic(BattleUnitState owner, PowderPlantPassiveAsset definition)
        { Owner = owner ?? throw new ArgumentNullException(nameof(owner)); _definition = definition ?? throw new ArgumentNullException(nameof(definition)); }
        public BattleUnitState Owner { get; }
        public void Handle(IBattleEvent battleEvent)
        {
            if (battleEvent is not SkillStatusAppliedEvent applied
                || !ReferenceEquals(applied.Source, Owner)
                || applied.Target.Side == Owner.Side
                || !Owner.IsAlive
                || _definition.LeafIncreasePerApplication <= 0) return;
            Owner.AddStatusStacks(BattleStatusId.LeafGrowth, BattleStatusCategory.None,
                Owner, _definition.LeafIncreasePerApplication, 1);
        }
    }
}
