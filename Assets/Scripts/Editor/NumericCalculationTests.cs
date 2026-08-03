using NUnit.Framework;
using Pachimon.Battle;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Passives;
using Pachimon.Skills;
using Pachimon.Items;
using Pachimon.Data;
using System.Linq;
using UnityEngine;

namespace Pachimon.Editor.Tests
{
    public sealed class NumericCalculationTests
    {
        private const int HydroelectricPowerPassiveId = 12;
        private const int ThermalPowerPassiveId = 20;
        private const int WindPowerPassiveId = 28;
        private static PassiveCatalog _passiveCatalog;
        private static PassiveStatModifierRegistry _passiveRegistry;

        [TestCase(100, 2.0)]
        [TestCase(0, 1.0)]
        [TestCase(-100, 0.5)]
        public void AmplificationMultiplier_SupportsSignedStats(
            int stat,
            double expected)
        {
            Assert.That(
                (double)SignedStatMath.AmplificationMultiplier(stat),
                Is.EqualTo(expected).Within(0.000001));
        }

        [TestCase(50, 100, 100.0)]
        [TestCase(50, 0, 50.0)]
        [TestCase(50, -100, 25.0)]
        public void ScaleFromBase_PreservesBaseAndSupportsSignedStats(
            int baseValue,
            int stat,
            double expected)
        {
            Assert.That(
                (double)SignedStatMath.ScaleFromBase(baseValue, stat),
                Is.EqualTo(expected).Within(0.000001));
        }

        [TestCase(100, 0.5)]
        [TestCase(0, 1.0)]
        [TestCase(-100, 2.0)]
        public void ReductionMultiplier_SupportsSignedStats(
            int stat,
            double expected)
        {
            Assert.That(
                (double)SignedStatMath.ReductionMultiplier(stat),
                Is.EqualTo(expected).Within(0.000001));
        }

        [Test]
        public void EffectiveStats_AllowNegativeCombatStats_ButClampResources()
        {
            var baseStats = CreateStats(
                (PachimonStatType.MaxHp, 10),
                (PachimonStatType.Speed, 10),
                (PachimonStatType.DamageBonus, 5));
            var modifiers = new TrainerModifierSet();
            modifiers.AddStat(PachimonStatType.MaxHp, -20);
            modifiers.AddStat(PachimonStatType.Speed, -30);
            modifiers.AddStat(PachimonStatType.DamageBonus, -15);

            var result = new EffectivePachimonStats(baseStats, modifiers);

            Assert.That(result.MaxHp, Is.Zero);
            Assert.That(result.GetValue(PachimonStatType.Speed), Is.EqualTo(-20));
            Assert.That(result.DamageBonus, Is.EqualTo(-10));
        }

        [Test]
        public void EffectiveStats_ApplyBadgeToNegativeAttribute()
        {
            var baseStats = CreateStats((PachimonStatType.Fire, -10));
            var modifiers = new TrainerModifierSet();
            modifiers.AddBadge(PachimonAttribute.Fire);

            var result = new EffectivePachimonStats(baseStats, modifiers);

            Assert.That(result.GetValue(PachimonStatType.Fire), Is.EqualTo(-13));
        }

        [TestCase(100, 100, 50)]
        [TestCase(100, 50, 67)]
        [TestCase(100, 0, 100)]
        [TestCase(100, -50, 150)]
        [TestCase(100, -100, 200)]
        public void Recovery_UsesSignedSpeedAndCeilsOnce(
            int baseTicks,
            int speed,
            int expected)
        {
            Assert.That(
                BattleTickMath.GetEffectiveRecovery(baseTicks, speed),
                Is.EqualTo(expected));
        }

        [Test]
        public void ZeroStartupAndCooldown_RemainZeroWithNegativeStats()
        {
            Assert.That(BattleTickMath.GetEffectiveStartup(0, -100), Is.Zero);
            Assert.That(BattleTickMath.GetEffectiveCooldown(0, -100), Is.Zero);
        }

        [Test]
        public void Timeline_PausesActionClockAndCooldownTogether()
        {
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player),
                CreateTestSide(BattleSide.Enemy));
            Assert.That(state.Timeline.TryBeginNextTurn(out var actor), Is.True);
            state.Timeline.CompleteImmediateAction(
                actor,
                usedSkillSlotId: 1,
                new BattleSkillTimingPlan(
                    startupTicks: 0,
                    recoveryTicks: 100,
                    cooldownTicks: 100));

            actor.SetActionClockPaused(true);
            state.Timeline.AdvanceToTick(state.CurrentTick + 50);

            Assert.That(actor.Timing.RemainingTicks, Is.EqualTo(100));
            Assert.That(actor.GetCooldownRemainingTicks(1), Is.EqualTo(100));

            actor.SetActionClockPaused(false);
            state.Timeline.AdvanceToTick(state.CurrentTick + 50);

            Assert.That(actor.Timing.RemainingTicks, Is.EqualTo(50));
            Assert.That(actor.GetCooldownRemainingTicks(1), Is.EqualTo(50));
        }

        [Test]
        public void Timeline_MarksOnlyTheSelectedSameTickUnitAsReady()
        {
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player),
                CreateTestSide(BattleSide.Enemy));

            Assert.That(state.Timeline.TryBeginNextTurn(out var actor), Is.True);

            Assert.That(actor.Timing.Phase, Is.EqualTo(BattleActionPhase.Ready));
            var waitingUnits = state.Player.Units
                .Concat(state.Enemy.Units)
                .Where(unit => !ReferenceEquals(unit, actor))
                .ToArray();
            Assert.That(
                waitingUnits.All(unit => unit.Timing.RemainingTicks == 0),
                Is.True);
            Assert.That(
                waitingUnits.All(unit =>
                    unit.Timing.Phase == BattleActionPhase.InitialDelay),
                Is.True);
        }

        [Test]
        public void TimedStuns_PauseUntilEveryIndependentStatusExpires()
        {
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player),
                CreateTestSide(BattleSide.Enemy));
            Assert.That(state.Timeline.TryBeginNextTurn(out var actor), Is.True);
            state.Timeline.CompleteImmediateAction(
                actor,
                usedSkillSlotId: 1,
                new BattleSkillTimingPlan(0, 100, 100));
            state.Statuses.ApplyStatus(
                actor,
                new BattleStatusInstance(
                    BattleStatusId.Stun,
                    BattleStatusCategory.Stun,
                    actor,
                    value: 0,
                    durationTicks: 30));
            state.Statuses.ApplyStatus(
                actor,
                new BattleStatusInstance(
                    BattleStatusId.Freeze,
                    BattleStatusCategory.Stun,
                    actor,
                    value: 0,
                    durationTicks: 60));

            state.Timeline.AdvanceToTick(state.CurrentTick + 30);

            Assert.That(actor.GetStatus(BattleStatusId.Stun), Is.Null);
            Assert.That(actor.GetStatus(BattleStatusId.Freeze), Is.Not.Null);
            Assert.That(actor.Timing.IsPaused, Is.True);
            Assert.That(actor.Timing.RemainingTicks, Is.EqualTo(100));
            Assert.That(actor.GetCooldownRemainingTicks(1), Is.EqualTo(100));

            state.Timeline.AdvanceToTick(state.CurrentTick + 30);

            Assert.That(actor.GetStatus(BattleStatusId.Freeze), Is.Null);
            Assert.That(actor.Timing.IsPaused, Is.False);
            Assert.That(actor.Timing.RemainingTicks, Is.EqualTo(100));

            state.Timeline.AdvanceToTick(state.CurrentTick + 50);

            Assert.That(actor.Timing.RemainingTicks, Is.EqualTo(50));
            Assert.That(actor.GetCooldownRemainingTicks(1), Is.EqualTo(50));
        }

        [Test]
        public void Timeline_RecoversWhenEveryUnitStartsStunned()
        {
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player),
                CreateTestSide(BattleSide.Enemy));
            foreach (var unit in state.Player.Units.Concat(state.Enemy.Units))
            {
                state.Statuses.ApplyStatus(
                    unit,
                    new BattleStatusInstance(
                        BattleStatusId.Stun,
                        BattleStatusCategory.Stun,
                        unit,
                        value: 0,
                        durationTicks: 50));
            }

            Assert.That(state.Timeline.TryBeginNextTurn(out var actor), Is.True);

            Assert.That(actor, Is.Not.Null);
            Assert.That(state.CurrentTick, Is.EqualTo(150));
            Assert.That(
                state.Player.Units
                    .Concat(state.Enemy.Units)
                    .All(unit => !unit.Timing.IsPaused),
                Is.True);
        }

        [Test]
        public void Slow_AffectsTheRunningPhaseAndDecaysAfterEachTick()
        {
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player),
                CreateTestSide(BattleSide.Enemy));
            var unit = state.Player.GetUnitAt(0);
            var originalInitialDelay = unit.Timing.RemainingTicks;
            state.Statuses.ApplyStatus(
                unit,
                new BattleStatusInstance(
                    BattleStatusId.Slow,
                    BattleStatusCategory.Slow,
                    unit,
                    value: 50));

            var timing = SkillTimingCalculator.CreatePlan(
                CreateBasicElectricSkill(),
                unit);

            Assert.That(unit.Timing.RemainingTicks, Is.EqualTo(originalInitialDelay));
            Assert.That(
                unit.GetBattleStatValue(PachimonStatType.Speed),
                Is.EqualTo(-50));
            Assert.That(unit.GetActionRemainingTicks(), Is.GreaterThan(100));
            Assert.That(timing.RecoveryTicks, Is.EqualTo(100));

            state.Timeline.AdvanceToTick(state.CurrentTick + 1);

            Assert.That(
                unit.GetStatus(BattleStatusId.Slow).Value,
                Is.EqualTo(49));
            Assert.That(
                unit.Timing.RemainingWork,
                Is.LessThan(100m));
        }

        [Test]
        public void Slow_WithTheSameIdAddsValueAndExpiresAtZero()
        {
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player),
                CreateTestSide(BattleSide.Enemy));
            var unit = state.Player.GetUnitAt(0);
            state.Statuses.ApplyStatus(
                unit,
                new BattleStatusInstance(
                    BattleStatusId.Paralysis,
                    BattleStatusCategory.Slow,
                    unit,
                    value: 10));
            state.Statuses.ApplyStatus(
                unit,
                new BattleStatusInstance(
                    BattleStatusId.Paralysis,
                    BattleStatusCategory.Slow,
                    unit,
                    value: 20));

            Assert.That(
                unit.GetStatusCategoryValue(BattleStatusCategory.Slow),
                Is.EqualTo(30));
            Assert.That(
                unit.Statuses.Count(status =>
                    status.StatusId == BattleStatusId.Paralysis),
                Is.EqualTo(1));

            state.Timeline.AdvanceToTick(state.CurrentTick + 10);

            Assert.That(
                unit.GetStatusCategoryValue(BattleStatusCategory.Slow),
                Is.EqualTo(20));
            state.Timeline.AdvanceToTick(state.CurrentTick + 20);
            Assert.That(
                unit.GetStatus(BattleStatusId.Paralysis),
                Is.Null);
        }

        [Test]
        public void ParalysisAndChill_UseAttributeDefenseBeforeAddingValue()
        {
            var target = CreateBattleUnitWithStats(
                "player_1",
                BattleSide.Player,
                0,
                2000,
                1,
                (PachimonStatType.Electric, 100),
                (PachimonStatType.Ice, 100));
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, target),
                CreateTestSide(BattleSide.Enemy));
            var source = state.Enemy.GetUnitAt(0);

            state.Statuses.ApplyStatus(
                target,
                new BattleStatusInstance(
                    BattleStatusId.Paralysis,
                    BattleStatusCategory.Slow,
                    source,
                    value: 150));
            state.Statuses.ApplyStatus(
                target,
                new BattleStatusInstance(
                    BattleStatusId.Paralysis,
                    BattleStatusCategory.Slow,
                    source,
                    value: 150));
            state.Statuses.ApplyStatus(
                target,
                new BattleStatusInstance(
                    BattleStatusId.Chill,
                    BattleStatusCategory.Slow,
                    source,
                    value: 150));

            Assert.That(
                target.GetStatus(BattleStatusId.Paralysis).Value,
                Is.EqualTo(150));
            Assert.That(
                target.GetStatus(BattleStatusId.Chill).Value,
                Is.EqualTo(75));
            Assert.That(
                target.GetStatusCategoryValue(BattleStatusCategory.Slow),
                Is.EqualTo(225));
        }

        [Test]
        public void ElectricShock_AppliesAttributeBasedParalysis()
        {
            var skill = CreateBasicElectricSkill();
            var source = CreateBattleUnitWithStats(
                "player_1",
                BattleSide.Player,
                0,
                2000,
                skill.SkillId,
                (PachimonStatType.Electric, 100),
                (PachimonStatType.Ice, 100));
            var enemies = CreateTestSide(BattleSide.Enemy);
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, source),
                enemies);

            BattleSkillResolver.Resolve(
                state,
                source,
                skill,
                new ElectricShockSkillLogic());

            var target = enemies.GetUnitAt(0);
            Assert.That(
                target.GetStatus(BattleStatusId.Paralysis).Value,
                Is.EqualTo(150));
        }

        [Test]
        public void AttributeBasedStatusValue_AllowsZeroForVeryNegativeStats()
        {
            var source = CreateBattleUnitWithStats(
                "player_1",
                BattleSide.Player,
                0,
                2000,
                CreateBasicElectricSkill().SkillId,
                (PachimonStatType.Electric, -10000),
                (PachimonStatType.Ice, -10000));

            Assert.That(ElectricShockMath.CalculateSlowValue(source), Is.Zero);
            Assert.That(ColdHandMath.CalculateChillValue(source), Is.Zero);
        }

        [Test]
        public void ZeroValueSlowAndLeak_AreNotStored()
        {
            var source = CreateTestSide(BattleSide.Player).GetUnitAt(0);
            var target = CreateBattleUnitWithStats(
                "enemy_1",
                BattleSide.Enemy,
                0,
                2000,
                CreateBasicElectricSkill().SkillId,
                (PachimonStatType.Electric, 10000));
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, source),
                CreateTestSide(BattleSide.Enemy, target));

            state.Statuses.ApplyStatus(
                target,
                new BattleStatusInstance(
                    BattleStatusId.Paralysis,
                    BattleStatusCategory.Slow,
                    source,
                    value: 1));
            state.Statuses.ApplyStatus(
                target,
                new BattleStatusInstance(
                    BattleStatusId.Leak,
                    BattleStatusCategory.Leak,
                    source,
                    value: 0));

            Assert.That(target.GetStatus(BattleStatusId.Paralysis), Is.Null);
            Assert.That(target.GetStatus(BattleStatusId.Leak), Is.Null);
        }

        [Test]
        public void ParalysisAndChillRemainSeparateAndCombineAsSlow()
        {
            var electricSkill = CreateBasicElectricSkill();
            var iceSkill = CreateBasicIceSkill();
            var source = CreateBattleUnitWithStats(
                "player_1",
                BattleSide.Player,
                0,
                2000,
                electricSkill.SkillId,
                (PachimonStatType.Electric, 100),
                (PachimonStatType.Ice, 100));
            var enemies = CreateTestSide(BattleSide.Enemy);
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, source),
                enemies);
            var target = enemies.GetUnitAt(0);

            BattleSkillResolver.Resolve(
                state,
                source,
                electricSkill,
                new ElectricShockSkillLogic());
            BattleSkillResolver.Resolve(
                state,
                source,
                iceSkill,
                new ColdHandSkillLogic());

            Assert.That(
                target.GetStatus(BattleStatusId.Paralysis).Value,
                Is.EqualTo(150));
            Assert.That(
                target.GetStatus(BattleStatusId.Chill).Value,
                Is.EqualTo(150));
            Assert.That(
                target.GetStatusCategoryValue(BattleStatusCategory.Slow),
                Is.EqualTo(300));
        }

        [Test]
        public void Charge_StacksIndependentlyAndTransitionsWithSnapshotValue()
        {
            var skill = CreateChargeSkill();
            var user = CreateBattleUnitWithStats(
                "player_1",
                BattleSide.Player,
                0,
                2000,
                skill.SkillId,
                (PachimonStatType.Electric, 100),
                (PachimonStatType.Speed, 20),
                (PachimonStatType.ResistBonus, 10));
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, user),
                CreateTestSide(BattleSide.Enemy));
            var logic = new ChargeSkillLogic(skill);

            logic.Resolve(new SkillExecutionContext(state, user, skill));
            logic.Resolve(new SkillExecutionContext(state, user, skill));

            Assert.That(
                user.Statuses
                    .Where(status => status.StatusId == BattleStatusId.Charging)
                    .Select(status => status.Value),
                Is.EquivalentTo(new[] { 100, 50 }));
            Assert.That(
                user.GetBattleStatValue(PachimonStatType.Electric),
                Is.EqualTo(25));
            Assert.That(
                user.GetBattleStatValue(PachimonStatType.ResistBonus),
                Is.EqualTo(70));

            state.Timeline.AdvanceToTick(state.CurrentTick + 200);

            Assert.That(
                user.Statuses.Single(status =>
                    status.StatusId == BattleStatusId.Charging).Value,
                Is.EqualTo(100));
            Assert.That(
                user.Statuses.Single(status =>
                    status.StatusId == BattleStatusId.Charged).Value,
                Is.EqualTo(50));
            Assert.That(
                user.GetBattleStatValue(PachimonStatType.Electric),
                Is.EqualTo(75));
            Assert.That(
                user.GetBattleStatValue(PachimonStatType.ResistBonus),
                Is.EqualTo(50));
            Assert.That(
                user.GetBattleStatValue(PachimonStatType.Speed),
                Is.EqualTo(70));

            state.Timeline.AdvanceToTick(state.CurrentTick + 200);

            Assert.That(
                user.Statuses.Single().StatusId,
                Is.EqualTo(BattleStatusId.Charged));
            Assert.That(user.Statuses.Single().Value, Is.EqualTo(100));
            Assert.That(
                user.GetBattleStatValue(PachimonStatType.Electric),
                Is.EqualTo(150));
            Assert.That(
                user.GetBattleStatValue(PachimonStatType.ResistBonus),
                Is.EqualTo(10));
            Assert.That(
                user.GetBattleStatValue(PachimonStatType.Speed),
                Is.EqualTo(120));
        }

        [Test]
        public void Defeat_RemovesEveryStatusIncludingCharge()
        {
            var skill = CreateChargeSkill();
            var user = CreateBattleUnitWithStats(
                "player_1",
                BattleSide.Player,
                0,
                2000,
                skill.SkillId,
                (PachimonStatType.Electric, 100));
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, user),
                CreateTestSide(BattleSide.Enemy));
            new ChargeSkillLogic(skill)
                .Resolve(new SkillExecutionContext(state, user, skill));

            user.ApplyDamage(user.CurrentHp);

            Assert.That(user.Statuses, Is.Empty);
        }

        [Test]
        public void StaticElectricity_AddsParalysisForEachAttackHit()
        {
            var definition = CreateStaticElectricityPassive();
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            catalog.SetPassivesForEditor(new PassiveAsset[] { definition });
            var defender = CreateBattleUnitWithPassive(
                "enemy_1",
                BattleSide.Enemy,
                0,
                definition.PassiveId,
                (PachimonStatType.Electric, 100),
                (PachimonStatType.Ice, 100));
            var players = CreateTestSide(BattleSide.Player);
            var state = new BattleState(
                123,
                players,
                CreateTestSide(BattleSide.Enemy, defender),
                new PassiveLogicRegistry(catalog));
            var attacker = players.GetUnitAt(0);

            ApplyTestElectricDamage(
                state,
                attacker,
                defender,
                DamageOriginKind.Skill,
                isAttack: true);
            ApplyTestElectricDamage(
                state,
                attacker,
                defender,
                DamageOriginKind.Skill,
                isAttack: true);

            Assert.That(
                attacker.GetStatus(BattleStatusId.Paralysis).Value,
                Is.EqualTo(120));

            ApplyTestElectricDamage(
                state,
                attacker,
                defender,
                DamageOriginKind.Status,
                isAttack: false);

            Assert.That(
                attacker.GetStatus(BattleStatusId.Paralysis).Value,
                Is.EqualTo(120));
        }

        [Test]
        public void StaticElectricity_ReactsToTrueAndSelfTargetedAttacks()
        {
            var definition = CreateStaticElectricityPassive();
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            catalog.SetPassivesForEditor(new PassiveAsset[] { definition });
            var defender = CreateBattleUnitWithPassive(
                "enemy_1",
                BattleSide.Enemy,
                0,
                definition.PassiveId,
                (PachimonStatType.Electric, 100),
                (PachimonStatType.Ice, 100));
            var players = CreateTestSide(BattleSide.Player);
            var state = new BattleState(
                123,
                players,
                CreateTestSide(BattleSide.Enemy, defender),
                new PassiveLogicRegistry(catalog));
            var attacker = players.GetUnitAt(0);

            BattleTrueDamageService.Apply(
                state,
                attacker,
                defender,
                new TrueDamageContext(
                    DamageOriginKind.Skill,
                    originId: 1,
                    damage: 0,
                    isAttack: true));

            Assert.That(
                attacker.GetStatus(BattleStatusId.Paralysis).Value,
                Is.EqualTo(60));

            BattleTrueDamageService.Apply(
                state,
                defender,
                defender,
                new TrueDamageContext(
                    DamageOriginKind.Self,
                    originId: 1,
                    damage: 10,
                    isAttack: false));

            Assert.That(
                attacker.GetStatus(BattleStatusId.Paralysis).Value,
                Is.EqualTo(60));

            BattleTrueDamageService.Apply(
                state,
                defender,
                defender,
                new TrueDamageContext(
                    DamageOriginKind.Skill,
                    originId: 1,
                    damage: 0,
                    isAttack: true));

            Assert.That(
                defender.GetStatus(BattleStatusId.Paralysis).Value,
                Is.EqualTo(60));
        }

        [Test]
        public void Damage_PreservesFractionsUntilFinalization()
        {
            var attacker = CreateEffectiveStats(
                (PachimonStatType.Fire, 33),
                (PachimonStatType.DamageBonus, 17));
            var defender = CreateEffectiveStats(
                (PachimonStatType.Fire, 40),
                (PachimonStatType.ResistBonus, 20));

            var unrounded = AttributeDamageCalculator.CalculateUnrounded(
                100,
                attacker,
                defender,
                PachimonAttribute.Fire);

            Assert.That((double)unrounded, Is.EqualTo(92.625).Within(0.000001));
            Assert.That(
                AttributeDamageCalculator.FinalizeNormalDamage(unrounded),
                Is.EqualTo(92));
        }

        [Test]
        public void Damage_UsesSignedOffenseAndDefenseStats()
        {
            var negativeAttacker = CreateEffectiveStats(
                (PachimonStatType.Fire, -100));
            var neutralStats = CreateEffectiveStats();
            var negativeDefender = CreateEffectiveStats(
                (PachimonStatType.Fire, -100));

            Assert.That(
                AttributeDamageCalculator.Calculate(
                    100,
                    negativeAttacker,
                    neutralStats,
                    PachimonAttribute.Fire),
                Is.EqualTo(50));
            Assert.That(
                AttributeDamageCalculator.Calculate(
                    100,
                    neutralStats,
                    negativeDefender,
                    PachimonAttribute.Fire),
                Is.EqualTo(200));
        }

        [Test]
        public void PassiveMultiplier_PreservesFractionUntilDamageFinalization()
        {
            const decimal unroundedBaseDamage = 92.625m;
            var modifiedDamage = unroundedBaseDamage * 130m / 100m;

            Assert.That(
                AttributeDamageCalculator.FinalizeNormalDamage(modifiedDamage),
                Is.EqualTo(120));
        }

        [Test]
        public void DamageContext_AppliesPenetrationToBothDefenseStats()
        {
            var result = AttributeDamageCalculator.Calculate(
                new DamageContext(
                    DamageOriginKind.Skill,
                    originId: 20,
                    baseDamage: 100m,
                    CreateEffectiveStats(),
                    CreateEffectiveStats(
                        (PachimonStatType.Electric, 100),
                        (PachimonStatType.ResistBonus, 100)),
                    PachimonAttribute.Electric,
                    isAttack: true,
                    penetrationPercent: 20m));

            Assert.That(result.PreDefenseDamage, Is.EqualTo(100m));
            Assert.That(result.EffectiveDefenderAttribute, Is.EqualTo(80m));
            Assert.That(result.EffectiveResistBonus, Is.EqualTo(80m));
            Assert.That(result.FinalDamage, Is.EqualTo(30));
        }

        [Test]
        public void DamageContext_DoesNotClampPenetrationAtOneHundredPercent()
        {
            var result = AttributeDamageCalculator.Calculate(
                new DamageContext(
                    DamageOriginKind.Skill,
                    originId: 20,
                    baseDamage: 100m,
                    CreateEffectiveStats(),
                    CreateEffectiveStats(
                        (PachimonStatType.Electric, 100),
                        (PachimonStatType.ResistBonus, 100)),
                    PachimonAttribute.Electric,
                    isAttack: true,
                    penetrationPercent: 120m));

            Assert.That(result.EffectiveDefenderAttribute, Is.EqualTo(-20m));
            Assert.That(result.EffectiveResistBonus, Is.EqualTo(-20m));
            Assert.That(result.FinalDamage, Is.EqualTo(144));
        }

        [Test]
        public void Backfire_TargetsBackEnemyAndUsesFireAndPoisonScaling()
        {
            var skill = ScriptableObject.CreateInstance<BackfireSkillAsset>();
            skill.ConfigureForEditor(
                skillId: 9,
                displayName: "バックファイア",
                baseRecoveryTicks: 100,
                baseCooldownTicks: 200,
                baseManaCost: 100,
                description: string.Empty,
                basePower: 100,
                fireScalingPercent: 100,
                basePenetrationPercent: 10,
                poisonScalingPercent: 100);
            var user = CreateBattleUnitWithStats(
                "player_1",
                BattleSide.Player,
                0,
                2000,
                skill.SkillId,
                (PachimonStatType.Fire, 100),
                (PachimonStatType.Poison, 100));
            var enemies = CreateTestSide(BattleSide.Enemy);
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, user),
                enemies);

            var resolution = BattleSkillResolver.Resolve(
                state,
                user,
                skill,
                new BackfireSkillLogic(skill));

            Assert.That(
                BackfireMath.CalculateBaseDamage(skill, fire: 100),
                Is.EqualTo(200m));
            Assert.That(
                BackfireMath.CalculatePenetrationPercent(skill, poison: 100),
                Is.EqualTo(20m));
            Assert.That(resolution.Effects.Single().Target.SlotIndex, Is.EqualTo(2));
            Assert.That(enemies.GetUnitAt(0).CurrentHp, Is.EqualTo(2000));
            Assert.That(enemies.GetUnitAt(1).CurrentHp, Is.EqualTo(2000));
            Assert.That(enemies.GetUnitAt(2).CurrentHp, Is.EqualTo(1800));
        }

        [Test]
        public void FireArrow_ReactivatesOnDefeatAndSpendsManaPerReactivation()
        {
            var skill = ScriptableObject.CreateInstance<FireArrowSkillAsset>();
            skill.ConfigureForEditor(
                skillId: 33,
                displayName: "ファイアアロー",
                baseRecoveryTicks: 100,
                baseCooldownTicks: 250,
                baseManaCost: 100,
                description: string.Empty,
                basePower: 100,
                fireScalingPercent: 100);
            var user = CreateBattleUnitWithPassive(
                "player_1",
                BattleSide.Player,
                0,
                passiveId: 1,
                (PachimonStatType.DamageBonus, 100));
            Assert.That(user.TrySpendMn(750), Is.True);
            Assert.That(user.TrySpendMn(skill.BaseManaCost), Is.True);
            var enemies = new BattleSideState(
                BattleSide.Enemy,
                new[]
                {
                    CreateBattleUnitWithStats(
                        "enemy_1", BattleSide.Enemy, 0, 500, 1),
                    CreateBattleUnitWithStats(
                        "enemy_2", BattleSide.Enemy, 1, 50, 1),
                    CreateBattleUnitWithStats(
                        "enemy_3", BattleSide.Enemy, 2, 100, 1),
                });
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, user),
                enemies);

            var resolution = BattleSkillResolver.Resolve(
                state,
                user,
                skill,
                new FireArrowSkillLogic(skill));

            Assert.That(resolution.Effects.Count, Is.EqualTo(2));
            Assert.That(resolution.Effects[0].Target.SlotIndex, Is.EqualTo(1));
            Assert.That(resolution.Effects[1].Target.SlotIndex, Is.EqualTo(2));
            Assert.That(enemies.GetUnitAt(0).CurrentHp, Is.EqualTo(500));
            Assert.That(enemies.GetUnitAt(1).IsDefeated, Is.True);
            Assert.That(enemies.GetUnitAt(2).IsDefeated, Is.True);
            Assert.That(user.CurrentMn, Is.EqualTo(50));
        }

        [Test]
        public void Combustion_RepeatsEnemyAndSelfDamageUntilManaRunsOut()
        {
            var skill = ScriptableObject.CreateInstance<CombustionSkillAsset>();
            skill.ConfigureForEditor(
                skillId: 41,
                displayName: "燃焼",
                baseRecoveryTicks: 100,
                baseCooldownTicks: 300,
                baseManaCost: 100,
                description: string.Empty,
                basePower: 100,
                fireScalingPercent: 100);
            var user = CreateBattleUnitWithStats(
                "player_1",
                BattleSide.Player,
                0,
                2000,
                skill.SkillId);
            Assert.That(user.TrySpendMn(750), Is.True);
            Assert.That(user.TrySpendMn(skill.BaseManaCost), Is.True);
            var enemies = CreateTestSide(BattleSide.Enemy);
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, user),
                enemies);

            var resolution = BattleSkillResolver.Resolve(
                state,
                user,
                skill,
                new CombustionSkillLogic(skill));

            Assert.That(resolution.Effects.Count, Is.EqualTo(4));
            Assert.That(enemies.GetUnitAt(0).CurrentHp, Is.EqualTo(1480));
            Assert.That(enemies.GetUnitAt(1).CurrentHp, Is.EqualTo(2000));
            Assert.That(user.CurrentHp, Is.EqualTo(1480));
            Assert.That(user.CurrentMn, Is.EqualTo(50));

            var selfDamage = new CombustionSkillLogic(skill)
                .CalculateSelfDamage(user);
            Assert.That(selfDamage.Context.OriginKind, Is.EqualTo(DamageOriginKind.Skill));
            Assert.That(selfDamage.Context.IsAttack, Is.True);
            Assert.That(selfDamage.Context.ApplyDamageBonusMultiplier, Is.True);
            Assert.That(selfDamage.Context.ApplyOutgoingModifiers, Is.True);
        }

        [Test]
        public void Combustion_PresentationKeepsDamageOrderAndRepeatMana()
        {
            var skill = ScriptableObject.CreateInstance<CombustionSkillAsset>();
            skill.ConfigureForEditor(
                skillId: 41,
                displayName: "燃焼",
                baseRecoveryTicks: 100,
                baseCooldownTicks: 300,
                baseManaCost: 100,
                description: string.Empty,
                basePower: 100,
                fireScalingPercent: 100);
            var user = CreateBattleUnitWithPassive(
                "player_1",
                BattleSide.Player,
                0,
                passiveId: 2);
            Assert.That(user.TrySpendMn(750), Is.True);
            var enemies = CreateTestSide(BattleSide.Enemy);
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, user),
                enemies);

            state.Presentation.Begin(user, skill);
            var initialMn = user.CurrentMn;
            Assert.That(user.TrySpendMn(skill.BaseManaCost), Is.True);
            state.Presentation.RecordInitialManaSpent(
                user,
                initialMn,
                user.CurrentMn);
            BattleSkillResolver.Resolve(
                state,
                user,
                skill,
                new CombustionSkillLogic(skill));
            var presentation = state.Presentation.Complete();
            var damageSteps = presentation.Steps
                .Where(step =>
                    step.Kind == BattlePresentationStepKind.DamageApplied)
                .ToArray();

            Assert.That(presentation.InitialManaTransition.MnBefore, Is.EqualTo(250));
            Assert.That(presentation.InitialManaTransition.MnAfter, Is.EqualTo(150));
            Assert.That(damageSteps.Length, Is.EqualTo(4));
            Assert.That(damageSteps[0].FocusUnit, Is.SameAs(enemies.GetUnitAt(0)));
            Assert.That(damageSteps[1].FocusUnit, Is.SameAs(user));
            Assert.That(damageSteps[2].FocusUnit, Is.SameAs(enemies.GetUnitAt(0)));
            Assert.That(damageSteps[3].FocusUnit, Is.SameAs(user));
            Assert.That(damageSteps.Select(step => step.BlockIndex), Is.EqualTo(
                new[] { 0, 0, 1, 1 }));

            var repeatMana = damageSteps[2].Transitions.Single(
                transition => ReferenceEquals(transition.Unit, user));
            Assert.That(repeatMana.MnBefore, Is.EqualTo(150));
            Assert.That(repeatMana.MnAfter, Is.EqualTo(50));
        }

        [Test]
        public void ElectricExplosion_UsesItsScriptableObjectTuningValues()
        {
            var skill =
                ScriptableObject.CreateInstance<ElectricExplosionSkillAsset>();
            skill.ConfigureForEditor(
                skillId: 20,
                displayName: "電気爆発",
                baseRecoveryTicks: 100,
                baseCooldownTicks: 250,
                baseManaCost: 130,
                description: string.Empty,
                basePower: 50,
                electricScalingPercent: 100,
                fireScalingPercent: 100,
                penetrationPercentAtFire100: 20);

            Assert.That(
                ElectricExplosionMath.CalculateBaseDamage(
                    skill,
                    electric: 100,
                    fire: 100),
                Is.EqualTo(200m));
            Assert.That(
                ElectricExplosionMath.CalculatePenetrationPercent(
                    skill,
                    fire: 100),
                Is.EqualTo(20m));
            Assert.That(skill.BaseManaCost, Is.EqualTo(130));
        }

        [Test]
        public void ElectricQuickAttack_UsesCompositeDamageAndWindTiming()
        {
            var skill =
                ScriptableObject.CreateInstance<ElectricQuickAttackSkillAsset>();
            skill.ConfigureForEditor(
                skillId: 28,
                displayName: "Electric Quick Attack",
                baseRecoveryTicks: 60,
                baseCooldownTicks: 100,
                baseManaCost: 60,
                description: string.Empty,
                electricBasePower: 25,
                fireBasePower: 10,
                windTimingPercent: 100);

            Assert.That(
                ElectricQuickAttackMath.CalculateElectricBaseDamage(
                    skill,
                    electric: 100),
                Is.EqualTo(50m));
            Assert.That(
                ElectricQuickAttackMath.CalculateFireBaseDamage(
                    skill,
                    fire: 100),
                Is.EqualTo(20m));

            var windMultiplier =
                SkillTimingCalculator.CalculateWindMultiplier(skill, wind: 100);
            Assert.That(windMultiplier, Is.EqualTo(0.5m));
            Assert.That(
                BattleTickMath.GetEffectiveRecovery(
                    skill.BaseRecoveryTicks,
                    speed: 0,
                    skillMultiplier: windMultiplier),
                Is.EqualTo(30));
            Assert.That(
                BattleTickMath.GetEffectiveCooldown(
                    skill.BaseCooldownTicks,
                    haste: 0,
                    skillMultiplier: windMultiplier),
                Is.EqualTo(50));
            Assert.That(skill.BaseManaCost, Is.EqualTo(60));
        }

        [Test]
        public void SkillMachine_AllowsDuplicateSkillsAndConsumesEachItem()
        {
            var (skill, machine, catalog) = CreateSkillMachine();
            var target = new PachimonInstance(
                "test_target",
                1,
                AllocationType.Electric,
                1,
                1,
                CreateStats());
            var inventory = new ItemInventory();
            var service = new ItemUseService(catalog);

            for (var index = 0; index < 2; index++)
            {
                Assert.That(
                    inventory.TryAdd(machine.ItemId, out var item, out _),
                    Is.True);
                var result = service.TryUse(
                    inventory,
                    item.InstanceId,
                    ItemUseContext.ForRun(
                        target,
                        target.MaxHp,
                        ItemTargetAffiliation.Ally));
                Assert.That(result.Succeeded, Is.True);
            }

            Assert.That(
                target.SkillIds.Count(skillId => skillId == skill.SkillId),
                Is.EqualTo(2));
            Assert.That(inventory.Count, Is.Zero);
        }

        [Test]
        public void SkillMachine_UpdatesRunAndBattleInstancesTogether()
        {
            var (skill, machine, catalog) = CreateSkillMachine();
            var runTarget = new PachimonInstance(
                "battle_test_target",
                1,
                AllocationType.Electric,
                1,
                1,
                CreateStats());
            var battleTarget = new BattleUnitState(
                runTarget.InstanceId,
                runTarget.SpeciesId,
                "Test Target",
                BattleSide.Player,
                0,
                CreateEffectiveStats(),
                0,
                0,
                runTarget.SkillSlots,
                runTarget.PassiveIds);
            var inventory = new ItemInventory();
            Assert.That(
                inventory.TryAdd(machine.ItemId, out var item, out _),
                Is.True);

            var result = new ItemUseService(catalog).TryUse(
                inventory,
                item.InstanceId,
                ItemUseContext.ForBattle(
                    battleTarget,
                    ItemTargetAffiliation.Ally,
                    runTarget));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(runTarget.SkillIds.Last(), Is.EqualTo(skill.SkillId));
            Assert.That(battleTarget.SkillIds.Last(), Is.EqualTo(skill.SkillId));
        }

        [Test]
        public void ElectromagneticCannon_TransfersOverflowWithNewDefense()
        {
            var skill = CreateElectromagneticCannon();
            var user = CreateBattleUnit(
                "player_1",
                BattleSide.Player,
                slotIndex: 0,
                currentHp: 1000,
                electric: 100,
                skillId: skill.SkillId);
            var playerSide = new BattleSideState(
                BattleSide.Player,
                new[]
                {
                    user,
                    CreateBattleUnit(
                        "player_2",
                        BattleSide.Player,
                        1,
                        1000,
                        0,
                        1),
                    CreateBattleUnit(
                        "player_3",
                        BattleSide.Player,
                        2,
                        1000,
                        0,
                        1),
                });
            var firstEnemy = CreateBattleUnit(
                "enemy_1",
                BattleSide.Enemy,
                0,
                300,
                0,
                1);
            var secondEnemy = CreateBattleUnit(
                "enemy_2",
                BattleSide.Enemy,
                1,
                100,
                100,
                1);
            var thirdEnemy = CreateBattleUnit(
                "enemy_3",
                BattleSide.Enemy,
                2,
                1000,
                0,
                1);
            var state = new BattleState(
                123,
                playerSide,
                new BattleSideState(
                    BattleSide.Enemy,
                    new[] { firstEnemy, secondEnemy, thirdEnemy }),
                new PassiveLogicRegistry(PassiveCatalog));
            var logic = new ElectromagneticCannonSkillLogic(
                skill);

            var resolution = logic.Resolve(
                new SkillExecutionContext(state, user, skill));

            Assert.That(resolution.Effects.Count, Is.EqualTo(3));
            Assert.That(firstEnemy.CurrentHp, Is.Zero);
            Assert.That(secondEnemy.CurrentHp, Is.Zero);
            Assert.That(thirdEnemy.CurrentHp, Is.EqualTo(850));
        }

        [Test]
        public void ElectromagneticCannon_HasConfiguredStartupAndOverflow()
        {
            var skill = CreateElectromagneticCannon();

            Assert.That(skill.BaseStartupTicks, Is.EqualTo(300));
            Assert.That(skill.BaseRecoveryTicks, Is.EqualTo(100));
            Assert.That(skill.BaseCooldownTicks, Is.EqualTo(500));
            Assert.That(skill.BaseManaCost, Is.EqualTo(500));
            Assert.That(skill.BasePower, Is.EqualTo(400));
            Assert.That(
                ElectromagneticCannonSkillLogic.CalculateOverflow(
                    damage: 800,
                    currentHp: 300),
                Is.EqualTo(500));
        }

        [Test]
        public void AquaShock_DealsTwoAttributesAndAppliesLeak()
        {
            var skill = CreateAquaShock();
            var user = CreateBattleUnitWithStats(
                "player_1",
                BattleSide.Player,
                0,
                2000,
                skill.SkillId,
                (PachimonStatType.Electric, 100),
                (PachimonStatType.Aqua, 100));
            var enemies = CreateTestSide(BattleSide.Enemy);
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, user),
                enemies,
                new PassiveLogicRegistry(PassiveCatalog));

            var resolution = new AquaShockSkillLogic(skill)
                .Resolve(new SkillExecutionContext(state, user, skill));
            var target = enemies.GetUnitAt(0);

            Assert.That(resolution.Effects.Single().Damage, Is.EqualTo(40));
            Assert.That(target.CurrentHp, Is.EqualTo(1960));
            Assert.That(target.Statuses.Single().StatusId, Is.EqualTo(BattleStatusId.Leak));
            Assert.That(target.Statuses.Single().Value, Is.EqualTo(20));
            Assert.That(target.Statuses.Single().Source, Is.SameAs(user));
        }

        [Test]
        public void Leak_ConsumesAndCanChainAcrossTheParty()
        {
            var source = CreateBattleUnitWithStats(
                "player_1",
                BattleSide.Player,
                0,
                2000,
                1);
            var enemies = CreateTestSide(BattleSide.Enemy);
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, source),
                enemies,
                new PassiveLogicRegistry(PassiveCatalog));
            enemies.GetUnitAt(0).ApplyOrReplaceStatus(
                new BattleStatusInstance(
                    BattleStatusId.Leak,
                    BattleStatusCategory.Leak,
                    source,
                    value: 20));
            enemies.GetUnitAt(1).ApplyOrReplaceStatus(
                new BattleStatusInstance(
                    BattleStatusId.Leak,
                    BattleStatusCategory.Leak,
                    source,
                    value: 50));

            BattleAttributeDamageService.Apply(
                state,
                source,
                enemies.GetUnitAt(0),
                new DamageContext(
                    DamageOriginKind.Skill,
                    1,
                    100,
                    source.StartingStats,
                    enemies.GetUnitAt(0).StartingStats,
                    PachimonAttribute.Electric,
                    isAttack: true,
                    applyAttackerAttributeMultiplier: false,
                    applyDamageBonusMultiplier: false));

            Assert.That(enemies.GetUnitAt(0).CurrentHp, Is.EqualTo(1870));
            Assert.That(enemies.GetUnitAt(1).CurrentHp, Is.EqualTo(1970));
            Assert.That(enemies.GetUnitAt(2).CurrentHp, Is.EqualTo(1970));
            Assert.That(enemies.GetUnitAt(0).Statuses, Is.Empty);
            Assert.That(enemies.GetUnitAt(1).Statuses, Is.Empty);
        }

        [Test]
        public void StoredCharge_ConsumesBeforeDamageAndRegainsAfterDamage()
        {
            var definition = CreateStoredChargePassive();
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            catalog.SetPassivesForEditor(new PassiveAsset[] { definition });
            var source = CreateBattleUnitWithPassive(
                "player_1",
                BattleSide.Player,
                0,
                definition.PassiveId);
            var enemies = CreateTestSide(BattleSide.Enemy);
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, source),
                enemies,
                new PassiveLogicRegistry(catalog));
            var target = enemies.GetUnitAt(0);
            var context = new DamageContext(
                DamageOriginKind.Skill,
                1,
                100,
                source.StartingStats,
                target.StartingStats,
                PachimonAttribute.Electric,
                isAttack: true,
                applyAttackerAttributeMultiplier: false,
                applyDamageBonusMultiplier: false);

            var first = BattleAttributeDamageService.Apply(
                state,
                source,
                target,
                context);
            var previewSkill = CreateBasicElectricSkill();
            var preview = BattleSkillPreviewSimulator.Simulate(
                state,
                source,
                previewSkill,
                new BasicAttributeDamageSkillLogic(
                    PachimonAttribute.Electric),
                spendMana: false);
            Assert.That(target.CurrentHp, Is.EqualTo(1900));
            Assert.That(
                source.GetStatus(BattleStatusId.StoredCharge).StackCount,
                Is.EqualTo(1));
            var second = BattleAttributeDamageService.Apply(
                state,
                source,
                target,
                context);

            Assert.That(first.FinalDamage, Is.EqualTo(100));
            Assert.That(
                preview.Effects.Single(effect =>
                    ReferenceEquals(effect.Target, target)).HpDelta,
                Is.EqualTo(-110));
            Assert.That(second.FinalDamage, Is.EqualTo(110));
            Assert.That(
                source.GetStatus(BattleStatusId.StoredCharge).StackCount,
                Is.EqualTo(1));
        }

        [Test]
        public void StoredCharge_GainsAStackWhenElectricDamageIsZero()
        {
            var definition = CreateStoredChargePassive();
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            catalog.SetPassivesForEditor(new PassiveAsset[] { definition });
            var source = CreateBattleUnitWithPassive(
                "player_1",
                BattleSide.Player,
                0,
                definition.PassiveId);
            var enemies = CreateTestSide(BattleSide.Enemy);
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, source),
                enemies,
                new PassiveLogicRegistry(catalog));
            var target = enemies.GetUnitAt(0);

            BattleAttributeDamageService.Apply(
                state,
                source,
                target,
                new DamageContext(
                    DamageOriginKind.Skill,
                    1,
                    0,
                    source.StartingStats,
                    target.StartingStats,
                    PachimonAttribute.Electric,
                    isAttack: true,
                    applyAttackerAttributeMultiplier: false,
                    applyDamageBonusMultiplier: false));

            Assert.That(
                source.GetStatus(BattleStatusId.StoredCharge).StackCount,
                Is.EqualTo(1));
        }

        [Test]
        public void PreviewSimulation_ResolvesLeakWithoutMutatingBattle()
        {
            var skill = CreateBasicElectricSkill();
            var source = CreateBattleUnitWithStats(
                "player_1",
                BattleSide.Player,
                0,
                2000,
                skill.SkillId);
            var enemies = CreateTestSide(BattleSide.Enemy);
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, source),
                enemies,
                new PassiveLogicRegistry(PassiveCatalog));
            var target = enemies.GetUnitAt(0);
            target.ApplyOrReplaceStatus(new BattleStatusInstance(
                BattleStatusId.Leak,
                BattleStatusCategory.Leak,
                source,
                value: 50));

            var preview = BattleSkillPreviewSimulator.Simulate(
                state,
                source,
                skill,
                new BasicAttributeDamageSkillLogic(
                    PachimonAttribute.Electric),
                spendMana: false);

            Assert.That(
                preview.Effects.Single(effect =>
                    ReferenceEquals(effect.Target, target)).HpDelta,
                Is.EqualTo(-150));
            Assert.That(
                preview.Effects.Single(effect =>
                    ReferenceEquals(
                        effect.Target,
                        enemies.GetUnitAt(1))).HpDelta,
                Is.EqualTo(-50));
            Assert.That(
                preview.Effects.Single(effect =>
                    ReferenceEquals(
                        effect.Target,
                        enemies.GetUnitAt(2))).HpDelta,
                Is.EqualTo(-50));
            Assert.That(enemies.Units.All(unit => unit.CurrentHp == 2000), Is.True);
            Assert.That(target.GetStatus(BattleStatusId.Leak), Is.Not.Null);
        }

        [Test]
        public void PreviewSimulation_ReportsManaWithoutMutatingBattle()
        {
            var skill = CreateAquaShock();
            var source = CreateBattleUnitWithStats(
                "player_1",
                BattleSide.Player,
                0,
                2000,
                skill.SkillId,
                (PachimonStatType.Electric, 100),
                (PachimonStatType.Aqua, 100));
            var enemies = CreateTestSide(BattleSide.Enemy);
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, source),
                enemies,
                new PassiveLogicRegistry(PassiveCatalog));
            var target = enemies.GetUnitAt(0);

            var preview = BattleSkillPreviewSimulator.Simulate(
                state,
                source,
                skill,
                new AquaShockSkillLogic(skill),
                spendMana: true);

            Assert.That(
                preview.Effects.Single(effect =>
                    ReferenceEquals(effect.Target, source)).MnDelta,
                Is.EqualTo(-80));
            Assert.That(source.CurrentMn, Is.EqualTo(1000));
            Assert.That(target.CurrentHp, Is.EqualTo(2000));
            Assert.That(target.GetStatus(BattleStatusId.Leak), Is.Null);
        }

        [TestCase(1001, 50, 500)]
        [TestCase(1, 50, 1)]
        [TestCase(0, 50, 0)]
        public void RestSpotRecovery_FloorsAndGuaranteesOneForPositiveRecovery(
            int maximum,
            int percent,
            int expected)
        {
            Assert.That(
                RestSpotRecoveryService.CalculateHealAmount(maximum, percent),
                Is.EqualTo(expected));
        }

        [Test]
        public void StatCalculator_AppliesAllModifierStagesInOrder()
        {
            var calculator = new StatCalculator();
            var modifiers = new IStatModifier[]
            {
                Fixed(
                    PachimonStatType.Fire,
                    StatModifierOperation.DirectAdditive,
                    20m,
                    "direct-fire"),
                new DerivedStatModifier(
                    PachimonStatType.Electric,
                    StatModifierOperation.DerivedAdditive,
                    stats => stats.GetValue(PachimonStatType.Aqua) * 0.3m,
                    Source("aqua-generation")),
                Fixed(
                    PachimonStatType.Electric,
                    StatModifierOperation.DirectMultiplicative,
                    1.5m,
                    "direct-electric-multiplier"),
                new DerivedStatModifier(
                    PachimonStatType.Electric,
                    StatModifierOperation.DerivedMultiplicative,
                    stats => 1m + stats.GetValue(PachimonStatType.Fire) / 600m,
                    Source("derived-electric-multiplier")),
            };

            var result = calculator.Calculate(
                CreateStats(
                    (PachimonStatType.Fire, 100),
                    (PachimonStatType.Aqua, 50)),
                modifiers);

            Assert.That(result.GetValue(PachimonStatType.Fire), Is.EqualTo(120));
            Assert.That(result.GetUnroundedValue(PachimonStatType.Electric), Is.EqualTo(27m));
            Assert.That(result.GetValue(PachimonStatType.Electric), Is.EqualTo(27));
            Assert.That(
                result.GetContributions(PachimonStatType.Electric).Count,
                Is.EqualTo(4));
        }

        [Test]
        public void StatCalculator_DerivedAdditionsUseOneSharedSnapshot()
        {
            var calculator = new StatCalculator();
            var modifiers = new IStatModifier[]
            {
                new DerivedStatModifier(
                    PachimonStatType.Speed,
                    StatModifierOperation.DerivedAdditive,
                    stats => stats.GetValue(PachimonStatType.Dragon),
                    Source("dragon-to-speed")),
                new DerivedStatModifier(
                    PachimonStatType.Dragon,
                    StatModifierOperation.DerivedAdditive,
                    stats => stats.GetValue(PachimonStatType.Speed),
                    Source("speed-to-dragon")),
            };

            var result = calculator.Calculate(
                CreateStats(
                    (PachimonStatType.Speed, 100),
                    (PachimonStatType.Dragon, 50)),
                modifiers);

            Assert.That(result.GetValue(PachimonStatType.Speed), Is.EqualTo(150));
            Assert.That(result.GetValue(PachimonStatType.Dragon), Is.EqualTo(150));
        }

        [Test]
        public void StatCalculator_PreservesFractionsUntilFinalStat()
        {
            var calculator = new StatCalculator();
            var modifiers = new IStatModifier[]
            {
                new DerivedStatModifier(
                    PachimonStatType.Electric,
                    StatModifierOperation.DerivedAdditive,
                    stats => stats.GetValue(PachimonStatType.Aqua) * 0.3m,
                    Source("aqua-generation")),
                Fixed(
                    PachimonStatType.Electric,
                    StatModifierOperation.DirectMultiplicative,
                    1.3m,
                    "badge"),
            };

            var result = calculator.Calculate(
                CreateStats((PachimonStatType.Aqua, 33)),
                modifiers);

            Assert.That(
                result.GetUnroundedValue(PachimonStatType.Electric),
                Is.EqualTo(12.87m));
            Assert.That(result.GetValue(PachimonStatType.Electric), Is.EqualTo(12));
        }

        [Test]
        public void EffectiveStats_ExposeTrainerModifierCalculationBreakdown()
        {
            var modifiers = new TrainerModifierSet();
            modifiers.AddStat(PachimonStatType.Fire, 20);
            modifiers.AddBadge(PachimonAttribute.Fire);

            var result = new EffectivePachimonStats(
                CreateStats((PachimonStatType.Fire, 100)),
                modifiers);
            var fireContributions = result.Calculation
                .GetContributions(PachimonStatType.Fire);

            Assert.That(result.GetValue(PachimonStatType.Fire), Is.EqualTo(156));
            Assert.That(
                fireContributions.Count(item =>
                    item.Operation == StatModifierOperation.DirectAdditive),
                Is.EqualTo(1));
            Assert.That(
                fireContributions.Count(item =>
                    item.Operation == StatModifierOperation.DirectMultiplicative),
                Is.EqualTo(1));
        }

        [Test]
        public void PachimonStatService_AppliesHydroelectricPowerInEveryContext()
        {
            var result = PachimonStatService.Calculate(
                CreateStats(
                    (PachimonStatType.Aqua, 33),
                    (PachimonStatType.Electric, 10)),
                trainerModifiers: null,
                passiveIds: new[]
                {
                    HydroelectricPowerPassiveId,
                },
                PassiveRegistry);

            var contribution = result.Calculation
                .GetContributions(PachimonStatType.Electric)
                .Single(item =>
                    item.Source.SourceId
                    == $"passive:{HydroelectricPowerPassiveId}");

            Assert.That(contribution.Value, Is.EqualTo(9.9m));
            Assert.That(result.GetValue(PachimonStatType.Electric), Is.EqualTo(19));
        }

        [Test]
        public void HydroelectricPower_ClampsItsContributionToZero()
        {
            var result = PachimonStatService.Calculate(
                CreateStats(
                    (PachimonStatType.Aqua, -100),
                    (PachimonStatType.Electric, 10)),
                trainerModifiers: null,
                passiveIds: new[]
                {
                    HydroelectricPowerPassiveId,
                },
                PassiveRegistry);

            Assert.That(result.GetValue(PachimonStatType.Electric), Is.EqualTo(10));
        }

        [TestCase(
            HydroelectricPowerPassiveId,
            PachimonStatType.Aqua)]
        [TestCase(
            ThermalPowerPassiveId,
            PachimonStatType.Fire)]
        [TestCase(
            WindPowerPassiveId,
            PachimonStatType.Wind)]
        public void PowerGenerationPassives_AddThirtyPercentOfReferenceStat(
            int passiveId,
            PachimonStatType referenceStat)
        {
            var result = PachimonStatService.Calculate(
                CreateStats(
                    (referenceStat, 33),
                    (PachimonStatType.Electric, 10)),
                trainerModifiers: null,
                passiveIds: new[] { passiveId },
                passiveRegistry: PassiveRegistry);

            var definition = GetPassiveDefinition(passiveId);
            var contribution = result.Calculation
                .GetContributions(PachimonStatType.Electric)
                .Single(item =>
                    item.Source.SourceId == $"passive:{definition.PassiveId}");

            Assert.That(contribution.Value, Is.EqualTo(9.9m));
            Assert.That(result.GetValue(PachimonStatType.Electric), Is.EqualTo(19));
        }

        [TestCase(
            ThermalPowerPassiveId,
            PachimonStatType.Fire)]
        [TestCase(
            WindPowerPassiveId,
            PachimonStatType.Wind)]
        public void AddedPowerGenerationPassives_ClampContributionToZero(
            int passiveId,
            PachimonStatType referenceStat)
        {
            var result = PachimonStatService.Calculate(
                CreateStats(
                    (referenceStat, -100),
                    (PachimonStatType.Electric, 10)),
                trainerModifiers: null,
                passiveIds: new[] { passiveId },
                passiveRegistry: PassiveRegistry);

            Assert.That(result.GetValue(PachimonStatType.Electric), Is.EqualTo(10));
        }

        [TestCase(HydroelectricPowerPassiveId)]
        [TestCase(ThermalPowerPassiveId)]
        [TestCase(WindPowerPassiveId)]
        public void ImplementedStatPassives_AreNotPlaceholderDamagePassives(
            int passiveId)
        {
            Assert.That(
                PassiveLogicRegistry.TryGetPlaceholderAttribute(
                    passiveId,
                    PassiveCatalog,
                    out _),
                Is.False);
        }

        private static DerivedAdditivePassiveAsset GetPassiveDefinition(
            int passiveId)
        {
            Assert.That(
                PassiveRegistry.TryGetDefinition(
                    passiveId,
                    out var definition),
                Is.True);
            return (DerivedAdditivePassiveAsset)definition;
        }

        private static (
            ElectricQuickAttackSkillAsset skill,
            SkillMachineItemAsset machine,
            ItemCatalog catalog) CreateSkillMachine()
        {
            var skill =
                ScriptableObject.CreateInstance<ElectricQuickAttackSkillAsset>();
            skill.ConfigureForEditor(
                skillId: 28,
                displayName: "Electric Quick Attack",
                baseRecoveryTicks: 60,
                baseCooldownTicks: 100,
                baseManaCost: 60,
                description: string.Empty,
                electricBasePower: 25,
                fireBasePower: 10,
                windTimingPercent: 100);
            var machine =
                ScriptableObject.CreateInstance<SkillMachineItemAsset>();
            machine.ConfigureForEditor(
                ItemIds.GetSkillMachineItemId(skill.SkillId),
                "Skill Machine",
                null,
                string.Empty,
                ItemCategory.SkillMachine,
                5000);
            machine.ConfigureSkillForEditor(skill);
            var catalog = ScriptableObject.CreateInstance<ItemCatalog>();
            catalog.SetItemsForEditor(new ItemAsset[] { machine });
            return (skill, machine, catalog);
        }

        private static ElectromagneticCannonSkillAsset
            CreateElectromagneticCannon()
        {
            var skill = ScriptableObject
                .CreateInstance<ElectromagneticCannonSkillAsset>();
            skill.ConfigureForEditor(
                skillId: 44,
                displayName: "Electromagnetic Cannon",
                baseStartupTicks: 300,
                baseRecoveryTicks: 100,
                baseCooldownTicks: 500,
                baseManaCost: 500,
                description: string.Empty,
                basePower: 400);
            return skill;
        }

        private static ChargeSkillAsset CreateChargeSkill()
        {
            var skill = ScriptableObject.CreateInstance<ChargeSkillAsset>();
            skill.ConfigureForEditor(
                skillId: 36,
                displayName: "Charge",
                baseRecoveryTicks: 200,
                baseCooldownTicks: 500,
                baseManaCost: 400,
                description: string.Empty,
                chargingDurationPercent: 400,
                chargingResistBonusPercent: 40,
                chargingElectricPercent: 50,
                chargedDurationPercent: 200,
                chargedElectricPercent: 150,
                chargedSpeedPercent: 100);
            return skill;
        }

        private static AquaShockSkillAsset CreateAquaShock()
        {
            var skill = ScriptableObject.CreateInstance<AquaShockSkillAsset>();
            skill.ConfigureForEditor(
                skillId: 12,
                displayName: "Aqua Shock",
                baseRecoveryTicks: 80,
                baseCooldownTicks: 200,
                baseManaCost: 80,
                description: string.Empty,
                electricBasePower: 10,
                aquaBasePower: 10,
                leakBaseValue: 10);
            return skill;
        }

        private static StoredChargePassiveAsset CreateStoredChargePassive()
        {
            var passive =
                ScriptableObject.CreateInstance<StoredChargePassiveAsset>();
            passive.ConfigureForEditor(
                passiveId: 44,
                displayName: "Stored Charge",
                description: string.Empty,
                damagePercentPerStack: 10);
            return passive;
        }

        private static StaticElectricityPassiveAsset
            CreateStaticElectricityPassive()
        {
            var passive =
                ScriptableObject.CreateInstance<StaticElectricityPassiveAsset>();
            passive.ConfigureForEditor(
                passiveId: StaticElectricityPassiveAsset.DefaultPassiveId,
                displayName: "Static Electricity",
                description: string.Empty,
                electricBaseValue: 20,
                iceBaseValue: 10);
            return passive;
        }

        private static void ApplyTestElectricDamage(
            BattleState state,
            BattleUnitState attacker,
            BattleUnitState defender,
            DamageOriginKind originKind,
            bool isAttack)
        {
            BattleAttributeDamageService.Apply(
                state,
                attacker,
                defender,
                new DamageContext(
                    originKind,
                    originId: 1,
                    baseDamage: 10,
                    attacker.StartingStats,
                    defender.StartingStats,
                    PachimonAttribute.Electric,
                    isAttack,
                    applyAttackerAttributeMultiplier: false,
                    applyDamageBonusMultiplier: false));
        }

        private static PlaceholderSkillAsset CreateBasicElectricSkill()
        {
            var skill = ScriptableObject.CreateInstance<PlaceholderSkillAsset>();
            skill.ConfigureForEditor(
                skillId: 1,
                displayName: "Basic Electric",
                allocationType: AllocationType.Electric,
                isMapAssignable: true,
                baseRecoveryTicks: 100,
                baseCooldownTicks: 200,
                description: string.Empty);
            return skill;
        }

        private static PlaceholderSkillAsset CreateBasicIceSkill()
        {
            var skill = ScriptableObject.CreateInstance<PlaceholderSkillAsset>();
            skill.ConfigureForEditor(
                skillId: 2,
                displayName: "Basic Ice",
                allocationType: AllocationType.Ice,
                isMapAssignable: true,
                baseRecoveryTicks: 100,
                baseCooldownTicks: 200,
                description: string.Empty);
            return skill;
        }

        private static BattleUnitState CreateBattleUnitWithPassive(
            string instanceId,
            BattleSide side,
            int slotIndex,
            int passiveId,
            params (PachimonStatType type, int value)[] stats)
        {
            var allStats = new[]
            {
                (PachimonStatType.MaxHp, 2000),
                (PachimonStatType.MaxMn, 1000),
            }.Concat(stats).ToArray();
            return new BattleUnitState(
                instanceId,
                slotIndex + 1,
                instanceId,
                side,
                slotIndex,
                CreateEffectiveStats(allStats),
                2000,
                1000,
                new[] { new PachimonSkillSlot(1, 1) },
                new[] { passiveId });
        }

        private static BattleSideState CreateTestSide(
            BattleSide side,
            BattleUnitState firstUnit = null)
        {
            return new BattleSideState(
                side,
                new[]
                {
                    firstUnit ?? CreateBattleUnitWithStats(
                        $"{side}_1",
                        side,
                        0,
                        2000,
                        1),
                    CreateBattleUnitWithStats(
                        $"{side}_2",
                        side,
                        1,
                        2000,
                        1),
                    CreateBattleUnitWithStats(
                        $"{side}_3",
                        side,
                        2,
                        2000,
                        1),
                });
        }

        private static BattleUnitState CreateBattleUnitWithStats(
            string instanceId,
            BattleSide side,
            int slotIndex,
            int currentHp,
            int skillId,
            params (PachimonStatType type, int value)[] stats)
        {
            var allStats = new[]
            {
                (PachimonStatType.MaxHp, 2000),
                (PachimonStatType.MaxMn, 1000),
            }.Concat(stats).ToArray();
            return new BattleUnitState(
                instanceId,
                slotIndex + 1,
                instanceId,
                side,
                slotIndex,
                CreateEffectiveStats(allStats),
                currentHp,
                1000,
                new[] { new PachimonSkillSlot(1, skillId) },
                new[] { 1 });
        }

        private static BattleUnitState CreateBattleUnit(
            string instanceId,
            BattleSide side,
            int slotIndex,
            int currentHp,
            int electric,
            int skillId)
        {
            return new BattleUnitState(
                instanceId,
                slotIndex + 1,
                instanceId,
                side,
                slotIndex,
                CreateEffectiveStats(
                    (PachimonStatType.MaxHp, 2000),
                    (PachimonStatType.MaxMn, 1000),
                    (PachimonStatType.Electric, electric)),
                currentHp,
                1000,
                new[] { new PachimonSkillSlot(1, skillId) },
                new[] { 1 });
        }

        private static PassiveStatModifierRegistry PassiveRegistry =>
            _passiveRegistry ??= new PassiveStatModifierRegistry(PassiveCatalog);

        private static PassiveCatalog PassiveCatalog
        {
            get
            {
                if (_passiveCatalog != null)
                {
                    return _passiveCatalog;
                }

                var hydro = CreateGenerationPassive(
                    HydroelectricPowerPassiveId,
                    "水力発電",
                    PachimonStatType.Aqua);
                var thermal = CreateGenerationPassive(
                    ThermalPowerPassiveId,
                    "火力発電",
                    PachimonStatType.Fire);
                var wind = CreateGenerationPassive(
                    WindPowerPassiveId,
                    "風力発電",
                    PachimonStatType.Wind);
                _passiveCatalog = ScriptableObject.CreateInstance<PassiveCatalog>();
                _passiveCatalog.SetPassivesForEditor(
                    new PassiveAsset[] { hydro, thermal, wind });
                return _passiveCatalog;
            }
        }

        private static DerivedAdditivePassiveAsset CreateGenerationPassive(
            int passiveId,
            string displayName,
            PachimonStatType referenceStat)
        {
            var passive =
                ScriptableObject.CreateInstance<DerivedAdditivePassiveAsset>();
            passive.ConfigureForEditor(
                passiveId,
                displayName,
                string.Empty,
                PachimonStatType.Electric,
                referenceStat,
                percent: 30,
                minimumContribution: 0);
            return passive;
        }

        private static FixedStatModifier Fixed(
            PachimonStatType statType,
            StatModifierOperation operation,
            decimal value,
            string sourceId)
        {
            return new FixedStatModifier(
                statType,
                operation,
                value,
                Source(sourceId));
        }

        private static StatModifierSource Source(string sourceId)
        {
            return new StatModifierSource(
                StatModifierSourceType.Passive,
                sourceId,
                sourceId);
        }

        private static EffectivePachimonStats CreateEffectiveStats(
            params (PachimonStatType statType, int value)[] values)
        {
            return new EffectivePachimonStats(CreateStats(values), null);
        }

        private static PachimonStats CreateStats(
            params (PachimonStatType statType, int value)[] values)
        {
            var valueUnits = new int[(int)PachimonStatType.Count];
            foreach (var (statType, value) in values)
            {
                valueUnits[(int)statType] = value;
            }

            return new PachimonStats(
                valueUnits,
                resourceDisplayMultiplier: 1,
                specialStatDivisor: 1);
        }
    }
}
