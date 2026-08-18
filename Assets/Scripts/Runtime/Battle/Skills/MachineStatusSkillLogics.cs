using System;
using System.Collections.Generic;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class SexyPoseSkillLogic : ISkillLogic
    {
        private readonly SexyPoseSkillAsset _skill;

        public SexyPoseSkillLogic(SexyPoseSkillAsset skill) =>
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            context.User.AddStatusStacks(
                BattleStatusId.Charm,
                BattleStatusCategory.None,
                context.User,
                value: 1,
                stackCount: _skill.CharmStacks,
                definition: _skill.CharmStatus);
            var charm = context.User.GetStatus(BattleStatusId.Charm)?.StackCount ?? 0;
            var stunTicks = SignedStatMath.FloorNonNegative(
                charm * _skill.StunRatio / 100m);
            var effects = new List<SkillEffectResult>();
            foreach (var target in context.Targets.GetAllEnemies())
            {
                var hit = context.BeginAttackHit(target);
                if (stunTicks > 0)
                {
                    hit.ApplyStatus(BattleStatusFactory.CreateStun(
                        context.User,
                        stunTicks,
                        _skill.StunStatus));
                }
                effects.Add(new SkillEffectResult(
                    hit.Target,
                    damage: 0,
                    isTrueDamage: false,
                    wasEvaded: hit.WasEvaded,
                    hit: hit));
            }
            return new SkillResolution(context.User, context.Skill, effects);
        }
    }

    public sealed class IntangibilitySkillLogic : IStartupSkillLogic
    {
        private readonly IntangibilitySkillAsset _skill;

        public IntangibilitySkillLogic(IntangibilitySkillAsset skill) =>
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));

        public object BeginStartup(SkillExecutionContext context)
        {
            context.User.AddStatusStacks(
                BattleStatusId.Intangible,
                BattleStatusCategory.None,
                context.User,
                value: 0,
                stackCount: 1,
                definition: _skill.Status);
            return null;
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            context.User.TryConsumeStatus(BattleStatusId.Intangible, out _);
            return new SkillResolution(
                context.User,
                context.Skill,
                Array.Empty<SkillEffectResult>());
        }
    }
}
