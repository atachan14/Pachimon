using System;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class WaterVeilSkillLogic : ISkillLogic
    {
        private readonly WaterVeilSkillAsset _skill;

        public WaterVeilSkillLogic(WaterVeilSkillAsset skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (!ReferenceEquals(context?.Skill, _skill))
            {
                throw new ArgumentException(
                    "Water Veil Logic received another Skill Asset.",
                    nameof(context));
            }

            var value = SignedStatMath.FloorNonNegative(
                SignedStatMath.ScaleFromBase(
                    _skill.BaseFieldValue,
                    context.User.GetBattleStatValue(PachimonStatType.Aqua),
                    _skill.AquaValueRatio));
            if (value > 0)
            {
                context.State.Fields.CreateOrAddWaterVeil(
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
