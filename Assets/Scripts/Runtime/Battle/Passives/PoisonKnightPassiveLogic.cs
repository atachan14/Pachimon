using System;
using System.Linq;
using Pachimon.Passives;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class PoisonKnightPassiveLogic : IPassiveLogic
    {
        private readonly PoisonKnightPassiveAsset _definition;

        public PoisonKnightPassiveLogic(
            BattleUnitState owner,
            PoisonKnightPassiveAsset definition)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _definition = definition
                ?? throw new ArgumentNullException(nameof(definition));
        }

        public BattleUnitState Owner { get; }

        public void Handle(IBattleEvent battleEvent)
        {
            switch (battleEvent)
            {
                case ShieldAppliedEvent shieldEvent:
                    ShareShield(shieldEvent);
                    break;
                case HpRestoredEvent restoredEvent:
                    ShareRecovery(restoredEvent);
                    break;
            }
        }

        private void ShareShield(ShieldAppliedEvent shieldEvent)
        {
            if (!ShouldShare(shieldEvent.Target, shieldEvent.IsSharedEffect))
            {
                return;
            }

            var sharedValue = CalculateSharedValue(shieldEvent.AppliedValue);
            if (sharedValue <= 0)
            {
                return;
            }

            foreach (var ally in GetOtherLivingAllies(shieldEvent.State))
            {
                shieldEvent.State.SupportEffects.ApplyShield(
                    Owner,
                    ally,
                    sharedValue,
                    shieldEvent.DurationTicks,
                    isSharedEffect: true);
            }
        }

        private void ShareRecovery(HpRestoredEvent restoredEvent)
        {
            if (!ShouldShare(restoredEvent.Target, restoredEvent.IsSharedEffect))
            {
                return;
            }

            var sharedValue = CalculateSharedValue(restoredEvent.RestoredValue);
            if (sharedValue <= 0)
            {
                return;
            }

            foreach (var ally in GetOtherLivingAllies(restoredEvent.State))
            {
                restoredEvent.State.SupportEffects.RestoreHp(
                    Owner,
                    ally,
                    sharedValue,
                    isSharedEffect: true);
            }
        }

        private bool ShouldShare(BattleUnitState target, bool isSharedEffect)
        {
            return !isSharedEffect
                && Owner.IsAlive
                && ReferenceEquals(target, Owner);
        }

        private int CalculateSharedValue(int receivedValue)
        {
            var poison = Owner.GetBattleStatValue(PachimonStatType.Poison);
            var sharePercent = SignedStatMath.ScaleFromBase(
                _definition.BaseSharePercent,
                poison,
                _definition.PoisonScalingPercent);
            return SignedStatMath.FloorNonNegative(
                receivedValue * sharePercent / 100m);
        }

        private BattleUnitState[] GetOtherLivingAllies(BattleState state)
        {
            var side = Owner.Side == BattleSide.Player
                ? state.Player
                : state.Enemy;
            return side.GetAllLiving()
                .Where(ally => !ReferenceEquals(ally, Owner))
                .ToArray();
        }
    }
}
