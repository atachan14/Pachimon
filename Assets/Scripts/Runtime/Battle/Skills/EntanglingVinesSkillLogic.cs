using System;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class EntanglingVinesSkillLogic : ISkillLogic
    {
        private readonly EntanglingVinesSkillAsset _skill;
        public EntanglingVinesSkillLogic(EntanglingVinesSkillAsset skill) => _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (!ReferenceEquals(context?.Skill, _skill)) throw new ArgumentException("Entangling Vines Logic received another Skill.", nameof(context));
            if (_skill.StunStatus == null) throw new InvalidOperationException("Entangling Vines requires a Stun Status.");
            var target = context.Targets.GetFrontEnemy();
            if (target == null) return new SkillResolution(context.User, _skill, Array.Empty<SkillEffectResult>(), wasTargetUnavailable: true);
            var duration = Math.Max(1, SignedStatMath.FloorNonNegative(
                context.ScaleFromAttribute(_skill.BaseStun, PachimonAttribute.Leaf, _skill.StunLeafRatio)));
            context.State.Statuses.ApplyAttackStatus(target, BattleStatusFactory.CreateStun(context.User, duration, _skill.StunStatus));
            context.State.Statuses.ApplyAttackStatus(context.User, BattleStatusFactory.CreateStun(context.User, duration, _skill.StunStatus));
            return new SkillResolution(context.User, _skill, Array.Empty<SkillEffectResult>());
        }
    }
}
