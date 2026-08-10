using System;
using System.Collections.Generic;
using Pachimon.Passives;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class WeatherChildPassiveLogic :
        IPassiveLogic,
        IBattleStatModifierProvider
    {
        private readonly WeatherChildPassiveAsset _definition;

        public WeatherChildPassiveLogic(
            BattleUnitState owner,
            WeatherChildPassiveAsset definition)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _definition = definition
                ?? throw new ArgumentNullException(nameof(definition));
        }

        public BattleUnitState Owner { get; }

        public void Handle(IBattleEvent battleEvent)
        {
        }

        public IEnumerable<IStatModifier> CreateStatModifiers(BattleState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (!Owner.IsAlive || state.Weather.ActiveWeatherTypeCount <= 0)
            {
                yield break;
            }

            var damageBonus = checked(
                state.Weather.ActiveWeatherTypeCount
                * _definition.DamageBonusPerWeather);
            yield return new FixedStatModifier(
                PachimonStatType.DamageBonus,
                StatModifierOperation.DirectAdditive,
                damageBonus,
                new StatModifierSource(
                    StatModifierSourceType.Passive,
                    $"passive:{_definition.PassiveId}",
                    _definition.DisplayName));
        }
    }
}
