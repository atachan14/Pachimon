using System;
using Pachimon.Passives;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class TargetSlowDamagePassiveLogic : IPassiveLogic
    {
        private readonly TargetSlowDamagePassiveAsset _definition;

        public TargetSlowDamagePassiveLogic(
            BattleUnitState owner,
            TargetSlowDamagePassiveAsset definition)
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

            var slow = damageEvent.Target.GetStatusCategoryValue(
                BattleStatusCategory.Slow);
            if (slow <= 0)
            {
                return;
            }

            damageEvent.MultiplyDamage(
                SignedStatMath.AmplificationMultiplier(
                    slow * _definition.SlowRatio / 100m));
        }
    }
}
