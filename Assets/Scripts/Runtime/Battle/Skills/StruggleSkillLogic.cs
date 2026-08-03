using System;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class StruggleSkillLogic : ISkillLogic
    {
        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            var target = GetTarget(context);
            var trueDamage = GetLowestAttributeValue(context.User);
            var targetDamage = BattleTrueDamageService.Apply(
                context.State,
                context.User,
                target,
                new TrueDamageContext(
                    DamageOriginKind.Skill,
                    context.Skill.SkillId,
                    trueDamage,
                    isAttack: true)).AppliedDamage;
            var selfDamage = BattleTrueDamageService.Apply(
                context.State,
                context.User,
                context.User,
                new TrueDamageContext(
                    DamageOriginKind.Self,
                    context.Skill.SkillId,
                    trueDamage,
                    isAttack: false)).AppliedDamage;
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
                    user.GetBattleStatValue((PachimonStatType)value));
            }

            return minimum;
        }
    }
}
