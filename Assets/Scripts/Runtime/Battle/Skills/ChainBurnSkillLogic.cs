using System;
using System.Collections.Generic;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class ChainBurnSkillLogic : ISkillLogic
    {
        private readonly ChainBurnSkillAsset _skill;

        public ChainBurnSkillLogic(ChainBurnSkillAsset skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            ValidateSkill(context);
            context.Targets.GetAllEnemies();
            context.UseContinuousPresentationBlocks();
            var additionalChainCount =
                ChainTargetNavigator.GetEffectiveAdditionalChainCount(
                    context.User,
                    _skill.BaseChainCount);
            var navigator = new ChainTargetNavigator(
                context.State.GetOpposingSide(context.User.Side));
            var effects = new List<SkillEffectResult>();

            for (var hitIndex = 0;
                 hitIndex <= additionalChainCount;
                 hitIndex++)
            {
                var target = navigator.GetNext();
                if (target == null || !context.User.IsAlive)
                {
                    break;
                }

                if (hitIndex > 0)
                {
                    context.BeginNextPresentationBlock();
                }

                var ratio = ChainTargetNavigator.GetDamageRatio(
                    hitIndex,
                    additionalChainCount);
                var baseDamage = context.ScaleFromAttribute(
                    _skill.BaseDamage,
                    PachimonAttribute.Fire,
                    _skill.FireScalingPercent)
                    * ratio;
                var result = BattleAttributeDamageService.Apply(
                    context.State,
                    context.User,
                    target,
                    new DamageContext(
                        DamageOriginKind.Skill,
                        _skill.SkillId,
                        baseDamage,
                        context.User.GetBattleStats(),
                        target.GetBattleStats(),
                        PachimonAttribute.Fire,
                        isAttack: true,
                        applyAttackerAttributeMultiplier: false));
                effects.Add(new SkillEffectResult(
                    result.ActualTarget,
                    result.AppliedDamage,
                    isTrueDamage: false));
            }

            if (effects.Count > 0)
            {
                context.State.Events.Publish(new ChainResolvedEvent(
                    context.State,
                    context.User,
                    context.Skill,
                    effects.Count - 1));
            }

            AddChainRuntime.AddUnits(
                context.User,
                context.User,
                _skill.AddChainGainUnits);
            return new SkillResolution(context.User, context.Skill, effects);
        }

        private void ValidateSkill(SkillExecutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!ReferenceEquals(context.Skill, _skill))
            {
                throw new ArgumentException(
                    "Chain Burn Logic received another Skill Asset.",
                    nameof(context));
            }
        }
    }
}
