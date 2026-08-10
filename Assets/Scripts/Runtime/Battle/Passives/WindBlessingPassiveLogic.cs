using System;
using System.Linq;
using Pachimon.Passives;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class WindBlessingPassiveLogic : IPassiveLogic
    {
        private readonly WindBlessingPassiveAsset _definition;
        public WindBlessingPassiveLogic(BattleUnitState owner, WindBlessingPassiveAsset definition)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        public BattleUnitState Owner { get; }

        public void Handle(IBattleEvent battleEvent)
        {
            if (battleEvent is not ShieldAppliedEvent shield
                || shield.IsSharedEffect
                || !Owner.IsAlive
                || !ReferenceEquals(shield.Target, Owner))
            {
                return;
            }

            var value = SignedStatMath.FloorNonNegative(
                shield.AppliedValue * _definition.SharedShieldPercent / 100m);
            if (value <= 0) return;
            int? duration = shield.DurationTicks.HasValue
                ? Math.Max(1, SignedStatMath.FloorNonNegative(
                    shield.DurationTicks.Value * _definition.DurationPercent / 100m))
                : null;
            var side = Owner.Side == BattleSide.Player
                ? shield.State.Player
                : shield.State.Enemy;
            foreach (var ally in side.GetAllLiving().Where(unit => !ReferenceEquals(unit, Owner)))
            {
                shield.State.SupportEffects.ApplyShield(
                    Owner,
                    ally,
                    value,
                    duration,
                    isSharedEffect: true);
            }
        }
    }
}
