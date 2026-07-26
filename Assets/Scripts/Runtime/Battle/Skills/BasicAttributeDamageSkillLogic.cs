using System;
using Pachimon.Reward;

namespace Pachimon.Battle
{
    public sealed class BasicAttributeDamageSkillLogic : ISkillLogic
    {
        public const int BaseDamage = 100;
        private readonly PachimonAttribute _attribute;

        public BasicAttributeDamageSkillLogic(PachimonAttribute attribute)
        {
            _attribute = attribute;
        }

        public SkillPreview Preview(SkillExecutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            var target = GetTarget(context);
            var damage = CalculatePreviewDamage(context, target);
            return new SkillPreview(
                context.User,
                context.Skill,
                new[]
                {
                    new SkillPreviewEffect(
                        target,
                        -Math.Min(target.CurrentHp, damage)),
                });
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            var target = GetTarget(context);
            var damage = AttributeDamageCalculator.Calculate(
                BaseDamage,
                context.User.StartingStats,
                target.StartingStats,
                _attribute);
            var beforeDamage = new BeforeAttributeDamageEvent(
                context.State,
                context.User,
                target,
                _attribute,
                damage);
            context.State.Events.Publish(beforeDamage);
            var appliedDamage = target.ApplyDamage(beforeDamage.Damage);
            return new SkillResolution(
                context.User,
                context.Skill,
                new[] { new SkillEffectResult(target, appliedDamage, false) });
        }

        private BattleUnitState GetTarget(SkillExecutionContext context)
        {
            return context.Targets.GetFrontEnemy()
                ?? throw new InvalidOperationException("No living Enemy target was found.");
        }

        private int CalculatePreviewDamage(
            SkillExecutionContext context,
            BattleUnitState target)
        {
            var damage = AttributeDamageCalculator.Calculate(
                BaseDamage,
                context.User.StartingStats,
                target.StartingStats,
                _attribute);
            foreach (var passiveId in context.User.PassiveIds)
            {
                if (!PassiveLogicRegistry.TryGetPlaceholderAttribute(
                        passiveId,
                        out var passiveAttribute)
                    || passiveAttribute != _attribute)
                {
                    continue;
                }

                var multiplied =
                    ((long)damage * OutgoingAttributeDamagePassiveLogic.DamagePercent) / 100L;
                damage = (int)Math.Max(1L, Math.Min(multiplied, int.MaxValue));
            }

            return damage;
        }
    }
}
