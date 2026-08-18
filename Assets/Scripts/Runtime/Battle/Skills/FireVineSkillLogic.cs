using System;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class FireVineSkillLogic : ISkillLogic
    {
        private readonly FireVineSkillAsset _skill;

        public FireVineSkillLogic(FireVineSkillAsset skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            var definition = _skill.FieldEffect;
            var leafValue = SignedStatMath.FloorNonNegative(
                context.ScaleFromAttribute(
                    definition.BaseLeafValue,
                    PachimonAttribute.Leaf,
                    definition.LeafValueRatio));
            var fireValue = SignedStatMath.FloorNonNegative(
                context.ScaleFromAttribute(
                    definition.BaseFireValue,
                    PachimonAttribute.Fire,
                    definition.FireValueRatio));
            if (leafValue > 0 && fireValue > 0)
            {
                context.State.Fields.CreateFireVine(
                    context.User,
                    definition,
                    leafValue,
                    fireValue);
            }
            return new SkillResolution(
                context.User,
                _skill,
                Array.Empty<SkillEffectResult>());
        }
    }
}
