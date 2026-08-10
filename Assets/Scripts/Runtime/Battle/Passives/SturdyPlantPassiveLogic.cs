using System;
using System.Collections.Generic;
using Pachimon.Passives;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class SturdyPlantPassiveLogic : IPassiveLogic, IBattleStatModifierProvider
    {
        private readonly SturdyPlantPassiveAsset _definition;
        public SturdyPlantPassiveLogic(BattleUnitState owner, SturdyPlantPassiveAsset definition)
        { Owner = owner ?? throw new ArgumentNullException(nameof(owner)); _definition = definition ?? throw new ArgumentNullException(nameof(definition)); }
        public BattleUnitState Owner { get; }
        public void Handle(IBattleEvent battleEvent) { }
        public IEnumerable<IStatModifier> CreateStatModifiers(BattleState state)
        {
            if (!Owner.IsAlive || (Owner.Timing.Phase != BattleActionPhase.Startup && Owner.GetStatus(BattleStatusId.Stun) == null)) yield break;
            yield return new DerivedStatModifier(
                PachimonStatType.ResistBonus,
                StatModifierOperation.DerivedAdditive,
                stats => decimal.Floor(
                    stats.GetValue(PachimonStatType.Leaf)
                    * _definition.LeafResistBonusRatio / 100m),
                new StatModifierSource(
                    StatModifierSourceType.Passive,
                    $"passive:{_definition.PassiveId}",
                    _definition.DisplayName));
        }
    }
}
