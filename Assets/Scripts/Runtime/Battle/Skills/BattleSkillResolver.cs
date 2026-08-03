using System;
using System.Linq;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public static class BattleSkillResolver
    {
        public static SkillResolution Resolve(
            BattleState state,
            BattleUnitState user,
            SkillAsset skill,
            ISkillLogic logic)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (skill == null) throw new ArgumentNullException(nameof(skill));
            if (logic == null) throw new ArgumentNullException(nameof(logic));

            state.Events.Publish(new BeforeSkillEvent(state, user, skill));
            state.Statuses.BeginSkillResolution();
            SkillResolution resolution;
            try
            {
                resolution = logic.Resolve(
                    new SkillExecutionContext(state, user, skill));
                var statusEffects = state.Statuses.EndSkillResolution();
                if (statusEffects.Count > 0)
                {
                    resolution = new SkillResolution(
                        resolution.User,
                        resolution.Skill,
                        resolution.Effects.Concat(statusEffects));
                }
            }
            catch
            {
                state.Statuses.CancelSkillResolution();
                throw;
            }

            state.Events.Publish(new SkillResolvedEvent(state, resolution));
            foreach (var defeatedUnit in resolution.Effects
                         .Where(effect =>
                             effect.Damage > 0
                             && effect.Target.IsDefeated)
                         .Select(effect => effect.Target)
                         .Distinct())
            {
                state.Events.Publish(
                    new UnitDefeatedEvent(state, user, defeatedUnit));
            }

            return resolution;
        }
    }
}
