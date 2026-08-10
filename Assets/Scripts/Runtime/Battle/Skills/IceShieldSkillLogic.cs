using System;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public static class IceShieldMath
    {
        public static int CalculateShieldValue(
            IceShieldSkillAsset skill,
            int ice)
        {
            if (skill == null) throw new ArgumentNullException(nameof(skill));
            return SignedStatMath.FloorNonNegative(
                SignedStatMath.ScaleFromBase(
                    skill.BaseShieldValue,
                    ice,
                    skill.IceShieldRatio));
        }
    }

    public sealed class IceShieldSkillLogic : ISkillLogic
    {
        private readonly IceShieldSkillAsset _skill;

        public IceShieldSkillLogic(IceShieldSkillAsset skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!ReferenceEquals(context.Skill, _skill))
            {
                throw new ArgumentException(
                    "Ice Shield Logic received another Skill Asset.",
                    nameof(context));
            }

            var side = context.User.Side == BattleSide.Player
                ? context.State.Player
                : context.State.Enemy;
            var target = side.GetFrontLiving()
                ?? throw new InvalidOperationException(
                    "Ice Shield requires a living ally.");
            var shieldValue = IceShieldMath.CalculateShieldValue(
                _skill,
                context.User.GetBattleStatValue(PachimonStatType.Ice));
            if (shieldValue > 0)
            {
                context.State.SupportEffects.ApplyShield(
                    context.User,
                    target,
                    shieldValue);
                context.State.AddLog(
                    $"{target.DisplayName}\u306F{shieldValue}\u306EShield\u3092\u5F97\u305F\uFF01");
            }

            return new SkillResolution(
                context.User,
                context.Skill,
                Array.Empty<SkillEffectResult>());
        }
    }
}
