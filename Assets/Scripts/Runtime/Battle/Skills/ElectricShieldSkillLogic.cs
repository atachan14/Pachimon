using System;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class ElectricShieldSkillLogic : ISkillLogic
    {
        private readonly ElectricShieldSkillAsset _skill;
        public ElectricShieldSkillLogic(ElectricShieldSkillAsset skill) =>
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            var electric = context.GetAttributeValue(PachimonAttribute.Electric);
            var ratio = context.GetAttributeRatio(PachimonAttribute.Electric, 100m);
            int Scale(int baseValue, int scalingRatio) =>
                SignedStatMath.FloorNonNegative(SignedStatMath.ScaleFromBase(
                    baseValue, electric, ratio * scalingRatio / 100m));

            var shieldValue = Scale(_skill.BaseShieldValue, _skill.ShieldElectricRatio);
            var selfParalysis = Scale(
                _skill.BaseSelfParalysis,
                _skill.SelfParalysisElectricRatio);
            var counterParalysis = Scale(
                _skill.BaseCounterParalysis,
                _skill.CounterParalysisElectricRatio);
            var shield = context.State.SupportEffects.ApplyShield(
                context.User,
                context.User,
                shieldValue,
                _skill.DurationTicks);
            context.State.Statuses.ApplyTransformedStatus(
                context.User,
                BattleStatusFactory.CreateSlow(
                    context.User,
                    selfParalysis,
                    _skill.ParalysisStatus));
            context.State.Statuses.ApplyTransformedStatus(
                context.User,
                new BattleStatusInstance(
                    BattleStatusId.ElectricShield,
                    BattleStatusCategory.None,
                    context.User,
                    counterParalysis,
                    durationTicks: _skill.DurationTicks,
                    runtimeData: new ElectricShieldRuntimeData(
                        shield.ApplicationOrder),
                    definition: _skill.ShieldStatus));
            context.State.AddLog(
                $"{context.User.DisplayName}は{shieldValue}のエレキシールドを得た！");
            return new SkillResolution(context.User, _skill,
                Array.Empty<SkillEffectResult>());
        }
    }
}
