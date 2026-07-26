using System;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class StruggleSkillLogic : ISkillLogic
    {
        public SkillPreview Preview(SkillExecutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            var target = GetTarget(context);
            var trueDamage = GetLowestAttributeValue(context.User);
            return new SkillPreview(
                context.User,
                context.Skill,
                new[]
                {
                    new SkillPreviewEffect(
                        target,
                        -Math.Min(target.CurrentHp, trueDamage)),
                    new SkillPreviewEffect(
                        context.User,
                        -Math.Min(context.User.CurrentHp, trueDamage)),
                });
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            var target = GetTarget(context);
            var trueDamage = GetLowestAttributeValue(context.User);
            var targetDamage = target.ApplyDamage(trueDamage);
            var selfDamage = context.User.ApplyDamage(trueDamage);
            return new SkillResolution(
                context.User,
                context.Skill,
                new[]
                {
                    new SkillEffectResult(target, targetDamage, true),
                    new SkillEffectResult(context.User, selfDamage, true),
                });
        }

        private static BattleUnitState GetTarget(SkillExecutionContext context)
        {
            return context.Targets.GetFrontEnemy()
                ?? throw new InvalidOperationException("No living Enemy target was found.");
        }

        private static int GetLowestAttributeValue(BattleUnitState user)
        {
            var minimum = int.MaxValue;
            for (var value = (int)PachimonStatType.Fire;
                 value <= (int)PachimonStatType.Dragon;
                 value++)
            {
                minimum = Math.Min(
                    minimum,
                    user.StartingStats.GetValue((PachimonStatType)value));
            }

            return minimum;
        }
    }
}
