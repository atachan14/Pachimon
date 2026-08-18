using System;
using System.Collections.Generic;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class CuttingDanceSkillLogic : ISkillLogic
    {
        private readonly CuttingDanceSkillAsset _skill;
        public CuttingDanceSkillLogic(CuttingDanceSkillAsset skill) =>
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            context.Targets.GetAllEnemies();
            context.UseContinuousPresentationBlocks();
            var chainCount = ChainTargetNavigator.GetEffectiveAdditionalChainCount(
                context.User, _skill.BaseChainCount);
            var navigator = new ChainTargetNavigator(
                context.State.GetOpposingSide(context.User.Side));
            var effects = new List<SkillEffectResult>();
            for (var index = 0; index <= chainCount; index++)
            {
                var target = navigator.GetNext();
                if (target == null || !context.User.IsAlive) break;
                if (index > 0) context.BeginNextPresentationBlock();
                var ratio = ChainTargetNavigator.GetDamageRatio(index, chainCount);
                var hit = context.BeginAttackHit(target);
                var damage = context.ScaleFromAttribute(_skill.BaseWindDamage,
                    PachimonAttribute.Wind, _skill.WindDamageRatio) * ratio;
                var result = BattleAttributeDamageService.Apply(context.State,
                    context.User, target, new DamageContext(
                        DamageOriginKind.Skill, _skill.SkillId, damage,
                        context.User.GetBattleStats(), target.GetBattleStats(),
                        PachimonAttribute.Wind, true,
                        applyAttackerAttributeMultiplier: false), hit);
                effects.Add(new SkillEffectResult(result.ActualTarget,
                    result.AppliedDamage, false, hit: hit));

                var erosion = SignedStatMath.FloorNonNegative(
                    context.ScaleFromAttribute(_skill.BaseErosion,
                        PachimonAttribute.Wind, _skill.ErosionWindRatio) * ratio);
                if (erosion > 0)
                {
                    hit.ApplyStatus(new BattleStatusInstance(
                        BattleStatusId.WindErosion,
                        BattleStatusCategory.None,
                        context.User,
                        erosion,
                        definition: _skill.ErosionStatus));
                }
            }
            if (effects.Count > 0)
                context.State.Events.Publish(new ChainResolvedEvent(context.State,
                    context.User, _skill, effects.Count - 1));
            AddChainRuntime.AddUnits(context.User, context.User,
                _skill.AddChainGainUnits);
            return new SkillResolution(context.User, _skill, effects);
        }
    }
}
