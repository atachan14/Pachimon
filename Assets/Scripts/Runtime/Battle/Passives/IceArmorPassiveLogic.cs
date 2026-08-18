using System;
using Pachimon.Passives;
using Pachimon.Reward;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class IceArmorPassiveLogic : IPassiveLogic, IShieldModifierProvider
    {
        private readonly IceArmorPassiveAsset _definition;
        public IceArmorPassiveLogic(BattleUnitState owner, IceArmorPassiveAsset definition)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }
        public BattleUnitState Owner { get; }
        public void Handle(IBattleEvent battleEvent) { }
        public ShieldApplicationPlan ModifyShield(BattleState state,
            BattleUnitState source, BattleUnitState target,
            ShieldApplicationPlan plan)
        {
            if (!Owner.IsAlive || !ReferenceEquals(target, Owner)) return plan;
            var ice = Owner.GetBattleStatValue(PachimonStatType.Ice);
            var multiplier = SignedStatMath.AmplificationMultiplier(
                ice * _definition.IceScalingPercent / 100m);
            return new ShieldApplicationPlan(plan.Value * multiplier,
                plan.DurationTicks * multiplier);
        }
    }
}
