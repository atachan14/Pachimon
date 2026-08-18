using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Passives;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class ParalysisGenerationPassiveLogic :
        IPassiveLogic,
        IBattleStatModifierProvider
    {
        private readonly ParalysisGenerationPassiveAsset _definition;
        public ParalysisGenerationPassiveLogic(
            BattleUnitState owner,
            ParalysisGenerationPassiveAsset definition)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }
        public BattleUnitState Owner { get; }
        public void Handle(IBattleEvent battleEvent) { }

        public IEnumerable<IStatModifier> CreateStatModifiers(BattleState state)
        {
            if (!Owner.IsAlive) yield break;
            var paralysis = Owner.GetStatuses(BattleStatusId.Paralysis)
                .Sum(status => checked(status.Value * status.StackCount));
            if (paralysis <= 0) yield break;
            yield return new FixedStatModifier(
                PachimonStatType.Electric,
                StatModifierOperation.DirectAdditive,
                paralysis * _definition.ElectricFromParalysisRatio / 100m,
                new StatModifierSource(
                    StatModifierSourceType.Passive,
                    $"passive:{_definition.PassiveId}",
                    _definition.DisplayName));
        }
    }
}
