using System;
using System.Collections.Generic;
using Pachimon.Passives;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class DragonSkeletonPassiveLogic :
        IPassiveLogic,
        IBattleStatModifierProvider
    {
        private readonly DragonSkeletonPassiveAsset _definition;

        public DragonSkeletonPassiveLogic(
            BattleUnitState owner,
            DragonSkeletonPassiveAsset definition)
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
                PachimonStatType.Dragon,
                StatModifierOperation.DerivedAdditive,
                stats => decimal.Floor(
                    (stats.GetValue(PachimonStatType.Speed)
                     - Owner.StartingStats.GetValue(PachimonStatType.Speed))
                    * _definition.DragonFromSpeedRatio / 100m),
                source);
            yield return new DerivedStatModifier(
                PachimonStatType.Speed,
                StatModifierOperation.DerivedAdditive,
                stats => decimal.Floor(
                    (stats.GetValue(PachimonStatType.Dragon)
                     - Owner.StartingStats.GetValue(PachimonStatType.Dragon))
                    * _definition.SpeedFromDragonRatio / 100m),
                source);
        }
    }
}
