using System;
using System.Collections.Generic;
using Pachimon.Passives;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class RainManPassiveLogic :
        IPassiveLogic,
        IBattleStatModifierProvider
    {
        private readonly RainManPassiveAsset _definition;

        public RainManPassiveLogic(
            BattleUnitState owner,
            RainManPassiveAsset definition)
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
            if (!Owner.IsAlive || !state.Weather.IsRaining)
            {
                yield break;
            }

            var speedPercent = checked(
                _definition.BaseSpeedPercent
                + SignedStatMath.FloorNonNegative(
                    state.Weather.GetEffectiveRainValue()
                    * _definition.RainValueRatio / 100m));
            yield return new FixedStatModifier(
                PachimonStatType.Speed,
                StatModifierOperation.DirectMultiplicative,
                speedPercent / 100m,
                new StatModifierSource(
                    StatModifierSourceType.Passive,
                    $"passive:{_definition.PassiveId}",
                    _definition.DisplayName));
        }
    }
}
