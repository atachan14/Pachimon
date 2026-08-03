using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Skills;

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
            BattlePresentationTimeline presentation = null)
        {
            User = user ?? throw new ArgumentNullException(nameof(user));
            Skill = skill ?? throw new ArgumentNullException(nameof(skill));
            Effects = effects?.ToArray() ?? Array.Empty<SkillEffectResult>();
            Presentation = presentation ?? BattlePresentationTimeline.Empty;
        }

        public BattleUnitState User { get; }
        public SkillAsset Skill { get; }
        public IReadOnlyList<SkillEffectResult> Effects { get; }
        public BattlePresentationTimeline Presentation { get; }

        public SkillResolution WithPresentation(
            BattlePresentationTimeline presentation)
        {
            return new SkillResolution(User, Skill, Effects, presentation);
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
            BattleSkillTimingPlan timing)
        {
            User = user ?? throw new ArgumentNullException(nameof(user));
            Skill = skill ?? throw new ArgumentNullException(nameof(skill));
            Effects = effects?.ToArray() ?? Array.Empty<SkillPreviewEffect>();
            Timing = timing;
        }

        public BattleUnitState User { get; }
        public SkillAsset Skill { get; }
        public IReadOnlyList<SkillPreviewEffect> Effects { get; }
        public BattleSkillTimingPlan Timing { get; }
    }

    public sealed class SkillExecutionContext
    {
        public SkillExecutionContext(
            BattleState state,
            BattleUnitState user,
            SkillAsset skill)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            User = user ?? throw new ArgumentNullException(nameof(user));
            Skill = skill ?? throw new ArgumentNullException(nameof(skill));
            Targets = new BattleTargetQuery(state, user);
        }

        public BattleState State { get; }
        public BattleUnitState User { get; }
        public SkillAsset Skill { get; }
        public BattleTargetQuery Targets { get; }

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
    }

    public interface ISkillLogic
    {
        SkillResolution Resolve(SkillExecutionContext context);
    }
}
