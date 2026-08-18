using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class PlantRageSkillLogic : ISkillLogic
    {
        private readonly PlantRageSkillAsset _skill;
        public PlantRageSkillLogic(PlantRageSkillAsset skill) =>
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            var definition = _skill.ResponsivePlant
                ?? throw new InvalidOperationException(
                    "Plant Rage requires a Responsive Plant Definition.");
            var value = SignedStatMath.FloorNonNegative(
                context.ScaleFromAttribute(
                    definition.BaseValue,
                    PachimonAttribute.Leaf,
                    definition.LeafRatio));
            if (value > 0)
                context.State.Fields.CreateResponsivePlant(
                    context.User,
                    definition,
                    value);
            context.State.Fields.AttackAllPlants(
                context.User.Side,
                damageBonusPercent: 100);
            return Empty(context);
        }

        private static SkillResolution Empty(SkillExecutionContext context) =>
            new(context.User, context.Skill, Array.Empty<SkillEffectResult>());
    }

    public sealed class ChainThunderSkillLogic : ISkillLogic
    {
        private readonly ChainThunderSkillAsset _skill;
        public ChainThunderSkillLogic(ChainThunderSkillAsset skill) =>
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            context.Targets.GetAllEnemies();
            context.UseContinuousPresentationBlocks();
            var chainCount = context.State.ElectricDamageCount;
            var navigator = new ChainTargetNavigator(
                context.State.GetOpposingSide(context.User.Side));
            var effects = new List<SkillEffectResult>();
            for (var hitIndex = 0; hitIndex <= chainCount; hitIndex++)
            {
                var target = navigator.GetNext();
                if (target == null || !context.User.IsAlive) break;
                if (hitIndex > 0) context.BeginNextPresentationBlock();
                var ratio = ChainTargetNavigator.GetDamageRatio(
                    hitIndex,
                    chainCount);
                var damage = context.ScaleFromAttribute(
                    _skill.BaseDamage,
                    PachimonAttribute.Electric,
                    _skill.ElectricRatio) * ratio;
                var result = BattleAttributeDamageService.Apply(
                    context.State,
                    context.User,
                    target,
                    new DamageContext(
                        DamageOriginKind.Skill,
                        _skill.SkillId,
                        damage,
                        context.User.GetBattleStats(),
                        target.GetBattleStats(),
                        PachimonAttribute.Electric,
                        isAttack: true,
                        applyAttackerAttributeMultiplier: false));
                effects.Add(new SkillEffectResult(
                    result.ActualTarget,
                    result.AppliedDamage,
                    false));
            }
            return new SkillResolution(context.User, context.Skill, effects);
        }
    }

    public sealed class DeathmatchSkillLogic : ISkillLogic
    {
        private readonly DeathmatchSkillAsset _skill;
        public DeathmatchSkillLogic(DeathmatchSkillAsset skill) =>
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            var value = context.User.CurrentHp;
            var targets = new[] { context.User }
                .Concat(context.Targets.GetAllEnemies())
                .ToArray();
            foreach (var target in targets)
            {
                context.BeginStatusHit(target).ApplyStatus(
                    BattleStatusFactory.CreateToxin(
                        context.User,
                        value,
                        _skill.ToxinStatus));
            }
            return new SkillResolution(
                context.User,
                context.Skill,
                Array.Empty<SkillEffectResult>());
        }
    }

    public sealed class FreezingSkillLogic : ISkillLogic
    {
        private readonly FreezingSkillAsset _skill;
        public FreezingSkillLogic(FreezingSkillAsset skill) =>
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            var target = MachineSkillTarget.Front(context);
            var hit = context.BeginAttackHit(target);
            if (hit.WasEvaded)
            {
                return new SkillResolution(context.User, context.Skill,
                    new[] { new SkillEffectResult(target, 0, true, hit: hit) });
            }

            var chill = target.GetStatus(BattleStatusId.Chill);
            var value = chill?.Value ?? 0;
            if (chill != null)
                context.State.Statuses.TryConsumeStatus(
                    target,
                    BattleStatusId.Chill,
                    out _);
            var result = BattleTrueDamageService.Apply(
                context.State,
                context.User,
                target,
                new TrueDamageContext(
                    DamageOriginKind.Skill,
                    _skill.SkillId,
                    value,
                    isAttack: true),
                hit);
            if (value > 0 && target.IsAlive)
            {
                hit.ApplyStatus(BattleStatusFactory.CreateFreeze(
                    context.User,
                    value,
                    _skill.FreezeStatus));
            }
            return new SkillResolution(context.User, context.Skill,
                new[]
                {
                    new SkillEffectResult(
                        result.ActualTarget,
                        result.AppliedDamage,
                        true,
                        hit: hit),
                });
        }
    }

    public sealed class WindGodSkillLogic : ISkillLogic
    {
        private readonly WindGodSkillAsset _skill;
        public WindGodSkillLogic(WindGodSkillAsset skill) =>
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            var target = MachineSkillTarget.Front(context);
            var hit = context.BeginAttackHit(target);
            var result = BattleAttributeDamageService.Apply(
                context.State,
                context.User,
                target,
                new DamageContext(
                    DamageOriginKind.Skill,
                    _skill.SkillId,
                    context.ScaleFromAttribute(
                        _skill.BaseDamage,
                        PachimonAttribute.Wind,
                        _skill.WindRatio),
                    context.User.GetBattleStats(),
                    target.GetBattleStats(),
                    PachimonAttribute.Wind,
                    isAttack: true,
                    applyAttackerAttributeMultiplier: false),
                hit);
            if (context.User.IsAlive)
            {
                context.State.Statuses.ApplyStatus(
                    context.User,
                    new BattleStatusInstance(
                        BattleStatusId.WindGod,
                        BattleStatusCategory.None,
                        context.User,
                        value: 0,
                        durationTicks: _skill.DurationTicks,
                        definition: _skill.Status));
            }
            return new SkillResolution(context.User, context.Skill,
                new[] { new SkillEffectResult(result.ActualTarget,
                    result.AppliedDamage, false, hit: hit) });
        }
    }

    public sealed class DragonInstallSkillLogic : ISkillLogic
    {
        private readonly DragonInstallSkillAsset _skill;
        public DragonInstallSkillLogic(DragonInstallSkillAsset skill) =>
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            var hpAfter = Math.Max(1, (context.User.CurrentHp + 1) / 2);
            context.User.ApplyDamage(context.User.CurrentHp - hpAfter);
            context.State.AddLog(
                $"{context.User.DisplayName}のHPが半分になった！");
            context.State.Statuses.ApplyStatus(
                context.User,
                new BattleStatusInstance(
                    BattleStatusId.DragonInstall,
                    BattleStatusCategory.None,
                    context.User,
                    _skill.MultiplierPercent,
                    durationTicks: _skill.DurationTicks,
                    definition: _skill.Status));
            return new SkillResolution(
                context.User,
                context.Skill,
                Array.Empty<SkillEffectResult>());
        }
    }
}
