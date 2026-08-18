using System;
using System.Collections.Generic;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class ChainVinesSkillLogic : ISkillLogic
    {
        private readonly ChainVinesSkillAsset _skill;

        public ChainVinesSkillLogic(ChainVinesSkillAsset skill) =>
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (!ReferenceEquals(context?.Skill, _skill)) throw new ArgumentException("Chain Vines Logic received another Skill.", nameof(context));
            if (_skill.SlowStatus == null) throw new InvalidOperationException("Chain Vines requires a Slow Status.");

            context.Targets.GetAllEnemies();
            context.UseContinuousPresentationBlocks();
            var chainCount = ChainTargetNavigator.GetEffectiveAdditionalChainCount(context.User, _skill.BaseChainCount);
            var navigator = new ChainTargetNavigator(context.State.GetOpposingSide(context.User.Side));
            var effects = new List<SkillEffectResult>();
            for (var hit = 0; hit <= chainCount; hit++)
            {
                var target = navigator.GetNext();
                if (target == null || !context.User.IsAlive) break;
                if (hit > 0) context.BeginNextPresentationBlock();
                var skillHit = context.BeginAttackHit(target);

                var ratio = ChainTargetNavigator.GetDamageRatio(hit, chainCount);
                var damage = context.ScaleFromAttribute(_skill.BaseLeafDamage, PachimonAttribute.Leaf, _skill.LeafDamageRatio) * ratio;
                var result = BattleAttributeDamageService.Apply(context.State, context.User, target,
                    new DamageContext(DamageOriginKind.Skill, _skill.SkillId, damage,
                        context.User.GetBattleStats(), target.GetBattleStats(), PachimonAttribute.Leaf,
                        isAttack: true, applyAttackerAttributeMultiplier: false),
                    skillHit);
                var actualTarget = result.ActualTarget;
                effects.Add(new SkillEffectResult(
                    actualTarget,
                    result.AppliedDamage,
                    false,
                    hit: skillHit));

                var rawSlow = context.ScaleFromAttribute(_skill.BaseSlow, PachimonAttribute.Leaf, _skill.SlowLeafRatio) * ratio;
                var modifiedSlow = context.State.Passives.ModifyOutgoingStatusValue(
                    context.State, context.User, actualTarget, _skill.SlowStatus.StatusId,
                    BattleStatusCategory.Slow, rawSlow);
                var slow = SignedStatMath.FloorNonNegative(modifiedSlow);
                if (slow > 0)
                {
                    skillHit.ApplyStatus(
                        BattleStatusFactory.CreateSlow(
                            context.User,
                            slow,
                            _skill.SlowStatus));
                }
            }

            if (effects.Count > 0)
            {
                context.State.Events.Publish(new ChainResolvedEvent(context.State, context.User, _skill, effects.Count - 1));
            }
            AddChainRuntime.AddUnits(context.User, context.User, _skill.AddChainGainUnits);
            return new SkillResolution(context.User, _skill, effects);
        }
    }
}
