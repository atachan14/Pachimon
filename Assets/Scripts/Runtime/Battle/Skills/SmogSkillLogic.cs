using System;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class SmogSkillLogic : ISkillLogic
    {
        private readonly SmogSkillAsset _skill;

        public SmogSkillLogic(SmogSkillAsset skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            var value = SignedStatMath.FloorNonNegative(
                SignedStatMath.ScaleFromBase(
                    _skill.BaseFieldValue,
                    context.User.GetBattleStatValue(PachimonStatType.Poison),
                    _skill.PoisonScalingPercent));
            if (value > 0)
            {
                context.State.Fields.CreateOrAddSmog(
                    context.User,
                    context.User.Side == BattleSide.Player
                        ? BattleSide.Enemy
                        : BattleSide.Player,
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
