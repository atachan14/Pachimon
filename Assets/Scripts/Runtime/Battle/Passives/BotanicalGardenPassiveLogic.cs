using System;
using System.Collections.Generic;
using Pachimon.Passives;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class BotanicalGardenPassiveLogic :
        IPassiveLogic,
        IBattleStatModifierProvider
    {
        private readonly BotanicalGardenPassiveAsset _definition;

        public BotanicalGardenPassiveLogic(
            BattleUnitState owner,
            BotanicalGardenPassiveAsset definition)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        public BattleUnitState Owner { get; }
        public void Handle(IBattleEvent battleEvent) { }

        public IEnumerable<IStatModifier> CreateStatModifiers(BattleState state)
        {
            if (!Owner.IsAlive) yield break;
            var count = state.Fields.CountEffects(
                Owner.Side,
                BattleFieldEffectCategory.Plant);
            var value = checked(count * _definition.DamageBonusPerPlant);
            if (value <= 0) yield break;
            yield return new FixedStatModifier(
                PachimonStatType.DamageBonus,
                StatModifierOperation.DirectAdditive,
                value,
                new StatModifierSource(
                    StatModifierSourceType.Passive,
                    $"passive:{_definition.PassiveId}",
                    _definition.DisplayName));
        }
    }
}
