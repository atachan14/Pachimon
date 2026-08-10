using System;
using System.Collections.Generic;
using Pachimon.Passives;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class SunnyManPassiveLogic :
        IPassiveLogic,
        IBattleStatModifierProvider
    {
        private readonly SunnyManPassiveAsset _definition;

        public SunnyManPassiveLogic(
            BattleUnitState owner,
            SunnyManPassiveAsset definition)
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
            if (!Owner.IsAlive || state.Weather.Temperature <= 0)
            {
                yield break;
            }

            yield return new FixedStatModifier(
                PachimonStatType.Speed,
                StatModifierOperation.DirectMultiplicative,
                _definition.SpeedPercent / 100m,
                new StatModifierSource(
                    StatModifierSourceType.Passive,
                    $"passive:{_definition.PassiveId}",
                    _definition.DisplayName));
        }
    }
}
