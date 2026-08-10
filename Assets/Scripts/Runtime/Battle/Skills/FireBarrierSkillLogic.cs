using System;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class FireBarrierSkillLogic : ISkillLogic
    {
        private readonly FireBarrierSkillAsset _skill;

        public FireBarrierSkillLogic(FireBarrierSkillAsset skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!ReferenceEquals(context.Skill, _skill))
            {
                throw new ArgumentException(
                    "Fire Barrier Logic received another Skill Asset.",
                    nameof(context));
            }
            if (_skill.FieldEffect == null)
            {
                throw new InvalidOperationException(
                    "Fire Barrier Field Effect is not assigned.");
            }

            var value = SignedStatMath.FloorNonNegative(
                context.ScaleFromAttribute(
                    _skill.BaseValue,
                    PachimonAttribute.Fire,
                    _skill.FireValueRatio));
            if (value > 0)
            {
                context.State.Fields.CreateOrAddFireBarrier(
                    context.User,
                    _skill.FieldEffect,
                    value);
            }

            return new SkillResolution(
                context.User,
                context.Skill,
                Array.Empty<SkillEffectResult>());
        }
    }
}
