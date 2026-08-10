using System;
using System.Collections.Generic;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class ParalysisPowderSkillLogic : ISkillLogic
    {
        private readonly ParalysisPowderSkillAsset _skill;
        public ParalysisPowderSkillLogic(ParalysisPowderSkillAsset skill) => _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (!ReferenceEquals(context?.Skill, _skill)) throw new ArgumentException("Paralysis Powder Logic received another Skill.", nameof(context));
            if (_skill.ParalysisStatus == null) throw new InvalidOperationException("Paralysis Powder requires a Paralysis Status.");
            var targets = context.Targets.GetAllEnemies();
            foreach (var target in targets)
            {
                var raw = context.ScaleFromAttribute(_skill.BaseLeafParalysis, PachimonAttribute.Leaf, _skill.LeafRatio)
                    + context.ScaleFromAttribute(_skill.BasePoisonParalysis, PachimonAttribute.Poison, _skill.PoisonRatio);
                raw = context.State.Passives.ModifyOutgoingStatusValue(
                    context.State, context.User, target, _skill.ParalysisStatus.StatusId,
                    BattleStatusCategory.Slow, raw);
                var value = SignedStatMath.FloorNonNegative(raw);
                if (value <= 0) continue;
                context.State.Statuses.ApplyAttackStatus(target,
                    BattleStatusFactory.CreateSlow(context.User, value, _skill.ParalysisStatus));
            }
            return new SkillResolution(context.User, _skill, Array.Empty<SkillEffectResult>(), wasTargetUnavailable: targets.Count == 0);
        }
    }
}
