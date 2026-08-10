using System;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class ChargeSkillLogic : IStartupSkillLogic
    {
        private readonly ChargeSkillAsset _skill;

        public ChargeSkillLogic(ChargeSkillAsset skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        }

        public object BeginStartup(SkillExecutionContext context)
        {
            ValidateContext(context);

            var value = Math.Max(
                1,
                context.User.GetBattleStatValue(PachimonStatType.Electric));
            var charging = BattleStatusFactory.CreateCharging(
                context.User,
                value,
                _skill.ChargeStatus);
            context.State.Statuses.ApplyStatus(
                context.User,
                charging);
            return charging;
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            ValidateContext(context);
            if (context.RuntimeData is BattleStatusInstance charging)
            {
                context.State.Statuses.TryCompleteCharge(
                    context.User,
                    charging);
            }
            else
            {
                // Preview resolves without advancing an actual Startup phase.
                var previewCharging = BattleStatusFactory.CreateCharging(
                    context.User,
                    Math.Max(
                        1,
                        context.User.GetBattleStatValue(PachimonStatType.Electric)),
                    _skill.ChargeStatus);
                context.State.Statuses.ApplyStatus(
                    context.User,
                    BattleStatusFactory.CreateCharged(previewCharging));
            }

            return new SkillResolution(
                context.User,
                context.Skill,
                Array.Empty<SkillEffectResult>());
        }

        private void ValidateContext(SkillExecutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!ReferenceEquals(context.Skill, _skill))
            {
                throw new ArgumentException(
                    "Charge Logic received another Skill Asset.",
                    nameof(context));
            }
        }
    }
}
