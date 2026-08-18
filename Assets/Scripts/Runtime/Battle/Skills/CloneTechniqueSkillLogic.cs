using System;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class CloneTechniqueSkillLogic : ISkillLogic
    {
        private readonly CloneTechniqueSkillAsset _skill;

        public CloneTechniqueSkillLogic(CloneTechniqueSkillAsset skill) =>
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            context.User.AddStatusStacks(
                BattleStatusId.Clone,
                BattleStatusCategory.None,
                context.User,
                value: 0,
                stackCount: _skill.Stacks,
                definition: _skill.Status);
            return new SkillResolution(
                context.User,
                context.Skill,
                Array.Empty<SkillEffectResult>());
        }
    }
}
