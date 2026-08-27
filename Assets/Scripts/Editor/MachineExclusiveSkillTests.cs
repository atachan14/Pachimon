using NUnit.Framework;
using Pachimon.Battle;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;
using UnityEngine;

namespace Pachimon.Editor.Tests
{
    public sealed class MachineExclusiveSkillTests
    {
        [Test]
        public void TriAttack_UsesThreeHighestAttributesInOneHit()
        {
            var skill = ScriptableObject.CreateInstance<TriAttackSkillAsset>();
            try
            {
                skill.ConfigureForEditor(1000, 100, 100, 200, 20,
                    100, 100, string.Empty);
                var user = CreateUnit(
                    "user", BattleSide.Player, 0, 5000,
                    stats: new[]
                    {
                        (PachimonStatType.Fire, 300),
                        (PachimonStatType.Aqua, 200),
                        (PachimonStatType.Leaf, 100),
                        (PachimonStatType.Electric, 50),
                    });
                var target = CreateUnit("target", BattleSide.Enemy, 0, 5000);
                var state = CreateState(user, target);

                var result = new TriAttackSkillLogic(skill).Resolve(
                    new SkillExecutionContext(state, user, skill));

                Assert.That(result.Effects, Has.Count.EqualTo(1));
                Assert.That(result.Effects[0].Damage, Is.EqualTo(900));
                Assert.That(target.CurrentHp, Is.EqualTo(4100));
            }
            finally
            {
                Object.DestroyImmediate(skill);
            }
        }

        [Test]
        public void BodySlam_UsesTheUsersCurrentHp()
        {
            var skill = ScriptableObject.CreateInstance<BodySlamSkillAsset>();
            try
            {
                skill.ConfigureForEditor(1001, 100, 100, 200, 20,
                    10, string.Empty);
                var user = CreateUnit("user", BattleSide.Player, 0, 800);
                var target = CreateUnit("target", BattleSide.Enemy, 0, 5000);
                var state = CreateState(user, target);

                var result = new BodySlamSkillLogic(skill).Resolve(
                    new SkillExecutionContext(state, user, skill));

                Assert.That(result.Effects[0].Damage, Is.EqualTo(80));
                Assert.That(target.CurrentHp, Is.EqualTo(4920));
            }
            finally
            {
                Object.DestroyImmediate(skill);
            }
        }

        [Test]
        public void FakeOut_CanBeUsedOncePerBattle()
        {
            var stun = ScriptableObject.CreateInstance<StunStatusAsset>();
            var skill = ScriptableObject.CreateInstance<FakeOutSkillAsset>();
            try
            {
                stun.ConfigureForEditor("Stun", string.Empty);
                skill.ConfigureForEditor(1002, 100, 100, stun, string.Empty);
                var user = CreateUnit(
                    "user", BattleSide.Player, 0, 5000,
                    skillSlots: new[] { new PachimonSkillSlot(1, 1002) });
                var target = CreateUnit("target", BattleSide.Enemy, 0, 5000);
                var state = CreateState(user, target);
                var logic = new FakeOutSkillLogic(skill);

                logic.Resolve(new SkillExecutionContext(
                    state, user, skill, skillSlotId: 1));

                Assert.That(
                    () => logic.Resolve(new SkillExecutionContext(
                        state, user, skill, skillSlotId: 1)),
                    Throws.InvalidOperationException);
            }
            finally
            {
                Object.DestroyImmediate(skill);
                Object.DestroyImmediate(stun);
            }
        }

        [Test]
        public void DestructionBeam_UsesTheTargetsMaxHp()
        {
            var skill = ScriptableObject.CreateInstance<DestructionBeamSkillAsset>();
            try
            {
                skill.ConfigureForEditor(1006, 100, 500, 1000, 100,
                    50, string.Empty);
                var user = CreateUnit("user", BattleSide.Player, 0, 5000);
                var target = CreateUnit(
                    "target",
                    BattleSide.Enemy,
                    0,
                    2000,
                    stats: new[] { (PachimonStatType.MaxHp, 2000) });
                target.ApplyDamage(500);
                var state = CreateState(user, target);

                var result = new DestructionBeamSkillLogic(skill).Resolve(
                    new SkillExecutionContext(state, user, skill));

                Assert.That(result.Effects[0].Damage, Is.EqualTo(1000));
                Assert.That(target.CurrentHp, Is.EqualTo(500));
            }
            finally
            {
                Object.DestroyImmediate(skill);
            }
        }

        [Test]
        public void Intangible_ClampsPositiveDamageToOne()
        {
            var definition = ScriptableObject
                .CreateInstance<IntangibleStatusAsset>();
            try
            {
                definition.ConfigureForEditor("Intangible", string.Empty);
                var source = CreateUnit("source", BattleSide.Player, 0, 5000);
                var target = CreateUnit("target", BattleSide.Enemy, 0, 5000);
                var state = CreateState(source, target);
                target.AddStatusStacks(
                    BattleStatusId.Intangible,
                    BattleStatusCategory.None,
                    target,
                    value: 0,
                    stackCount: 1,
                    definition: definition);

                BattleAttributeDamageService.Apply(
                    state,
                    source,
                    target,
                    new DamageContext(
                        DamageOriginKind.Skill,
                        1000,
                        500m,
                        source.GetBattleStats(),
                        target.GetBattleStats(),
                        PachimonAttribute.Fire,
                        isAttack: true,
                        applyAttackerAttributeMultiplier: false));
                BattleTrueDamageService.Apply(
                    state,
                    source,
                    target,
                    new TrueDamageContext(
                        DamageOriginKind.Skill,
                        1001,
                        500,
                        isAttack: true));
                BattleStatusDamageService.Apply(
                    state,
                    target,
                    BattleStatusId.Toxin,
                    PachimonAttribute.Poison,
                    500);

                Assert.That(target.CurrentHp, Is.EqualTo(4997));
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void ElectricDamageCount_IsCopiedToPreviewSimulation()
        {
            var user = CreateUnit("user", BattleSide.Player, 0, 5000);
            var target = CreateUnit("target", BattleSide.Enemy, 0, 5000);
            var state = CreateState(user, target);

            BattleAttributeDamageService.Apply(
                state,
                user,
                target,
                new DamageContext(
                    DamageOriginKind.Skill,
                    1,
                    100m,
                    user.GetBattleStats(),
                    target.GetBattleStats(),
                    PachimonAttribute.Electric,
                    isAttack: true,
                    applyAttackerAttributeMultiplier: false));

            Assert.That(state.ElectricDamageCount, Is.EqualTo(1));
            Assert.That(
                BattleSimulationSnapshot.Create(state).State.ElectricDamageCount,
                Is.EqualTo(1));
        }

        [Test]
        public void WindGod_ZeroesAllAttributesAndResistBonus()
        {
            var definition = ScriptableObject.CreateInstance<WindGodStatusAsset>();
            try
            {
                definition.ConfigureForEditor("Wind God", string.Empty);
                var user = CreateUnit("user", BattleSide.Player, 0, 5000,
                    new[]
                    {
                        (PachimonStatType.Fire, 100),
                        (PachimonStatType.Wind, 200),
                        (PachimonStatType.ResistBonus, 50),
                    });
                var target = CreateUnit("target", BattleSide.Enemy, 0, 5000);
                var state = CreateState(user, target);
                state.Statuses.ApplyStatus(user, new BattleStatusInstance(
                    BattleStatusId.WindGod,
                    BattleStatusCategory.None,
                    user,
                    value: 0,
                    durationTicks: 300,
                    definition: definition));

                Assert.That(user.GetBattleStatValue(PachimonStatType.Fire),
                    Is.Zero);
                Assert.That(user.GetBattleStatValue(PachimonStatType.Wind),
                    Is.Zero);
                Assert.That(user.GetBattleStatValue(
                    PachimonStatType.ResistBonus), Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void DragonInstall_StacksAsIndependentMultipliers()
        {
            var definition = ScriptableObject
                .CreateInstance<DragonInstallStatusAsset>();
            try
            {
                definition.ConfigureForEditor("Dragon Install", string.Empty);
                var user = CreateUnit("user", BattleSide.Player, 0, 5000,
                    new[]
                    {
                        (PachimonStatType.Dragon, 100),
                        (PachimonStatType.Speed, 100),
                        (PachimonStatType.Haste, 100),
                    });
                var target = CreateUnit("target", BattleSide.Enemy, 0, 5000);
                var state = CreateState(user, target);
                for (var index = 0; index < 2; index++)
                {
                    state.Statuses.ApplyStatus(user, new BattleStatusInstance(
                        BattleStatusId.DragonInstall,
                        BattleStatusCategory.None,
                        user,
                        value: 200,
                        durationTicks: 400,
                        definition: definition));
                }

                Assert.That(user.GetStatuses(BattleStatusId.DragonInstall),
                    Has.Count.EqualTo(2));
                Assert.That(user.GetBattleStatValue(PachimonStatType.Dragon),
                    Is.EqualTo(400));
                Assert.That(user.GetBattleStatValue(PachimonStatType.Speed),
                    Is.EqualTo(400));
                Assert.That(user.GetBattleStatValue(PachimonStatType.Haste),
                    Is.EqualTo(400));
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }

        private static BattleState CreateState(
            BattleUnitState player,
            BattleUnitState enemy)
        {
            return new BattleState(
                123,
                new BattleSideState(BattleSide.Player, new[] { player }),
                new BattleSideState(BattleSide.Enemy, new[] { enemy }));
        }

        private static BattleUnitState CreateUnit(
            string id,
            BattleSide side,
            int slot,
            int currentHp,
            (PachimonStatType stat, int value)[] stats = null,
            PachimonSkillSlot[] skillSlots = null)
        {
            var values = new int[(int)PachimonStatType.Count];
            values[(int)PachimonStatType.MaxHp] = 5000;
            values[(int)PachimonStatType.MaxMn] = 1000;
            foreach (var (stat, value) in stats
                         ?? System.Array.Empty<(PachimonStatType, int)>())
            {
                values[(int)stat] = value;
            }

            var baseStats = new PachimonStats(
                values,
                resourceDisplayMultiplier: 1,
                specialStatDivisor: 1);
            return new BattleUnitState(
                id,
                slot + 1,
                id,
                side,
                slot,
                new EffectivePachimonStats(baseStats, null),
                currentHp,
                1000,
                skillSlots ?? new[] { new PachimonSkillSlot(1, 1000) },
                new[] { 1 });
        }

    }
}
