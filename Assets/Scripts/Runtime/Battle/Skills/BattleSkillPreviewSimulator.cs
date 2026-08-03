using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public static class BattleSkillPreviewSimulator
    {
        public static SkillPreview Simulate(
            BattleState state,
            BattleUnitState user,
            SkillAsset skill,
            ISkillLogic logic,
            bool spendMana)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (skill == null) throw new ArgumentNullException(nameof(skill));
            if (logic == null) throw new ArgumentNullException(nameof(logic));

            var snapshot = BattleSimulationSnapshot.Create(state);
            var simulationUser = snapshot.GetSimulationUnit(user);
            if (spendMana
                && !simulationUser.TrySpendMn(skill.BaseManaCost))
            {
                throw new InvalidOperationException(
                    $"Unit '{user.InstanceId}' could not spend "
                    + $"{skill.BaseManaCost} MN in Preview.");
            }

            BattleSkillResolver.Resolve(
                snapshot.State,
                simulationUser,
                skill,
                logic);

            var effects = new List<SkillPreviewEffect>();
            foreach (var original in state.Player.Units.Concat(state.Enemy.Units))
            {
                var simulation = snapshot.GetSimulationUnit(original);
                var hpDelta = simulation.CurrentHp - original.CurrentHp;
                var mnDelta = simulation.CurrentMn - original.CurrentMn;
                if (hpDelta == 0 && mnDelta == 0)
                {
                    continue;
                }

                effects.Add(new SkillPreviewEffect(
                    original,
                    hpDelta,
                    mnDelta));
            }

            return new SkillPreview(
                user,
                skill,
                effects,
                SkillTimingCalculator.CreatePlan(
                    skill,
                    simulationUser));
        }
    }
}
