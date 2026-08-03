using System;
using Pachimon.Passives;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class StaticElectricityPassiveLogic : IPassiveLogic
    {
        private readonly StaticElectricityPassiveAsset _definition;

        public StaticElectricityPassiveLogic(
            BattleUnitState owner,
            StaticElectricityPassiveAsset definition)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _definition = definition
                ?? throw new ArgumentNullException(nameof(definition));
        }

        public BattleUnitState Owner { get; }

        public void Handle(IBattleEvent battleEvent)
        {
            if (battleEvent is not AttackReceivedEvent attackEvent
                || !ReferenceEquals(attackEvent.Target, Owner))
            {
                return;
            }

            attackEvent.State.Statuses.ApplyStatus(
                attackEvent.Source,
                new BattleStatusInstance(
                    BattleStatusId.Paralysis,
                    BattleStatusCategory.Slow,
                    Owner,
                    CalculateParalysisValue()));
        }

        private int CalculateParalysisValue()
        {
            var electric = SignedStatMath.FloorNonNegative(
                SignedStatMath.ScaleFromBase(
                    _definition.ElectricBaseValue,
                    Owner.GetBattleStatValue(PachimonStatType.Electric)));
            var ice = SignedStatMath.FloorNonNegative(
                SignedStatMath.ScaleFromBase(
                    _definition.IceBaseValue,
                    Owner.GetBattleStatValue(PachimonStatType.Ice)));
            return checked(electric + ice);
        }

    }
}
