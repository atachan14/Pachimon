using System;
using System.Collections.Generic;
using Pachimon.Passives;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class ThunderManPassiveLogic : IPassiveLogic, IBattleStatModifierProvider
    {
        private readonly ThunderManPassiveAsset _definition;
        public ThunderManPassiveLogic(BattleUnitState owner, ThunderManPassiveAsset definition)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }
        public BattleUnitState Owner { get; }
        public void Handle(IBattleEvent battleEvent) { }

        public IEnumerable<IStatModifier> CreateStatModifiers(BattleState state)
        {
            if (!Owner.IsAlive || !state.Weather.Has(BattleWeatherId.Thunder))
                yield break;
            yield return new FixedStatModifier(
                PachimonStatType.Speed,
                StatModifierOperation.DirectAdditive,
                _definition.SpeedBonus,
                new StatModifierSource(
                    StatModifierSourceType.Passive,
                    $"passive:{_definition.PassiveId}",
                    _definition.DisplayName));
        }
    }
}
