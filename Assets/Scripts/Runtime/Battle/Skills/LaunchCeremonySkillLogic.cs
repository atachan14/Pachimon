using System;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class LaunchCeremonySkillLogic : ISkillLogic
    {
        private readonly LaunchCeremonySkillAsset _skill;

        public LaunchCeremonySkillLogic(LaunchCeremonySkillAsset skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (!ReferenceEquals(context?.Skill, _skill))
            {
                throw new ArgumentException(
                    "Launch Ceremony Logic received another Skill Asset.",
                    nameof(context));
            }
            if (_skill.StatusDefinition == null)
            {
                throw new InvalidOperationException(
                    "Launch Ceremony Status Definition is not assigned.");
            }

            context.State.Statuses.ApplyStatus(
                context.User,
                new BattleStatusInstance(
                    BattleStatusId.LaunchCeremony,
                    BattleStatusCategory.None,
                    context.User,
                    value: 0,
                    definition: _skill.StatusDefinition));

            return new SkillResolution(
                context.User,
                context.Skill,
                Array.Empty<SkillEffectResult>());
        }
    }
}
