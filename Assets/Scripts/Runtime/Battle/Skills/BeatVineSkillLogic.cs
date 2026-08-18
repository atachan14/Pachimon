using System;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class BeatVineSkillLogic : ISkillLogic
    {
        private readonly BeatVineSkillAsset _skill;

        public BeatVineSkillLogic(BeatVineSkillAsset skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            var definition = _skill.FieldEffect;
            var value = SignedStatMath.FloorNonNegative(
                context.ScaleFromAttribute(
                    definition.BaseValue,
                    PachimonAttribute.Leaf,
                    definition.LeafValueRatio));
            if (value > 0)
                context.State.Fields.CreateBeatVine(
                    context.User,
                    definition,
                    value);
            return new SkillResolution(
                context.User,
                _skill,
                Array.Empty<SkillEffectResult>());
        }
    }
}
