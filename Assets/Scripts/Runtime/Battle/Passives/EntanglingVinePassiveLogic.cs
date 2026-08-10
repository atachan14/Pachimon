using System;
using Pachimon.Passives;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class EntanglingVinePassiveLogic : IPassiveLogic, IOutgoingStatusValueModifierProvider
    {
        private readonly EntanglingVinePassiveAsset _definition;
        public EntanglingVinePassiveLogic(BattleUnitState owner, EntanglingVinePassiveAsset definition)
        { Owner = owner ?? throw new ArgumentNullException(nameof(owner)); _definition = definition ?? throw new ArgumentNullException(nameof(definition)); }
        public BattleUnitState Owner { get; }
        public void Handle(IBattleEvent battleEvent) { }
        public decimal ModifyOutgoingStatusValue(BattleState state, BattleUnitState source, BattleUnitState target,
            BattleStatusId statusId, BattleStatusCategory categories, decimal value)
        {
            if (!Owner.IsAlive || !ReferenceEquals(source, Owner) || (categories & BattleStatusCategory.Slow) == 0) return value;
            return value * SignedStatMath.AmplificationMultiplier(
                Owner.GetBattleStatValue(PachimonStatType.Leaf) * _definition.LeafSlowRatio / 100m);
        }
    }
}
