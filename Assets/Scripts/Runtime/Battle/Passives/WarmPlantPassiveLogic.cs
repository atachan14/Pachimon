using System;
using System.Collections.Generic;
using Pachimon.Passives;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class WarmPlantPassiveLogic : IPassiveLogic, IBattleStatModifierProvider
    {
        private readonly WarmPlantPassiveAsset _definition;
        public WarmPlantPassiveLogic(BattleUnitState owner, WarmPlantPassiveAsset definition)
        { Owner = owner ?? throw new ArgumentNullException(nameof(owner)); _definition = definition ?? throw new ArgumentNullException(nameof(definition)); }
        public BattleUnitState Owner { get; }
        public void Handle(IBattleEvent battleEvent) { }
        public IEnumerable<IStatModifier> CreateStatModifiers(BattleState state)
        {
            if (!Owner.IsAlive || state.Weather.Temperature <= 0) yield break;
            var value = decimal.Floor(state.Weather.Temperature * _definition.TemperatureSpeedRatio / 100m);
            if (value != 0)
            {
                yield return new FixedStatModifier(PachimonStatType.Speed, StatModifierOperation.DirectAdditive, value,
                    new StatModifierSource(StatModifierSourceType.Passive, $"passive:{_definition.PassiveId}", _definition.DisplayName));
            }
        }
    }
}
