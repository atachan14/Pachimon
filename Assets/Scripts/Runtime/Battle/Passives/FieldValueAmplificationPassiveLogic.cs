using System;
using Pachimon.Passives;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class FieldValueAmplificationPassiveLogic : IPassiveLogic
    {
        private readonly FieldValueAmplificationPassiveAsset _definition;

        public FieldValueAmplificationPassiveLogic(
            BattleUnitState owner,
            FieldValueAmplificationPassiveAsset definition)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _definition = definition
                ?? throw new ArgumentNullException(nameof(definition));
        }

        public BattleUnitState Owner { get; }

        public void Handle(IBattleEvent battleEvent)
        {
            if (battleEvent is not BeforeFieldEffectValueAppliedEvent fieldEvent
                || !ReferenceEquals(fieldEvent.Source, Owner))
            {
                return;
            }

            var poison = Owner.GetBattleStatValue(PachimonStatType.Poison);
            var amplifiedValue = SignedStatMath.FloorNonNegative(
                fieldEvent.Value
                * SignedStatMath.AmplificationMultiplier(
                    poison * _definition.PoisonScalingPercent / 100m));
            if (amplifiedValue == fieldEvent.Value)
            {
                return;
            }

            fieldEvent.SetValue(amplifiedValue);
            fieldEvent.State.AddLog($"{Owner.DisplayName}の{_definition.DisplayName}！");
        }
    }
}
