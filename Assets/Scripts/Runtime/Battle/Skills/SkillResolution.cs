using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Skills;
using Pachimon.Reward;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class SkillEffectResult
    {
        public SkillEffectResult(BattleUnitState target, int damage, bool isTrueDamage)
        {
            Target = target ?? throw new ArgumentNullException(nameof(target));
            Damage = damage;
            IsTrueDamage = isTrueDamage;
        }

        public BattleUnitState Target { get; }
        public int Damage { get; }
        public bool IsTrueDamage { get; }
    }

    public sealed class SkillResolution
    {
        public SkillResolution(
            BattleUnitState user,
            SkillAsset skill,
            IEnumerable<SkillEffectResult> effects,
            BattlePresentationTimeline presentation = null,
            bool wasTargetUnavailable = false,
            int actualManaSpent = 0,
            decimal effectiveManaSpent = 0m)
        {
            User = user ?? throw new ArgumentNullException(nameof(user));
            Skill = skill ?? throw new ArgumentNullException(nameof(skill));
            Effects = effects?.ToArray() ?? Array.Empty<SkillEffectResult>();
            Presentation = presentation ?? BattlePresentationTimeline.Empty;
            WasTargetUnavailable = wasTargetUnavailable;
            ActualManaSpent = actualManaSpent;
            EffectiveManaSpent = effectiveManaSpent;
        }

        public BattleUnitState User { get; }
        public SkillAsset Skill { get; }
        public IReadOnlyList<SkillEffectResult> Effects { get; }
        public BattlePresentationTimeline Presentation { get; }
        public bool WasTargetUnavailable { get; }
        public int ActualManaSpent { get; }
        public decimal EffectiveManaSpent { get; }

        public SkillResolution WithPresentation(
            BattlePresentationTimeline presentation)
        {
            return new SkillResolution(
                User,
                Skill,
                Effects,
                presentation,
                WasTargetUnavailable,
                ActualManaSpent,
                EffectiveManaSpent);
        }

        public SkillResolution WithManaSpent(
            int actualManaSpent,
            decimal effectiveManaSpent)
        {
            return new SkillResolution(
                User,
                Skill,
                Effects,
                Presentation,
                WasTargetUnavailable,
                actualManaSpent,
                effectiveManaSpent);
        }
    }

    public sealed class SkillPreviewEffect
    {
        public SkillPreviewEffect(
            BattleUnitState target,
            int hpDelta,
            int mnDelta = 0)
        {
            Target = target ?? throw new ArgumentNullException(nameof(target));
            HpDelta = hpDelta;
            MnDelta = mnDelta;
        }

        public BattleUnitState Target { get; }
        public int HpDelta { get; }
        public int MnDelta { get; }
    }

    public sealed class SkillPreview
    {
        public SkillPreview(
            BattleUnitState user,
            SkillAsset skill,
            IEnumerable<SkillPreviewEffect> effects,
            BattleSkillTimingPlan timing,
            bool wasTargetUnavailable = false)
        {
            User = user ?? throw new ArgumentNullException(nameof(user));
            Skill = skill ?? throw new ArgumentNullException(nameof(skill));
            Effects = effects?.ToArray() ?? Array.Empty<SkillPreviewEffect>();
            Timing = timing;
            WasTargetUnavailable = wasTargetUnavailable;
        }

        public BattleUnitState User { get; }
        public SkillAsset Skill { get; }
        public IReadOnlyList<SkillPreviewEffect> Effects { get; }
        public BattleSkillTimingPlan Timing { get; }
        public bool WasTargetUnavailable { get; }
    }

    public sealed class SkillExecutionContext
    {
        public SkillExecutionContext(
            BattleState state,
            BattleUnitState user,
            SkillAsset skill,
            object runtimeData = null,
            int actualManaSpent = 0,
            decimal effectiveManaSpent = 0m)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            User = user ?? throw new ArgumentNullException(nameof(user));
            Skill = skill ?? throw new ArgumentNullException(nameof(skill));
            RuntimeData = runtimeData;
            ActualManaSpent = actualManaSpent;
            EffectiveManaSpent = effectiveManaSpent;
            Targets = new BattleTargetQuery(state, user);
        }

        public BattleState State { get; }
        public BattleUnitState User { get; }
        public SkillAsset Skill { get; }
        public object RuntimeData { get; }
        public int ActualManaSpent { get; }
        public decimal EffectiveManaSpent { get; }
        public BattleTargetQuery Targets { get; }

        public int GetAttributeValue(PachimonAttribute attribute)
        {
            return User.GetBattleStatValue(
                PachimonStatTypeUtility.FromAttribute(attribute));
        }

        public decimal GetAttributeRatio(
            PachimonAttribute attribute,
            decimal baseRatio = 100m)
        {
            return State.ResolveAttributeRatio(attribute, baseRatio);
        }

        public decimal ScaleFromAttribute(
            decimal baseValue,
            PachimonAttribute attribute,
            decimal baseRatio = 100m)
        {
            return SignedStatMath.ScaleFromBase(
                baseValue,
                GetAttributeValue(attribute),
                GetAttributeRatio(attribute, baseRatio));
        }

        public bool TrySpendAdditionalMn(int amount)
        {
            var before = User.CurrentMn;
            if (!User.TrySpendMn(amount))
            {
                return false;
            }

            State.Presentation.RecordAdditionalManaSpent(
                User,
                before,
                User.CurrentMn);
            return true;
        }

        public void BeginNextPresentationBlock()
        {
            State.Presentation.BeginNextBlock();
        }

        public void UseContinuousPresentationBlocks()
        {
            State.Presentation.UseContinuousBlocks();
        }
    }

    public interface ISkillLogic
    {
        SkillResolution Resolve(SkillExecutionContext context);
    }

    public interface IStartupSkillLogic : ISkillLogic
    {
        object BeginStartup(SkillExecutionContext context);
    }
}
