using System;
using Pachimon.Passives;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class ResistAdvantageDamagePassiveLogic : IPassiveLogic
    {
        private readonly ResistAdvantageDamagePassiveAsset _definition;

        public ResistAdvantageDamagePassiveLogic(
            BattleUnitState owner,
            ResistAdvantageDamagePassiveAsset definition)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _definition = definition
                ?? throw new ArgumentNullException(nameof(definition));
        }

        public BattleUnitState Owner { get; }

        public void Handle(IBattleEvent battleEvent)
        {
            if (battleEvent is not BeforeAttributeDamageEvent damageEvent
                || !ReferenceEquals(damageEvent.Source, Owner)
                || damageEvent.Calculation?.Context.ApplyOutgoingModifiers
                    == false)
            {
                return;
            }

            var difference = Owner.GetBattleStatValue(
                    PachimonStatType.ResistBonus)
                - damageEvent.Target.GetBattleStatValue(
                    PachimonStatType.ResistBonus);
            if (difference <= 0)
            {
                return;
            }

            damageEvent.MultiplyDamage(
                SignedStatMath.AmplificationMultiplier(
                    difference * _definition.ResistDifferenceRatio / 100m));
        }
    }
}
