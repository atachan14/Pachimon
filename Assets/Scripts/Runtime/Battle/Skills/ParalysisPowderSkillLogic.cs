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
            if (_skill.PollenStatus == null) throw new InvalidOperationException("Paralysis Powder requires a Pollen Status.");
            var targets = context.Targets.GetAllEnemies();
            foreach (var target in targets)
            {
                var hit = context.BeginStatusHit(target);
                var raw = context.ScaleFromAttribute(
                        _skill.BaseElectricValue,
                        PachimonAttribute.Electric,
                        _skill.ElectricValueRatio)
                    + context.ScaleFromAttribute(
                        _skill.BasePoisonValue,
                        PachimonAttribute.Poison,
                        _skill.PoisonValueRatio);
                raw = context.State.Passives.ModifyOutgoingStatusValue(
                    context.State, context.User, target, _skill.ParalysisStatus.StatusId,
                    BattleStatusCategory.Slow, raw);
                var value = SignedStatMath.FloorNonNegative(raw);
                if (value > 0)
                {
                    hit.ApplyStatus(BattleStatusFactory.CreateSlow(
                            context.User,
                            value,
                            _skill.ParalysisStatus,
                            Math.Max(1, SignedStatMath.FloorNonNegative(
                                context.ScaleFromAttribute(
                                    _skill.BaseDurationTicks,
                                    PachimonAttribute.Leaf,
                                    _skill.DurationLeafRatio)))));
                }
                var pollenValue = SignedStatMath.FloorNonNegative(
                    context.ScaleFromAttribute(
                        _skill.PollenBaseValue,
                        PachimonAttribute.Poison,
                        _skill.PollenPoisonRatio));
                if (pollenValue > 0)
                {
                    hit.ApplyStatus(BattleStatusFactory.CreatePollen(
                        context.User,
                        pollenValue,
                        _skill.PollenStatus));
                }
            }
            return new SkillResolution(context.User, _skill, Array.Empty<SkillEffectResult>(), wasTargetUnavailable: targets.Count == 0);
        }
    }
}
