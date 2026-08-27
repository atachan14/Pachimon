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
            var hit = context.BeginAttackHit(target);
            var trueDamage = GetAverageAttributeValue(context.User);
            var targetResult = BattleTrueDamageService.Apply(
                context.State,
                context.User,
                target,
                new TrueDamageContext(
                    DamageOriginKind.Skill,
                    context.Skill.SkillId,
                    trueDamage,
                    isAttack: true),
                hit);
            var selfResult = BattleTrueDamageService.Apply(
                context.State,
                context.User,
                context.User,
                new TrueDamageContext(
                    DamageOriginKind.Self,
                    context.Skill.SkillId,
                    trueDamage,
                    isAttack: false));
            return new SkillResolution(
                context.User,
                context.Skill,
                new[]
                {
                    new SkillEffectResult(
                        targetResult.ActualTarget,
                        targetResult.AppliedDamage,
                        true,
                        hit: hit),
                    new SkillEffectResult(selfResult.ActualTarget, selfResult.AppliedDamage, true),
                });
        }

        private static BattleUnitState GetTarget(SkillExecutionContext context)
        {
            return context.Targets.GetFrontEnemy()
                ?? throw new InvalidOperationException("No living Enemy target was found.");
        }

        private static int GetAverageAttributeValue(BattleUnitState user)
        {
            var total = 0m;
            var count = 0;
            for (var value = (int)PachimonStatType.Fire;
                 value <= (int)PachimonStatType.Dragon;
                 value++)
            {
                total += user.GetBattleStatValue((PachimonStatType)value);
                count++;
            }

            return count == 0
                ? 0
                : SignedStatMath.FloorNonNegative(total / count);
        }
    }
}
