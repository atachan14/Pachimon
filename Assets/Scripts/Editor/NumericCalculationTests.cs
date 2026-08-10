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
        private const int PoisonRunnerPassiveId = 29;
        private const int HeatToxinPassiveId = 37;
        private static PassiveCatalog _passiveCatalog;
        private static PassiveStatModifierRegistry _passiveRegistry;
        private static ToxinStatusAsset _toxinStatus;
        private static SmogFieldEffectAsset _smogFieldEffect;
        private static StunStatusAsset _stunStatus;
        private static SlowStatusAsset _paralysisStatus;
        private static SlowStatusAsset _chillStatus;
        private static ChargeStatusAsset _chargeStatus;

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
        public void ZeroTimings_RemainZeroWithNegativeStats()
        {
            Assert.That(BattleTickMath.GetEffectiveStartup(0, -100), Is.Zero);
            Assert.That(BattleTickMath.GetEffectiveRecovery(0, -100), Is.Zero);
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
        public void Toxin_DealsAndDecaysOnePercentPerTick()
        {
            var target = CreateBattleUnitWithStats(
                "player_1",
                BattleSide.Player,
                0,
                2000,
                1);
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, target),
                CreateTestSide(BattleSide.Enemy));
            var source = state.Enemy.GetUnitAt(0);
            state.Statuses.ApplyStatus(
                target,
                BattleStatusFactory.CreateToxin(source, 100, ToxinStatus));

            state.Timeline.AdvanceToTick(state.CurrentTick + 1);

            var toxin = target.GetStatus(BattleStatusId.Toxin);
            Assert.That(target.CurrentHp, Is.EqualTo(1999));
            Assert.That(toxin.Value, Is.EqualTo(99));
            Assert.That(toxin.DamageWork, Is.Zero);
            Assert.That(toxin.DecayWork, Is.Zero);
            Assert.That(state.ToxinPresentation.Drain().Single().HpBefore,
                Is.EqualTo(2000));
        }

        [Test]
        public void Toxin_PreservesFractionalWorkAndApplicationHistoryOnReapply()
        {
            var target = CreateBattleUnitWithStats(
                "player_1",
                BattleSide.Player,
                0,
                2000,
                1);
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, target),
                CreateTestSide(BattleSide.Enemy));
            var firstSource = state.Enemy.GetUnitAt(0);
            var secondSource = state.Enemy.GetUnitAt(1);
            state.Statuses.ApplyStatus(
                target,
                BattleStatusFactory.CreateToxin(firstSource, 50, ToxinStatus));
            state.Timeline.AdvanceToTick(state.CurrentTick + 1);
            state.Statuses.ApplyStatus(
                target,
                BattleStatusFactory.CreateToxin(secondSource, 25, ToxinStatus));

            var toxin = target.GetStatus(BattleStatusId.Toxin);
            Assert.That(toxin.Value, Is.EqualTo(75));
            Assert.That(toxin.DamageWork, Is.EqualTo(0.5m));
            Assert.That(toxin.DecayWork, Is.EqualTo(0.5m));
            Assert.That(toxin.ToxinApplications.Count, Is.EqualTo(2));
            Assert.That(
                toxin.ToxinApplications[1].SourceInstanceId,
                Is.EqualTo(secondSource.InstanceId));

            state.Timeline.AdvanceToTick(state.CurrentTick + 1);

            Assert.That(target.CurrentHp, Is.EqualTo(1999));
            Assert.That(toxin.Value, Is.EqualTo(74));
            Assert.That(toxin.DamageWork, Is.EqualTo(0.25m));
            Assert.That(toxin.DecayWork, Is.EqualTo(0.25m));
        }

        [Test]
        public void ToxinAdaptation_IncreasesPoisonOncePerToxinApplication()
        {
            var definition = CreateToxinGrowthPassive();
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            catalog.SetPassivesForEditor(new PassiveAsset[] { definition });
            var source = CreateBattleUnitWithPassive(
                "player_1",
                BattleSide.Player,
                0,
                definition.PassiveId,
                (PachimonStatType.Poison, 100));
            var enemies = CreateTestSide(BattleSide.Enemy);
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, source),
                enemies,
                new PassiveLogicRegistry(catalog));

            state.Statuses.ApplyStatus(
                enemies.GetUnitAt(0),
                BattleStatusFactory.CreateToxin(source, 100, ToxinStatus));
            state.Statuses.ApplyStatus(
                enemies.GetUnitAt(1),
                BattleStatusFactory.CreateToxin(source, 100, ToxinStatus));

            Assert.That(
                source.GetStatus(BattleStatusId.ToxinGrowth).StackCount,
                Is.EqualTo(2));
            Assert.That(
                source.GetBattleStatValue(PachimonStatType.Poison),
                Is.EqualTo(120));

            state.Statuses.ApplyStatus(
                source,
                BattleStatusFactory.CreateToxin(
                    enemies.GetUnitAt(0),
                    100,
                    ToxinStatus));

            Assert.That(
                source.GetStatus(BattleStatusId.ToxinGrowth).StackCount,
                Is.EqualTo(2));
            Assert.That(
                source.GetBattleStatValue(PachimonStatType.Poison),
                Is.EqualTo(120));
        }

        [Test]
        public void PoisonKnight_SharesShieldAndActualRecoveryWithoutRecursion()
        {
            var definition = CreatePoisonKnightPassive();
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            catalog.SetPassivesForEditor(new PassiveAsset[] { definition });
            var owner = CreateBattleUnitWithPassive(
                "player_1",
                BattleSide.Player,
                0,
                definition.PassiveId,
                (PachimonStatType.Poison, 100));
            var players = CreateTestSide(BattleSide.Player, owner);
            var state = new BattleState(
                123,
                players,
                CreateTestSide(BattleSide.Enemy),
                new PassiveLogicRegistry(catalog));

            state.SupportEffects.ApplyShield(owner, owner, 100);

            Assert.That(owner.Shields.Single().Value, Is.EqualTo(100));
            Assert.That(players.GetUnitAt(1).Shields.Single().Value, Is.EqualTo(60));
            Assert.That(players.GetUnitAt(2).Shields.Single().Value, Is.EqualTo(60));

            owner.ApplyDamage(100);
            players.GetUnitAt(1).ApplyDamage(200);
            players.GetUnitAt(2).ApplyDamage(200);
            state.SupportEffects.RestoreHp(owner, owner, 100);

            Assert.That(owner.CurrentHp, Is.EqualTo(2000));
            Assert.That(players.GetUnitAt(1).CurrentHp, Is.EqualTo(1860));
            Assert.That(players.GetUnitAt(2).CurrentHp, Is.EqualTo(1860));
            Assert.That(players.GetUnitAt(1).Shields.Count, Is.EqualTo(1));
            Assert.That(players.GetUnitAt(2).Shields.Count, Is.EqualTo(1));
        }

        [Test]
        public void BurningMan_GainsFireFromEveryAppliedDamageKind()
        {
            var definition = CreateBurningManPassive();
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            catalog.SetPassivesForEditor(new PassiveAsset[] { definition });
            var owner = CreateBattleUnitWithPassive(
                "player_1",
                BattleSide.Player,
                0,
                definition.PassiveId,
                (PachimonStatType.Fire, 100));
            var enemies = CreateTestSide(BattleSide.Enemy);
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, owner),
                enemies,
                new PassiveLogicRegistry(catalog));
            var source = enemies.GetUnitAt(0);

            BattleAttributeDamageService.Apply(
                state,
                source,
                owner,
                new DamageContext(
                    DamageOriginKind.Skill,
                    1,
                    10,
                    source.StartingStats,
                    owner.StartingStats,
                    PachimonAttribute.Aqua,
                    isAttack: true,
                    applyAttackerAttributeMultiplier: false,
                    applyDamageBonusMultiplier: false));
            Assert.That(owner.GetBattleStatValue(PachimonStatType.Fire),
                Is.EqualTo(120));

            owner.AddShield(100);
            BattleAttributeDamageService.Apply(
                state,
                source,
                owner,
                new DamageContext(
                    DamageOriginKind.Skill,
                    1,
                    10,
                    source.StartingStats,
                    owner.StartingStats,
                    PachimonAttribute.Aqua,
                    isAttack: true,
                    applyAttackerAttributeMultiplier: false,
                    applyDamageBonusMultiplier: false));
            Assert.That(owner.GetBattleStatValue(PachimonStatType.Fire),
                Is.EqualTo(140));

            BattleTrueDamageService.Apply(
                state,
                source,
                owner,
                new TrueDamageContext(
                    DamageOriginKind.Skill,
                    1,
                    10,
                    isAttack: true));
            Assert.That(owner.GetBattleStatValue(PachimonStatType.Fire),
                Is.EqualTo(160));

            BattleStatusDamageService.Apply(
                state,
                owner,
                BattleStatusId.Toxin,
                PachimonAttribute.Poison,
                10);
            Assert.That(owner.GetBattleStatValue(PachimonStatType.Fire),
                Is.EqualTo(180));

            BattleTrueDamageService.Apply(
                state,
                source,
                owner,
                new TrueDamageContext(
                    DamageOriginKind.Skill,
                    1,
                    0,
                    isAttack: true));
            Assert.That(owner.GetBattleStatValue(PachimonStatType.Fire),
                Is.EqualTo(180));
        }

        [Test]
        public void DarkFlame_AddsPoisonDamageFromPreDefenseFireDamage()
        {
            var definition = CreateDarkFlamePassive();
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            catalog.SetPassivesForEditor(new PassiveAsset[] { definition });
            var source = CreateBattleUnitWithPassive(
                "player_1",
                BattleSide.Player,
                0,
                definition.PassiveId,
                (PachimonStatType.Fire, 100),
                (PachimonStatType.Poison, 100));
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
                    100,
                    source.GetBattleStats(),
                    target.GetBattleStats(),
                    PachimonAttribute.Fire,
                    isAttack: true));

            Assert.That(target.CurrentHp, Is.EqualTo(1720));
        }

        [Test]
        public void FireArcher_AddsFireDamageFromTargetsMissingHp()
        {
            var definition = CreateFireArcherPassive();
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            catalog.SetPassivesForEditor(new PassiveAsset[] { definition });
            var source = CreateBattleUnitWithPassive(
                "player_1",
                BattleSide.Player,
                0,
                definition.PassiveId,
                (PachimonStatType.Fire, 100));
            var target = CreateBattleUnitWithStats(
                "enemy_1",
                BattleSide.Enemy,
                0,
                1000,
                1);
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, source),
                CreateTestSide(BattleSide.Enemy, target),
                new PassiveLogicRegistry(catalog));

            BattleAttributeDamageService.Apply(
                state,
                source,
                target,
                new DamageContext(
                    DamageOriginKind.Skill,
                    1,
                    100,
                    source.GetBattleStats(),
                    target.GetBattleStats(),
                    PachimonAttribute.Fire,
                    isAttack: true));

            Assert.That(target.CurrentHp, Is.EqualTo(680));
        }

        [Test]
        public void Toxin_UsesTargetPoisonAndResistBonusOnly()
        {
            var target = CreateBattleUnitWithStats(
                "target",
                BattleSide.Enemy,
                0,
                2000,
                1,
                (PachimonStatType.Poison, 100),
                (PachimonStatType.ResistBonus, 100));

            var damage = BattleStatusDamageService.CalculateUnrounded(
                100m,
                target,
                PachimonAttribute.Poison);

            Assert.That(damage, Is.EqualTo(25m));
        }

        [Test]
        public void LethalToxin_StopsTimelineOnTheDefeatTick()
        {
            var player = CreateTestSide(BattleSide.Player);
            var enemy = CreateTestSide(BattleSide.Enemy);
            var state = new BattleState(123, player, enemy);
            var source = enemy.GetUnitAt(0);
            foreach (var target in player.Units)
            {
                state.Statuses.ApplyStatus(
                    target,
                    BattleStatusFactory.CreateToxin(
                        source,
                        200000,
                        ToxinStatus));
            }

            state.Timeline.AdvanceToTick(100);

            Assert.That(state.CurrentTick, Is.EqualTo(1));
            Assert.That(state.EvaluateOutcome(),
                Is.EqualTo(BattleOutcome.PlayerDefeat));
            Assert.That(state.ToxinPresentation.Drain().Count,
                Is.EqualTo(BattleSideState.PartySize));
        }

        [Test]
        public void PoisonNeedle_AppliesScaledToxinAfterDamage()
        {
            var skill = CreateBasicPoisonSkill();
            var source = CreateBattleUnitWithStats(
                "player_1",
                BattleSide.Player,
                0,
                2000,
                skill.SkillId,
                (PachimonStatType.Poison, 100));
            var enemies = CreateTestSide(BattleSide.Enemy);
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, source),
                enemies);

            var resolution = BattleSkillResolver.Resolve(
                state,
                source,
                skill,
                new PoisonNeedleSkillLogic());

            var toxin = enemies.GetUnitAt(0).GetStatus(BattleStatusId.Toxin);
            Assert.That(toxin, Is.Not.Null);
            Assert.That(toxin.Value, Is.EqualTo(200));
            Assert.That(toxin.Source, Is.Null);
            Assert.That(toxin.ToxinApplications.Single().AppliedValue,
                Is.EqualTo(200));
            Assert.That(
                resolution.Presentation.Steps.Any(step =>
                    step.Text == $"{enemies.GetUnitAt(0).DisplayName}に200の毒素を与えた！"),
                Is.True);
        }

        [Test]
        public void Smog_CreatesEnemyFieldAndAppliesToxinFromTheNextTick()
        {
            var skill = CreateSmogSkill();
            var source = CreateBattleUnitWithStats(
                "player_1",
                BattleSide.Player,
                0,
                2000,
                skill.SkillId,
                (PachimonStatType.Poison, 100));
            var enemies = CreateTestSide(BattleSide.Enemy);
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, source),
                enemies);

            var resolution = BattleSkillResolver.Resolve(
                state,
                source,
                skill,
                new SmogSkillLogic(skill));

            var smog = state.Fields.Effects.Single();
            Assert.That(smog.EffectId, Is.EqualTo(BattleFieldEffectId.Smog));
            Assert.That(smog.TargetSide, Is.EqualTo(BattleSide.Enemy));
            Assert.That(smog.Value, Is.EqualTo(600));
            Assert.That(resolution.Presentation.Steps.Any(step =>
                step.Text == "敵陣にValue 600のスモッグを生成した！"),
                Is.True);

            state.Timeline.AdvanceToTick(state.CurrentTick + 1);

            Assert.That(smog.Value, Is.EqualTo(594));
            foreach (var target in enemies.Units)
            {
                Assert.That(target.CurrentHp, Is.EqualTo(2000));
                var toxin = target.GetStatus(BattleStatusId.Toxin);
                Assert.That(toxin.Value, Is.EqualTo(6));
                Assert.That(
                    toxin.ToxinApplications.Single().SourceInstanceId,
                    Is.EqualTo(source.InstanceId));
            }
        }

        [Test]
        public void Neurotoxin_AppliesScaledStunAndToxinToBackEnemy()
        {
            var skill = CreateNeurotoxinSkill();
            var source = CreateBattleUnitWithStats(
                "player_1",
                BattleSide.Player,
                0,
                2000,
                skill.SkillId,
                (PachimonStatType.Poison, 100),
                (PachimonStatType.Electric, 50));
            var enemies = CreateTestSide(BattleSide.Enemy);
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, source),
                enemies);

            var resolution = BattleSkillResolver.Resolve(
                state,
                source,
                skill,
                new NeurotoxinSkillLogic(skill));

            var front = enemies.GetUnitAt(0);
            var back = enemies.GetUnitAt(2);
            Assert.That(front.GetStatus(BattleStatusId.Stun), Is.Null);
            Assert.That(front.GetStatus(BattleStatusId.Toxin), Is.Null);
            Assert.That(
                back.GetStatus(BattleStatusId.Stun).RemainingTicks,
                Is.EqualTo(175));
            Assert.That(
                back.GetStatus(BattleStatusId.Toxin).Value,
                Is.EqualTo(200));
            Assert.That(resolution.Presentation.Steps.Any(step =>
                step.Text == $"{back.DisplayName}に175tickのStunと200の毒素を与えた！"),
                Is.True);
        }

        [Test]
        public void ToxinTransfer_UsesDifferentFrontmostTargetsWhenValuesTie()
        {
            var skill = CreateToxinTransferSkill();
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
                enemies);
            foreach (var enemy in enemies.Units)
            {
                state.Statuses.ApplyStatus(
                    enemy,
                    BattleStatusFactory.CreateToxin(source, 100, ToxinStatus));
            }

            BattleSkillResolver.Resolve(
                state,
                source,
                skill,
                new ToxinTransferSkillLogic(skill));

            Assert.That(
                enemies.GetUnitAt(0).GetStatus(BattleStatusId.Toxin).Value,
                Is.EqualTo(50));
            Assert.That(
                enemies.GetUnitAt(1).GetStatus(BattleStatusId.Toxin).Value,
                Is.EqualTo(200));
            Assert.That(
                enemies.GetUnitAt(2).GetStatus(BattleStatusId.Toxin).Value,
                Is.EqualTo(100));
        }

        [Test]
        public void ToxinTransfer_WithOneLivingEnemy_ReappliesToSameTarget()
        {
            var skill = CreateToxinTransferSkill();
            var source = CreateBattleUnitWithStats(
                "player_1",
                BattleSide.Player,
                0,
                2000,
                skill.SkillId);
            var enemies = CreateTestSide(BattleSide.Enemy);
            enemies.GetUnitAt(0).ApplyDamage(2000);
            enemies.GetUnitAt(1).ApplyDamage(2000);
            var target = enemies.GetUnitAt(2);
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, source),
                enemies);
            state.Statuses.ApplyStatus(
                target,
                BattleStatusFactory.CreateToxin(source, 100, ToxinStatus));

            BattleSkillResolver.Resolve(
                state,
                source,
                skill,
                new ToxinTransferSkillLogic(skill));

            Assert.That(
                target.GetStatus(BattleStatusId.Toxin).Value,
                Is.EqualTo(150));
        }

        [Test]
        public void ToxinExplosion_ConsumesFrontmostMaximumAndDamagesAllEnemies()
        {
            var skill = CreateToxinExplosionSkill();
            var source = CreateBattleUnitWithStats(
                "player_1",
                BattleSide.Player,
                0,
                2000,
                skill.SkillId,
                (PachimonStatType.Poison, 100),
                (PachimonStatType.Fire, 100));
            var enemies = CreateTestSide(BattleSide.Enemy);
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, source),
                enemies);
            foreach (var enemy in enemies.Units)
            {
                state.Statuses.ApplyStatus(
                    enemy,
                    BattleStatusFactory.CreateToxin(source, 100, ToxinStatus));
            }

            var resolution = BattleSkillResolver.Resolve(
                state,
                source,
                skill,
                new ToxinExplosionSkillLogic(skill));

            Assert.That(
                enemies.GetUnitAt(0).GetStatus(BattleStatusId.Toxin),
                Is.Null);
            Assert.That(
                enemies.GetUnitAt(1).GetStatus(BattleStatusId.Toxin).Value,
                Is.EqualTo(100));
            Assert.That(
                enemies.GetUnitAt(2).GetStatus(BattleStatusId.Toxin).Value,
                Is.EqualTo(100));
            Assert.That(
                enemies.Units.All(enemy => enemy.CurrentHp == 1700),
                Is.True);
            Assert.That(
                resolution.Effects.All(effect => effect.Damage == 300),
                Is.True);
        }

        [Test]
        public void PoisonShield_AddsShieldAndReducesOwnToxin()
        {
            var skill = CreatePoisonShieldSkill();
            var source = CreateBattleUnitWithStats(
                "player_1",
                BattleSide.Player,
                0,
                2000,
                skill.SkillId,
                (PachimonStatType.Poison, 100));
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, source),
                CreateTestSide(BattleSide.Enemy));
            state.Statuses.ApplyStatus(
                source,
                BattleStatusFactory.CreateToxin(source, 100, ToxinStatus));

            BattleSkillResolver.Resolve(
                state,
                source,
                skill,
                new PoisonShieldSkillLogic(skill));

            Assert.That(source.TotalShield, Is.EqualTo(600));
            Assert.That(source.GetStatus(BattleStatusId.Toxin), Is.Null);

            var damage = BattleTrueDamageService.Apply(
                state,
                state.Enemy.GetUnitAt(0),
                source,
                new TrueDamageContext(
                    DamageOriginKind.Skill,
                    originId: 1,
                    damage: 700,
                    isAttack: true));

            Assert.That(damage.ShieldAbsorbedDamage, Is.EqualTo(600));
            Assert.That(damage.AppliedDamage, Is.EqualTo(100));
            Assert.That(source.CurrentHp, Is.EqualTo(1900));
            Assert.That(source.TotalShield, Is.Zero);
        }

        [Test]
        public void Shield_ConsumesShortestDurationBeforeEarlierShield()
        {
            var target = CreateTestSide(BattleSide.Player).GetUnitAt(0);
            var earlier = target.AddShield(100, durationTicks: 20);
            target.AddShield(200, durationTicks: 10);

            var result = target.AbsorbDamage(250);

            Assert.That(result.AbsorbedDamage, Is.EqualTo(250));
            Assert.That(result.RemainingDamage, Is.Zero);
            Assert.That(earlier.Value, Is.EqualTo(50));
            Assert.That(target.TotalShield, Is.EqualTo(50));
        }

        [Test]
        public void Smog_RecastAddsValueAndPreservesFractionalWork()
        {
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player),
                CreateTestSide(BattleSide.Enemy));
            var firstSource = state.Player.GetUnitAt(0);
            var secondSource = state.Player.GetUnitAt(1);
            var smog = state.Fields.CreateOrAddSmog(
                firstSource,
                BattleSide.Enemy,
                SmogFieldEffect,
                50);
            state.Timeline.AdvanceToTick(state.CurrentTick + 1);

            state.Fields.CreateOrAddSmog(
                secondSource,
                BattleSide.Enemy,
                SmogFieldEffect,
                25);

            Assert.That(state.Fields.Effects.Count, Is.EqualTo(1));
            Assert.That(smog.Value, Is.EqualTo(75));
            Assert.That(smog.ApplicationWork, Is.EqualTo(0.5m));
            Assert.That(smog.DecayWork, Is.EqualTo(0.5m));
            Assert.That(smog.Source, Is.SameAs(secondSource));
        }

        [Test]
        public void ScienceCraft_AmplifiesInitialAndRecastFieldValue()
        {
            var passive = CreateScienceCraftPassive();
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            catalog.SetPassivesForEditor(new PassiveAsset[] { passive });
            var source = CreateBattleUnitWithPassive(
                "player_1",
                BattleSide.Player,
                0,
                passive.PassiveId,
                (PachimonStatType.Poison, 100));
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, source),
                CreateTestSide(BattleSide.Enemy),
                new PassiveLogicRegistry(catalog));

            var smog = state.Fields.CreateOrAddSmog(
                source,
                BattleSide.Enemy,
                SmogFieldEffect,
                300);
            state.Fields.CreateOrAddSmog(
                source,
                BattleSide.Enemy,
                SmogFieldEffect,
                300);

            Assert.That(smog.Value, Is.EqualTo(780));
            Assert.That(
                state.LogEntries.Count(entry => entry.Contains("科学工作")),
                Is.EqualTo(2));
        }

        [Test]
        public void AttributeStatuses_UseMatchingDefenseBeforeAddingValue()
        {
            var target = CreateBattleUnitWithStats(
                "player_1",
                BattleSide.Player,
                0,
                2000,
                1,
                (PachimonStatType.Electric, 100),
                (PachimonStatType.Ice, 100),
                (PachimonStatType.Fire, 100),
                (PachimonStatType.Poison, 100));
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
                    BattleStatusId.Burn,
                    BattleStatusCategory.Burn,
                    source,
                    value: 150));
            state.Statuses.ApplyStatus(
                target,
                new BattleStatusInstance(
                    BattleStatusId.Burn,
                    BattleStatusCategory.Burn,
                    source,
                    value: 150));
            state.Statuses.ApplyStatus(
                target,
                BattleStatusFactory.CreateToxin(
                    source,
                    150,
                    ToxinStatus));
            state.Statuses.ApplyStatus(
                target,
                BattleStatusFactory.CreateToxin(
                    source,
                    150,
                    ToxinStatus));
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
                target.GetStatus(BattleStatusId.Burn).Value,
                Is.EqualTo(150));
            Assert.That(
                target.GetStatus(BattleStatusId.Toxin).Value,
                Is.EqualTo(150));
            Assert.That(
                target.GetStatus(BattleStatusId.Toxin)
                    .ToxinApplications
                    .Select(application => application.AppliedValue),
                Is.EqualTo(new[] { 75, 75 }));
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
            Assert.That(
                state.LogEntries,
                Does.Contain($"{target.DisplayName}に150の冷気を与えた！"));
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

            var firstCharging = (BattleStatusInstance)logic.BeginStartup(
                new SkillExecutionContext(state, user, skill));
            var secondCharging = (BattleStatusInstance)logic.BeginStartup(
                new SkillExecutionContext(state, user, skill));

            Assert.That(
                user.GetChargeStatuses(ChargePhase.Charging)
                    .Select(status => status.Value),
                Is.EquivalentTo(new[] { 100, 50 }));
            Assert.That(
                user.GetChargeStatuses(ChargePhase.Charging).First()
                    .DisplayName,
                Does.StartWith("Charging"));
            Assert.That(
                user.GetChargeStatuses(ChargePhase.Charging).First()
                    .Description,
                Is.EqualTo("Defensive charge phase."));
            Assert.That(
                user.GetChargeStatuses(ChargePhase.Charging)
                    .All(status => !status.RemainingTicks.HasValue),
                Is.True);
            Assert.That(
                user.GetBattleStatValue(PachimonStatType.Electric),
                Is.EqualTo(25));
            Assert.That(
                user.GetBattleStatValue(PachimonStatType.ResistBonus),
                Is.EqualTo(70));

            logic.Resolve(new SkillExecutionContext(
                state,
                user,
                skill,
                secondCharging));

            Assert.That(
                user.GetChargeStatuses(ChargePhase.Charging).Single().Value,
                Is.EqualTo(100));
            Assert.That(
                user.GetChargeStatuses(ChargePhase.Charged).Single().Value,
                Is.EqualTo(50));
            Assert.That(
                user.GetChargeStatuses(ChargePhase.Charged).Single()
                    .DisplayName,
                Does.StartWith("Charged"));
            Assert.That(
                user.GetChargeStatuses(ChargePhase.Charged).Single()
                    .Description,
                Is.EqualTo("Offensive charge phase."));
            Assert.That(
                user.GetBattleStatValue(PachimonStatType.Electric),
                Is.EqualTo(75));
            Assert.That(
                user.GetBattleStatValue(PachimonStatType.ResistBonus),
                Is.EqualTo(50));
            Assert.That(
                user.GetBattleStatValue(PachimonStatType.Speed),
                Is.EqualTo(70));

            state.Timeline.AdvanceToTick(state.CurrentTick + 100);

            Assert.That(
                user.GetChargeStatuses(ChargePhase.Charging).Single(),
                Is.SameAs(firstCharging));
            Assert.That(
                user.GetChargeStatuses(ChargePhase.Charged),
                Is.Empty);

            logic.Resolve(new SkillExecutionContext(
                state,
                user,
                skill,
                firstCharging));

            Assert.That(
                user.Statuses.Single().StatusId,
                Is.EqualTo(BattleStatusId.Charge));
            Assert.That(
                ((ChargeStatusRuntimeState)user.Statuses.Single().RuntimeData)
                    .Phase,
                Is.EqualTo(ChargePhase.Charged));
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
                .BeginStartup(new SkillExecutionContext(state, user, skill));

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
        public void ItemInventory_TryAddAtPreservesConfiguredSlot()
        {
            var inventory = new ItemInventory();

            Assert.That(
                inventory.TryAddAt(4, 123, out var configuredItem),
                Is.True);
            Assert.That(inventory.GetAt(0), Is.Null);
            Assert.That(inventory.GetAt(4), Is.SameAs(configuredItem));

            Assert.That(
                inventory.TryAdd(456, out var addedItem, out var slotIndex),
                Is.True);
            Assert.That(slotIndex, Is.Zero);
            Assert.That(inventory.GetAt(0), Is.SameAs(addedItem));
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
        public void SkillMachine_DerivesSkillIdFromItemIdDuringAssetMigration()
        {
            var machine = ScriptableObject
                .CreateInstance<SkillMachineItemAsset>();
            machine.ConfigureForEditor(
                ItemIds.GetSkillMachineItemId(17),
                "Chain Burn Machine",
                null,
                string.Empty,
                ItemCategory.SkillMachine,
                5000);
            var catalog = ScriptableObject.CreateInstance<ItemCatalog>();
            catalog.SetItemsForEditor(new ItemAsset[] { machine });
            var target = new PachimonInstance(
                "chain_burn_target",
                1,
                AllocationType.Fire,
                1,
                1,
                CreateStats());
            var inventory = new ItemInventory();
            Assert.That(
                inventory.TryAdd(machine.ItemId, out var item, out _),
                Is.True);

            var result = new ItemUseService(catalog).TryUse(
                inventory,
                item.InstanceId,
                ItemUseContext.ForRun(
                    target,
                    target.MaxHp,
                    ItemTargetAffiliation.Ally));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(target.SkillIds.Last(), Is.EqualTo(17));
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

            state.Statuses.ApplyStatus(
                target,
                new BattleStatusInstance(
                    BattleStatusId.Leak,
                    BattleStatusCategory.Leak,
                    user,
                    value: 5));
            Assert.That(target.GetStatus(BattleStatusId.Leak)?.Value, Is.EqualTo(25));
        }

        [Test]
        public void Leak_TriggersOnlyFromPachimonAttackAndDamagesParty()
        {
            var source = CreateBattleUnitWithPassive(
                "player_1",
                BattleSide.Player,
                0,
                passiveId: 4);
            var enemies = CreateTestSide(BattleSide.Enemy);
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, source),
                enemies,
                new PassiveLogicRegistry());
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
                    applyDamageBonusMultiplier: false,
                    applyOutgoingModifiers: false));

            Assert.That(enemies.GetUnitAt(0).CurrentHp, Is.EqualTo(1870));
            Assert.That(enemies.GetUnitAt(1).CurrentHp, Is.EqualTo(1970));
            Assert.That(enemies.GetUnitAt(2).CurrentHp, Is.EqualTo(1970));
            Assert.That(enemies.GetUnitAt(0).Statuses, Is.Empty);
            Assert.That(
                enemies.GetUnitAt(1).GetStatus(BattleStatusId.Leak)?.Value,
                Is.EqualTo(50));
            Assert.That(
                state.LogEntries.Last(),
                Is.EqualTo($"{enemies.GetUnitAt(0).DisplayName}は漏電している！"));
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
        [TestCase(PoisonRunnerPassiveId)]
        [TestCase(HeatToxinPassiveId)]
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

        [Test]
        public void HeatToxin_AddsFireToPoisonInEveryContext()
        {
            var result = PachimonStatService.Calculate(
                CreateStats(
                    (PachimonStatType.Fire, 80),
                    (PachimonStatType.Poison, 20)),
                trainerModifiers: null,
                passiveIds: new[] { HeatToxinPassiveId },
                passiveRegistry: PassiveRegistry);

            var contribution = result.Calculation
                .GetContributions(PachimonStatType.Poison)
                .Single(item =>
                    item.Source.SourceId
                    == $"passive:{HeatToxinPassiveId}");

            Assert.That(contribution.Value, Is.EqualTo(80m));
            Assert.That(result.GetValue(PachimonStatType.Poison), Is.EqualTo(100));
        }

        [Test]
        public void PoisonRunner_AddsThirtyPercentOfPoisonToSpeed()
        {
            var result = PachimonStatService.Calculate(
                CreateStats(
                    (PachimonStatType.Poison, 80),
                    (PachimonStatType.Speed, 20)),
                trainerModifiers: null,
                passiveIds: new[] { PoisonRunnerPassiveId },
                passiveRegistry: PassiveRegistry);

            var contribution = result.Calculation
                .GetContributions(PachimonStatType.Speed)
                .Single(item =>
                    item.Source.SourceId
                    == $"passive:{PoisonRunnerPassiveId}");

            Assert.That(contribution.Value, Is.EqualTo(24m));
            Assert.That(result.GetValue(PachimonStatType.Speed), Is.EqualTo(44));
        }

        [Test]
        public void ChainTargetNavigator_BouncesAcrossLivingSlots()
        {
            var side = CreateTestSide(BattleSide.Enemy);
            var navigator = new ChainTargetNavigator(side);

            var slots = Enumerable.Range(0, 6)
                .Select(_ => navigator.GetNext().SlotIndex)
                .ToArray();

            Assert.That(slots, Is.EqualTo(new[] { 0, 1, 2, 1, 0, 1 }));
            Assert.That(
                Enumerable.Range(0, 6)
                    .Select(index => ChainTargetNavigator.GetDamageRatio(index, 5))
                    .ToArray(),
                Is.EqualTo(new[]
                {
                    1m,
                    5m / 6m,
                    4m / 6m,
                    3m / 6m,
                    2m / 6m,
                    1m / 6m,
                }));
        }

        [Test]
        public void ChainBurn_GainsHalfAddChainAndFloorsEffectiveCount()
        {
            var skill = CreateChainBurnSkill();
            var user = CreateBattleUnitWithPassive(
                "player_1",
                BattleSide.Player,
                0,
                passiveId: 2);
            var enemies = CreateTestSide(BattleSide.Enemy);
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, user),
                enemies);
            var logic = new ChainBurnSkillLogic(skill);

            state.Presentation.Begin(user, skill);
            var first = BattleSkillResolver.Resolve(state, user, skill, logic);
            var firstPresentation = state.Presentation.Complete();
            Assert.That(first.Effects.Count, Is.EqualTo(2));
            Assert.That(
                firstPresentation.BlockStyle,
                Is.EqualTo(BattlePresentationBlockStyle.Continuous));
            Assert.That(
                firstPresentation.Steps
                    .Where(step => step.Kind
                        == BattlePresentationStepKind.DamageApplied)
                    .Select(step => step.BlockIndex),
                Is.EqualTo(new[] { 0, 1 }));
            Assert.That(
                state.LogEntries.Any(entry => entry.Contains("アドチェイン")),
                Is.False);
            Assert.That(enemies.GetUnitAt(0).CurrentHp, Is.EqualTo(1920));
            Assert.That(enemies.GetUnitAt(1).CurrentHp, Is.EqualTo(1960));
            Assert.That(
                user.GetStatus(BattleStatusId.AddChain)?.Value,
                Is.EqualTo(50));
            Assert.That(AddChainRuntime.GetWholeChains(user), Is.Zero);

            BattleSkillResolver.Resolve(state, user, skill, logic);
            Assert.That(
                user.GetStatus(BattleStatusId.AddChain)?.Value,
                Is.EqualTo(100));
            Assert.That(AddChainRuntime.GetWholeChains(user), Is.EqualTo(1));

            var third = BattleSkillResolver.Resolve(state, user, skill, logic);
            Assert.That(third.Effects.Count, Is.EqualTo(3));
            Assert.That(
                user.GetStatus(BattleStatusId.AddChain)?.Value,
                Is.EqualTo(150));
            Assert.That(AddChainRuntime.GetWholeChains(user), Is.EqualTo(1));
            Assert.That(enemies.GetUnitAt(0).CurrentHp, Is.EqualTo(1760));
            Assert.That(enemies.GetUnitAt(1).CurrentHp, Is.EqualTo(1867));
            Assert.That(enemies.GetUnitAt(2).CurrentHp, Is.EqualTo(1974));
        }

        [Test]
        public void ComboMaster_UsesMaximumCompletedAdditionalChainCount()
        {
            var skill = CreateChainBurnSkill();
            var passive = CreateComboMasterPassive();
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            catalog.SetPassivesForEditor(new PassiveAsset[] { passive });
            var user = CreateBattleUnitWithPassive(
                "player_1",
                BattleSide.Player,
                0,
                passiveId: 17);
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, user),
                CreateTestSide(BattleSide.Enemy),
                new PassiveLogicRegistry(catalog));
            var logic = new ChainBurnSkillLogic(skill);

            BattleSkillResolver.Resolve(state, user, skill, logic);
            Assert.That(
                user.GetStatus(BattleStatusId.ComboMasterBonus)?.StackCount,
                Is.EqualTo(1));
            Assert.That(
                user.GetBattleStatValue(PachimonStatType.DamageBonus),
                Is.EqualTo(10));

            AddChainRuntime.AddUnits(user, user, 200);
            BattleSkillResolver.Resolve(state, user, skill, logic);

            Assert.That(
                user.GetStatus(BattleStatusId.ComboMasterBonus)?.StackCount,
                Is.EqualTo(3));
            Assert.That(
                user.GetBattleStatValue(PachimonStatType.DamageBonus),
                Is.EqualTo(30));
        }

        [Test]
        public void PositiveTemperature_ChangesOffenseButNotDefense()
        {
            var source = CreateBattleUnitWithStats(
                "sunny_source",
                BattleSide.Player,
                0,
                2000,
                1,
                (PachimonStatType.Fire, 100));
            var target = CreateBattleUnitWithStats(
                "sunny_target",
                BattleSide.Enemy,
                0,
                2000,
                1,
                (PachimonStatType.Fire, 100));
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, source),
                CreateTestSide(BattleSide.Enemy, target));
            var weather = CreateSunnyWeather();
            state.Weather.AddTemperature(source, weather, 500);

            Assert.That(
                state.ResolveAttributeRatio(PachimonAttribute.Fire, 100m),
                Is.EqualTo(150m));
            Assert.That(
                state.ResolveAttributeRatio(PachimonAttribute.Aqua, 100m),
                Is.EqualTo(50m));
            Assert.That(
                state.ResolveAttributeRatio(PachimonAttribute.Ice, 100m),
                Is.EqualTo(50m));
            Assert.That(
                target.GetBattleStatValue(PachimonStatType.Fire),
                Is.EqualTo(100));

            var result = BattleAttributeDamageService.Apply(
                state,
                source,
                target,
                new DamageContext(
                    DamageOriginKind.Skill,
                    1,
                    100,
                    source.GetBattleStats(),
                    target.GetBattleStats(),
                    PachimonAttribute.Fire,
                    isAttack: true,
                    applyOutgoingModifiers: false));

            Assert.That(result.FinalDamage, Is.EqualTo(125));
        }

        [Test]
        public void NegativeTemperature_ReducesFireAndRaisesIceUntilNeutralized()
        {
            var source = CreateBattleUnitWithStats(
                "cold_source",
                BattleSide.Player,
                0,
                2000,
                1);
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, source),
                CreateTestSide(BattleSide.Enemy));
            var temperature = CreateSunnyWeather();

            state.Weather.AddTemperature(source, temperature, -500);

            Assert.That(state.Weather.Temperature, Is.EqualTo(-500));
            Assert.That(
                state.ResolveAttributeRatio(PachimonAttribute.Fire, 100m),
                Is.EqualTo(50m));
            Assert.That(
                state.ResolveAttributeRatio(PachimonAttribute.Ice, 100m),
                Is.EqualTo(150m));
            Assert.That(
                state.ResolveAttributeRatio(PachimonAttribute.Aqua, 100m),
                Is.EqualTo(100m));

            state.Weather.AddTemperature(source, temperature, 500);

            Assert.That(state.Weather.Temperature, Is.Zero);
            Assert.That(state.Weather.Weather, Is.Empty);
            Assert.That(
                state.ResolveAttributeRatio(PachimonAttribute.Fire, 100m),
                Is.EqualTo(100m));
        }

        [Test]
        public void Warming_RecastPermanentlyAddsSelfAmplifiedTemperature()
        {
            var source = CreateBattleUnitWithStats(
                "sunny_source",
                BattleSide.Player,
                0,
                2000,
                49,
                (PachimonStatType.Fire, 100));
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, source),
                CreateTestSide(BattleSide.Enemy));
            var weather = CreateSunnyWeather();
            var skill = ScriptableObject.CreateInstance<SunnyDaySkillAsset>();
            skill.ConfigureForEditor(
                49,
                "Warming",
                100,
                300,
                100,
                string.Empty,
                400,
                100,
                weather);
            var logic = new SunnyDaySkillLogic(skill);

            BattleSkillResolver.Resolve(state, source, skill, logic);
            Assert.That(state.Weather.Weather.Single().Value, Is.EqualTo(500));
            BattleSkillResolver.Resolve(state, source, skill, logic);

            Assert.That(state.Weather.Weather.Single().Value, Is.EqualTo(1050));
            state.Timeline.AdvanceToTick(100);
            Assert.That(state.Weather.Temperature, Is.EqualTo(1050));
        }

        [Test]
        public void Cooling_RecastPermanentlyAddsSelfAmplifiedCold()
        {
            var source = CreateBattleUnitWithStats(
                "snow_source",
                BattleSide.Player,
                0,
                2000,
                30,
                (PachimonStatType.Ice, 100));
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, source),
                CreateTestSide(BattleSide.Enemy));
            var temperature = CreateSunnyWeather();
            var skill = ScriptableObject.CreateInstance<HeavySnowSkillAsset>();
            skill.ConfigureForEditor(
                30,
                "Cooling",
                100,
                300,
                100,
                string.Empty,
                400,
                100,
                temperature);
            var logic = new HeavySnowSkillLogic(skill);

            BattleSkillResolver.Resolve(state, source, skill, logic);
            Assert.That(state.Weather.Temperature, Is.EqualTo(-500));
            BattleSkillResolver.Resolve(state, source, skill, logic);

            Assert.That(state.Weather.Temperature, Is.EqualTo(-1050));
            state.Timeline.AdvanceToTick(100);
            Assert.That(state.Weather.Temperature, Is.EqualTo(-1050));
        }

        [Test]
        public void SunnyMan_RaisesSpeedOnlyWhileTemperatureIsPositive()
        {
            var passive = ScriptableObject.CreateInstance<SunnyManPassiveAsset>();
            passive.ConfigureForEditor(
                49,
                "Sunny Man",
                string.Empty,
                speedPercent: 130);
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            catalog.SetPassivesForEditor(new PassiveAsset[] { passive });
            var owner = CreateBattleUnitWithPassive(
                "sunny_owner",
                BattleSide.Player,
                0,
                49,
                (PachimonStatType.Speed, 100));
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, owner),
                CreateTestSide(BattleSide.Enemy),
                new PassiveLogicRegistry(catalog));
            var temperature = CreateSunnyWeather();
            state.Weather.AddTemperature(owner, temperature, 100);

            Assert.That(
                owner.GetBattleStatValue(PachimonStatType.Speed),
                Is.EqualTo(130));
            state.Timeline.AdvanceToTick(10);
            Assert.That(state.Weather.Temperature, Is.EqualTo(100));

            state.Weather.AddTemperature(owner, temperature, -200);
            Assert.That(
                owner.GetBattleStatValue(PachimonStatType.Speed),
                Is.EqualTo(100));
            state.Timeline.AdvanceToTick(20);
            Assert.That(state.Weather.Temperature, Is.EqualTo(-100));
        }

        [Test]
        public void RainDance_AddsDecayingRainAndAppliesRainRatios()
        {
            var source = CreateBattleUnitWithStats(
                "rain_source",
                BattleSide.Player,
                0,
                2000,
                18,
                (PachimonStatType.Aqua, 100));
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, source),
                CreateTestSide(BattleSide.Enemy));
            var rain = CreateRainWeather();
            var skill = ScriptableObject.CreateInstance<RainDanceSkillAsset>();
            skill.ConfigureForEditor(
                18,
                "Rain Dance",
                100,
                300,
                100,
                string.Empty,
                400,
                100,
                rain);

            BattleSkillResolver.Resolve(
                state,
                source,
                skill,
                new RainDanceSkillLogic(skill));

            Assert.That(state.Weather.IsRaining, Is.True);
            Assert.That(
                state.Weather.Get(BattleWeatherId.Rain).Value,
                Is.EqualTo(500));
            Assert.That(
                state.ResolveAttributeRatio(PachimonAttribute.Aqua, 100m),
                Is.EqualTo(150m));
            Assert.That(
                state.ResolveAttributeRatio(PachimonAttribute.Fire, 100m),
                Is.EqualTo(50m));

            state.Timeline.AdvanceToTick(100);
            Assert.That(
                state.Weather.Get(BattleWeatherId.Rain).Value,
                Is.EqualTo(400));
        }

        [Test]
        public void WindStorm_AmplifiesWindSpeedAndRainEffects()
        {
            var source = CreateBattleUnitWithStats(
                "wind_source",
                BattleSide.Player,
                0,
                2000,
                47,
                (PachimonStatType.Wind, 100),
                (PachimonStatType.Speed, 100));
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, source),
                CreateTestSide(BattleSide.Enemy));
            var wind = CreateWindWeather();
            var skill = ScriptableObject.CreateInstance<WindStormSkillAsset>();
            skill.ConfigureForEditor(
                47,
                "Wind Storm",
                100,
                300,
                100,
                string.Empty,
                400,
                100,
                wind);

            BattleSkillResolver.Resolve(
                state,
                source,
                skill,
                new WindStormSkillLogic(skill));

            Assert.That(
                state.Weather.Get(BattleWeatherId.Wind).Value,
                Is.EqualTo(500));
            Assert.That(
                state.ResolveAttributeRatio(PachimonAttribute.Wind, 100m),
                Is.EqualTo(150m));
            Assert.That(
                source.GetBattleStatValue(PachimonStatType.Speed),
                Is.EqualTo(120));

            state.Weather.CreateOrAdd(source, CreateRainWeather(), 500);

            Assert.That(state.Weather.GetEffectiveRainValue(), Is.EqualTo(750m));
            Assert.That(
                state.ResolveAttributeRatio(PachimonAttribute.Aqua, 100m),
                Is.EqualTo(175m));
        }

        [Test]
        public void WeatherChild_AddsDamageBonusPerActiveWeatherType()
        {
            var passive = ScriptableObject.CreateInstance<WeatherChildPassiveAsset>();
            passive.ConfigureForEditor(
                47,
                "Weather Child",
                string.Empty,
                damageBonusPerWeather: 20);
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            catalog.SetPassivesForEditor(new PassiveAsset[] { passive });
            var owner = CreateBattleUnitWithPassive(
                "weather_child",
                BattleSide.Player,
                0,
                47,
                (PachimonStatType.DamageBonus, 0));
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, owner),
                CreateTestSide(BattleSide.Enemy),
                new PassiveLogicRegistry(catalog));

            Assert.That(state.Weather.ActiveWeatherTypeCount, Is.Zero);
            Assert.That(
                owner.GetBattleStatValue(PachimonStatType.DamageBonus),
                Is.Zero);

            var temperature = CreateSunnyWeather();
            state.Weather.AddTemperature(owner, temperature, 100);
            Assert.That(state.Weather.ActiveWeatherTypeCount, Is.EqualTo(1));
            Assert.That(
                owner.GetBattleStatValue(PachimonStatType.DamageBonus),
                Is.EqualTo(20));

            state.Weather.CreateOrAdd(owner, CreateRainWeather(), 500);
            state.Weather.CreateOrAdd(owner, CreateWindWeather(), 500);
            Assert.That(state.Weather.ActiveWeatherTypeCount, Is.EqualTo(3));
            Assert.That(
                owner.GetBattleStatValue(PachimonStatType.DamageBonus),
                Is.EqualTo(60));

            state.Weather.AddTemperature(owner, temperature, -100);
            Assert.That(state.Weather.ActiveWeatherTypeCount, Is.EqualTo(2));
            Assert.That(
                owner.GetBattleStatValue(PachimonStatType.DamageBonus),
                Is.EqualTo(40));
        }

        [Test]
        public void TeamWindDamage_IncreasesAlliedWindDamageWhileOwnerLives()
        {
            var passive = ScriptableObject
                .CreateInstance<TeamAttributeDamagePassiveAsset>();
            passive.ConfigureForEditor(
                31,
                "Team Wind Damage",
                string.Empty,
                PachimonAttribute.Wind,
                damagePercent: 115);
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            catalog.SetPassivesForEditor(new PassiveAsset[] { passive });
            var owner = CreateBattleUnitWithPassive(
                "wind_support",
                BattleSide.Player,
                0,
                31);
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, owner),
                CreateTestSide(BattleSide.Enemy),
                new PassiveLogicRegistry(catalog));
            var ally = state.Player.GetUnitAt(1);
            var enemy = state.Enemy.GetUnitAt(0);

            var boosted = ApplyUnscaledAttributeDamage(
                state,
                ally,
                enemy,
                PachimonAttribute.Wind);
            Assert.That(boosted.FinalDamage, Is.EqualTo(115));

            var enemyAttack = ApplyUnscaledAttributeDamage(
                state,
                state.Enemy.GetUnitAt(1),
                state.Player.GetUnitAt(2),
                PachimonAttribute.Wind);
            Assert.That(enemyAttack.FinalDamage, Is.EqualTo(100));

            owner.ApplyDamage(owner.CurrentHp);
            var afterDefeat = ApplyUnscaledAttributeDamage(
                state,
                ally,
                state.Enemy.GetUnitAt(2),
                PachimonAttribute.Wind);
            Assert.That(afterDefeat.FinalDamage, Is.EqualTo(100));
        }

        [Test]
        public void ResistAdvantage_IncreasesDamageOnlyForPositiveDifference()
        {
            var passive = ScriptableObject
                .CreateInstance<ResistAdvantageDamagePassiveAsset>();
            passive.ConfigureForEditor(
                23,
                "Resist Advantage",
                string.Empty,
                resistDifferenceRatio: 30);
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            catalog.SetPassivesForEditor(new PassiveAsset[] { passive });
            var owner = CreateBattleUnitWithPassive(
                "resist_owner",
                BattleSide.Player,
                0,
                23,
                (PachimonStatType.ResistBonus, 200));
            var target = CreateBattleUnitWithStats(
                "resist_target",
                BattleSide.Enemy,
                0,
                2000,
                1,
                (PachimonStatType.ResistBonus, 100));
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, owner),
                CreateTestSide(BattleSide.Enemy, target),
                new PassiveLogicRegistry(catalog));

            var boosted = ApplyUnscaledAttributeDamage(
                state,
                owner,
                target,
                PachimonAttribute.Wind);
            Assert.That(boosted.FinalDamage, Is.EqualTo(65));

            var equalOwner = CreateBattleUnitWithPassive(
                "equal_owner",
                BattleSide.Player,
                0,
                23,
                (PachimonStatType.ResistBonus, 100));
            var equalTarget = CreateBattleUnitWithStats(
                "equal_target",
                BattleSide.Enemy,
                0,
                2000,
                1,
                (PachimonStatType.ResistBonus, 100));
            var equalState = new BattleState(
                124,
                CreateTestSide(BattleSide.Player, equalOwner),
                CreateTestSide(BattleSide.Enemy, equalTarget),
                new PassiveLogicRegistry(catalog));

            var unboosted = ApplyUnscaledAttributeDamage(
                equalState,
                equalOwner,
                equalTarget,
                PachimonAttribute.Wind);
            Assert.That(unboosted.FinalDamage, Is.EqualTo(50));
        }

        [Test]
        public void IncomingIceDamagePassive_ReducesOnlyIceDamage()
        {
            var passive = ScriptableObject
                .CreateInstance<IncomingAttributeDamagePassiveAsset>();
            passive.ConfigureForEditor(
                14,
                "Ice Damage Reduction",
                string.Empty,
                PachimonAttribute.Ice,
                damagePercent: 85);
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            catalog.SetPassivesForEditor(new PassiveAsset[] { passive });
            var target = CreateBattleUnitWithPassive(
                "ice_defender",
                BattleSide.Enemy,
                0,
                14);
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player),
                CreateTestSide(BattleSide.Enemy, target),
                new PassiveLogicRegistry(catalog));
            var source = state.Player.GetUnitAt(0);

            var iceDamage = ApplyUnscaledAttributeDamage(
                state,
                source,
                target,
                PachimonAttribute.Ice);
            Assert.That(iceDamage.FinalDamage, Is.EqualTo(85));

            var windDamage = ApplyUnscaledAttributeDamage(
                state,
                source,
                target,
                PachimonAttribute.Wind);
            Assert.That(windDamage.FinalDamage, Is.EqualTo(100));
        }

        [Test]
        public void IceShield_AppliesIceScaledShieldToFrontLivingAlly()
        {
            var skill = ScriptableObject.CreateInstance<IceShieldSkillAsset>();
            skill.ConfigureForEditor(
                14,
                "Ice Shield",
                100,
                300,
                100,
                string.Empty,
                baseShieldValue: 300,
                iceShieldRatio: 100);
            var defeatedFront = CreateBattleUnitWithStats(
                "defeated_front",
                BattleSide.Player,
                0,
                0,
                1);
            var livingFront = CreateBattleUnitWithStats(
                "living_front",
                BattleSide.Player,
                1,
                2000,
                1);
            var user = CreateBattleUnitWithStats(
                "ice_shield_user",
                BattleSide.Player,
                2,
                2000,
                14,
                (PachimonStatType.Ice, 100));
            var state = new BattleState(
                123,
                new BattleSideState(
                    BattleSide.Player,
                    new[] { defeatedFront, livingFront, user }),
                CreateTestSide(BattleSide.Enemy));

            BattleSkillResolver.Resolve(
                state,
                user,
                skill,
                new IceShieldSkillLogic(skill));

            Assert.That(defeatedFront.Shields, Is.Empty);
            Assert.That(livingFront.Shields.Single().Value, Is.EqualTo(600));
            Assert.That(user.Shields, Is.Empty);
        }

        [Test]
        public void IceShard_UsesDifferentDamageAndChillForFrontAndOthers()
        {
            var skill = ScriptableObject.CreateInstance<IceShardSkillAsset>();
            skill.ConfigureForEditor(
                22,
                "Ice Shard",
                100,
                300,
                150,
                string.Empty,
                frontBaseDamage: 100,
                frontDamageIceRatio: 100,
                frontBaseChill: 75,
                frontChillIceRatio: 100,
                otherBaseDamage: 50,
                otherDamageIceRatio: 100,
                otherBaseChill: 50,
                otherChillIceRatio: 100,
                chillStatus: ChillStatus);
            var user = CreateBattleUnitWithStats(
                "ice_shard_user",
                BattleSide.Player,
                0,
                2000,
                22,
                (PachimonStatType.Ice, 100));
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, user),
                CreateTestSide(BattleSide.Enemy));

            var resolution = BattleSkillResolver.Resolve(
                state,
                user,
                skill,
                new IceShardSkillLogic(skill));

            Assert.That(
                resolution.Effects.Select(effect => effect.Damage),
                Is.EqualTo(new[] { 200, 100, 100 }));
            Assert.That(
                state.Enemy.Units.Select(unit =>
                    unit.GetStatus(BattleStatusId.Chill)?.Value ?? 0),
                Is.EqualTo(new[] { 150, 100, 100 }));
            Assert.That(
                state.LogEntries,
                Is.EqualTo(state.Enemy.Units.Select((unit, index) =>
                    $"{unit.DisplayName}に{(index == 0 ? 150 : 100)}の冷気を与えた！")));
        }

        [Test]
        public void TargetSlowDamagePassive_UsesCombinedSlowValue()
        {
            var passive = ScriptableObject
                .CreateInstance<TargetSlowDamagePassiveAsset>();
            passive.ConfigureForEditor(
                22,
                "Target Slow Damage",
                string.Empty,
                slowRatio: 30);
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            catalog.SetPassivesForEditor(new PassiveAsset[] { passive });
            var owner = CreateBattleUnitWithPassive(
                "slow_attacker",
                BattleSide.Player,
                0,
                22);
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, owner),
                CreateTestSide(BattleSide.Enemy),
                new PassiveLogicRegistry(catalog));
            var slowedTarget = state.Enemy.GetUnitAt(0);
            state.Statuses.ApplyStatus(
                slowedTarget,
                BattleStatusFactory.CreateSlow(
                    owner,
                    60,
                    ParalysisStatus));
            state.Statuses.ApplyStatus(
                slowedTarget,
                BattleStatusFactory.CreateSlow(
                    owner,
                    40,
                    ChillStatus));

            Assert.That(
                slowedTarget.GetStatusCategoryValue(BattleStatusCategory.Slow),
                Is.EqualTo(100));
            var boosted = ApplyUnscaledAttributeDamage(
                state,
                owner,
                slowedTarget,
                PachimonAttribute.Ice);
            Assert.That(boosted.FinalDamage, Is.EqualTo(130));

            var unboosted = ApplyUnscaledAttributeDamage(
                state,
                owner,
                state.Enemy.GetUnitAt(1),
                PachimonAttribute.Ice);
            Assert.That(unboosted.FinalDamage, Is.EqualTo(100));
        }

        [Test]
        public void TargetStatusDamagePassive_IncreasesDamageForStunnedTarget()
        {
            var passive = ScriptableObject
                .CreateInstance<TargetStatusDamagePassiveAsset>();
            passive.ConfigureForEditor(
                56,
                "Stun Target Damage",
                string.Empty,
                BattleStatusCategory.Stun,
                damagePercent: 130);
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            catalog.SetPassivesForEditor(new PassiveAsset[] { passive });
            var owner = CreateBattleUnitWithPassive(
                "stun_attacker",
                BattleSide.Player,
                0,
                56);
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, owner),
                CreateTestSide(BattleSide.Enemy),
                new PassiveLogicRegistry(catalog));
            var stunnedTarget = state.Enemy.GetUnitAt(0);
            state.Statuses.ApplyStatus(
                stunnedTarget,
                BattleStatusFactory.CreateStun(
                    owner,
                    100,
                    StunStatus));

            var boosted = ApplyUnscaledAttributeDamage(
                state,
                owner,
                stunnedTarget,
                PachimonAttribute.Dragon);
            Assert.That(boosted.FinalDamage, Is.EqualTo(130));

            var unboosted = ApplyUnscaledAttributeDamage(
                state,
                owner,
                state.Enemy.GetUnitAt(1),
                PachimonAttribute.Dragon);
            Assert.That(unboosted.FinalDamage, Is.EqualTo(100));
        }

        [Test]
        public void Rain_AddsValueToNormalLeakEachTick()
        {
            var source = CreateBattleUnitWithStats(
                "rain_source",
                BattleSide.Player,
                0,
                2000,
                1);
            var target = CreateBattleUnitWithStats(
                "rain_target",
                BattleSide.Enemy,
                0,
                2000,
                1);
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, source),
                CreateTestSide(BattleSide.Enemy, target));
            state.Statuses.ApplyStatus(
                target,
                new BattleStatusInstance(
                    BattleStatusId.Leak,
                    BattleStatusCategory.Leak,
                    source,
                    value: 25));
            state.Weather.CreateOrAdd(source, CreateRainWeather(), 500);

            Assert.That(target.GetStatus(BattleStatusId.Leak)?.Value, Is.EqualTo(25));

            state.Timeline.AdvanceToTick(100);
            Assert.That(target.GetStatus(BattleStatusId.Leak)?.Value, Is.EqualTo(56));
            state.Weather.AddTemperature(source, CreateSunnyWeather(), -100);
            Assert.That(state.Weather.IsSnowing, Is.True);
            Assert.That(
                state.Weather.Get(BattleWeatherId.Rain).DisplayName,
                Is.EqualTo("雪"));
            Assert.That(target.GetStatus(BattleStatusId.Leak)?.Value, Is.EqualTo(56));
            state.Weather.AddTemperature(source, CreateSunnyWeather(), 100);
            Assert.That(state.Weather.IsRaining, Is.True);
            Assert.That(
                state.Weather.Get(BattleWeatherId.Rain).DisplayName,
                Is.EqualTo("雨"));

            BattleAttributeDamageService.Apply(
                state,
                source,
                target,
                new DamageContext(
                    DamageOriginKind.Skill,
                    1,
                    10,
                    source.GetBattleStats(),
                    target.GetBattleStats(),
                    PachimonAttribute.Electric,
                    isAttack: true,
                    applyAttackerAttributeMultiplier: false,
                    applyDamageBonusMultiplier: false,
                    applyOutgoingModifiers: false));
            Assert.That(target.GetStatus(BattleStatusId.Leak), Is.Null);

            state.Timeline.AdvanceToTick(104);

            Assert.That(target.GetStatus(BattleStatusId.Leak)?.Value, Is.EqualTo(1));
        }

        [Test]
        public void Snow_AddsChillForTrueDamageButNotStatusDamage()
        {
            var source = CreateBattleUnitWithStats(
                "snow_source",
                BattleSide.Player,
                0,
                2000,
                1);
            var target = CreateBattleUnitWithStats(
                "snow_target",
                BattleSide.Enemy,
                0,
                2000,
                1);
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, source),
                CreateTestSide(BattleSide.Enemy, target));
            state.Weather.CreateOrAdd(source, CreateRainWeather(), 1000);
            state.Weather.AddTemperature(source, CreateSunnyWeather(), -100);

            Assert.That(state.Weather.IsSnowing, Is.True);
            Assert.That(target.GetStatus(BattleStatusId.Leak), Is.Null);
            BattleTrueDamageService.Apply(
                state,
                source,
                target,
                new TrueDamageContext(
                    DamageOriginKind.Skill,
                    1,
                    10,
                    isAttack: true));
            Assert.That(target.GetStatus(BattleStatusId.Chill)?.Value, Is.EqualTo(40));

            BattleStatusDamageService.Apply(
                state,
                target,
                BattleStatusId.Toxin,
                PachimonAttribute.Poison,
                10);
            Assert.That(target.GetStatus(BattleStatusId.Chill)?.Value, Is.EqualTo(40));
        }

        [Test]
        public void RainMan_RaisesSpeedFromRainValueButNotSnow()
        {
            var passive = ScriptableObject.CreateInstance<RainManPassiveAsset>();
            passive.ConfigureForEditor(
                18,
                "Rain Man",
                string.Empty,
                baseSpeedPercent: 100,
                rainValueRatio: 3);
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            catalog.SetPassivesForEditor(new PassiveAsset[] { passive });
            var owner = CreateBattleUnitWithPassive(
                "rain_owner",
                BattleSide.Player,
                0,
                18,
                (PachimonStatType.Speed, 100));
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, owner),
                CreateTestSide(BattleSide.Enemy),
                new PassiveLogicRegistry(catalog));
            state.Weather.CreateOrAdd(owner, CreateRainWeather(), 1000);

            Assert.That(
                owner.GetBattleStatValue(PachimonStatType.Speed),
                Is.EqualTo(130));

            state.Weather.AddTemperature(owner, CreateSunnyWeather(), -1);
            Assert.That(
                owner.GetBattleStatValue(PachimonStatType.Speed),
                Is.EqualTo(100));
        }

        private static SunnyWeatherAsset CreateSunnyWeather()
        {
            var weather = ScriptableObject.CreateInstance<SunnyWeatherAsset>();
            weather.ConfigureForEditor(
                "Temperature",
                string.Empty,
                fireRatioScalingPercent: 10,
                aquaRatioScalingPercent: 20,
                iceRatioScalingPercent: 20,
                coldFireRatioScalingPercent: 20,
                coldIceRatioScalingPercent: 10);
            return weather;
        }

        private static RainWeatherAsset CreateRainWeather()
        {
            var weather = ScriptableObject.CreateInstance<RainWeatherAsset>();
            weather.ConfigureForEditor(
                "Rain",
                string.Empty,
                aquaRatioScalingPercent: 10,
                fireRatioScalingPercent: 20,
                leakValueRatioPerTick: 7,
                snowChillBaseValue: 20,
                snowChillTemperatureRatio: 100,
                ChillStatus);
            return weather;
        }

        private static WindWeatherAsset CreateWindWeather()
        {
            var weather = ScriptableObject.CreateInstance<WindWeatherAsset>();
            weather.ConfigureForEditor(
                "Wind",
                string.Empty,
                windRatioScalingPercent: 10,
                speedFromWindRatio: 20,
                rainEffectRatioScalingPercent: 10);
            return weather;
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

        private static ChainBurnSkillAsset CreateChainBurnSkill()
        {
            var skill = ScriptableObject.CreateInstance<ChainBurnSkillAsset>();
            skill.ConfigureForEditor(
                skillId: 17,
                displayName: "Chain Burn",
                baseRecoveryTicks: 130,
                baseCooldownTicks: 250,
                baseManaCost: 100,
                description: string.Empty,
                basePower: 80,
                fireScalingPercent: 100,
                baseChainCount: 1,
                addChainGainUnits: 50);
            return skill;
        }

        private static ComboMasterPassiveAsset CreateComboMasterPassive()
        {
            var passive = ScriptableObject
                .CreateInstance<ComboMasterPassiveAsset>();
            passive.ConfigureForEditor(
                passiveId: 17,
                displayName: "Combo Master",
                description: string.Empty,
                damageBonusPerChain: 10);
            return passive;
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
                baseStartupTicks: 300,
                baseRecoveryTicks: 0,
                baseCooldownTicks: 500,
                baseManaCost: 400,
                description: string.Empty,
                chargeStatus: ChargeStatus);
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
                iceBaseValue: 10,
                paralysisStatus: ParalysisStatus);
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
            skill.ConfigureStatusForEditor(
                statusBaseValue: 0,
                statusScalingPercent: 100,
                paralysisStatus: ParalysisStatus);
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
            skill.ConfigureStatusForEditor(
                statusBaseValue: 0,
                statusScalingPercent: 100,
                chillStatus: ChillStatus);
            return skill;
        }

        private static PlaceholderSkillAsset CreateBasicPoisonSkill()
        {
            var skill = ScriptableObject.CreateInstance<PlaceholderSkillAsset>();
            skill.ConfigureForEditor(
                skillId: 5,
                displayName: "Poison Needle",
                allocationType: AllocationType.Poison,
                isMapAssignable: true,
                baseRecoveryTicks: 100,
                baseCooldownTicks: 200,
                description: string.Empty);
            skill.ConfigureStatusForEditor(100, 100, ToxinStatus);
            return skill;
        }

        private static SmogSkillAsset CreateSmogSkill()
        {
            var skill = ScriptableObject.CreateInstance<SmogSkillAsset>();
            skill.ConfigureForEditor(
                skillId: 21,
                displayName: "Smog",
                baseRecoveryTicks: 100,
                baseCooldownTicks: 300,
                baseManaCost: 100,
                description: string.Empty,
                baseFieldValue: 300,
                poisonScalingPercent: 100,
                fieldEffect: SmogFieldEffect);
            return skill;
        }

        private static NeurotoxinSkillAsset CreateNeurotoxinSkill()
        {
            var skill = ScriptableObject.CreateInstance<NeurotoxinSkillAsset>();
            skill.ConfigureForEditor(
                skillId: 13,
                displayName: "Neurotoxin",
                baseRecoveryTicks: 100,
                baseCooldownTicks: 300,
                baseManaCost: 100,
                description: string.Empty,
                basePoisonStunTicks: 50,
                poisonStunScalingPercent: 100,
                baseElectricStunTicks: 50,
                electricStunScalingPercent: 100,
                baseToxinValue: 100,
                toxinScalingPercent: 100,
                toxinStatus: ToxinStatus,
                stunStatus: StunStatus);
            return skill;
        }

        private static ToxinTransferSkillAsset CreateToxinTransferSkill()
        {
            var skill = ScriptableObject.CreateInstance<ToxinTransferSkillAsset>();
            skill.ConfigureForEditor(
                skillId: 29,
                displayName: "Toxin Transfer",
                baseRecoveryTicks: 100,
                baseCooldownTicks: 300,
                baseManaCost: 100,
                description: string.Empty,
                removalPercent: 50,
                applicationPercent: 200);
            return skill;
        }

        private static ToxinExplosionSkillAsset CreateToxinExplosionSkill()
        {
            var skill = ScriptableObject.CreateInstance<ToxinExplosionSkillAsset>();
            skill.ConfigureForEditor(
                skillId: 37,
                displayName: "Toxin Explosion",
                baseRecoveryTicks: 100,
                baseCooldownTicks: 400,
                baseManaCost: 200,
                description: string.Empty,
                toxinConversionPercent: 100,
                basePoisonPower: 50,
                poisonScalingPercent: 100,
                baseFirePower: 50,
                fireScalingPercent: 100);
            return skill;
        }

        private static PoisonShieldSkillAsset CreatePoisonShieldSkill()
        {
            var skill = ScriptableObject.CreateInstance<PoisonShieldSkillAsset>();
            skill.ConfigureForEditor(
                skillId: 45,
                displayName: "Poison Shield",
                baseRecoveryTicks: 100,
                baseCooldownTicks: 300,
                baseManaCost: 100,
                description: string.Empty,
                baseShieldValue: 300,
                shieldPoisonScalingPercent: 100,
                baseToxinReductionPercent: 50,
                reductionPoisonScalingPercent: 100);
            return skill;
        }

        private static FieldValueAmplificationPassiveAsset
            CreateScienceCraftPassive()
        {
            var passive = ScriptableObject
                .CreateInstance<FieldValueAmplificationPassiveAsset>();
            passive.ConfigureForEditor(
                passiveId: 21,
                displayName: "科学工作",
                description: string.Empty,
                poisonScalingPercent: 30);
            return passive;
        }

        private static ToxinGrowthPassiveAsset CreateToxinGrowthPassive()
        {
            var passive = ScriptableObject
                .CreateInstance<ToxinGrowthPassiveAsset>();
            passive.ConfigureForEditor(
                passiveId: 13,
                displayName: "毒素適応",
                description: string.Empty,
                poisonPercentPerApplication: 10);
            return passive;
        }

        private static PoisonKnightPassiveAsset CreatePoisonKnightPassive()
        {
            var passive = ScriptableObject
                .CreateInstance<PoisonKnightPassiveAsset>();
            passive.ConfigureForEditor(
                passiveId: 45,
                displayName: "毒の騎士",
                description: string.Empty,
                baseSharePercent: 30,
                poisonScalingPercent: 100);
            return passive;
        }

        private static FireGrowthOnDamagePassiveAsset CreateBurningManPassive()
        {
            var passive = ScriptableObject
                .CreateInstance<FireGrowthOnDamagePassiveAsset>();
            passive.ConfigureForEditor(
                passiveId: 41,
                displayName: "燃える男",
                description: string.Empty,
                fireIncreasePerDamage: 20);
            return passive;
        }

        private static DarkFlamePassiveAsset CreateDarkFlamePassive()
        {
            var passive = ScriptableObject
                .CreateInstance<DarkFlamePassiveAsset>();
            passive.ConfigureForEditor(
                passiveId: 9,
                displayName: "闇の炎",
                description: string.Empty,
                baseConversionPercent: 20,
                poisonScalingPercent: 100);
            return passive;
        }

        private static FireArcherPassiveAsset CreateFireArcherPassive()
        {
            var passive = ScriptableObject
                .CreateInstance<FireArcherPassiveAsset>();
            passive.ConfigureForEditor(
                passiveId: 33,
                displayName: "ファイアアーチャー",
                description: string.Empty,
                missingHpPercent: 5,
                fireScalingPercent: 100);
            return passive;
        }

        [Test]
        public void FrozenGround_TransformsReducedChillWithoutReducingTwice()
        {
            var freeze = ScriptableObject.CreateInstance<FreezeStatusAsset>();
            freeze.ConfigureForEditor(
                "凍結",
                string.Empty,
                fireDamagePerDecay: 10);
            var field = ScriptableObject
                .CreateInstance<FrozenGroundFieldEffectAsset>();
            field.ConfigureForEditor(
                "氷の大地",
                string.Empty,
                iceValueRatio: 100,
                thresholdNumerator: 30000,
                thresholdOffset: 200,
                freezeStatus: freeze);
            var passive = ScriptableObject
                .CreateInstance<FrozenGroundPassiveAsset>();
            passive.ConfigureForEditor(
                passiveId: 30,
                displayName: "氷の大地",
                description: string.Empty,
                fieldEffect: field);
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            catalog.SetPassivesForEditor(new PassiveAsset[] { passive });
            var owner = CreateBattleUnitWithPassive(
                "player_1",
                BattleSide.Player,
                0,
                passive.PassiveId,
                (PachimonStatType.Ice, 100));
            var target = CreateBattleUnitWithStats(
                "enemy_1",
                BattleSide.Enemy,
                0,
                2000,
                1,
                (PachimonStatType.Ice, 100));
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, owner),
                CreateTestSide(BattleSide.Enemy, target),
                new PassiveLogicRegistry(catalog));

            state.Fields.CreateFrozenGround(target, field);

            Assert.That(state.Fields.Effects.Count, Is.EqualTo(1));
            Assert.That(state.Fields.Effects.Single().Value, Is.EqualTo(200));
            Assert.That(field.CalculateFreezeThreshold(100), Is.EqualTo(100));
            Assert.That(field.CalculateFreezeThreshold(200), Is.EqualTo(75));
            Assert.That(field.CalculateFreezeThreshold(300), Is.EqualTo(60));
            Assert.That(field.CalculateFreezeThreshold(1000), Is.EqualTo(25));

            state.Statuses.ApplyStatus(
                target,
                new BattleStatusInstance(
                    BattleStatusId.Chill,
                    BattleStatusCategory.Slow,
                    owner,
                    value: 200));

            Assert.That(target.GetStatus(BattleStatusId.Chill), Is.Null);
            Assert.That(
                target.GetStatus(BattleStatusId.Freeze)?.Value,
                Is.EqualTo(100));

            state.Statuses.TryConsumeStatus(
                target,
                BattleStatusId.Freeze,
                out _);
            state.Statuses.ApplyStatus(
                target,
                BattleStatusFactory.CreateFreeze(owner, 100, freeze));
            Assert.That(
                target.GetStatus(BattleStatusId.Freeze)?.Value,
                Is.EqualTo(50));

            ApplyUnscaledAttributeDamage(
                state,
                owner,
                target,
                PachimonAttribute.Fire);
            Assert.That(
                target.GetStatus(BattleStatusId.Freeze)?.Value,
                Is.EqualTo(40));
        }

        [Test]
        public void IceGrowthPassive_GainsIceFromAttackAndStatusDamage()
        {
            var passive = ScriptableObject
                .CreateInstance<IceGrowthOnDamagePassiveAsset>();
            passive.ConfigureForEditor(
                passiveId: 38,
                displayName: "氷005",
                description: string.Empty,
                iceIncreasePerDamage: 10);
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            catalog.SetPassivesForEditor(new PassiveAsset[] { passive });
            var owner = CreateBattleUnitWithPassive(
                "player_1",
                BattleSide.Player,
                0,
                passive.PassiveId,
                (PachimonStatType.Ice, 100));
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, owner),
                CreateTestSide(BattleSide.Enemy),
                new PassiveLogicRegistry(catalog));
            var target = state.Enemy.GetUnitAt(0);

            ApplyUnscaledAttributeDamage(
                state,
                owner,
                target,
                PachimonAttribute.Ice);
            Assert.That(
                owner.GetBattleStatValue(PachimonStatType.Ice),
                Is.EqualTo(110));

            BattleStatusDamageService.ApplyAttribute(
                state,
                target,
                BattleStatusId.Chill,
                PachimonAttribute.Ice,
                baseDamage: 100m);
            Assert.That(
                owner.GetBattleStatValue(PachimonStatType.Ice),
                Is.EqualTo(120));

            ApplyUnscaledAttributeDamage(
                state,
                owner,
                target,
                PachimonAttribute.Fire);
            Assert.That(
                owner.GetBattleStatValue(PachimonStatType.Ice),
                Is.EqualTo(120));
        }

        [Test]
        public void IceBlade_UsesReducedChillAndAddsRecastDuration()
        {
            var field = ScriptableObject
                .CreateInstance<IceBladeFieldEffectAsset>();
            field.ConfigureForEditor("氷の刃", string.Empty);
            var skill = ScriptableObject.CreateInstance<IceBladeSkillAsset>();
            skill.ConfigureForEditor(
                skillId: 38,
                displayName: "氷の刃",
                baseRecoveryTicks: 100,
                baseCooldownTicks: 300,
                baseManaCost: 100,
                description: string.Empty,
                baseDurationTicks: 200,
                scalingDurationTicks: 100,
                iceDurationRatio: 100,
                fieldEffect: field);
            Assert.That(IceBladeSkillLogic.CalculateDuration(skill, 0),
                Is.EqualTo(300));
            Assert.That(IceBladeSkillLogic.CalculateDuration(skill, 100),
                Is.EqualTo(400));
            Assert.That(IceBladeSkillLogic.CalculateDuration(skill, 200),
                Is.EqualTo(500));

            var source = CreateBattleUnitWithStats(
                "player_1",
                BattleSide.Player,
                0,
                2000,
                skill.SkillId);
            var target = CreateBattleUnitWithStats(
                "enemy_1",
                BattleSide.Enemy,
                0,
                2000,
                1,
                (PachimonStatType.Ice, 100));
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, source),
                CreateTestSide(BattleSide.Enemy, target));
            state.Fields.CreateOrAddIceBlade(source, field, 300);
            state.Fields.CreateOrAddIceBlade(source, field, 100);

            state.Presentation.Begin(source, skill);
            state.Statuses.ApplyAttackStatus(
                target,
                new BattleStatusInstance(
                    BattleStatusId.Chill,
                    BattleStatusCategory.Slow,
                    source,
                    value: 200));
            var presentation = state.Presentation.Complete();

            var blade = state.Fields.Effects.Single(effect =>
                effect.EffectId == BattleFieldEffectId.IceBlade);
            Assert.That(blade.RemainingTicks, Is.EqualTo(400));
            Assert.That(target.GetStatus(BattleStatusId.Chill)?.Value,
                Is.EqualTo(100));
            Assert.That(target.CurrentHp, Is.EqualTo(1950));
            Assert.That(
                presentation.Steps.Select(step => step.Kind),
                Is.EqualTo(new[]
                {
                    BattlePresentationStepKind.PassiveTriggered,
                    BattlePresentationStepKind.PassiveTriggered,
                    BattlePresentationStepKind.DamageApplied,
                }));
            Assert.That(
                presentation.Steps[0].Text,
                Is.EqualTo($"{target.DisplayName}に100の冷気を与えた！"));
            Assert.That(presentation.Steps[1].Text, Is.EqualTo("氷の刃の攻撃！"));
        }

        [Test]
        public void IceWitch_DistributesDamageAndChainsFromDefeatedTargets()
        {
            var passive = ScriptableObject
                .CreateInstance<IceWitchPassiveAsset>();
            passive.ConfigureForEditor(
                passiveId: 46,
                displayName: "氷の魔女",
                description: string.Empty,
                baseIceDamage: 200,
                iceDamageRatio: 100);
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            catalog.SetPassivesForEditor(new PassiveAsset[] { passive });
            var owner = CreateBattleUnitWithPassive(
                "player_1",
                BattleSide.Player,
                0,
                passive.PassiveId,
                (PachimonStatType.Ice, 100));
            var defeated = CreateBattleUnitWithStats(
                "enemy_1",
                BattleSide.Enemy,
                0,
                100,
                1);
            var firstTarget = CreateBattleUnitWithStats(
                "enemy_2",
                BattleSide.Enemy,
                1,
                100,
                2);
            var lastTarget = CreateBattleUnitWithStats(
                "enemy_3",
                BattleSide.Enemy,
                2,
                1000,
                3);
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, owner),
                new BattleSideState(
                    BattleSide.Enemy,
                    new[] { defeated, firstTarget, lastTarget }),
                new PassiveLogicRegistry(catalog));
            defeated.ApplyDamage(defeated.CurrentHp);

            state.Events.Publish(new UnitDefeatedEvent(
                state,
                owner,
                defeated));

            Assert.That(firstTarget.IsDefeated, Is.True);
            Assert.That(lastTarget.CurrentHp, Is.EqualTo(400));
            Assert.That(
                state.LogEntries.Count(entry => entry.EndsWith("の氷の魔女！")),
                Is.EqualTo(2));
        }

        [Test]
        public void FrozenBreak_LowHpLocksTargetingAndAccumulatesFractionalHealing()
        {
            var freeze = ScriptableObject.CreateInstance<FreezeStatusAsset>();
            var selfStatus = ScriptableObject
                .CreateInstance<FrozenBreakStatusAsset>();
            var skill = ScriptableObject.CreateInstance<FrozenBreakSkillAsset>();
            freeze.ConfigureForEditor("凍結", string.Empty, 10);
            selfStatus.ConfigureForEditor(
                "フローズンブレイク（セルフ）",
                string.Empty);
            skill.ConfigureForEditor(
                46,
                "フローズンブレイク",
                200,
                1,
                300,
                100,
                string.Empty,
                100,
                100,
                70,
                40,
                1,
                50,
                freeze,
                selfStatus);
            var user = CreateBattleUnitWithStats(
                "player_1",
                BattleSide.Player,
                0,
                500,
                46,
                (PachimonStatType.Ice, 100));
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, user),
                CreateTestSide(BattleSide.Enemy));

            new FrozenBreakSkillLogic(skill).Resolve(
                new SkillExecutionContext(state, user, skill));

            var status = user.GetStatus(BattleStatusId.FrozenBreakSelf);
            Assert.That(status, Is.Not.Null);
            Assert.That(status.RemainingTicks, Is.EqualTo(110));
            Assert.That(user.IsTargetable, Is.False);
            Assert.That(user.Timing.IsPaused, Is.True);

            state.Timeline.AdvanceToTick(2);

            Assert.That(user.CurrentHp, Is.EqualTo(503));
            Assert.That(status.RemainingTicks, Is.EqualTo(108));
        }

        [Test]
        public void FrozenBreak_HighHpDamagesAndAppliesTimedFreeze()
        {
            var freeze = ScriptableObject.CreateInstance<FreezeStatusAsset>();
            var selfStatus = ScriptableObject
                .CreateInstance<FrozenBreakStatusAsset>();
            var skill = ScriptableObject.CreateInstance<FrozenBreakSkillAsset>();
            freeze.ConfigureForEditor("凍結", string.Empty, 10);
            selfStatus.ConfigureForEditor(
                "フローズンブレイク（セルフ）",
                string.Empty);
            skill.ConfigureForEditor(
                46, "フローズンブレイク", 200, 1, 300, 100,
                string.Empty, 100, 100, 70, 40, 1, 50, freeze, selfStatus);
            var user = CreateBattleUnitWithStats(
                "player_1",
                BattleSide.Player,
                0,
                1000,
                46,
                (PachimonStatType.Ice, 100));
            var target = CreateBattleUnitWithStats(
                "enemy_1",
                BattleSide.Enemy,
                0,
                2000,
                1);
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, user),
                CreateTestSide(BattleSide.Enemy, target));

            new FrozenBreakSkillLogic(skill).Resolve(
                new SkillExecutionContext(state, user, skill));

            Assert.That(target.CurrentHp, Is.EqualTo(1800));
            Assert.That(
                target.GetStatus(BattleStatusId.Freeze)?.RemainingTicks,
                Is.EqualTo(110));
            Assert.That(
                state.LogEntries,
                Does.Contain($"{target.DisplayName}に110の凍結を与えた！"));
        }

        [Test]
        public void SkillResolver_RecordsUnavailableTargetWithoutThrowing()
        {
            var skill = ScriptableObject.CreateInstance<PlaceholderSkillAsset>();
            skill.ConfigureForEditor(
                1,
                "ひのこ",
                AllocationType.Fire,
                true,
                100,
                200,
                string.Empty);
            var user = CreateBattleUnitWithStats(
                "player_1", BattleSide.Player, 0, 2000, 1);
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, user),
                CreateTestSide(BattleSide.Enemy));
            foreach (var enemy in state.Enemy.Units)
            {
                enemy.ApplyOrReplaceStatus(new BattleStatusInstance(
                    BattleStatusId.FrozenBreakSelf,
                    BattleStatusCategory.Stun
                        | BattleStatusCategory.Untargetable,
                    enemy,
                    value: 0,
                    durationTicks: 10));
            }

            var result = BattleSkillResolver.Resolve(
                state,
                user,
                skill,
                new BasicAttributeDamageSkillLogic(PachimonAttribute.Fire));

            Assert.That(result.WasTargetUnavailable, Is.True);
            Assert.That(result.Effects, Is.Empty);
            Assert.That(state.LogEntries.Last(), Is.EqualTo("対象がいなかった！"));
        }

        [Test]
        public void OneTwo_ReducesRecoveryAndConsumesOnlyCapturedValue()
        {
            var status = ScriptableObject.CreateInstance<OneTwoStatusAsset>();
            var skill = ScriptableObject.CreateInstance<DragonJabSkillAsset>();
            try
            {
                status.ConfigureForEditor("ワン・ツー", string.Empty);
                skill.ConfigureForEditor(
                    16, "ドラゴンジャブ", 100, 250, 100, string.Empty,
                    100, 100, 30, status);
                var unit = CreateBattleUnitWithStats(
                    "dragon",
                    BattleSide.Player,
                    0,
                    2000,
                    16);
                unit.ApplyOrReplaceStatus(new BattleStatusInstance(
                    BattleStatusId.OneTwo,
                    BattleStatusCategory.None,
                    unit,
                    30,
                    definition: status));
                var state = new BattleState(
                    123,
                    new BattleSideState(BattleSide.Player, new[] { unit }),
                    CreateTestSide(BattleSide.Enemy));

                var timing = SkillTimingCalculator.CreatePlan(skill, unit, state);
                var snapshot = state.Statuses.CaptureSkillStatusConsumption(unit);
                state.Statuses.ApplyStatus(
                    unit,
                    new BattleStatusInstance(
                        BattleStatusId.OneTwo,
                        BattleStatusCategory.None,
                        unit,
                        20,
                        definition: status));
                state.Statuses.CompleteSkillStatusConsumption(unit, snapshot);

                Assert.That(
                    timing.RecoveryWork,
                    Is.EqualTo(10000m / 130m).Within(0.000001m));
                Assert.That(
                    unit.GetStatus(BattleStatusId.OneTwo)?.Value,
                    Is.EqualTo(20));
            }
            finally
            {
                Object.DestroyImmediate(skill);
                Object.DestroyImmediate(status);
            }
        }

        [Test]
        public void DragonBoxer_GainsOnDragonAndHalvesOnOtherAttribute()
        {
            var status = ScriptableObject
                .CreateInstance<DragonBoxerStatusAsset>();
            var passive = ScriptableObject
                .CreateInstance<DragonBoxerPassiveAsset>();
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            try
            {
                status.ConfigureForEditor("ドラゴンボクサー", string.Empty);
                passive.ConfigureForEditor(
                    16,
                    "ドラゴンボクサー",
                    string.Empty,
                    stackGain: 10,
                    damagePercentPerStack: 1,
                    status);
                catalog.SetPassivesForEditor(new PassiveAsset[] { passive });
                var source = CreateBattleUnitWithPassive(
                    "dragon",
                    BattleSide.Player,
                    0,
                    passive.PassiveId);
                var enemies = CreateTestSide(BattleSide.Enemy);
                var state = new BattleState(
                    123,
                    new BattleSideState(BattleSide.Player, new[] { source }),
                    enemies,
                    new PassiveLogicRegistry(catalog));
                var target = enemies.GetUnitAt(0);

                var first = ApplyUnscaledAttributeDamage(
                    state, source, target, PachimonAttribute.Dragon);
                var second = ApplyUnscaledAttributeDamage(
                    state, source, target, PachimonAttribute.Dragon);
                ApplyUnscaledAttributeDamage(
                    state, source, target, PachimonAttribute.Fire);

                Assert.That(first.FinalDamage, Is.EqualTo(100));
                Assert.That(second.FinalDamage, Is.EqualTo(110));
                Assert.That(
                    source.GetStatus(BattleStatusId.DragonBoxer)?.StackCount,
                    Is.EqualTo(10));
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(passive);
                Object.DestroyImmediate(status);
            }
        }

        [Test]
        public void DragonFootwork_EvadesNextAttackAndSweetScienceAddsSpeed()
        {
            var footwork = ScriptableObject.CreateInstance<FootworkStatusAsset>();
            var speedStatus = ScriptableObject.CreateInstance<SweetScienceStatusAsset>();
            var skill = ScriptableObject.CreateInstance<DragonFootworkSkillAsset>();
            var passive = ScriptableObject.CreateInstance<SweetSciencePassiveAsset>();
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            try
            {
                footwork.ConfigureForEditor("フットワーク", string.Empty);
                speedStatus.ConfigureForEditor("スイートサイエンス", string.Empty);
                skill.ConfigureForEditor(
                    24,
                    "ドラゴンフットワーク",
                    100,
                    200,
                    0,
                    string.Empty,
                    footwork);
                passive.ConfigureForEditor(
                    24,
                    "スイートサイエンス",
                    string.Empty,
                    20,
                    speedStatus);
                catalog.SetPassivesForEditor(new PassiveAsset[] { passive });

                var player = CreateBattleUnitWithPassive(
                    "footwork",
                    BattleSide.Player,
                    0,
                    passive.PassiveId,
                    (PachimonStatType.Speed, 100));
                var enemies = CreateTestSide(BattleSide.Enemy);
                var state = new BattleState(
                    123,
                    new BattleSideState(BattleSide.Player, new[] { player }),
                    enemies,
                    new PassiveLogicRegistry(catalog));

                new DragonFootworkSkillLogic(skill).Resolve(
                    new SkillExecutionContext(state, player, skill));
                var damage = ApplyUnscaledAttributeDamage(
                    state,
                    enemies.GetUnitAt(0),
                    player,
                    PachimonAttribute.Fire);

                Assert.That(damage.FinalDamage, Is.Zero);
                Assert.That(player.CurrentHp, Is.EqualTo(2000));
                Assert.That(player.GetStatus(BattleStatusId.Footwork), Is.Null);
                Assert.That(
                    player.GetBattleStatValue(PachimonStatType.Speed),
                    Is.EqualTo(120));
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(passive);
                Object.DestroyImmediate(skill);
                Object.DestroyImmediate(speedStatus);
                Object.DestroyImmediate(footwork);
            }
        }

        [Test]
        public void DragonDance_WithDragonSkeletonUsesSameStageSnapshot()
        {
            var status = ScriptableObject.CreateInstance<DragonDanceStatusAsset>();
            var skill = ScriptableObject.CreateInstance<DragonDanceSkillAsset>();
            var passive = ScriptableObject.CreateInstance<DragonSkeletonPassiveAsset>();
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            try
            {
                status.ConfigureForEditor("龍の舞", string.Empty);
                skill.ConfigureForEditor(
                    32,
                    "龍の舞",
                    100,
                    200,
                    0,
                    string.Empty,
                    50,
                    20,
                    status);
                passive.ConfigureForEditor(
                    32,
                    "龍の骨格",
                    string.Empty,
                    20,
                    20);
                catalog.SetPassivesForEditor(new PassiveAsset[] { passive });

                var baseStats = CreateStats(
                    (PachimonStatType.MaxHp, 2000),
                    (PachimonStatType.MaxMn, 1000),
                    (PachimonStatType.Dragon, 100),
                    (PachimonStatType.Speed, 100));
                var startingStats = EffectivePachimonStats.Calculate(
                    baseStats,
                    new PassiveStatModifierRegistry(catalog)
                        .CreateModifiers(new[] { passive.PassiveId }));
                var player = new BattleUnitState(
                    "dragon_dancer",
                    32,
                    "dragon_dancer",
                    BattleSide.Player,
                    0,
                    startingStats,
                    2000,
                    1000,
                    new[] { new PachimonSkillSlot(1, skill.SkillId) },
                    new[] { passive.PassiveId });
                var state = new BattleState(
                    123,
                    new BattleSideState(BattleSide.Player, new[] { player }),
                    CreateTestSide(BattleSide.Enemy),
                    new PassiveLogicRegistry(catalog));

                Assert.That(
                    player.GetBattleStatValue(PachimonStatType.Dragon),
                    Is.EqualTo(120));
                Assert.That(
                    player.GetBattleStatValue(PachimonStatType.Speed),
                    Is.EqualTo(120));

                new DragonDanceSkillLogic(skill).Resolve(
                    new SkillExecutionContext(state, player, skill));

                Assert.That(
                    player.GetBattleStatValue(PachimonStatType.Dragon),
                    Is.EqualTo(174));
                Assert.That(
                    player.GetBattleStatValue(PachimonStatType.Speed),
                    Is.EqualTo(150));
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(passive);
                Object.DestroyImmediate(skill);
                Object.DestroyImmediate(status);
            }
        }

        [Test]
        public void DragonBreak_RemovesAllShieldsBeforeDamage()
        {
            var skill = ScriptableObject.CreateInstance<DragonBreakSkillAsset>();
            try
            {
                skill.ConfigureForEditor(
                    40, "ドラゴンブレイク", 120, 350, 100, string.Empty,
                    100, 100);
                var player = CreateBattleUnitWithStats(
                    "breaker", BattleSide.Player, 0, 2000, skill.SkillId);
                var target = CreateBattleUnitWithStats(
                    "shielded", BattleSide.Enemy, 0, 2000, 1);
                target.AddShield(300);
                target.AddShield(200);
                var state = new BattleState(
                    123,
                    new BattleSideState(BattleSide.Player, new[] { player }),
                    CreateTestSide(BattleSide.Enemy, target));

                new DragonBreakSkillLogic(skill).Resolve(
                    new SkillExecutionContext(state, player, skill));

                Assert.That(target.TotalShield, Is.Zero);
                Assert.That(target.CurrentHp, Is.EqualTo(1900));
            }
            finally
            {
                Object.DestroyImmediate(skill);
            }
        }

        [Test]
        public void DragonRage_AddsPenetrationFromDragon()
        {
            var passive = ScriptableObject.CreateInstance<DragonRagePassiveAsset>();
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            try
            {
                passive.ConfigureForEditor(
                    40, "龍の怒り", string.Empty, 20);
                catalog.SetPassivesForEditor(new PassiveAsset[] { passive });
                var source = CreateBattleUnitWithPassive(
                    "rage", BattleSide.Player, 0, passive.PassiveId,
                    (PachimonStatType.Dragon, 100));
                var target = CreateBattleUnitWithStats(
                    "defender", BattleSide.Enemy, 0, 2000, 1,
                    (PachimonStatType.Fire, 100),
                    (PachimonStatType.ResistBonus, 100));
                var state = new BattleState(
                    123,
                    new BattleSideState(BattleSide.Player, new[] { source }),
                    CreateTestSide(BattleSide.Enemy, target),
                    new PassiveLogicRegistry(catalog));

                var result = ApplyUnscaledAttributeDamage(
                    state, source, target, PachimonAttribute.Fire);

                Assert.That(result.FinalDamage, Is.EqualTo(30));
                Assert.That(
                    result.Calculation.Context.PenetrationPercent,
                    Is.EqualTo(20m));
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(passive);
            }
        }

        [Test]
        public void DragonCranker_StacksAndCombinesWithManyHitsUntilDragonDamage()
        {
            var status = ScriptableObject.CreateInstance<DragonCrankerStatusAsset>();
            var skill = ScriptableObject.CreateInstance<DragonHookSkillAsset>();
            var passive = ScriptableObject.CreateInstance<ManyHitsPassiveAsset>();
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            try
            {
                status.ConfigureForEditor("ドラゴンクランカー", string.Empty);
                skill.ConfigureForEditor(
                    48, "ドラゴンフック", 100, 300, 80, string.Empty,
                    100, 100, 30, 10, status);
                passive.ConfigureForEditor(
                    48, "滅多打ち", string.Empty, 150);
                catalog.SetPassivesForEditor(new PassiveAsset[] { passive });
                var source = CreateBattleUnitWithPassive(
                    "hook", BattleSide.Player, 0, passive.PassiveId,
                    (PachimonStatType.Dragon, 100));
                var target = CreateBattleUnitWithStats(
                    "target", BattleSide.Enemy, 0, 2000, 1);
                var state = new BattleState(
                    123,
                    new BattleSideState(BattleSide.Player, new[] { source }),
                    CreateTestSide(BattleSide.Enemy, target),
                    new PassiveLogicRegistry(catalog));

                new DragonHookSkillLogic(skill).Resolve(
                    new SkillExecutionContext(state, source, skill));
                Assert.That(
                    target.GetStatus(BattleStatusId.DragonCranker)?.Value,
                    Is.EqualTo(40));

                var fire = ApplyUnscaledAttributeDamage(
                    state, source, target, PachimonAttribute.Fire);
                Assert.That(fire.FinalDamage, Is.EqualTo(150));
                Assert.That(
                    target.GetStatus(BattleStatusId.DragonCranker)?.Value,
                    Is.EqualTo(40));

                var dragon = ApplyUnscaledAttributeDamage(
                    state, source, target, PachimonAttribute.Dragon);
                Assert.That(dragon.FinalDamage, Is.EqualTo(210));
                Assert.That(
                    target.GetStatus(BattleStatusId.DragonCranker),
                    Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(passive);
                Object.DestroyImmediate(skill);
                Object.DestroyImmediate(status);
            }
        }

        [Test]
        public void DragonUpper_KnockoutExtendsFromReceivedDamage()
        {
            var status = ScriptableObject.CreateInstance<KnockoutStatusAsset>();
            var skill = ScriptableObject.CreateInstance<DragonUpperSkillAsset>();
            try
            {
                status.ConfigureForEditor("ノックアウト", string.Empty, 10);
                skill.ConfigureForEditor(
                    56, "ドラゴンアッパー", 120, 400, 120, string.Empty,
                    100, 100, 200, status);
                var source = CreateBattleUnitWithStats(
                    "upper", BattleSide.Player, 0, 2000, skill.SkillId);
                var target = CreateBattleUnitWithStats(
                    "target", BattleSide.Enemy, 0, 2000, 1);
                var state = new BattleState(
                    123,
                    new BattleSideState(BattleSide.Player, new[] { source }),
                    CreateTestSide(BattleSide.Enemy, target));

                new DragonUpperSkillLogic(skill).Resolve(
                    new SkillExecutionContext(state, source, skill));
                var knockout = target.GetStatus(BattleStatusId.Knockout);
                Assert.That(knockout, Is.Not.Null);
                Assert.That(
                    (knockout.Categories & BattleStatusCategory.Stun) != 0,
                    Is.True);
                Assert.That(knockout.RemainingTicks, Is.EqualTo(200));

                ApplyUnscaledAttributeDamage(
                    state, source, target, PachimonAttribute.Fire);
                Assert.That(knockout.RemainingTicks, Is.EqualTo(210));
            }
            finally
            {
                Object.DestroyImmediate(skill);
                Object.DestroyImmediate(status);
            }
        }

        [Test]
        public void DragonDefense_RedirectsAttacksButNotStatusDamage()
        {
            var status = ScriptableObject.CreateInstance<DragonDefenseStatusAsset>();
            var skill = ScriptableObject.CreateInstance<DragonDefenseSkillAsset>();
            try
            {
                status.ConfigureForEditor("ドラゴンディフェンス", string.Empty);
                skill.ConfigureForEditor(
                    64, "ドラゴンディフェンス", 100, 400, 120,
                    string.Empty, 300, 100, 500, status);
                var intended = CreateBattleUnitWithStats(
                    "ally_front", BattleSide.Player, 0, 2000, 1);
                var protector = CreateBattleUnitWithStats(
                    "protector", BattleSide.Player, 1, 2000, skill.SkillId);
                var allyBack = CreateBattleUnitWithStats(
                    "ally_back", BattleSide.Player, 2, 2000, 1);
                var enemies = CreateTestSide(BattleSide.Enemy);
                var state = new BattleState(
                    123,
                    new BattleSideState(
                        BattleSide.Player,
                        new[] { intended, protector, allyBack }),
                    enemies);

                new DragonDefenseSkillLogic(skill).Resolve(
                    new SkillExecutionContext(state, protector, skill));
                var attack = ApplyUnscaledAttributeDamage(
                    state,
                    enemies.GetUnitAt(0),
                    intended,
                    PachimonAttribute.Fire);

                Assert.That(attack.ActualTarget, Is.SameAs(protector));
                Assert.That(intended.CurrentHp, Is.EqualTo(2000));
                Assert.That(protector.CurrentHp, Is.EqualTo(2000));
                Assert.That(protector.TotalShield, Is.EqualTo(200));

                BattleStatusDamageService.ApplyAttribute(
                    state,
                    intended,
                    BattleStatusId.Toxin,
                    PachimonAttribute.Poison,
                    100m);
                Assert.That(intended.CurrentHp, Is.EqualTo(1900));
                Assert.That(protector.TotalShield, Is.EqualTo(200));
            }
            finally
            {
                Object.DestroyImmediate(skill);
                Object.DestroyImmediate(status);
            }
        }

        [Test]
        public void DragonGuard_TracksDragonGainedDuringBattle()
        {
            var guard = ScriptableObject.CreateInstance<DragonGuardPassiveAsset>();
            var danceStatus = ScriptableObject.CreateInstance<DragonDanceStatusAsset>();
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            try
            {
                guard.ConfigureForEditor(64, "龍の守り", string.Empty, 20);
                danceStatus.ConfigureForEditor("龍の舞", string.Empty);
                catalog.SetPassivesForEditor(new PassiveAsset[] { guard });
                var startingStats = EffectivePachimonStats.Calculate(
                    CreateStats(
                        (PachimonStatType.MaxHp, 2000),
                        (PachimonStatType.MaxMn, 1000),
                        (PachimonStatType.Dragon, 100)),
                    new PassiveStatModifierRegistry(catalog)
                        .CreateModifiers(new[] { guard.PassiveId }));
                var unit = new BattleUnitState(
                    "guard", 64, "guard", BattleSide.Player, 0,
                    startingStats, 2000, 1000,
                    new[] { new PachimonSkillSlot(1, 1) },
                    new[] { guard.PassiveId });
                var state = new BattleState(
                    123,
                    new BattleSideState(BattleSide.Player, new[] { unit }),
                    CreateTestSide(BattleSide.Enemy),
                    new PassiveLogicRegistry(catalog));

                Assert.That(
                    unit.GetBattleStatValue(PachimonStatType.ResistBonus),
                    Is.EqualTo(20));
                state.Statuses.ApplyStatus(
                    unit,
                    new BattleStatusInstance(
                        BattleStatusId.DragonDance,
                        BattleStatusCategory.None,
                        unit,
                        0,
                        runtimeData: new DragonDanceRuntimeData(50, 0),
                        definition: danceStatus));
                Assert.That(
                    unit.GetBattleStatValue(PachimonStatType.Dragon),
                    Is.EqualTo(150));
                Assert.That(
                    unit.GetBattleStatValue(PachimonStatType.ResistBonus),
                    Is.EqualTo(30));
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(danceStatus);
                Object.DestroyImmediate(guard);
            }
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

        private static BattleDamageApplicationResult ApplyUnscaledAttributeDamage(
            BattleState state,
            BattleUnitState source,
            BattleUnitState target,
            PachimonAttribute attribute)
        {
            return BattleAttributeDamageService.Apply(
                state,
                source,
                target,
                new DamageContext(
                    DamageOriginKind.Skill,
                    1,
                    100,
                    source.GetBattleStats(),
                    target.GetBattleStats(),
                    attribute,
                    isAttack: true,
                    applyAttackerAttributeMultiplier: false,
                    applyDamageBonusMultiplier: false));
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

        [Test]
        public void FireBarrier_RecastAddsResourcesAndReplacesDefenseSnapshot()
        {
            var burn = ScriptableObject.CreateInstance<BurnStatusAsset>();
            var barrierDefinition = ScriptableObject
                .CreateInstance<FireBarrierFieldEffectAsset>();
            try
            {
                burn.ConfigureForEditor("火傷", "DamageBonusを減少する。");
                barrierDefinition.ConfigureForEditor(
                    "炎の障壁",
                    "攻撃を肩代わりする。",
                    valueHpRatio: 100,
                    valueDurationRatio: 100,
                    valueBurnRatio: 20,
                    defenseSnapshotRatio: 50,
                    burn);
                var first = CreateBattleUnitWithStats(
                    "first_generator",
                    BattleSide.Player,
                    0,
                    2000,
                    1,
                    (PachimonStatType.Fire, 100),
                    (PachimonStatType.ResistBonus, 100));
                var second = CreateBattleUnitWithStats(
                    "second_generator",
                    BattleSide.Player,
                    1,
                    2000,
                    1,
                    (PachimonStatType.Fire, 300),
                    (PachimonStatType.ResistBonus, 200));
                var enemy = CreateBattleUnitWithStats(
                    "enemy",
                    BattleSide.Enemy,
                    0,
                    2000,
                    1);
                var state = new BattleState(
                    123,
                    new BattleSideState(BattleSide.Player, new[] { first, second }),
                    new BattleSideState(BattleSide.Enemy, new[] { enemy }));

                var barrier = state.Fields.CreateOrAddFireBarrier(
                    first,
                    barrierDefinition,
                    100);
                var recast = state.Fields.CreateOrAddFireBarrier(
                    second,
                    barrierDefinition,
                    100);

                Assert.That(recast, Is.SameAs(barrier));
                Assert.That(barrier.Value, Is.EqualTo(200));
                Assert.That(barrier.CurrentHp, Is.EqualTo(200));
                Assert.That(barrier.MaxHp, Is.EqualTo(200));
                Assert.That(barrier.RemainingTicks, Is.EqualTo(200));
                Assert.That(barrier.Source, Is.SameAs(second));
                Assert.That(
                    barrier.DefenseSnapshot.GetAttribute(PachimonAttribute.Fire),
                    Is.EqualTo(150m));
                Assert.That(barrier.DefenseSnapshot.ResistBonus, Is.EqualTo(100m));
            }
            finally
            {
                Object.DestroyImmediate(barrierDefinition);
                Object.DestroyImmediate(burn);
            }
        }

        [Test]
        public void FireBarrier_NewBurnSurvivesUntilTheFollowingSkillResolves()
        {
            var burn = ScriptableObject.CreateInstance<BurnStatusAsset>();
            var barrierDefinition = ScriptableObject
                .CreateInstance<FireBarrierFieldEffectAsset>();
            try
            {
                burn.ConfigureForEditor("火傷", "DamageBonusを減少する。");
                barrierDefinition.ConfigureForEditor(
                    "炎の障壁",
                    "攻撃を肩代わりする。",
                    valueHpRatio: 100,
                    valueDurationRatio: 100,
                    valueBurnRatio: 20,
                    defenseSnapshotRatio: 50,
                    burn);
                var defender = CreateBattleUnitWithStats(
                    "defender",
                    BattleSide.Player,
                    0,
                    2000,
                    1,
                    (PachimonStatType.Fire, 100),
                    (PachimonStatType.ResistBonus, 100));
                var attacker = CreateBattleUnitWithStats(
                    "attacker",
                    BattleSide.Enemy,
                    0,
                    2000,
                    1);
                var state = new BattleState(
                    123,
                    new BattleSideState(BattleSide.Player, new[] { defender }),
                    new BattleSideState(BattleSide.Enemy, new[] { attacker }));
                state.Fields.CreateOrAddFireBarrier(
                    defender,
                    barrierDefinition,
                    100);
                var currentSkillConsumption = state.Statuses
                    .CaptureSkillStatusConsumption(attacker);

                var result = BattleAttributeDamageService.Apply(
                    state,
                    attacker,
                    defender,
                    new DamageContext(
                        DamageOriginKind.Skill,
                        originId: 1,
                        baseDamage: 450m,
                        attacker.GetBattleStats(),
                        defender.GetBattleStats(),
                        PachimonAttribute.Fire,
                        isAttack: true,
                        applyAttackerAttributeMultiplier: false,
                        applyDamageBonusMultiplier: false));

                Assert.That(result.FinalDamage, Is.EqualTo(25));
                Assert.That(defender.CurrentHp, Is.EqualTo(1975));
                Assert.That(state.Fields.Effects, Is.Empty);
                Assert.That(attacker.GetStatus(BattleStatusId.Burn)?.Value,
                    Is.EqualTo(20));
                Assert.That(attacker.GetBattleStats().DamageBonus, Is.EqualTo(-20));

                state.Statuses.CompleteSkillStatusConsumption(
                    attacker,
                    currentSkillConsumption);

                Assert.That(attacker.GetStatus(BattleStatusId.Burn)?.Value,
                    Is.EqualTo(20));
                Assert.That(attacker.GetBattleStats().DamageBonus, Is.EqualTo(-20));

                var followingSkillConsumption = state.Statuses
                    .CaptureSkillStatusConsumption(attacker);
                state.Statuses.CompleteSkillStatusConsumption(
                    attacker,
                    followingSkillConsumption);

                Assert.That(attacker.GetStatus(BattleStatusId.Burn), Is.Null);
                Assert.That(attacker.GetBattleStats().DamageBonus, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(barrierDefinition);
                Object.DestroyImmediate(burn);
            }
        }

        [Test]
        public void FireBarrier_FullAbsorptionDoesNotPresentZeroDamageToTarget()
        {
            var burn = ScriptableObject.CreateInstance<BurnStatusAsset>();
            var barrierDefinition = ScriptableObject
                .CreateInstance<FireBarrierFieldEffectAsset>();
            var skill = CreateBasicElectricSkill();
            try
            {
                burn.ConfigureForEditor("火傷", "DamageBonusを減少する。");
                barrierDefinition.ConfigureForEditor(
                    "炎の障壁",
                    "攻撃を肩代わりする。",
                    valueHpRatio: 100,
                    valueDurationRatio: 100,
                    valueBurnRatio: 20,
                    defenseSnapshotRatio: 50,
                    burn);
                var defender = CreateBattleUnitWithStats(
                    "defender",
                    BattleSide.Player,
                    0,
                    2000,
                    1);
                var attacker = CreateBattleUnitWithStats(
                    "attacker",
                    BattleSide.Enemy,
                    0,
                    2000,
                    1);
                var state = new BattleState(
                    123,
                    new BattleSideState(BattleSide.Player, new[] { defender }),
                    new BattleSideState(BattleSide.Enemy, new[] { attacker }));
                state.Fields.CreateOrAddFireBarrier(
                    defender,
                    barrierDefinition,
                    500);
                state.Presentation.Begin(attacker, skill);

                var result = BattleAttributeDamageService.Apply(
                    state,
                    attacker,
                    defender,
                    new DamageContext(
                        DamageOriginKind.Skill,
                        originId: skill.SkillId,
                        baseDamage: 100m,
                        attacker.GetBattleStats(),
                        defender.GetBattleStats(),
                        PachimonAttribute.Fire,
                        isAttack: true,
                        applyAttackerAttributeMultiplier: false,
                        applyDamageBonusMultiplier: false));
                var presentation = state.Presentation.Complete();

                Assert.That(result.FinalDamage, Is.Zero);
                Assert.That(result.AppliedDamage, Is.Zero);
                Assert.That(defender.CurrentHp, Is.EqualTo(2000));
                Assert.That(presentation.Steps.Any(step =>
                        step.Kind == BattlePresentationStepKind.DamageApplied
                        && step.FocusUnit == defender),
                    Is.False);
                Assert.That(presentation.Steps.Any(step =>
                        step.Text.Contains("炎の障壁")),
                    Is.True);
            }
            finally
            {
                Object.DestroyImmediate(skill);
                Object.DestroyImmediate(barrierDefinition);
                Object.DestroyImmediate(burn);
            }
        }

        private static ToxinStatusAsset ToxinStatus
        {
            get
            {
                if (_toxinStatus != null)
                {
                    return _toxinStatus;
                }

                _toxinStatus = ScriptableObject.CreateInstance<ToxinStatusAsset>();
                _toxinStatus.ConfigureForEditor(
                    "毒素",
                    "毎tickダメージを与えながら減衰する。",
                    damagePerTickRatio: 1,
                    decayPerTickRatio: 1);
                return _toxinStatus;
            }
        }

        private static StunStatusAsset StunStatus
        {
            get
            {
                if (_stunStatus != null)
                {
                    return _stunStatus;
                }

                _stunStatus = ScriptableObject.CreateInstance<StunStatusAsset>();
                _stunStatus.ConfigureForEditor("Stun", "行動を停止する。");
                return _stunStatus;
            }
        }

        private static SlowStatusAsset ParalysisStatus =>
            _paralysisStatus ??= CreateSlowStatus(
                BattleStatusId.Paralysis,
                "麻痺",
                usesAttributeDefense: true,
                PachimonAttribute.Electric);

        private static SlowStatusAsset ChillStatus =>
            _chillStatus ??= CreateSlowStatus(
                BattleStatusId.Chill,
                "冷気",
                usesAttributeDefense: true,
                PachimonAttribute.Ice);

        private static ChargeStatusAsset ChargeStatus
        {
            get
            {
                if (_chargeStatus != null)
                {
                    return _chargeStatus;
                }

                _chargeStatus = ScriptableObject
                    .CreateInstance<ChargeStatusAsset>();
                _chargeStatus.ConfigureForEditor(
                    displayName: "Charge",
                    description: "Stores Electric and changes phase over time.",
                    chargingDisplayName: "Charging",
                    chargingDescription: "Defensive charge phase.",
                    chargedDisplayName: "Charged",
                    chargedDescription: "Offensive charge phase.",
                    chargingResistBonusRatio: 40,
                    chargingElectricRatio: 50,
                    chargedDurationRatio: 200,
                    chargedElectricRatio: 150,
                    chargedSpeedRatio: 100);
                return _chargeStatus;
            }
        }

        private static SlowStatusAsset CreateSlowStatus(
            BattleStatusId statusId,
            string displayName,
            bool usesAttributeDefense,
            PachimonAttribute defenseAttribute)
        {
            var definition = ScriptableObject.CreateInstance<SlowStatusAsset>();
            definition.ConfigureForEditor(
                statusId,
                displayName,
                "Speedを減少する。",
                decayPerTick: 1,
                usesAttributeDefense,
                defenseAttribute);
            return definition;
        }

        private static SmogFieldEffectAsset SmogFieldEffect
        {
            get
            {
                if (_smogFieldEffect != null)
                {
                    return _smogFieldEffect;
                }

                _smogFieldEffect = ScriptableObject
                    .CreateInstance<SmogFieldEffectAsset>();
                _smogFieldEffect.ConfigureForEditor(
                    "スモッグ",
                    "毎tick、Valueの一部を敵陣へ毒素として付与する。",
                    toxinApplicationRatio: 1,
                    decayPerTickRatio: 1,
                    ToxinStatus);
                return _smogFieldEffect;
            }
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
                var heatToxin = CreateDerivedPassive(
                    HeatToxinPassiveId,
                    "熱毒",
                    PachimonStatType.Poison,
                    PachimonStatType.Fire,
                    percent: 100);
                var poisonRunner = CreateDerivedPassive(
                    PoisonRunnerPassiveId,
                    "毒走り",
                    PachimonStatType.Speed,
                    PachimonStatType.Poison,
                    percent: 30);
                _passiveCatalog = ScriptableObject.CreateInstance<PassiveCatalog>();
                _passiveCatalog.SetPassivesForEditor(
                    new PassiveAsset[]
                    {
                        hydro,
                        thermal,
                        wind,
                        poisonRunner,
                        heatToxin,
                    });
                return _passiveCatalog;
            }
        }

        private static DerivedAdditivePassiveAsset CreateGenerationPassive(
            int passiveId,
            string displayName,
            PachimonStatType referenceStat)
        {
            return CreateDerivedPassive(
                passiveId,
                displayName,
                PachimonStatType.Electric,
                referenceStat,
                percent: 30);
        }

        private static DerivedAdditivePassiveAsset CreateDerivedPassive(
            int passiveId,
            string displayName,
            PachimonStatType targetStat,
            PachimonStatType referenceStat,
            int percent)
        {
            var passive =
                ScriptableObject.CreateInstance<DerivedAdditivePassiveAsset>();
            passive.ConfigureForEditor(
                passiveId,
                displayName,
                string.Empty,
                targetStat,
                referenceStat,
                percent,
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
