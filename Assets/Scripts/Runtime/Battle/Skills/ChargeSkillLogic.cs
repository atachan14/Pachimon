using System;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class ChargeSkillLogic : ISkillLogic
    {
        private readonly ChargeSkillAsset _skill;

        public ChargeSkillLogic(ChargeSkillAsset skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!ReferenceEquals(context.Skill, _skill))
            {
                throw new ArgumentException(
                    "Charge Logic received another Skill Asset.",
                    nameof(context));
            }

            var value = Math.Max(
                1,
                context.User.GetBattleStatValue(PachimonStatType.Electric));
            context.State.Statuses.ApplyStatus(
                context.User,
                BattleStatusFactory.CreateCharging(
                    context.User,
                    value,
                    _skill));

            return new SkillResolution(
                context.User,
                context.Skill,
                Array.Empty<SkillEffectResult>());
        }
    }
}
