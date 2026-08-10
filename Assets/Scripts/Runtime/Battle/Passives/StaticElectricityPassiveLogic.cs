using System;
using Pachimon.Passives;
using Pachimon.Reward;
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
                BattleStatusFactory.CreateSlow(
                    Owner,
                    CalculateParalysisValue(attackEvent.State),
                    _definition.ParalysisStatus));
        }

        private int CalculateParalysisValue(BattleState state)
        {
            var electric = SignedStatMath.FloorNonNegative(
                SignedStatMath.ScaleFromBase(
                    _definition.ElectricBaseValue,
                    Owner.GetBattleStatValue(PachimonStatType.Electric),
                    state.ResolveAttributeRatio(
                        PachimonAttribute.Electric,
                        100m)));
            var ice = SignedStatMath.FloorNonNegative(
                SignedStatMath.ScaleFromBase(
                    _definition.IceBaseValue,
                    Owner.GetBattleStatValue(PachimonStatType.Ice),
                    state.ResolveAttributeRatio(
                        PachimonAttribute.Ice,
                        100m)));
            return checked(electric + ice);
        }

    }
}
