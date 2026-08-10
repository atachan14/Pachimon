using System;
using Pachimon.Passives;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class LifeWaterPassiveLogic : IPassiveLogic
    {
        private readonly LifeWaterPassiveAsset _definition;

        public LifeWaterPassiveLogic(
            BattleUnitState owner,
            LifeWaterPassiveAsset definition)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _definition = definition
                ?? throw new ArgumentNullException(nameof(definition));
        }

        public BattleUnitState Owner { get; }

        public void Handle(IBattleEvent battleEvent)
        {
            if (battleEvent is not SkillResolvedEvent resolved
                || !ReferenceEquals(resolved.Source, Owner)
                || !Owner.IsAlive
                || resolved.Resolution.EffectiveManaSpent <= 0m)
            {
                return;
            }

            var aqua = Owner.GetBattleStatValue(PachimonStatType.Aqua);
            var recoveryRatio = Math.Max(
                0m,
                _definition.BaseRecoveryRatio
                + aqua * _definition.AquaRecoveryRatio / 100m);
            var recovery = SignedStatMath.FloorNonNegative(
                resolved.Resolution.EffectiveManaSpent
                * recoveryRatio / 100m);
            if (recovery <= 0)
            {
                return;
            }

            var restored = resolved.State.SupportEffects.RestoreHp(
                Owner,
                Owner,
                recovery);
            if (restored > 0)
            {
                resolved.State.AddLog(
                    $"{Owner.DisplayName}は{restored}回復した！");
            }
        }
    }
}
