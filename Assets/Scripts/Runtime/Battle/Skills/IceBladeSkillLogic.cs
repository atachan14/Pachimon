using System;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class IceBladeSkillLogic : ISkillLogic
    {
        private readonly IceBladeSkillAsset _skill;

        public IceBladeSkillLogic(IceBladeSkillAsset skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!ReferenceEquals(context.Skill, _skill))
            {
                throw new ArgumentException(
                    "Ice Blade Logic received another Skill Asset.",
                    nameof(context));
            }
            if (_skill.FieldEffect == null)
            {
                throw new InvalidOperationException(
                    "Ice Blade Field Effect is not assigned.");
            }

            var duration = CalculateDuration(
                _skill,
                context.GetAttributeValue(PachimonAttribute.Ice));
            context.State.Fields.CreateOrAddIceBlade(
                context.User,
                _skill.FieldEffect,
                duration);

            return new SkillResolution(
                context.User,
                context.Skill,
                Array.Empty<SkillEffectResult>());
        }

        public static int CalculateDuration(
            IceBladeSkillAsset skill,
            decimal ice)
        {
            if (skill == null) throw new ArgumentNullException(nameof(skill));
            var scaledIce = ice * skill.IceDurationRatio / 100m;
            return Math.Max(
                1,
                SignedStatMath.CeilPositive(
                    skill.BaseDurationTicks
                    + skill.ScalingDurationTicks
                    * SignedStatMath.AmplificationMultiplier(scaledIce)));
        }
    }
}
