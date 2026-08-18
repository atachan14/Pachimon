using System;
using System.Collections.Generic;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class SpiritBombSkillLogic : ISkillLogic
    {
        private readonly SpiritBombSkillAsset _skill;

        public SpiritBombSkillLogic(SpiritBombSkillAsset skill) =>
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            var totalSpent = 0;
            foreach (var ally in context.Targets.GetAllAllies())
            {
                var spend = SignedStatMath.FloorNonNegative(
                    ally.CurrentMn * _skill.CurrentMnPercent / 100m);
                if (!context.TrySpendAdditionalMn(ally, spend))
                    throw new InvalidOperationException("Failed to spend Spirit Bomb MN.");
                totalSpent = checked(totalSpent + spend);
            }

            IReadOnlyList<BattleUnitState> targets;
            try
            {
                targets = context.Targets.GetAllEnemies();
            }
            catch (SkillTargetUnavailableException)
            {
                return new SkillResolution(
                    context.User,
                    context.Skill,
                    Array.Empty<SkillEffectResult>(),
                    wasTargetUnavailable: true);
            }

            var totalDamage = checked(totalSpent * _skill.DamageMultiplier);
            var damagePerTarget = targets.Count == 0 ? 0 : totalDamage / targets.Count;
            var remainder = targets.Count == 0 ? 0 : totalDamage % targets.Count;
            var effects = new List<SkillEffectResult>(targets.Count);
            for (var index = 0; index < targets.Count; index++)
            {
                var target = targets[index];
                var damage = damagePerTarget + (index < remainder ? 1 : 0);
                var hit = context.BeginAttackHit(target);
                var applied = BattleTrueDamageService.Apply(
                    context.State,
                    context.User,
                    target,
                    new TrueDamageContext(
                        DamageOriginKind.Skill,
                        _skill.SkillId,
                        damage,
                        isAttack: true),
                    hit);
                effects.Add(new SkillEffectResult(
                    applied.ActualTarget,
                    applied.AppliedDamage,
                    isTrueDamage: true,
                    wasEvaded: applied.WasEvaded,
                    hit: hit));
            }
            return new SkillResolution(context.User, context.Skill, effects);
        }
    }
}
