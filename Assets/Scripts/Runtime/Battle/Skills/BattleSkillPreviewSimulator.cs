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
            bool spendMana,
            int skillSlotId = 0,
            int upgradeLevel = 0,
            int resolutionCount = 1)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (skill == null) throw new ArgumentNullException(nameof(skill));
            if (logic == null) throw new ArgumentNullException(nameof(logic));

            var snapshot = BattleSimulationSnapshot.Create(state);
            var simulationUser = snapshot.GetSimulationUnit(user);
            simulationUser.TryConsumeStatus(BattleStatusId.Clone, out _);
            var manaPlan = spendMana
                ? BattleSkillManaCostCalculator.CreatePlan(
                    snapshot.State,
                    simulationUser,
                    skill,
                    upgradeLevel)
                : new BattleSkillManaSpendPlan(0, 0m);
            var manaSpent = manaPlan.Actual;
            if (spendMana
                && (manaSpent <= 0 && skill.ConsumesAllCurrentMana
                    || !simulationUser.TrySpendMn(manaSpent)))
            {
                throw new InvalidOperationException(
                    $"Unit '{user.InstanceId}' could not spend "
                    + $"{manaSpent} MN in Preview.");
            }

            var wasTargetUnavailable = true;
            for (var index = 0; index < resolutionCount; index++)
            {
                var resolution = BattleSkillResolver.Resolve(
                    snapshot.State,
                    simulationUser,
                    skill,
                    logic,
                    actualManaSpent: index == 0 ? manaSpent : 0,
                    effectiveManaSpent: manaPlan.Effective,
                    skillSlotId: skillSlotId);
                wasTargetUnavailable &= resolution.WasTargetUnavailable;
            }

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
                    simulationUser,
                    snapshot.State,
                    upgradeLevel),
                wasTargetUnavailable);
        }
    }
}
