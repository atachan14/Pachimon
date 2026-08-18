using System;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class WindErosionSkillLogic : ISkillLogic
    {
        private readonly WindErosionSkillAsset _skill;
        public WindErosionSkillLogic(WindErosionSkillAsset skill) =>
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (!ReferenceEquals(context?.Skill, _skill))
                throw new ArgumentException("Wind Erosion Logic received another Skill.", nameof(context));
            if (_skill.StatusDefinition == null)
                throw new InvalidOperationException("Wind Erosion requires a Status Definition.");

            var targets = context.Targets.GetAllEnemies();
            var value = SignedStatMath.FloorNonNegative(
                context.ScaleFromAttribute(
                    _skill.BaseErosionValue,
                    PachimonAttribute.Wind,
                    _skill.WindValueRatio));
            foreach (var target in targets)
            {
                if (value <= 0) continue;
                context.BeginStatusHit(target).ApplyStatus(
                    new BattleStatusInstance(
                        BattleStatusId.WindErosion,
                        BattleStatusCategory.None,
                        context.User,
                        value,
                        definition: _skill.StatusDefinition));
            }

            return new SkillResolution(
                context.User,
                _skill,
                Array.Empty<SkillEffectResult>(),
                wasTargetUnavailable: targets.Count == 0);
        }
    }
}
