using System;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class DragonDanceSkillLogic : ISkillLogic
    {
        private readonly DragonDanceSkillAsset _skill;

        public DragonDanceSkillLogic(DragonDanceSkillAsset skill) =>
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (!ReferenceEquals(context?.Skill, _skill))
                throw new ArgumentException("Dragon Dance Logic received another Skill.", nameof(context));
            if (_skill.StatusDefinition == null)
                throw new InvalidOperationException("Dragon Dance requires a Status Definition.");

            var existing = context.User.GetStatus(BattleStatusId.DragonDance);
            var current = existing?.RuntimeData as DragonDanceRuntimeData;
            var dragonBonus = checked((current?.DragonBonus ?? 0) + _skill.DragonBonus);
            var speedBonus = checked((current?.SpeedBonus ?? 0) + _skill.SpeedBonus);
            context.State.Statuses.ApplyStatus(
                context.User,
                new BattleStatusInstance(
                    BattleStatusId.DragonDance,
                    BattleStatusCategory.None,
                    context.User,
                    value: 0,
                    runtimeData: new DragonDanceRuntimeData(
                        dragonBonus,
                        speedBonus),
                    definition: _skill.StatusDefinition));
            context.State.Presentation.RecordLog(
                $"{context.User.DisplayName}は龍の舞を踊った！");

            return new SkillResolution(
                context.User,
                _skill,
                Array.Empty<SkillEffectResult>());
        }
    }
}
