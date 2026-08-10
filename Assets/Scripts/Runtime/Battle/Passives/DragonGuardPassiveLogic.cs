using System;
using System.Collections.Generic;
using Pachimon.Passives;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class DragonGuardPassiveLogic :
        IPassiveLogic,
        IBattleStatModifierProvider
    {
        private readonly DragonGuardPassiveAsset _definition;

        public DragonGuardPassiveLogic(
            BattleUnitState owner,
            DragonGuardPassiveAsset definition)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        public BattleUnitState Owner { get; }
        public void Handle(IBattleEvent battleEvent) { }

        public IEnumerable<IStatModifier> CreateStatModifiers(BattleState state)
        {
            var source = new StatModifierSource(
                StatModifierSourceType.Passive,
                $"passive:{_definition.PassiveId}:battle",
                _definition.DisplayName);
            yield return new DerivedStatModifier(
                PachimonStatType.ResistBonus,
                StatModifierOperation.DerivedAdditive,
                stats => decimal.Floor(
                    (stats.GetValue(PachimonStatType.Dragon)
                     - Owner.StartingStats.GetValue(PachimonStatType.Dragon))
                    * _definition.ResistFromDragonRatio / 100m),
                source);
        }
    }
}
