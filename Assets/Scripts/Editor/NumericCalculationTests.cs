using NUnit.Framework;
using Pachimon.Battle;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Passives;
using Pachimon.Skills;
using Pachimon.Items;
using Pachimon.Data;
using Pachimon.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Pachimon.Editor.Tests
{
    public sealed class NumericCalculationTests
    {
        [TestCase(1000, 0, 1000d)]
        [TestCase(1000, 100, 2000d)]
        [TestCase(1000, -50, 666.6666666666666d)]
        public void PachimonDurability_UsesMaxHpAndSignedResistBonus(
            int maxHp,
            int resistBonus,
            double expected)
        {
            Assert.That(
                PachimonDurabilityCalculator.Calculate(maxHp, resistBonus),
                Is.EqualTo((decimal)expected).Within(0.000001m));
        }

        [Test]
        public void DamageLog_UsesAttributeIconAndColorForAttributeDamage()
        {
            var text = BattleDamageLogFormatter.FormatDamage(
                "Target",
                120,
                PachimonAttribute.Fire,
                isTrueDamage: false);

            StringAssert.Contains(
                "<sprite=\"AttributeIcons\" name=\"Fire\">",
                text);
            StringAssert.Contains(
                $"<color={RewardElementPalette.GetAttributeColorHex(PachimonAttribute.Fire)}>120</color>",
                text);
            StringAssert.EndsWith("のダメージ！", text);
        }

        [Test]
        public void DamageLog_KeepsTrueDamageWithoutAttributeDecoration()
        {
            var text = BattleDamageLogFormatter.FormatDamage(
                "Target",
                120,
                attribute: null,
                isTrueDamage: true);

            Assert.That(text, Is.EqualTo("Targetに120の確定ダメージ！"));
        }

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

        [Test]
        public void DescriptionTemplateFormatter_FormatsRichTokensAndValues()
        {
            var context = new DescriptionTemplateContext()
                .Set("damage", 250);

            var result = DescriptionTemplateFormatter.Format(
                "{icon:Fire}{color:Fire}{value:damage}{/color} "
                + "{term:FireDamage|炎ダメージ}{br}続き",
                context);

            StringAssert.Contains("<sprite=\"AttributeIcons\" name=\"Fire\">", result);
            StringAssert.Contains("<color=", result);
            StringAssert.Contains(">250</color>", result);
            StringAssert.Contains(
                "<link=\"term:FireDamage\"><u>炎ダメージ</u></link>",
                result);
            StringAssert.EndsWith("\n続き", result);
        }

        [Test]
        public void DescriptionTemplateFormatter_PreservesUnknownTokens()
        {
            Assert.That(
                DescriptionTemplateFormatter.Format("{value:missing}"),
                Is.EqualTo("{value:missing}"));
        }

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

        [Test]
        public void InitialAttributeDamage_IgnoresLegacySerializedRatio()
        {
            var skill = ScriptableObject.CreateInstance<EmberSkillAsset>();
            try
            {
                skill.ConfigureForEditor(
                    1,
                    "ひのこ",
                    100,
                    300,
                    20,
                    string.Empty,
                    200,
                    ratio: 25);

                Assert.That(
                    skill.DamageRatio,
                    Is.EqualTo(AttributeDamageRules.ScalingRatio));
                Assert.That(
                    SignedStatMath.ScaleFromBase(
                        skill.BaseDamage,
                        stat: 100,
                        skill.DamageRatio),
                    Is.EqualTo(400m));
            }
            finally
            {
                Object.DestroyImmediate(skill);
            }
        }

        [Test]
        public void FirstTouch_UsesFixedDamageRatioAndConfigurableToxinRatio()
        {
            var skill = ScriptableObject.CreateInstance<FirstTouchSkillAsset>();
            try
            {
                skill.ConfigureForEditor(
                    57,
                    "ファーストタッチ",
                    100,
                    300,
                    20,
                    "{value:damage}|{value:normalToxin}|"
                        + "{value:bonusDamage}|{value:toxin}",
                    baseDamage: 100,
                    baseNormalToxinValue: 50,
                    bonusBaseDamage: 150,
                    baseToxinValue: 150,
                    poisonRatio: 50,
                    toxinStatus: null);
                var owner = new PachimonPreviewContent(
                    null,
                    "test",
                    1000,
                    1000,
                    0,
                    1000,
                    1000,
                    new[]
                    {
                        new PachimonStatPreview(PachimonDisplayStat.Poison, 100),
                    },
                    null,
                    null,
                    null);

                Assert.That(
                    SkillDescriptionValueProviderRegistry.TryCreateContext(
                        skill,
                        owner,
                        out var context),
                    Is.True);
                Assert.That(
                    DescriptionTemplateFormatter.Format(skill.Description, context),
                    Is.EqualTo("200|75|300|225"));
            }
            finally
            {
                Object.DestroyImmediate(skill);
            }
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
        public void AttributeDamage_AllowsMultipleIndependentStunsOnTarget()
        {
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player),
                CreateTestSide(BattleSide.Enemy));
            var source = state.Player.GetUnitAt(0);
            var target = state.Enemy.GetUnitAt(0);
            state.Statuses.ApplyStatus(
                target,
                BattleStatusFactory.CreateStun(source, 30, StunStatus));
            state.Statuses.ApplyStatus(
                target,
                BattleStatusFactory.CreateStun(source, 60, StunStatus));

            Assert.That(
                target.GetStatuses(BattleStatusId.Stun).Count,
                Is.EqualTo(2));
            Assert.DoesNotThrow(() => ApplyUnscaledAttributeDamage(
                state,
                source,
                target,
                PachimonAttribute.Wind));
            Assert.That(target.CurrentHp, Is.EqualTo(1900));
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
        public void Paralysis_StacksIndependentlyAndKeepsValueUntilExpiry()
        {
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player),
                CreateTestSide(BattleSide.Enemy));
            var unit = state.Player.GetUnitAt(0);
            state.Statuses.ApplyStatus(
                unit,
                BattleStatusFactory.CreateSlow(
                    unit,
                    10,
                    ParalysisStatus,
                    durationTicks: 10));
            state.Statuses.ApplyStatus(
                unit,
                BattleStatusFactory.CreateSlow(
                    unit,
                    20,
                    ParalysisStatus,
                    durationTicks: 30));

            Assert.That(
                unit.GetStatusCategoryValue(BattleStatusCategory.Slow),
                Is.EqualTo(30));
            Assert.That(
                unit.Statuses.Count(status =>
                    status.StatusId == BattleStatusId.Paralysis),
                Is.EqualTo(2));

            state.Timeline.AdvanceToTick(state.CurrentTick + 10);

            Assert.That(
                unit.GetStatusCategoryValue(BattleStatusCategory.Slow),
                Is.EqualTo(20));
            Assert.That(
                unit.GetStatus(BattleStatusId.Paralysis).Value,
                Is.EqualTo(20));
            state.Timeline.AdvanceToTick(state.CurrentTick + 20);
            Assert.That(
                unit.GetStatus(BattleStatusId.Paralysis),
                Is.Null);
        }

        [Test]
        public void HostileStatusValue_UsesSourceMasteryAndTargetResistance()
        {
            var source = CreateBattleUnitWithStats(
                "status_source",
                BattleSide.Player,
                0,
                2000,
                1,
                (PachimonStatType.StatusMastery, 100));
            var target = CreateBattleUnitWithStats(
                "status_target",
                BattleSide.Enemy,
                0,
                2000,
                1,
                (PachimonStatType.StatusResistance, 100));
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, source),
                CreateTestSide(BattleSide.Enemy, target));

            state.Statuses.ApplyStatus(
                target,
                new BattleStatusInstance(
                    BattleStatusId.Slow,
                    BattleStatusCategory.Slow,
                    source,
                    value: 100));

            Assert.That(
                target.GetStatus(BattleStatusId.Slow).Value,
                Is.EqualTo(100));
        }

        [Test]
        public void Toxin_DealsOnePercentAndDecaysOneValuePerTick()
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
                BattleStatusFactory.CreateToxin(source, 200, ToxinStatus));

            state.Timeline.AdvanceToTick(state.CurrentTick + 1);

            var toxin = target.GetStatus(BattleStatusId.Toxin);
            Assert.That(target.CurrentHp, Is.EqualTo(1998));
            Assert.That(toxin.Value, Is.EqualTo(199));
            Assert.That(toxin.DamageWork, Is.Zero);
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
            Assert.That(toxin.Value, Is.EqualTo(74));
            Assert.That(toxin.DamageWork, Is.EqualTo(0.5m));
            Assert.That(toxin.ToxinApplications.Count, Is.EqualTo(2));
            Assert.That(
                toxin.ToxinApplications[1].SourceInstanceId,
                Is.EqualTo(secondSource.InstanceId));

            state.Timeline.AdvanceToTick(state.CurrentTick + 1);

            Assert.That(target.CurrentHp, Is.EqualTo(1999));
            Assert.That(toxin.Value, Is.EqualTo(73));
            Assert.That(toxin.DamageWork, Is.EqualTo(0.24m));
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
                enemies.GetUnitAt(0),
                BattleStatusFactory.CreateToxin(
                    source,
                    100,
                    ToxinStatus),
                StatusApplicationTag.OverTime);

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
        public void BurningMan_GainsFireFromNonDotDamageOnly()
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
                10,
                DamageTag.DamageOverTime);
            Assert.That(owner.GetBattleStatValue(PachimonStatType.Fire),
                Is.EqualTo(160));

            BattleStatusDamageService.Apply(
                state,
                owner,
                BattleStatusId.Leak,
                PachimonAttribute.Electric,
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

            Assert.That(smog.Value, Is.EqualTo(599));
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
                Is.EqualTo(75));
            Assert.That(
                back.GetStatus(BattleStatusId.Toxin).Value,
                Is.EqualTo(200));
            Assert.That(resolution.Presentation.Steps.Any(step =>
                step.Text == $"{back.DisplayName}に75tickのStunと200の毒素を与えた！"),
                Is.True);
        }

        [Test]
        public void ToxinTransfer_DistributesToBothOtherTargetsWhenValuesTie()
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
                Is.EqualTo(220));
            Assert.That(
                enemies.GetUnitAt(2).GetStatus(BattleStatusId.Toxin).Value,
                Is.EqualTo(220));
        }

        [Test]
        public void ToxinTransfer_WithTwoLivingEnemies_AppliesAllToOtherTarget()
        {
            var skill = CreateToxinTransferSkill();
            var source = CreateBattleUnitWithStats(
                "player_1",
                BattleSide.Player,
                0,
                2000,
                skill.SkillId,
                (PachimonStatType.Poison, 100));
            var enemies = CreateTestSide(BattleSide.Enemy);
            enemies.GetUnitAt(2).ApplyDamage(2000);
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, source),
                enemies);
            foreach (var enemy in enemies.GetAllLiving())
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
                Is.EqualTo(590));
        }

        [TestCase(0, 120)]
        [TestCase(100, 140)]
        public void ToxinTransfer_ApplicationPercentUsesScaledPoisonBonus(
            int poison,
            int expectedPercent)
        {
            var skill = CreateToxinTransferSkill();
            try
            {
                Assert.That(
                    ToxinTransferMath.CalculateApplicationPercent(skill, poison),
                    Is.EqualTo(expectedPercent));
            }
            finally
            {
                Object.DestroyImmediate(skill);
            }
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
                Is.EqualTo(290));
        }

        [Test]
        public void ToxinTransfer_WithoutExistingToxin_AppliesBaseToFront()
        {
            var skill = CreateToxinTransferSkill();
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

            BattleSkillResolver.Resolve(
                state,
                source,
                skill,
                new ToxinTransferSkillLogic(skill));

            Assert.That(
                enemies.GetUnitAt(0).GetStatus(BattleStatusId.Toxin).Value,
                Is.EqualTo(300));
            Assert.That(enemies.GetUnitAt(1).GetStatus(BattleStatusId.Toxin),
                Is.Null);
        }

        [Test]
        public void ToxinExplosion_ConsumesAllAndCreatesMainAndAoeHits()
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

            Assert.That(enemies.Units.All(enemy =>
                    enemy.GetStatus(BattleStatusId.Toxin) == null),
                Is.True);
            Assert.That(
                enemies.Units.All(enemy => enemy.CurrentHp == 1740),
                Is.True);
            Assert.That(
                resolution.Effects.Count(effect => effect.Damage == 200),
                Is.EqualTo(3));
            Assert.That(
                resolution.Effects.Count(effect => effect.Damage == 20),
                Is.EqualTo(9));
        }

        [Test]
        public void ToxinExplosion_WithoutToxin_DealsNoDamage()
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

            var resolution = BattleSkillResolver.Resolve(
                state,
                source,
                skill,
                new ToxinExplosionSkillLogic(skill));

            Assert.That(resolution.Effects, Is.Empty);
            Assert.That(
                enemies.Units.All(enemy => enemy.CurrentHp == 2000),
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
            Assert.That(source.Shields.Single().RemainingTicks,
                Is.EqualTo(80));
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
        public void WaterPulse_SpendsOnlyManaRequiredForItsOwnLethalDamage()
        {
            var skill = CreateWaterPulseSkill();
            try
            {
                var user = CreateBattleUnitWithStats(
                    "player_1", BattleSide.Player, 0, 2000, skill.SkillId,
                    (PachimonStatType.Aqua, 100));
                var target = CreateBattleUnitWithStats(
                    "enemy_1", BattleSide.Enemy, 0, 301, skill.SkillId);
                var state = new BattleState(
                    123,
                    CreateTestSide(BattleSide.Player, user),
                    CreateTestSide(BattleSide.Enemy, target));

                var plan = BattleSkillManaCostCalculator.CreatePlan(
                    state, user, skill);

                Assert.That(plan.Actual, Is.EqualTo(151));
                Assert.That(plan.Effective, Is.EqualTo(151m));
            }
            finally
            {
                Object.DestroyImmediate(skill);
            }
        }

        [Test]
        public void WaterPulse_RequiredManaIncludesTargetShield()
        {
            var skill = CreateWaterPulseSkill();
            try
            {
                var user = CreateBattleUnitWithStats(
                    "player_1", BattleSide.Player, 0, 2000, skill.SkillId,
                    (PachimonStatType.Aqua, 100));
                var target = CreateBattleUnitWithStats(
                    "enemy_1", BattleSide.Enemy, 0, 301, skill.SkillId);
                target.AddShield(100);
                var state = new BattleState(
                    123,
                    CreateTestSide(BattleSide.Player, user),
                    CreateTestSide(BattleSide.Enemy, target));

                var plan = BattleSkillManaCostCalculator.CreatePlan(
                    state, user, skill);

                Assert.That(plan.Actual, Is.EqualTo(201));
            }
            finally
            {
                Object.DestroyImmediate(skill);
            }
        }

        [Test]
        public void WaterPulse_SpendsAllManaWhenOwnDamageCannotDefeatTarget()
        {
            var skill = CreateWaterPulseSkill();
            try
            {
                var user = CreateBattleUnitWithStats(
                    "player_1", BattleSide.Player, 0, 2000, skill.SkillId);
                var target = CreateBattleUnitWithStats(
                    "enemy_1", BattleSide.Enemy, 0, 2000, skill.SkillId);
                target.AddShield(1);
                var state = new BattleState(
                    123,
                    CreateTestSide(BattleSide.Player, user),
                    CreateTestSide(BattleSide.Enemy, target));

                var plan = BattleSkillManaCostCalculator.CreatePlan(
                    state, user, skill);

                Assert.That(plan.Actual, Is.EqualTo(user.CurrentMn));
            }
            finally
            {
                Object.DestroyImmediate(skill);
            }
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
        public void Smog_RecastAddsValueAndPreservesApplicationWork()
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
            Assert.That(smog.Value, Is.EqualTo(74));
            Assert.That(smog.ApplicationWork, Is.EqualTo(0.5m));
            Assert.That(smog.DecayWork, Is.Zero);
            Assert.That(smog.Source, Is.SameAs(secondSource));
        }

        [Test]
        public void GenerationPower_AmplifiesInitialAndRecastFieldValue()
        {
            var source = CreateBattleUnitWithStats(
                "support_field_source",
                BattleSide.Player,
                0,
                2000,
                1,
                (PachimonStatType.Aqua, 100));
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, source),
                CreateTestSide(BattleSide.Enemy));

            var smog = state.Fields.CreateOrAddSmog(
                source,
                BattleSide.Enemy,
                SmogFieldEffect,
                100);
            state.Fields.CreateOrAddSmog(
                source,
                BattleSide.Enemy,
                SmogFieldEffect,
                100);

            Assert.That(
                source.GetBattleStatValue(PachimonStatType.GenerationPower),
                Is.EqualTo(100));
            Assert.That(smog.Value, Is.EqualTo(300));
        }

        [Test]
        public void GenerationPower_AmplifiesWeatherAndSignedTemperatureValue()
        {
            var source = CreateBattleUnitWithStats(
                "support_weather_source",
                BattleSide.Player,
                0,
                2000,
                1,
                (PachimonStatType.Aqua, 100));
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, source),
                CreateTestSide(BattleSide.Enemy));
            var rain = CreateRainWeather();
            var temperature = CreateSunnyWeather();
            try
            {
                state.Weather.CreateOrAdd(source, rain, 100);
                state.Weather.CreateOrAdd(source, rain, 100);
                state.Weather.AddTemperature(source, temperature, -100);

                Assert.That(
                    state.Weather.Get(BattleWeatherId.Rain).Value,
                    Is.EqualTo(300));
                Assert.That(state.Weather.Temperature, Is.EqualTo(-150));
            }
            finally
            {
                Object.DestroyImmediate(temperature);
                Object.DestroyImmediate(rain);
            }
        }

        [Test]
        public void SustainPower_AmplifiesHealingAndShieldWithoutAmplifyingGeneration()
        {
            var source = CreateBattleUnitWithStats(
                "sustain_source",
                BattleSide.Player,
                0,
                2000,
                1,
                (PachimonStatType.Wind, 100));
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, source),
                CreateTestSide(BattleSide.Enemy));

            source.ApplyDamage(500);
            var restored = state.SupportEffects.RestoreHp(source, source, 100);
            var shield = state.SupportEffects.ApplyShield(source, source, 100);
            var smog = state.Fields.CreateOrAddSmog(
                source,
                BattleSide.Enemy,
                SmogFieldEffect,
                100);

            Assert.That(
                source.GetBattleStatValue(PachimonStatType.SustainPower),
                Is.EqualTo(100));
            Assert.That(restored, Is.EqualTo(150));
            Assert.That(shield.Value, Is.EqualTo(150));
            Assert.That(smog.Value, Is.EqualTo(100));
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
        public void AttributeStatus_ReducedToZero_DoesNotAddToExistingValue()
        {
            var target = CreateBattleUnitWithStats(
                "player_1",
                BattleSide.Player,
                0,
                2000,
                1,
                (PachimonStatType.Fire, 100));
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, target),
                CreateTestSide(BattleSide.Enemy));
            var enemy = state.Enemy.GetUnitAt(0);

            state.Statuses.ApplyStatus(
                target,
                new BattleStatusInstance(
                    BattleStatusId.Burn,
                    BattleStatusCategory.Burn,
                    target,
                    value: 10));

            Assert.DoesNotThrow(() => state.Statuses.ApplyStatus(
                target,
                new BattleStatusInstance(
                    BattleStatusId.Burn,
                    BattleStatusCategory.Burn,
                    enemy,
                    value: 1)));
            Assert.That(
                target.GetStatus(BattleStatusId.Burn).Value,
                Is.EqualTo(10));
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
                Is.EqualTo(160));
            Assert.That(
                target.GetStatus(BattleStatusId.Paralysis).RemainingTicks,
                Is.EqualTo(100));
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

            Assert.That(
                ElectricShockMath.CalculateParalysisValue(source),
                Is.Zero);
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
                Is.EqualTo(160));
            Assert.That(
                target.GetStatus(BattleStatusId.Chill).Value,
                Is.EqualTo(150));
            Assert.That(
                target.GetStatusCategoryValue(BattleStatusCategory.Slow),
                Is.EqualTo(310));
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
                attacker.GetStatuses(BattleStatusId.Paralysis).Count,
                Is.EqualTo(2));
            Assert.That(
                attacker.GetStatusCategoryValue(BattleStatusCategory.Slow),
                Is.EqualTo(100));
            Assert.That(
                attacker.GetStatuses(BattleStatusId.Paralysis)
                    .All(status => status.RemainingTicks == 50),
                Is.True);

            ApplyTestElectricDamage(
                state,
                attacker,
                defender,
                DamageOriginKind.Status,
                isAttack: false);

            Assert.That(
                attacker.GetStatusCategoryValue(BattleStatusCategory.Slow),
                Is.EqualTo(100));
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
                Is.EqualTo(50));

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
                Is.EqualTo(50));

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
                Is.EqualTo(50));
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

            Assert.That((double)unrounded, Is.EqualTo(114.375).Within(0.000001));
            Assert.That(
                AttributeDamageCalculator.FinalizeNormalDamage(unrounded),
                Is.EqualTo(114));
        }

        [Test]
        public void Damage_AddsAttributeAndDamageBonusBeforeAmplification()
        {
            var result = AttributeDamageCalculator.Calculate(
                new DamageContext(
                    DamageOriginKind.Skill,
                    originId: 1,
                    baseDamage: 100m,
                    CreateEffectiveStatsWithoutBindings(
                        (PachimonStatType.Fire, 50),
                        (PachimonStatType.DamageBonus, 50)),
                    CreateEffectiveStatsWithoutBindings(),
                    PachimonAttribute.Fire,
                    isAttack: true));

            Assert.That(result.PreDefenseDamage, Is.EqualTo(200m));
            Assert.That(result.FinalDamage, Is.EqualTo(200));
        }

        [Test]
        public void Damage_AddsAttributeAndResistBonusBeforeReduction()
        {
            var result = AttributeDamageCalculator.Calculate(
                new DamageContext(
                    DamageOriginKind.Skill,
                    originId: 1,
                    baseDamage: 100m,
                    CreateEffectiveStatsWithoutBindings(),
                    CreateEffectiveStatsWithoutBindings(
                        (PachimonStatType.Fire, 50),
                        (PachimonStatType.ResistBonus, 50)),
                    PachimonAttribute.Fire,
                    isAttack: true));

            Assert.That(result.FinalDamage, Is.EqualTo(50));
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
                Is.EqualTo(33));
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
        public void DamageContext_AppliesEachPenetrationToItsDefenseStat()
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
                    penetration: new DamagePenetration(
                        attributePercentage: 20m,
                        resistBonusFixed: 20m)));

            Assert.That(result.PreDefenseDamage, Is.EqualTo(100m));
            Assert.That(result.EffectiveDefenderAttribute, Is.EqualTo(80m));
            Assert.That(result.EffectiveResistBonus, Is.EqualTo(80m));
            Assert.That(result.FinalDamage, Is.EqualTo(38));
        }

        [Test]
        public void DamageContext_FixedPenetrationCanMakeDefenseNegative()
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
                    penetration: new DamagePenetration(
                        attributeFixed: 120m,
                        resistBonusFixed: 120m)));

            Assert.That(result.EffectiveDefenderAttribute, Is.EqualTo(-20m));
            Assert.That(result.EffectiveResistBonus, Is.EqualTo(-20m));
            Assert.That(result.FinalDamage, Is.EqualTo(140));
        }

        [Test]
        public void PenetrationMath_UsesDiminishingPercentageAndIgnoresNegativeValue()
        {
            Assert.That(
                PenetrationMath.CalculateDiminishingPercentage(50m),
                Is.EqualTo(100m / 3m));
            Assert.That(
                PenetrationMath.CalculateDiminishingPercentage(-50m),
                Is.Zero);
        }

        [Test]
        public void PenetrationMath_CombinesPercentageSourcesMultiplicatively()
        {
            Assert.That(
                PenetrationMath.CombinePercentages(20m, 20m),
                Is.EqualTo(36m));
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
                baseDamage: 100,
                fireScalingPercent: 100,
                baseAttributeFixedPenetration: 10,
                poisonPenetrationRatio: 100);
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
                BackfireMath.CalculateAttributeFixedPenetration(
                    skill,
                    poison: 100),
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
                baseDamage: 100,
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
        public void Combustion_RepeatsWithoutAdditionalManaUntilEitherUnitFalls()
        {
            var skill = ScriptableObject.CreateInstance<CombustionSkillAsset>();
            skill.ConfigureForEditor(
                skillId: 41,
                displayName: "燃焼",
                baseRecoveryTicks: 100,
                baseCooldownTicks: 300,
                baseManaCost: 100,
                description: string.Empty,
                baseDamage: 100,
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

            Assert.That(resolution.Effects.Count, Is.EqualTo(16));
            Assert.That(enemies.GetUnitAt(0).CurrentHp, Is.Zero);
            Assert.That(enemies.GetUnitAt(1).CurrentHp, Is.EqualTo(2000));
            Assert.That(user.CurrentHp, Is.Zero);
            Assert.That(user.CurrentMn, Is.EqualTo(150));

            var selfDamage = new CombustionSkillLogic(skill)
                .CalculateSelfDamage(user);
            Assert.That(selfDamage.Context.OriginKind, Is.EqualTo(DamageOriginKind.Skill));
            Assert.That(selfDamage.Context.IsAttack, Is.True);
            Assert.That(selfDamage.Context.ApplyDamageBonusMultiplier, Is.True);
            Assert.That(selfDamage.Context.ApplyOutgoingModifiers, Is.True);
        }

        [Test]
        public void Combustion_PresentationKeepsDamageOrderWithoutRepeatMana()
        {
            var skill = ScriptableObject.CreateInstance<CombustionSkillAsset>();
            skill.ConfigureForEditor(
                skillId: 41,
                displayName: "燃焼",
                baseRecoveryTicks: 100,
                baseCooldownTicks: 300,
                baseManaCost: 100,
                description: string.Empty,
                baseDamage: 100,
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
            Assert.That(damageSteps.Length, Is.GreaterThanOrEqualTo(4));
            Assert.That(damageSteps.Length % 2, Is.Zero);
            Assert.That(damageSteps[0].FocusUnit, Is.SameAs(enemies.GetUnitAt(0)));
            Assert.That(damageSteps[1].FocusUnit, Is.SameAs(user));
            Assert.That(damageSteps[2].FocusUnit, Is.SameAs(enemies.GetUnitAt(0)));
            Assert.That(damageSteps[3].FocusUnit, Is.SameAs(user));
            Assert.That(damageSteps.Select(step => step.BlockIndex), Is.EqualTo(
                new[] { 0, 0, 1, 1 }));

            Assert.That(damageSteps
                    .SelectMany(step => step.Transitions)
                    .Where(transition => ReferenceEquals(transition.Unit, user))
                    .All(transition => transition.MnBefore == transition.MnAfter),
                Is.True);
            Assert.That(state.LogEntries.Count(entry =>
                    entry == $"{user.DisplayName}は燃焼している！"),
                Is.EqualTo(damageSteps.Length / 2 - 1));
            Assert.That(user.CurrentMn, Is.EqualTo(150));
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
                baseDamage: 50,
                electricScalingPercent: 100,
                firePenetrationRatio: 25);

            Assert.That(
                ElectricExplosionMath.CalculateBaseDamage(
                    skill,
                    electric: 100),
                Is.EqualTo(100m));
            Assert.That(
                ElectricExplosionMath.CalculateAttributePenetrationValue(
                    skill,
                    fire: 100),
                Is.EqualTo(25m));
            Assert.That(
                PenetrationMath.CalculateDiminishingPercentage(25m),
                Is.EqualTo(20m));
            Assert.That(skill.BaseManaCost, Is.EqualTo(130));
        }

        [Test]
        public void ElectricQuickAttack_UsesElectricDamageAndFireActionTiming()
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
                electricBaseDamage: 25,
                fireTimingPercent: 100);

            Assert.That(
                ElectricQuickAttackMath.CalculateElectricBaseDamage(
                    skill,
                    electric: 100),
                Is.EqualTo(50m));
            var fireTimingMultiplier =
                SkillTimingCalculator.CalculateFireTimingMultiplier(
                    skill,
                    fire: 100);
            Assert.That(fireTimingMultiplier, Is.EqualTo(0.5m));
            Assert.That(
                BattleTickMath.GetEffectiveStartup(
                    baseStartup: 60,
                    speed: 0,
                    skillMultiplier: fireTimingMultiplier),
                Is.EqualTo(30));
            Assert.That(
                BattleTickMath.GetEffectiveRecovery(
                    skill.BaseRecoveryTicks,
                    speed: 0,
                    skillMultiplier: fireTimingMultiplier),
                Is.EqualTo(30));
            Assert.That(
                BattleTickMath.GetEffectiveCooldown(
                    skill.BaseCooldownTicks,
                    haste: 0),
                Is.EqualTo(100));
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
        public void RecoveryItems_RestoreConfiguredFixedAmount()
        {
            var hpPotion = ScriptableObject.CreateInstance<HealingItemAsset>();
            var mnPotion = ScriptableObject.CreateInstance<HealingItemAsset>();
            try
            {
                hpPotion.ConfigureHealingForEditor(
                    RecoveryResourceType.Hp,
                    500,
                    false);
                mnPotion.ConfigureHealingForEditor(
                    RecoveryResourceType.Mn,
                    500,
                    false);
                var target = new PachimonInstance(
                    "recovery_target",
                    1,
                    AllocationType.Aqua,
                    1,
                    1,
                    CreateStats(
                        (PachimonStatType.MaxHp, 1000),
                        (PachimonStatType.MaxMn, 800)));
                target.SetCurrentHp(100);
                target.SetCurrentMn(100);
                var context = ItemUseContext.ForRun(
                    target,
                    1200,
                    900,
                    ItemTargetAffiliation.Ally);
                var logic = new HealingItemLogic();

                Assert.That(logic.Apply(hpPotion, context), Is.EqualTo(500));
                Assert.That(target.CurrentHp, Is.EqualTo(600));
                Assert.That(logic.Apply(mnPotion, context), Is.EqualTo(500));
                Assert.That(target.CurrentMn, Is.EqualTo(600));
            }
            finally
            {
                Object.DestroyImmediate(hpPotion);
                Object.DestroyImmediate(mnPotion);
            }
        }

        [Test]
        public void RecoveryItems_IgnoreSustainPowerDuringBattle()
        {
            var potion = ScriptableObject.CreateInstance<HealingItemAsset>();
            try
            {
                potion.ConfigureHealingForEditor(
                    RecoveryResourceType.Hp,
                    100,
                    false);
                var target = CreateBattleUnitWithStats(
                    "recovery_item_target",
                    BattleSide.Player,
                    0,
                    2000,
                    1,
                    (PachimonStatType.Wind, 100));
                var state = new BattleState(
                    123,
                    CreateTestSide(BattleSide.Player, target),
                    CreateTestSide(BattleSide.Enemy));
                target.ApplyDamage(500);
                var context = ItemUseContext.ForBattle(
                    target,
                    ItemTargetAffiliation.Ally,
                    battleState: state);

                var restored = new HealingItemLogic().Apply(potion, context);

                Assert.That(
                    target.GetBattleStatValue(PachimonStatType.SustainPower),
                    Is.EqualTo(100));
                Assert.That(restored, Is.EqualTo(100));
                Assert.That(target.CurrentHp, Is.EqualTo(1600));
            }
            finally
            {
                Object.DestroyImmediate(potion);
            }
        }

        [Test]
        public void PercentageRecoveryItem_RestoresHpAndMnFromEachMaximum()
        {
            var item = ScriptableObject.CreateInstance<HealingItemAsset>();
            try
            {
                item.ConfigureHealingForEditor(
                    RecoveryResourceType.HpAndMn,
                    100,
                    false,
                    valueMode: RecoveryValueMode.MaximumPercent);
                var target = new PachimonInstance(
                    "percentage_recovery_target",
                    1,
                    AllocationType.Aqua,
                    1,
                    1,
                    CreateStats(
                        (PachimonStatType.MaxHp, 1000),
                        (PachimonStatType.MaxMn, 800)));
                target.SetCurrentHp(100);
                target.SetCurrentMn(100);
                var context = ItemUseContext.ForRun(
                    target,
                    1000,
                    800,
                    ItemTargetAffiliation.Ally);
                var instance = new ItemInstance(
                    "percentage_recovery_item",
                    new GeneratedItemData(ItemIds.SuperRecovery, 75));

                var restored = new HealingItemLogic().Apply(
                    item,
                    instance,
                    context);

                Assert.That(restored, Is.EqualTo(1350));
                Assert.That(target.CurrentHp, Is.EqualTo(850));
                Assert.That(target.CurrentMn, Is.EqualTo(700));
            }
            finally
            {
                Object.DestroyImmediate(item);
            }
        }

        [Test]
        public void LeagueGateStock_ContainsRecoveryAndThreeEngravingsPerStat()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(
                "Assets/GameData/Item/ItemCatalog.asset");

            var stock = new LeagueGateStockGenerator().Generate(123, catalog);

            Assert.That(stock.Count(entry => entry.ItemId == ItemIds.SuperPotion),
                Is.EqualTo(LeagueGateStockGenerator.RecoveryCopiesPerItem));
            Assert.That(stock.Count(entry => entry.ItemId == ItemIds.SuperMnPotion),
                Is.EqualTo(LeagueGateStockGenerator.RecoveryCopiesPerItem));
            Assert.That(stock.Count(entry => entry.ItemId == ItemIds.SuperRecovery),
                Is.EqualTo(LeagueGateStockGenerator.RecoveryCopiesPerItem));
            Assert.That(stock.Count(entry => entry.ItemId == ItemIds.MaxRevive),
                Is.EqualTo(LeagueGateStockGenerator.RecoveryCopiesPerItem));
            Assert.That(
                stock.Where(entry => entry.ItemId is >= ItemIds.SuperPotion
                        and <= ItemIds.MaxRevive)
                    .All(entry => entry.GeneratedData.PrimaryEffectValue
                        is >= LeagueGateStockGenerator.MinimumRecoveryPercent
                        and <= LeagueGateStockGenerator.MaximumRecoveryPercent),
                Is.True);
            foreach (var stat in Enumerable.Range(0, (int)PachimonStatType.Count)
                         .Select(index => (PachimonStatType)index)
                         .Where(PachimonStatTypeUtility.IsGeneratedStat))
            {
                Assert.That(
                    stock.Count(entry => entry.GeneratedData.StatChanges.Any(
                        change => change.StatType == stat && change.Amount > 0)),
                    Is.EqualTo(LeagueGateStockGenerator.EngravingCopiesPerStat));
            }
        }

        [Test]
        public void RecoveryItems_UseGeneratedRecoveryAmount()
        {
            var potion = ScriptableObject.CreateInstance<HealingItemAsset>();
            try
            {
                potion.ConfigureHealingForEditor(
                    RecoveryResourceType.Hp,
                    500,
                    false);
                var target = new PachimonInstance(
                    "generated_recovery_target",
                    1,
                    AllocationType.Aqua,
                    1,
                    1,
                    CreateStats((PachimonStatType.MaxHp, 1000)));
                target.SetCurrentHp(100);
                var context = ItemUseContext.ForRun(
                    target,
                    1000,
                    ItemTargetAffiliation.Ally);
                var itemInstance = new ItemInstance(
                    "generated_potion",
                    new GeneratedItemData(ItemIds.Potion, 350));

                var recovered = new HealingItemLogic().Apply(
                    potion,
                    itemInstance,
                    context);

                Assert.That(recovered, Is.EqualTo(350));
                Assert.That(target.CurrentHp, Is.EqualTo(450));
            }
            finally
            {
                Object.DestroyImmediate(potion);
            }
        }

        [Test]
        public void ReviveShard_OnlyRestoresDefeatedTarget()
        {
            var reviveShard = ScriptableObject.CreateInstance<HealingItemAsset>();
            try
            {
                reviveShard.ConfigureHealingForEditor(
                    RecoveryResourceType.Hp,
                    500,
                    true,
                    true);
                var target = new PachimonInstance(
                    "revive_target",
                    1,
                    AllocationType.Fire,
                    1,
                    1,
                    CreateStats((PachimonStatType.MaxHp, 1000)));
                var context = ItemUseContext.ForRun(
                    target,
                    1000,
                    ItemTargetAffiliation.Ally);
                var logic = new HealingItemLogic();

                Assert.That(
                    logic.CanUse(reviveShard, null, context),
                    Is.EqualTo(ItemUseFailureReason.InvalidTarget));

                target.ApplyDamage(target.CurrentHp);
                var instance = new ItemInstance(
                    "generated_revive_shard",
                    new GeneratedItemData(ItemIds.ReviveShard, 350));

                Assert.That(
                    logic.CanUse(reviveShard, instance, context),
                    Is.EqualTo(ItemUseFailureReason.None));
                Assert.That(logic.Apply(reviveShard, instance, context), Is.EqualTo(350));
                Assert.That(target.CurrentHp, Is.EqualTo(350));
            }
            finally
            {
                Object.DestroyImmediate(reviveShard);
            }
        }

        [Test]
        public void CityStockGenerator_DistributesRunWideItemsAndMachines()
        {
            var potion = ScriptableObject.CreateInstance<HealingItemAsset>();
            var mnPotion = ScriptableObject.CreateInstance<HealingItemAsset>();
            var reviveShard = ScriptableObject.CreateInstance<HealingItemAsset>();
            var skillForget = ScriptableObject.CreateInstance<SkillForgetItemAsset>();
            var catalog = ScriptableObject.CreateInstance<ItemCatalog>();
            var generatedAssets = new List<Object>();
            try
            {
                potion.ConfigureForEditor(
                    ItemIds.Potion,
                    "Potion",
                    null,
                    string.Empty,
                    ItemCategory.Pharmacy,
                    300);
                mnPotion.ConfigureForEditor(
                    ItemIds.MnPotion,
                    "MN Potion",
                    null,
                    string.Empty,
                    ItemCategory.Pharmacy,
                    300);
                reviveShard.ConfigureForEditor(
                    ItemIds.ReviveShard,
                    "Revive Shard",
                    null,
                    string.Empty,
                    ItemCategory.Pharmacy,
                    1500);
                potion.ConfigureHealingForEditor(
                    RecoveryResourceType.Hp,
                    500,
                    false);
                mnPotion.ConfigureHealingForEditor(
                    RecoveryResourceType.Mn,
                    500,
                    false);
                reviveShard.ConfigureHealingForEditor(
                    RecoveryResourceType.Hp,
                    500,
                    true,
                    true);
                skillForget.ConfigureForEditor(
                    ItemIds.SkillForget,
                    "Skill Forget",
                    null,
                    string.Empty,
                    ItemCategory.SkillMachine,
                    500);
                var items = new List<ItemAsset>
                {
                    potion,
                    mnPotion,
                    reviveShard,
                    skillForget,
                };
                for (var index = 0; index < 16; index++)
                {
                    var skill = ScriptableObject.CreateInstance<PlaceholderSkillAsset>();
                    var allocationType = index < 8
                        ? AllocationType.Unassigned
                        : (AllocationType)((index - 8) + 1);
                    skill.ConfigureForEditor(
                        SkillIdRanges.FirstMachineExclusiveId + index,
                        $"Machine Skill {index}",
                        allocationType,
                        false,
                        100,
                        200,
                        string.Empty);
                    var machine = ScriptableObject.CreateInstance<SkillMachineItemAsset>();
                    machine.ConfigureForEditor(
                        ItemIds.GetSkillMachineItemId(skill.SkillId),
                        $"Machine {index}",
                        null,
                        string.Empty,
                        ItemCategory.SkillMachine,
                        1000);
                    machine.ConfigureSkillForEditor(skill);
                    generatedAssets.Add(skill);
                    generatedAssets.Add(machine);
                    items.Add(machine);
                }

                for (var index = 0; index < (int)PachimonStatType.Count; index++)
                {
                    var statType = (PachimonStatType)index;
                    if (!PachimonStatTypeUtility.IsGeneratedStat(statType))
                    {
                        continue;
                    }

                    var engraving = ScriptableObject.CreateInstance<EngravingItemAsset>();
                    var baseValue = PachimonStatTypeUtility.IsResource(statType)
                        ? 50
                        : PachimonStatTypeUtility.IsSpecialScaledStat(statType)
                            ? 10
                            : 30;
                    engraving.ConfigureForEditor(
                        ItemIds.FirstEngraving + index,
                        $"{statType} Engraving",
                        null,
                        string.Empty,
                        ItemCategory.Engraving,
                        500);
                    engraving.ConfigureEngravingForEditor(statType, baseValue);
                    generatedAssets.Add(engraving);
                    items.Add(engraving);
                }

                var equipmentId = ItemIds.FirstEquipment;
                foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
                {
                    foreach (PachimonAttribute attribute in System.Enum.GetValues(
                                 typeof(PachimonAttribute)))
                    {
                        var equipment = ScriptableObject
                            .CreateInstance<EquipmentItemAsset>();
                        equipment.ConfigureForEditor(
                            equipmentId++,
                            $"{attribute} {slot}",
                            null,
                            string.Empty,
                            ItemCategory.Equipment,
                            2000);
                        equipment.ConfigureEquipmentForEditor(slot, attribute);
                        generatedAssets.Add(equipment);
                        items.Add(equipment);
                    }
                }

                catalog.SetItemsForEditor(items);
                var requests = Enumerable.Range(1, 8)
                    .Select(index => new CityStockRequest(
                        $"test_city_{index:D2}",
                        12345 + index))
                    .ToArray();

                var stocks = new CityStockGenerator().Generate(
                    requests,
                    catalog);
                var allEntries = stocks.Values.SelectMany(stock => stock).ToArray();

                Assert.That(
                    allEntries.Count(entry => entry.ItemId == ItemIds.Potion),
                    Is.EqualTo(CityStockGenerator.PotionTotalCopies));
                Assert.That(
                    allEntries.Count(entry => entry.ItemId == ItemIds.MnPotion),
                    Is.EqualTo(CityStockGenerator.MnPotionTotalCopies));
                Assert.That(
                    allEntries.Count(entry => entry.ItemId == ItemIds.ReviveShard),
                    Is.EqualTo(CityStockGenerator.ReviveShardTotalCopies));
                Assert.That(
                    allEntries.Count(entry => entry.ItemId == ItemIds.SkillForget),
                    Is.EqualTo(
                        requests.Length
                        * CityStockGenerator.SkillForgetCopiesPerCity));
                Assert.That(
                    stocks.Values.All(stock => stock.Count(
                        entry => entry.ItemId == ItemIds.SkillForget)
                        == CityStockGenerator.SkillForgetCopiesPerCity),
                    Is.True);
                Assert.That(
                    stocks.Values.All(stock =>
                        stock.Count(entry => catalog.Get(entry.ItemId)
                            is SkillMachineItemAsset machine
                            && machine.Skill.AllocationType == AllocationType.Unassigned) == 1),
                    Is.True);
                Assert.That(
                    stocks.Values.All(stock =>
                        stock.Count(entry => catalog.Get(entry.ItemId)
                            is SkillMachineItemAsset machine
                            && machine.Skill.AllocationType != AllocationType.Unassigned) == 1),
                    Is.True);
                Assert.That(
                    allEntries
                        .Where(entry => catalog.Get(entry.ItemId) is SkillMachineItemAsset)
                        .Select(entry => entry.ItemId)
                        .Distinct()
                        .Count(),
                    Is.EqualTo(16));
                Assert.That(
                    items.OfType<SkillMachineItemAsset>()
                        .All(machine => machine.BasePrice == 1000),
                    Is.True);
                foreach (var engraving in items.OfType<EngravingItemAsset>())
                {
                    var entries = allEntries
                        .Where(entry => entry.ItemId == engraving.ItemId)
                        .ToArray();
                    Assert.That(
                        entries.Length,
                        Is.EqualTo(CityStockGenerator.EngravingCopiesPerStat));
                    Assert.That(
                        stocks.Values.All(stock => stock.Any(
                            entry => entry.ItemId == engraving.ItemId)),
                        Is.True);
                    foreach (var stock in stocks.Values)
                    {
                        var cityEntries = stock
                            .Where(entry => entry.ItemId == engraving.ItemId)
                            .ToArray();
                        Assert.That(
                            cityEntries.Sum(entry => entry.GeneratedData.StatChanges
                                .Single(change => change.Amount > 0).Amount),
                            Is.EqualTo(
                                engraving.BaseEffectValue
                                * CityStockGenerator.EngravingMainEffectUnits
                                * cityEntries.Length));
                        Assert.That(
                            cityEntries.All(entry =>
                            {
                                var changes = entry.GeneratedData.StatChanges;
                                var main = changes.Single(change => change.Amount > 0);
                                var downside = changes.Single(change => change.Amount < 0);
                                return changes.Count == 2
                                    && main.StatType == engraving.TargetStat
                                    && downside.StatType != engraving.TargetStat;
                            }),
                            Is.True);
                        Assert.That(
                            cityEntries.All(cheaper => cityEntries.All(expensive =>
                                expensive.Price <= cheaper.Price
                                || expensive.GeneratedData.StatChanges
                                    .Single(change => change.Amount > 0).Amount
                                    >= cheaper.GeneratedData.StatChanges
                                        .Single(change => change.Amount > 0).Amount)),
                            Is.True);
                    }
                }
                Assert.That(
                    stocks.Values.All(stock => stock.Count(entry =>
                        catalog.Get(entry.ItemId) is EquipmentItemAsset)
                        == CityStockGenerator.EquipmentPerCity),
                    Is.True);
                foreach (var equipment in items.OfType<EquipmentItemAsset>())
                {
                    var entries = allEntries
                        .Where(entry => entry.ItemId == equipment.ItemId)
                        .ToArray();
                    Assert.That(
                        entries.Length,
                        Is.EqualTo(CityStockGenerator.EquipmentCopiesPerDefinition));
                    Assert.That(
                        entries.All(entry =>
                            entry.GeneratedData.EquipmentSlot == equipment.Slot),
                        Is.True);
                }
                foreach (var stock in stocks.Values)
                {
                    var equipmentEntries = stock
                        .Where(entry => catalog.Get(entry.ItemId)
                            is EquipmentItemAsset)
                        .ToArray();
                    Assert.That(
                        equipmentEntries.All(cheaper =>
                            equipmentEntries.All(expensive =>
                                expensive.Price <= cheaper.Price
                                || GetEquipmentRankValue(catalog, expensive)
                                    >= GetEquipmentRankValue(catalog, cheaper))),
                        Is.True);
                }
                Assert.That(
                    stocks.Values.All(stock => stock.Sum(entry => entry.Price)
                        == stock.Sum(entry => entry.BasePrice)),
                    Is.True);
                foreach (var stock in stocks.Values)
                {
                    foreach (var itemId in new[]
                             {
                                 ItemIds.Potion,
                                 ItemIds.MnPotion,
                                 ItemIds.ReviveShard,
                             })
                    {
                        var entries = stock
                            .Where(entry => entry.ItemId == itemId)
                            .ToArray();
                        Assert.That(
                            entries.Sum(entry =>
                                entry.GeneratedData.PrimaryEffectValue.Value),
                            Is.EqualTo(500 * entries.Length));
                        Assert.That(
                            entries.All(entry =>
                                entry.GeneratedData.PrimaryEffectValue is >= 350 and <= 650),
                            Is.True);
                        Assert.That(
                            entries.All(cheaper => entries.All(expensive =>
                                expensive.Price <= cheaper.Price
                                || expensive.GeneratedData.PrimaryEffectValue.Value
                                    >= cheaper.GeneratedData.PrimaryEffectValue.Value)),
                            Is.True);
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(potion);
                Object.DestroyImmediate(mnPotion);
                Object.DestroyImmediate(reviveShard);
                Object.DestroyImmediate(skillForget);
                Object.DestroyImmediate(catalog);
                foreach (var asset in generatedAssets)
                {
                    Object.DestroyImmediate(asset);
                }
            }
        }

        private static int GetEquipmentRankValue(
            ItemCatalog catalog,
            CityStockEntry entry)
        {
            var equipment = (EquipmentItemAsset)catalog.Get(entry.ItemId);
            var mainStat = PachimonStatTypeUtility.FromAttribute(
                equipment.MainAttribute);
            var value = entry.GeneratedData.StatChanges
                .Single(change => change.StatType == mainStat)
                .Amount;
            return equipment.Slot == EquipmentSlot.Head
                ? value / CityStockGenerator.HeadMainEffectMultiplier
                : value;
        }

        [Test]
        public void SkillMachine_UpgradesDuplicateSkillAndConsumesItem()
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

            Assert.That(inventory.TryAdd(machine.ItemId, out var first, out _), Is.True);
            var firstResult = service.TryUse(
                inventory,
                first.InstanceId,
                ItemUseContext.ForRun(target, target.MaxHp, ItemTargetAffiliation.Ally));
            Assert.That(firstResult.Succeeded, Is.True);

            Assert.That(inventory.TryAdd(machine.ItemId, out var duplicate, out _), Is.True);
            var duplicateResult = service.TryUse(
                inventory,
                duplicate.InstanceId,
                ItemUseContext.ForRun(target, target.MaxHp, ItemTargetAffiliation.Ally));

            Assert.That(
                target.SkillIds.Count(skillId => skillId == skill.SkillId),
                Is.EqualTo(1));
            Assert.That(
                target.SkillSlots.Single(slot => slot.SkillId == skill.SkillId)
                    .UpgradeLevel,
                Is.EqualTo(1));
            Assert.That(duplicateResult.Succeeded, Is.True);
            Assert.That(inventory.Count, Is.EqualTo(0));
        }

        [Test]
        public void PachimonInstance_UpgradesDuplicateEvenWhenSlotsAreFull()
        {
            var target = new PachimonInstance(
                "forget_target",
                1,
                AllocationType.Fire,
                1,
                1,
                CreateStats());
            Assert.That(target.AddSkill(7), Is.True);
            Assert.That(target.AddSkill(7), Is.True);
            Assert.That(target.AddSkill(8), Is.True);
            Assert.That(target.AddSkill(9), Is.True);
            Assert.That(target.AddSkill(10), Is.True);
            Assert.That(target.AddSkill(11), Is.True);
            Assert.That(target.AddSkill(12), Is.False);
            Assert.That(target.AddSkill(7), Is.True);

            Assert.That(target.SkillSlots.Count, Is.EqualTo(PachimonInstance.MaxSkillSlots));
            Assert.That(target.SkillIds.Count(skillId => skillId == 7), Is.EqualTo(1));
            Assert.That(
                target.SkillSlots.Single(slot => slot.SkillId == 7).UpgradeLevel,
                Is.EqualTo(2));
        }

        [Test]
        public void SkillUpgradeMath_ScalesFromBaseWithoutIntermediateRounding()
        {
            Assert.That(
                SkillUpgradeMath.ScaleTiming(100m, 1),
                Is.EqualTo(200m / 3m));
            Assert.That(
                SkillUpgradeMath.ScaleTiming(100m, 2),
                Is.EqualTo(400m / 9m));
            Assert.That(
                SkillUpgradeMath.ScaleManaCost(20m, 1),
                Is.EqualTo(80m / 3m));
            Assert.That(
                SkillUpgradeMath.ScaleManaCost(20m, 2),
                Is.EqualTo(320m / 9m));
        }

        [Test]
        public void PachimonInstance_CanForgetItsLastSkill()
        {
            var target = new PachimonInstance(
                "forget_last_target",
                1,
                AllocationType.Fire,
                1,
                1,
                CreateStats());

            Assert.That(
                target.TryForgetSkillSlot(target.SkillSlots[0].SlotId, out _),
                Is.True);
            Assert.That(target.SkillSlots, Is.Empty);
            Assert.That(target.SkillIds, Is.Empty);
        }

        [Test]
        public void EngravingItem_AppliesPermanentMainAndDownsideStats()
        {
            var engraving = ScriptableObject.CreateInstance<EngravingItemAsset>();
            var catalog = ScriptableObject.CreateInstance<ItemCatalog>();
            try
            {
                engraving.ConfigureForEditor(
                    ItemIds.FirstEngraving,
                    "Life Engraving",
                    null,
                    string.Empty,
                    ItemCategory.Engraving,
                    500);
                engraving.ConfigureEngravingForEditor(
                    PachimonStatType.MaxHp,
                    50);
                catalog.SetItemsForEditor(new ItemAsset[] { engraving });
                var target = new PachimonInstance(
                    "engraving_target",
                    1,
                    AllocationType.Fire,
                    1,
                    1,
                    CreateStats(
                        (PachimonStatType.MaxHp, 1000),
                        (PachimonStatType.Fire, 100)));
                var inventory = new ItemInventory();
                var generatedData = new GeneratedItemData(
                    engraving.ItemId,
                    statChanges: new[]
                    {
                        new GeneratedStatChange(PachimonStatType.MaxHp, 60),
                        new GeneratedStatChange(PachimonStatType.Fire, -18),
                    });
                Assert.That(
                    inventory.TryAdd(generatedData, out var item, out _),
                    Is.True);

                var result = new ItemUseService(catalog).TryUse(
                    inventory,
                    item.InstanceId,
                    ItemUseContext.ForRun(
                        target,
                        1000,
                        target.MaxMn,
                        ItemTargetAffiliation.Ally));
                var stats = EffectivePachimonStats.Calculate(
                    target.Stats,
                    target.PermanentStatModifiers);

                Assert.That(result.Succeeded, Is.True);
                Assert.That(stats.MaxHp, Is.EqualTo(1060));
                Assert.That(
                    stats.GetValue(PachimonStatType.Fire),
                    Is.EqualTo(82));
                Assert.That(target.CurrentHp, Is.EqualTo(1060));
                Assert.That(target.Engravings.Count, Is.EqualTo(1));
                Assert.That(
                    target.Engravings[0].DisplayName,
                    Is.EqualTo("Life Engraving"));
                Assert.That(
                    target.Engravings[0].GeneratedData,
                    Is.SameAs(generatedData));
                Assert.That(inventory.Count, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(engraving);
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void PachimonInstance_EngravingCapacityIsNine()
        {
            var target = new PachimonInstance(
                "engraving_capacity_target",
                1,
                AllocationType.Fire,
                1,
                1,
                CreateStats((PachimonStatType.MaxHp, 1000)));
            var generatedData = new GeneratedItemData(
                ItemIds.FirstEngraving,
                statChanges: new[]
                {
                    new GeneratedStatChange(PachimonStatType.Fire, 30),
                    new GeneratedStatChange(PachimonStatType.Aqua, -15),
                });

            for (var index = 0; index < PachimonInstance.MaxEngravings; index++)
            {
                Assert.That(target.CanAddEngravings(), Is.True);
                target.RecordAppliedEngraving(
                    ItemIds.FirstEngraving,
                    $"Engraving {index + 1}",
                    generatedData);
            }

            Assert.That(target.Engravings.Count,
                Is.EqualTo(PachimonInstance.MaxEngravings));
            Assert.That(target.CanAddEngravings(), Is.False);
            Assert.That(target.CanAddEngravings(2), Is.False);
            Assert.Throws<System.InvalidOperationException>(() =>
                target.RecordAppliedEngraving(
                    ItemIds.FirstEngraving,
                    "Engraving 10",
                    generatedData));
        }

        [Test]
        public void BattleRewardSession_AbandonRemaining_KeepsClaimedRewardsAndCompletes()
        {
            var runState = new RunState(1, "test");
            var pool = new RunPachimonPool();
            var partyIds = new string[RunState.PartySize];
            for (var index = 0; index < partyIds.Length; index++)
            {
                var instanceId = $"reward_target_{index}";
                partyIds[index] = instanceId;
                pool.Add(new PachimonInstance(
                    instanceId,
                    index + 1,
                    AllocationType.Fire,
                    1,
                    1,
                    CreateStats()));
            }
            Assert.That(runState.TrySetInitialParty(partyIds), Is.True);

            var session = new BattleRewardSession(
                runState,
                pool,
                new NodeReward(100, null, null, null),
                PassiveRegistry);

            Assert.That(session.ClaimGold(), Is.True);
            Assert.That(runState.Gold, Is.EqualTo(100));
            Assert.That(session.AbandonRemaining(), Is.True);
            Assert.That(session.IsComplete, Is.True);
            Assert.That(runState.Gold, Is.EqualTo(100));
            Assert.That(session.AbandonRemaining(), Is.False);
        }

        [Test]
        public void BattleRewardSession_ItemChoice_AddsOneFixedRecoveryItem()
        {
            var runState = new RunState(1, "test");
            var pool = new RunPachimonPool();
            var partyIds = new string[RunState.PartySize];
            for (var index = 0; index < partyIds.Length; index++)
            {
                var instanceId = $"item_reward_target_{index}";
                partyIds[index] = instanceId;
                pool.Add(new PachimonInstance(
                    instanceId,
                    index + 1,
                    AllocationType.Fire,
                    1,
                    1,
                    CreateStats()));
            }
            Assert.That(runState.TrySetInitialParty(partyIds), Is.True);

            var session = new BattleRewardSession(
                runState,
                pool,
                new NodeReward(100, null, null, null),
                PassiveRegistry);

            Assert.That(session.CanClaimItem(ItemIds.Potion), Is.True);
            Assert.That(session.ClaimItem(ItemIds.Potion), Is.True);
            Assert.That(session.ClaimItem(ItemIds.MnPotion), Is.False);
            Assert.That(runState.ItemInventory.Count, Is.EqualTo(1));
            var item = runState.ItemInventory.Slots.Single(entry => entry != null);
            Assert.That(item.ItemId, Is.EqualTo(ItemIds.Potion));
            Assert.That(
                item.GeneratedData.PrimaryEffectValue,
                Is.EqualTo(BattleRewardSession.RewardItemRecoveryAmount));
            Assert.That(
                session.IsClaimed(BattleRewardSlot.Item),
                Is.True);
        }

        [Test]
        public void BattleRewardSession_ItemChoice_IsUnavailableWhenBagIsFull()
        {
            var runState = new RunState(1, "test");
            for (var index = 0; index < ItemInventory.Capacity; index++)
            {
                Assert.That(
                    runState.ItemInventory.TryAdd(ItemIds.Stone, out _, out _),
                    Is.True);
            }

            var pool = new RunPachimonPool();
            var partyIds = new string[RunState.PartySize];
            for (var index = 0; index < partyIds.Length; index++)
            {
                var instanceId = $"full_bag_reward_target_{index}";
                partyIds[index] = instanceId;
                pool.Add(new PachimonInstance(
                    instanceId,
                    index + 1,
                    AllocationType.Fire,
                    1,
                    1,
                    CreateStats()));
            }
            Assert.That(runState.TrySetInitialParty(partyIds), Is.True);

            var session = new BattleRewardSession(
                runState,
                pool,
                new NodeReward(100, null, null, null),
                PassiveRegistry);

            Assert.That(session.CanClaimItem(ItemIds.Potion), Is.False);
            Assert.That(session.ClaimItem(ItemIds.Potion), Is.False);
            Assert.That(session.IsClaimed(BattleRewardSlot.Item), Is.False);
        }

        [Test]
        public void Equipment_CanOnlyFillEachSlotOnce()
        {
            var equipment = ScriptableObject.CreateInstance<EquipmentItemAsset>();
            try
            {
                equipment.ConfigureForEditor(
                    ItemIds.FirstEquipment,
                    "Fire Crown",
                    null,
                    string.Empty,
                    ItemCategory.Equipment,
                    2000);
                equipment.ConfigureEquipmentForEditor(
                    EquipmentSlot.Head,
                    PachimonAttribute.Fire);
                var target = new PachimonInstance(
                    "equipment_target",
                    1,
                    AllocationType.Fire,
                    1,
                    1,
                    CreateStats());
                var generatedData = new GeneratedItemData(
                    equipment.ItemId,
                    statChanges: new[]
                    {
                        new GeneratedStatChange(PachimonStatType.Fire, 180),
                        new GeneratedStatChange(PachimonStatType.Aqua, 60),
                    },
                    equipmentSlot: EquipmentSlot.Head);

                Assert.That(
                    target.TryEquip(equipment, generatedData, "equipment:first"),
                    Is.True);
                Assert.That(target.CanEquip(EquipmentSlot.Head), Is.False);
                Assert.That(
                    target.TryEquip(equipment, generatedData, "equipment:second"),
                    Is.False);
                Assert.That(target.Equipment.Count, Is.EqualTo(1));
                Assert.That(
                    target.Equipment[EquipmentSlot.Head].DisplayName,
                    Is.EqualTo("Fire Crown"));
                Assert.That(target.PermanentStatModifiers.Count, Is.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(equipment);
            }
        }

        [Test]
        public void Equipment_SubStatChangeAddsFixedStatWithoutChangingBindingRatio()
        {
            var equipment = ScriptableObject.CreateInstance<EquipmentItemAsset>();
            try
            {
                equipment.ConfigureForEditor(
                    ItemIds.FirstEquipment,
                    "Fire Shoes",
                    null,
                    string.Empty,
                    ItemCategory.Equipment,
                    2000);
                equipment.ConfigureEquipmentForEditor(
                    EquipmentSlot.Feet,
                    PachimonAttribute.Fire);
                var target = new PachimonInstance(
                    "equipment_ratio_target",
                    1,
                    AllocationType.Fire,
                    1,
                    1,
                    CreateStats((PachimonStatType.Electric, 100)));
                var generatedData = new GeneratedItemData(
                    equipment.ItemId,
                    statChanges: new[]
                    {
                        new GeneratedStatChange(PachimonStatType.Fire, 30),
                        new GeneratedStatChange(PachimonStatType.Aqua, 10),
                        new GeneratedStatChange(PachimonStatType.Speed, 40),
                    },
                    equipmentSlot: EquipmentSlot.Feet);

                Assert.That(
                    target.TryEquip(equipment, generatedData, "equipment:ratio"),
                    Is.True);
                var stats = EffectivePachimonStats.Calculate(
                    target.Stats,
                    target.PermanentStatModifiers,
                    target.SubStatBindings);
                Assert.That(
                    target.SubStatBindings.GetDerivationRatio(PachimonStatType.Speed),
                    Is.EqualTo(50));
                Assert.That(stats.GetValue(PachimonStatType.Electric), Is.EqualTo(100));
                Assert.That(stats.GetValue(PachimonStatType.Speed), Is.EqualTo(90));
                Assert.That(target.PermanentStatModifiers.Count, Is.EqualTo(3));
            }
            finally
            {
                Object.DestroyImmediate(equipment);
            }
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
                1000);
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
            Assert.That(skill.BaseDamage, Is.EqualTo(400));
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
        public void Leak_TriggersWhenElectricDamageDefeatsItsHolder()
        {
            var skill = CreateBasicElectricSkill();
            var source = CreateBattleUnitWithStats(
                "player_1", BattleSide.Player, 0, 2000, skill.SkillId);
            var holder = CreateBattleUnitWithStats(
                "leak_holder", BattleSide.Enemy, 0, 100, 1);
            var second = CreateBattleUnitWithStats(
                "enemy_2", BattleSide.Enemy, 1, 2000, 1);
            var third = CreateBattleUnitWithStats(
                "enemy_3", BattleSide.Enemy, 2, 2000, 1);
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, source),
                new BattleSideState(
                    BattleSide.Enemy,
                    new[] { holder, second, third }));
            state.Statuses.ApplyStatus(
                holder,
                new BattleStatusInstance(
                    BattleStatusId.Leak,
                    BattleStatusCategory.Leak,
                    source,
                    value: 20));

            BattleAttributeDamageService.Apply(
                state,
                source,
                holder,
                new DamageContext(
                    DamageOriginKind.Skill,
                    skill.SkillId,
                    100,
                    source.GetBattleStats(),
                    holder.GetBattleStats(),
                    PachimonAttribute.Electric,
                    isAttack: true,
                    applyAttackerAttributeMultiplier: false,
                    applyDamageBonusMultiplier: false,
                    applyOutgoingModifiers: false));

            Assert.That(holder.IsDefeated, Is.True);
            Assert.That(second.CurrentHp, Is.EqualTo(1980));
            Assert.That(third.CurrentHp, Is.EqualTo(1980));
            Assert.That(
                state.LogEntries.Last(),
                Is.EqualTo($"{holder.DisplayName}は漏電している！"));
        }

        [Test]
        public void DefeatedTarget_RejectsLaterDamageAndStatusFromSameHit()
        {
            var skill = CreateBasicElectricSkill();
            var source = CreateBattleUnitWithStats(
                "player_1", BattleSide.Player, 0, 2000, skill.SkillId);
            var target = CreateBattleUnitWithStats(
                "enemy_1", BattleSide.Enemy, 0, 100, 1);
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, source),
                CreateTestSide(BattleSide.Enemy, target));
            var hit = new SkillExecutionContext(state, source, skill)
                .BeginAttackHit(target);
            var damageContext = new DamageContext(
                DamageOriginKind.Skill,
                skill.SkillId,
                100,
                source.GetBattleStats(),
                target.GetBattleStats(),
                PachimonAttribute.Electric,
                isAttack: true,
                applyAttackerAttributeMultiplier: false,
                applyDamageBonusMultiplier: false,
                applyOutgoingModifiers: false);

            BattleAttributeDamageService.Apply(
                state, source, target, damageContext, hit);
            var skippedDamage = BattleAttributeDamageService.Apply(
                state, source, target, damageContext, hit);
            var statusApplied = hit.ApplyStatus(
                BattleStatusFactory.CreateSlow(
                    source,
                    100,
                    ParalysisStatus));
            var presentation = state.Presentation.Complete();

            Assert.That(target.IsDefeated, Is.True);
            Assert.That(skippedDamage.AppliedDamage, Is.Zero);
            Assert.That(statusApplied, Is.False);
            Assert.That(target.Statuses, Is.Empty);
            Assert.That(
                presentation.Steps.Count(step =>
                    step.Kind == BattlePresentationStepKind.DamageApplied),
                Is.EqualTo(1));
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
                Is.EqualTo(-220));
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
        public void StoredCharge_GainsAStackFromSourceLessElectricStatusDamage()
        {
            var definition = CreateStoredChargePassive();
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            catalog.SetPassivesForEditor(new PassiveAsset[] { definition });
            var owner = CreateBattleUnitWithPassive(
                "player_1",
                BattleSide.Player,
                0,
                definition.PassiveId);
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, owner),
                CreateTestSide(BattleSide.Enemy),
                new PassiveLogicRegistry(catalog));

            BattleStatusDamageService.ApplyAttribute(
                state,
                state.Enemy.GetUnitAt(0),
                BattleStatusId.Leak,
                PachimonAttribute.Electric,
                baseDamage: 100m);

            Assert.That(
                owner.GetStatus(BattleStatusId.StoredCharge)?.StackCount,
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
                Is.EqualTo(-250));
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
        public void StatCalculator_CombinesBindingRatioAndDirectSubStatModifier()
        {
            var bindings = PachimonSubStatBindings.CreateDefault();
            bindings.AddDerivationRatio(PachimonStatType.Speed, 50);
            var result = new StatCalculator().Calculate(
                CreateStats((PachimonStatType.Electric, 100)),
                new[]
                {
                    Fixed(
                        PachimonStatType.Speed,
                        StatModifierOperation.DirectAdditive,
                        -200m,
                        "slow"),
                },
                bindings);

            Assert.That(result.GetValue(PachimonStatType.Electric), Is.EqualTo(100));
            Assert.That(result.GetValue(PachimonStatType.Speed), Is.EqualTo(-100));
        }

        [Test]
        public void StatCalculator_DerivesSubStatFromFinalAttribute()
        {
            var result = new StatCalculator().Calculate(
                CreateStats(
                    (PachimonStatType.Electric, 100),
                    (PachimonStatType.Dragon, 50)),
                new IStatModifier[]
                {
                    new DerivedStatModifier(
                        PachimonStatType.Electric,
                        StatModifierOperation.DerivedAdditive,
                        _ => 50m,
                        Source("electric-buff")),
                    new DerivedStatModifier(
                        PachimonStatType.Dragon,
                        StatModifierOperation.DerivedAdditive,
                        stats => stats.GetValue(PachimonStatType.Speed) * 0.5m,
                        Source("speed-to-dragon")),
                },
                PachimonSubStatBindings.CreateDefault());

            Assert.That(result.GetValue(PachimonStatType.Electric), Is.EqualTo(150));
            Assert.That(result.GetValue(PachimonStatType.Speed), Is.EqualTo(75));
            Assert.That(result.GetValue(PachimonStatType.Dragon), Is.EqualTo(75));
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
        public void ChainBurn_GainsItsOwnChainAndUsesItOnTheNextCast()
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
                state.LogEntries.Any(entry => entry.Contains("追加連鎖数")),
                Is.False);
            Assert.That(enemies.GetUnitAt(0).CurrentHp, Is.EqualTo(1920));
            Assert.That(enemies.GetUnitAt(1).CurrentHp, Is.EqualTo(1960));
            Assert.That(
                user.GetStatus(BattleStatusId.ChainBurnChain)?.Value,
                Is.EqualTo(1));
            Assert.That(SkillChainRuntime.GetAdditionalChainCount(
                user, BattleStatusId.ChainBurnChain), Is.EqualTo(1));

            BattleSkillResolver.Resolve(state, user, skill, logic);
            Assert.That(
                user.GetStatus(BattleStatusId.ChainBurnChain)?.Value,
                Is.EqualTo(2));
            Assert.That(SkillChainRuntime.GetAdditionalChainCount(
                user, BattleStatusId.ChainBurnChain), Is.EqualTo(2));

            var third = BattleSkillResolver.Resolve(state, user, skill, logic);
            Assert.That(third.Effects.Count, Is.EqualTo(4));
            Assert.That(
                user.GetStatus(BattleStatusId.ChainBurnChain)?.Value,
                Is.EqualTo(3));
            Assert.That(SkillChainRuntime.GetAdditionalChainCount(
                user, BattleStatusId.ChainBurnChain), Is.EqualTo(3));
            Assert.That(enemies.GetUnitAt(0).CurrentHp, Is.EqualTo(1760));
            Assert.That(enemies.GetUnitAt(1).CurrentHp, Is.EqualTo(1827));
            Assert.That(enemies.GetUnitAt(2).CurrentHp, Is.EqualTo(1934));
        }

        [Test]
        public void SkillChainRuntime_KeepsEachSkillChainIndependent()
        {
            var user = CreateBattleUnitWithPassive(
                "chain_owner",
                BattleSide.Player,
                0,
                passiveId: 2);

            SkillChainRuntime.Add(user, user,
                BattleStatusId.ChainBurnChain, 2);
            SkillChainRuntime.Add(user, user,
                BattleStatusId.ChainVinesChain, 1);

            Assert.That(SkillChainRuntime.GetAdditionalChainCount(
                user, BattleStatusId.ChainBurnChain), Is.EqualTo(2));
            Assert.That(SkillChainRuntime.GetAdditionalChainCount(
                user, BattleStatusId.ChainVinesChain), Is.EqualTo(1));
            Assert.That(SkillChainRuntime.GetAdditionalChainCount(
                user, BattleStatusId.CuttingDanceChain), Is.Zero);
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

            SkillChainRuntime.Add(user, user,
                BattleStatusId.ChainBurnChain, 2);
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
                Is.EqualTo(100m));
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
        public void SolarBeam_UsesConfiguredStartupAndPositiveTemperatureReduction()
        {
            var skill = AssetDatabase.LoadAssetAtPath<SolarBeamSkillAsset>(
                "Assets/GameData/Skill/Placeholder/Skill_027.asset");
            Assert.That(skill, Is.Not.Null);
            Assert.That(skill.BaseStartupTicks, Is.EqualTo(100));

            var user = CreateBattleUnitWithStats(
                "solar_beam_user",
                BattleSide.Player,
                0,
                2000,
                skill.SkillId);
            var state = new BattleState(
                123,
                new BattleSideState(BattleSide.Player, new[] { user }),
                CreateTestSide(BattleSide.Enemy));

            Assert.That(
                SkillTimingCalculator.CreatePlan(skill, user, state).StartupTicks,
                Is.EqualTo(100));

            var temperature = CreateSunnyWeather();
            state.Weather.AddTemperature(user, temperature, 100);

            Assert.That(
                SkillTimingCalculator.CreatePlan(skill, user, state).StartupTicks,
                Is.EqualTo(50));
            Object.DestroyImmediate(temperature);
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
        public void WeatherDetails_ShowCalculatedTemperaturePercentages()
        {
            var temperature = ScriptableObject.CreateInstance<SunnyWeatherAsset>();
            try
            {
                temperature.ConfigureForEditor(
                    "Temperature",
                    "Fire +{value:increasePercent}% / Ice -{value:decreasePercent}%",
                    fireRatioScalingPercent: 10,
                    aquaRatioScalingPercent: 20,
                    iceRatioScalingPercent: 20,
                    coldFireRatioScalingPercent: 20,
                    coldIceRatioScalingPercent: 10,
                    negativeDescription:
                        "Ice +{value:increasePercent}% / Fire -{value:decreasePercent}%");

                Assert.That(
                    WeatherDetailDescriptionFormatter.Format(100, temperature),
                    Is.EqualTo("Fire +10% / Ice -16.67%"));
                Assert.That(
                    WeatherDetailDescriptionFormatter.Format(-100, temperature),
                    Is.EqualTo("Ice +10% / Fire -16.67%"));
            }
            finally
            {
                Object.DestroyImmediate(temperature);
            }
        }

        [Test]
        public void DamageDrivenSignedWeather_DiminishesOnlySameDirectionGrowth()
        {
            Assert.That(
                BattleWeatherRuntime.CalculateDamageDrivenSignedChange(0, 10m),
                Is.EqualTo(10m));
            Assert.That(
                BattleWeatherRuntime.CalculateDamageDrivenSignedChange(25, 10m),
                Is.EqualTo(5m));
            Assert.That(
                BattleWeatherRuntime.CalculateDamageDrivenSignedChange(50, 10m),
                Is.EqualTo(10m / 3m));
            Assert.That(
                BattleWeatherRuntime.CalculateDamageDrivenSignedChange(-25, -10m),
                Is.EqualTo(-5m));
            Assert.That(
                BattleWeatherRuntime.CalculateDamageDrivenSignedChange(50, -10m),
                Is.EqualTo(-10m));
        }

        [Test]
        public void SignedWeatherGameData_UsesHalfPercentDamageChange()
        {
            var temperature = AssetDatabase.LoadAssetAtPath<SunnyWeatherAsset>(
                "Assets/GameData/Battle/Weather/SunnyWeather.asset");
            var moisture = AssetDatabase.LoadAssetAtPath<
                PairedAttributeEnvironmentAsset>(
                    "Assets/GameData/Battle/Weather/MoistureEnvironment.asset");
            var plasma = AssetDatabase.LoadAssetAtPath<
                PairedAttributeEnvironmentAsset>(
                    "Assets/GameData/Battle/Weather/PlasmaEnvironment.asset");

            Assert.That(temperature, Is.Not.Null);
            Assert.That(moisture, Is.Not.Null);
            Assert.That(plasma, Is.Not.Null);
            Assert.That(temperature.DamageChangePercent, Is.EqualTo(0.5f));
            Assert.That(moisture.DamageChangePercent, Is.EqualTo(0.5f));
            Assert.That(plasma.DamageChangePercent, Is.EqualTo(0.5f));
        }

        [Test]
        public void TemperatureGameData_UsesFullValueForBothDirections()
        {
            var temperature = AssetDatabase.LoadAssetAtPath<SunnyWeatherAsset>(
                "Assets/GameData/Battle/Weather/SunnyWeather.asset");

            Assert.That(temperature, Is.Not.Null);
            Assert.That(temperature.FireRatioScalingPercent, Is.EqualTo(100));
            Assert.That(temperature.IceRatioScalingPercent, Is.EqualTo(100));
            Assert.That(temperature.ColdFireRatioScalingPercent, Is.EqualTo(100));
            Assert.That(temperature.ColdIceRatioScalingPercent, Is.EqualTo(100));
        }

        [Test]
        public void NegativePlasma_UsesNatureNameAndCalculatedDescription()
        {
            var plasma = AssetDatabase.LoadAssetAtPath<
                PairedAttributeEnvironmentAsset>(
                    "Assets/GameData/Battle/Weather/PlasmaEnvironment.asset");

            Assert.That(plasma, Is.Not.Null);
            Assert.That(plasma.NegativeDisplayName, Is.EqualTo("大自然"));
            var description = WeatherDetailDescriptionFormatter.Format(-100, plasma);
            Assert.That(description, Does.Contain("100%増加"));
            Assert.That(description, Does.Contain("50%減少"));
            Assert.That(description, Does.Contain("name=\"Leaf\""));
            Assert.That(description, Does.Contain("name=\"Electric\""));
        }

        [Test]
        public void WeatherDetails_ShowRuntimeRainWindAndThunderValues()
        {
            var rain = AssetDatabase.LoadAssetAtPath<RainWeatherAsset>(
                "Assets/GameData/Battle/Weather/RainWeather.asset");
            var wind = AssetDatabase.LoadAssetAtPath<WindWeatherAsset>(
                "Assets/GameData/Battle/Weather/WindWeather.asset");
            var thunder = AssetDatabase.LoadAssetAtPath<ThunderWeatherAsset>(
                "Assets/GameData/Battle/Weather/ThunderWeather.asset");
            var temperature = AssetDatabase.LoadAssetAtPath<SunnyWeatherAsset>(
                "Assets/GameData/Battle/Weather/SunnyWeather.asset");
            Assert.That(rain, Is.Not.Null);
            Assert.That(wind, Is.Not.Null);
            Assert.That(thunder, Is.Not.Null);
            Assert.That(temperature, Is.Not.Null);

            var source = CreateBattleUnitWithStats(
                "weather_detail_source",
                BattleSide.Player,
                0,
                2000,
                1);
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, source),
                CreateTestSide(BattleSide.Enemy));
            state.Weather.CreateOrAdd(source, wind, 100);
            var rainInstance = state.Weather.CreateOrAdd(source, rain, 100);

            var rainDescription = WeatherDetailDescriptionFormatter.Format(
                rainInstance);
            Assert.That(rainDescription, Does.Contain("11%"));
            Assert.That(rainDescription, Does.Contain("18.03%"));
            Assert.That(rainDescription, Does.Contain("0.077"));
            Assert.That(rainDescription, Does.Contain("湿潤を1.1"));

            state.Weather.AddTemperature(source, temperature, -100);
            var snowDescription = WeatherDetailDescriptionFormatter.Format(
                rainInstance);
            Assert.That(rainInstance.DisplayName, Is.EqualTo("雪"));
            Assert.That(snowDescription, Does.Contain("冷気Valueを42"));

            state.Weather.AddPrecipitation(source, rain, -200);
            var sunnyDescription = WeatherDetailDescriptionFormatter.Format(
                rainInstance);
            Assert.That(rainInstance.DisplayName, Is.EqualTo("晴天"));
            Assert.That(sunnyDescription, Does.Contain("10%"));
            Assert.That(sunnyDescription, Does.Contain("16.67%"));
            Assert.That(sunnyDescription, Does.Contain("気温を1"));

            var windDescription = WeatherDetailDescriptionFormatter.Format(
                state.Weather.Get(BattleWeatherId.Wind));
            Assert.That(windDescription, Does.Contain("雨・雪の効果を10%"));
            Assert.That(windDescription, Does.Contain("Damageの10%"));

            var thunderInstance = state.Weather.CreateOrAdd(
                source,
                thunder,
                300);
            var thunderDescription = WeatherDetailDescriptionFormatter.Format(
                thunderInstance);
            Assert.That(thunderDescription, Does.Contain("30%"));
            Assert.That(thunderDescription, Does.Contain("150tick"));
            Assert.That(thunderDescription, Does.Contain("軽減前100"));
            Assert.That(thunderDescription, Does.Not.Contain("{value:"));
        }

        [Test]
        public void SunnyDay_RecastAddsSelfAmplifiedSunnyPrecipitation()
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
            var precipitation = CreateRainWeather();
            var skill = ScriptableObject.CreateInstance<SunnyDaySkillAsset>();
            skill.ConfigureForEditor(
                49,
                "Warming",
                100,
                300,
                100,
                string.Empty,
                200,
                100,
                weather);
            skill.SetPrecipitationDefinitionForEditor(precipitation);
            var logic = new SunnyDaySkillLogic(skill);

            BattleSkillResolver.Resolve(state, source, skill, logic);
            Assert.That(
                state.Weather.Get(BattleWeatherId.Rain).Value,
                Is.EqualTo(-800));
            BattleSkillResolver.Resolve(state, source, skill, logic);

            Assert.That(
                state.Weather.Get(BattleWeatherId.Rain).Value,
                Is.EqualTo(-1920));
            state.Timeline.AdvanceToTick(100);
            Assert.That(
                state.Weather.Get(BattleWeatherId.Rain).Value,
                Is.EqualTo(-1820));
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
            Assert.That(state.Weather.Temperature, Is.EqualTo(-800));
            BattleSkillResolver.Resolve(state, source, skill, logic);

            Assert.That(state.Weather.Temperature, Is.EqualTo(-1920));
            state.Timeline.AdvanceToTick(100);
            Assert.That(state.Weather.Temperature, Is.EqualTo(-1920));
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
                200,
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
                Is.EqualTo(300));
            Assert.That(
                state.ResolveAttributeRatio(PachimonAttribute.Aqua, 100m),
                Is.EqualTo(130m));
            Assert.That(
                state.ResolveAttributeRatio(PachimonAttribute.Fire, 100m),
                Is.EqualTo(62.5m));

            state.Timeline.AdvanceToTick(100);
            Assert.That(
                state.Weather.Get(BattleWeatherId.Rain).Value,
                Is.EqualTo(200));
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
                200,
                100,
                wind);

            BattleSkillResolver.Resolve(
                state,
                source,
                skill,
                new WindStormSkillLogic(skill));

            Assert.That(
                state.Weather.Get(BattleWeatherId.Wind).Value,
                Is.EqualTo(300));
            Assert.That(
                state.ResolveAttributeRatio(PachimonAttribute.Wind, 100m),
                Is.EqualTo(130m));
            Assert.That(
                source.GetBattleStatValue(PachimonStatType.Speed),
                Is.EqualTo(120));

            state.Weather.CreateOrAdd(source, CreateRainWeather(), 500);

            Assert.That(state.Weather.GetEffectiveRainValue(), Is.EqualTo(650m));
            Assert.That(
                state.ResolveAttributeRatio(PachimonAttribute.Aqua, 100m),
                Is.EqualTo(165m));
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

        [Test]
        public void BattleFlow_RecalculatesPendingResolveAndSpendsManaAtActivation()
        {
            var skill = ScriptableObject.CreateInstance<PlaceholderSkillAsset>();
            var skillCatalog = ScriptableObject.CreateInstance<SkillCatalog>();
            var passive = ScriptableObject.CreateInstance<RainManPassiveAsset>();
            var passiveCatalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            var rain = CreateRainWeather();
            try
            {
                skill.ConfigureForEditor(
                    skillId: 1,
                    displayName: "Dynamic Startup",
                    allocationType: AllocationType.Fire,
                    isMapAssignable: true,
                    baseRecoveryTicks: 100,
                    baseCooldownTicks: 200,
                    description: string.Empty,
                    baseManaCost: 100,
                    baseStartupTicks: 100);
                skillCatalog.SetSkillsForEditor(new SkillAsset[] { skill });
                passive.ConfigureForEditor(
                    passiveId: 18,
                    displayName: "Rain Man",
                    description: string.Empty,
                    baseSpeedPercent: 100,
                    rainValueRatio: 100);
                passiveCatalog.SetPassivesForEditor(
                    new PassiveAsset[] { passive });

                var owner = CreateBattleUnitWithPassive(
                    "dynamic_speed_owner",
                    BattleSide.Player,
                    0,
                    passive.PassiveId,
                    (PachimonStatType.Speed, 100));
                var state = new BattleState(
                    123,
                    CreateTestSide(BattleSide.Player, owner),
                    CreateTestSide(BattleSide.Enemy),
                    new PassiveLogicRegistry(passiveCatalog));
                state.Weather.CreateOrAdd(owner, rain, 100);
                var flow = new BattleFlowController(
                    state,
                    new BattleSkillRuntime(skillCatalog, passiveCatalog));

                var input = flow.Advance();
                Assert.That(input.Kind, Is.EqualTo(BattleFlowStepKind.PlayerInputRequired));
                Assert.That(input.Actor, Is.SameAs(owner));
                Assert.That(
                    flow.SubmitPlayerSkill(1).Kind,
                    Is.EqualTo(BattleFlowStepKind.ActionStarted));
                Assert.That(owner.CurrentMn, Is.EqualTo(1000));

                BattleFlowStep resolved = null;
                Assert.DoesNotThrow(() => resolved = flow.Advance());
                Assert.That(resolved.Kind, Is.EqualTo(BattleFlowStepKind.ActionResolved));
                Assert.That(resolved.Actor, Is.SameAs(owner));
                Assert.That(owner.CurrentMn, Is.EqualTo(900));
            }
            finally
            {
                Object.DestroyImmediate(rain);
                Object.DestroyImmediate(passiveCatalog);
                Object.DestroyImmediate(passive);
                Object.DestroyImmediate(skillCatalog);
                Object.DestroyImmediate(skill);
            }
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
                baseDamage: 80,
                fireScalingPercent: 100,
                baseChainCount: 1,
                chainGain: 1);
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
                electricBaseDamage: 25,
                fireTimingPercent: 100);
            var machine =
                ScriptableObject.CreateInstance<SkillMachineItemAsset>();
            machine.ConfigureForEditor(
                ItemIds.GetSkillMachineItemId(skill.SkillId),
                "Skill Machine",
                null,
                string.Empty,
                ItemCategory.SkillMachine,
                1000);
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
                baseDamage: 400);
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
                electricBaseDamage: 10,
                aquaBaseDamage: 10,
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
                baseValue: 25,
                baseDurationTicks: 25,
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
                baseToxinValue: 150,
                poisonScalingPercent: 100,
                baseApplicationPercent: 100,
                scaledApplicationBasePercent: 20,
                applicationPoisonScalingPercent: 100,
                toxinStatus: ToxinStatus);
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
                poisonScalingPercent: 100,
                aoeFirePercent: 5,
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
                durationTicks: 80,
                baseShieldValue: 300,
                shieldPoisonScalingPercent: 100,
                baseToxinReductionPercent: 50,
                reductionPoisonScalingPercent: 100);
            return skill;
        }

        private static WaterPulseSkillAsset CreateWaterPulseSkill()
        {
            var skill = ScriptableObject.CreateInstance<WaterPulseSkillAsset>();
            skill.ConfigureForEditor(
                skillId: 10,
                displayName: "Water Pulse",
                baseRecoveryTicks: 150,
                baseCooldownTicks: 300,
                description: string.Empty,
                aquaDamageRatio: 100);
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
        public void FrozenGround_SlowsChillDecayWithoutTransformingIt()
        {
            var chill = ScriptableObject.CreateInstance<SlowStatusAsset>();
            chill.ConfigureForEditor(
                BattleStatusId.Chill,
                "Chill",
                string.Empty,
                decayPerTick: 1,
                usesAttributeDefense: false,
                speedReductionScale: 50);
            var field = ScriptableObject
                .CreateInstance<FrozenGroundFieldEffectAsset>();
            field.ConfigureForEditor(
                "Frozen Ground",
                string.Empty,
                iceValueRatio: 100,
                durationDoubleValue: 500);
            var passive = ScriptableObject
                .CreateInstance<FrozenGroundPassiveAsset>();
            passive.ConfigureForEditor(
                passiveId: 30,
                displayName: "Frozen Ground",
                description: string.Empty,
                fieldEffect: field);
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            catalog.SetPassivesForEditor(new PassiveAsset[] { passive });
            var owner = CreateBattleUnitWithPassive(
                "player_1",
                BattleSide.Player,
                0,
                passive.PassiveId,
                (PachimonStatType.Ice, 250));
            var target = CreateBattleUnitWithStats(
                "enemy_1",
                BattleSide.Enemy,
                0,
                2000,
                1,
                (PachimonStatType.Ice, 250));
            var state = new BattleState(
                123,
                CreateTestSide(BattleSide.Player, owner),
                CreateTestSide(BattleSide.Enemy, target),
                new PassiveLogicRegistry(catalog));

            state.Fields.CreateFrozenGround(target, field);

            Assert.That(state.Fields.Effects.Count, Is.EqualTo(1));
            Assert.That(state.Fields.Effects.Single().Value, Is.EqualTo(500));
            Assert.That(field.CalculateChillDecayMultiplier(400), Is.EqualTo(5m / 9m));
            Assert.That(field.CalculateChillDecayMultiplier(500), Is.EqualTo(0.5m));
            Assert.That(field.CalculateChillDecayMultiplier(600), Is.EqualTo(5m / 11m));

            state.Statuses.ApplyStatus(
                target,
                new BattleStatusInstance(
                    BattleStatusId.Chill,
                    BattleStatusCategory.Slow,
                    owner,
                    value: 100,
                    definition: chill));

            Assert.That(target.GetStatus(BattleStatusId.Chill)?.Value, Is.EqualTo(100));
            Assert.That(target.GetStatus(BattleStatusId.Freeze), Is.Null);
            Assert.That(target.GetStatus(BattleStatusId.Chill)?.GetSpeedReduction(),
                Is.EqualTo(70));

            state.Timeline.AdvanceToTick(state.CurrentTick + 99);
            Assert.That(target.GetStatus(BattleStatusId.Chill)?.Value, Is.EqualTo(51));
            state.Timeline.AdvanceToTick(state.CurrentTick + 1);
            Assert.That(target.GetStatus(BattleStatusId.Chill)?.Value, Is.EqualTo(50));
        }

        [TestCase(50, 50)]
        [TestCase(100, 70)]
        [TestCase(150, 86)]
        [TestCase(200, 100)]
        [TestCase(500, 158)]
        public void Chill_UsesSquareRootSpeedReduction(int value, int expected)
        {
            var chill = ScriptableObject.CreateInstance<SlowStatusAsset>();
            chill.ConfigureForEditor(
                BattleStatusId.Chill,
                "Chill",
                string.Empty,
                decayPerTick: 1,
                usesAttributeDefense: false,
                speedReductionScale: 50);
            var status = new BattleStatusInstance(
                BattleStatusId.Chill,
                BattleStatusCategory.Slow,
                source: null,
                value: value,
                definition: chill);

            Assert.That(status.GetSpeedReduction(), Is.EqualTo(expected));
        }

        [Test]
        public void IceGrowthPassive_GainsIceFromNonDotDamageOnly()
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

            BattleStatusDamageService.ApplyAttribute(
                state,
                target,
                BattleStatusId.Chill,
                PachimonAttribute.Ice,
                baseDamage: 100m,
                tags: DamageTag.DamageOverTime);
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
            field.ConfigureForEditor(
                "氷の刃",
                string.Empty,
                damagePercent: 50);
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
                skill.SkillId,
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
            Assert.That(target.CurrentHp, Is.EqualTo(1900));
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

                var passiveDamage = BattleAttributeDamageService.Apply(
                    state,
                    source,
                    target,
                    new DamageContext(
                        DamageOriginKind.Passive,
                        passive.PassiveId,
                        100,
                        source.StartingStats,
                        target.StartingStats,
                        PachimonAttribute.Dragon,
                        isAttack: false,
                        applyAttackerAttributeMultiplier: false,
                        applyDamageBonusMultiplier: false));

                Assert.That(first.FinalDamage, Is.EqualTo(100));
                Assert.That(second.FinalDamage, Is.EqualTo(110));
                Assert.That(passiveDamage.FinalDamage, Is.EqualTo(100));
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
                    80,
                    100,
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
                Assert.That(
                    player.GetStatus(BattleStatusId.Footwork)?.RemainingTicks,
                    Is.EqualTo(80));
                var attackHit = new SkillExecutionContext(
                    state,
                    enemies.GetUnitAt(0),
                    skill).BeginAttackHit(player);
                var damage = ApplyUnscaledAttributeDamage(
                    state,
                    enemies.GetUnitAt(0),
                    player,
                    PachimonAttribute.Fire,
                    attackHit);

                Assert.That(damage.FinalDamage, Is.Zero);
                Assert.That(attackHit.Outcome, Is.EqualTo(SkillHitOutcome.Evaded));
                Assert.That(player.CurrentHp, Is.EqualTo(2000));
                Assert.That(player.GetStatus(BattleStatusId.Footwork), Is.Null);
                Assert.That(
                    player.GetBattleStatValue(PachimonStatType.Speed),
                    Is.EqualTo(120));

                attackHit.ApplyStatus(
                    new BattleStatusInstance(
                        BattleStatusId.Paralysis,
                        BattleStatusCategory.Slow,
                        enemies.GetUnitAt(0),
                        value: 100));
                Assert.That(
                    player.GetStatus(BattleStatusId.Paralysis),
                    Is.Null,
                    "A Status attached to the evaded attack must also be evaded.");

                new DragonFootworkSkillLogic(skill).Resolve(
                    new SkillExecutionContext(state, player, skill));
                state.Statuses.ApplyAttackStatus(
                    player,
                    new BattleStatusInstance(
                        BattleStatusId.Paralysis,
                        BattleStatusCategory.Slow,
                        enemies.GetUnitAt(0),
                        value: 100));

                Assert.That(
                    player.GetStatus(BattleStatusId.Paralysis),
                    Is.Null,
                    "A Status-only enemy attack must be evaded.");
                Assert.That(player.GetStatus(BattleStatusId.Footwork), Is.Null);
                Assert.That(
                    player.GetBattleStatValue(PachimonStatType.Speed),
                    Is.EqualTo(140));

                new DragonFootworkSkillLogic(skill).Resolve(
                    new SkillExecutionContext(state, player, skill));
                var multiStatusHit = new SkillExecutionContext(
                    state,
                    enemies.GetUnitAt(0),
                    skill).BeginStatusHit(player);
                multiStatusHit.ApplyStatus(new BattleStatusInstance(
                    BattleStatusId.Paralysis,
                    BattleStatusCategory.Slow,
                    enemies.GetUnitAt(0),
                    value: 100));
                multiStatusHit.ApplyStatus(new BattleStatusInstance(
                    BattleStatusId.Chill,
                    BattleStatusCategory.Slow,
                    enemies.GetUnitAt(0),
                    value: 100));

                Assert.That(player.GetStatus(BattleStatusId.Paralysis), Is.Null);
                Assert.That(player.GetStatus(BattleStatusId.Chill), Is.Null);
                Assert.That(
                    player.GetBattleStatValue(PachimonStatType.Speed),
                    Is.EqualTo(160),
                    "One evaded Hit must emit only one AttackEvadedEvent.");
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
        public void HealingWind_TargetsAllyWithLowestHpPercentage()
        {
            var skill = ScriptableObject.CreateInstance<HealingWindSkillAsset>();
            try
            {
                skill.ConfigureForEditor(
                    31,
                    "治癒の風",
                    100,
                    300,
                    80,
                    string.Empty,
                    100);

                var user = CreateBattleUnitWithMaxHp(
                    "healer", BattleSide.Player, 0, 1000, 1000, 31,
                    (PachimonStatType.Wind, 0));
                var lowerPercentage = CreateBattleUnitWithMaxHp(
                    "lower_percentage", BattleSide.Player, 1, 2000, 500, 31);
                var lowerAbsoluteHp = CreateBattleUnitWithMaxHp(
                    "lower_absolute", BattleSide.Player, 2, 1000, 300, 31);
                var state = new BattleState(
                    123,
                    new BattleSideState(
                        BattleSide.Player,
                        new[] { user, lowerPercentage, lowerAbsoluteHp }),
                    CreateTestSide(BattleSide.Enemy));

                new HealingWindSkillLogic(skill).Resolve(
                    new SkillExecutionContext(state, user, skill));

                Assert.That(lowerPercentage.CurrentHp, Is.EqualTo(600));
                Assert.That(lowerAbsoluteHp.CurrentHp, Is.EqualTo(300));
                Assert.That(
                    lowerPercentage.GetStatus(BattleStatusId.HealingWind),
                    Is.Null);
                Assert.That(
                    lowerAbsoluteHp.GetStatus(BattleStatusId.HealingWind),
                    Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(skill);
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
                    40, "龍の怒り", string.Empty, 25);
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

                Assert.That(result.FinalDamage, Is.EqualTo(27));
                Assert.That(
                    result.Calculation.Context.Penetration
                        .ResistBonusPercentage,
                    Is.EqualTo(20m));
                Assert.That(
                    result.Calculation.Context.Penetration
                        .AttributePercentage,
                    Is.Zero);
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
        public void DragonDefense_RedirectsHitsButNotStatusDamage()
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

                var statusOnlyHit = new SkillExecutionContext(
                    state,
                    enemies.GetUnitAt(0),
                    skill).BeginStatusHit(intended);
                statusOnlyHit.ApplyStatus(new BattleStatusInstance(
                    BattleStatusId.Paralysis,
                    BattleStatusCategory.Slow,
                    enemies.GetUnitAt(0),
                    value: 100));
                Assert.That(
                    intended.GetStatus(BattleStatusId.Paralysis),
                    Is.Null);
                Assert.That(
                    protector.GetStatus(BattleStatusId.Paralysis),
                    Is.Not.Null,
                    "A Status-only Hit must be redirected.");

                var attachedStatusHit = new SkillExecutionContext(
                    state,
                    enemies.GetUnitAt(0),
                    skill).BeginAttackHit(intended);
                ApplyUnscaledAttributeDamage(
                    state,
                    enemies.GetUnitAt(0),
                    intended,
                    PachimonAttribute.Fire,
                    attachedStatusHit);
                attachedStatusHit.ApplyStatus(new BattleStatusInstance(
                    BattleStatusId.Chill,
                    BattleStatusCategory.Slow,
                    enemies.GetUnitAt(0),
                    value: 100));
                Assert.That(intended.GetStatus(BattleStatusId.Chill), Is.Null);
                Assert.That(
                    protector.GetStatus(BattleStatusId.Chill),
                    Is.Not.Null,
                    "A Status attached to damage must follow its redirected Hit.");
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
            PachimonAttribute attribute,
            SkillHit hit = null)
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
                    applyDamageBonusMultiplier: false),
                hit);
        }

        [Test]
        public void WaterCutter_ScalesDamageAndPenetrationSeparately()
        {
            var skill = ScriptableObject.CreateInstance<WaterCutterSkillAsset>();
            try
            {
                skill.ConfigureForEditor(
                    42,
                    "ウォーターカッター",
                    100,
                    300,
                    100,
                    string.Empty,
                    100,
                    100,
                    25);
                var user = CreateBattleUnitWithStats(
                    "water_cutter_user",
                    BattleSide.Player,
                    0,
                    2000,
                    skill.SkillId,
                    (PachimonStatType.Aqua, 100),
                    (PachimonStatType.Wind, 100));
                var target = CreateBattleUnitWithStats(
                    "water_cutter_target",
                    BattleSide.Enemy,
                    0,
                    2000,
                    1,
                    (PachimonStatType.Aqua, 100));
                var state = new BattleState(
                    123,
                    CreateTestSide(BattleSide.Player, user),
                    CreateTestSide(BattleSide.Enemy, target));

                var resolution = new WaterCutterSkillLogic(skill)
                    .Resolve(new SkillExecutionContext(state, user, skill));

                Assert.That(resolution.Effects.Single().Damage, Is.EqualTo(111));
                Assert.That(target.CurrentHp, Is.EqualTo(1889));
            }
            finally
            {
                Object.DestroyImmediate(skill);
            }
        }

        [Test]
        public void MuddyWater_AppliesDamageAndSlowThroughOneHit()
        {
            var slow = CreateSlowStatus(
                BattleStatusId.Slow,
                "鈍足",
                usesAttributeDefense: false,
                PachimonAttribute.Poison);
            var skill = ScriptableObject.CreateInstance<MuddyWaterSkillAsset>();
            try
            {
                skill.ConfigureForEditor(
                    50,
                    "泥水",
                    100,
                    300,
                    100,
                    string.Empty,
                    100,
                    100,
                    100,
                    100,
                    slow);
                var user = CreateBattleUnitWithStats(
                    "muddy_water_user",
                    BattleSide.Player,
                    0,
                    2000,
                    skill.SkillId,
                    (PachimonStatType.Aqua, 100),
                    (PachimonStatType.Poison, 100));
                var target = CreateBattleUnitWithStats(
                    "muddy_water_target",
                    BattleSide.Enemy,
                    0,
                    2000,
                    1);
                var state = new BattleState(
                    123,
                    CreateTestSide(BattleSide.Player, user),
                    CreateTestSide(BattleSide.Enemy, target));

                var resolution = new MuddyWaterSkillLogic(skill)
                    .Resolve(new SkillExecutionContext(state, user, skill));

                Assert.That(resolution.Effects.Single().Damage, Is.EqualTo(200));
                Assert.That(target.GetStatus(BattleStatusId.Slow)?.Value,
                    Is.EqualTo(200));
                Assert.That(resolution.Effects.Single().Hit, Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(skill);
                Object.DestroyImmediate(slow);
            }
        }

        [Test]
        public void WaterSpout_AddsCurrentHpRatioToAquaMultiplier()
        {
            var skill = ScriptableObject.CreateInstance<WaterSpoutSkillAsset>();
            try
            {
                skill.ConfigureForEditor(
                    58,
                    "しおふき",
                    120,
                    350,
                    120,
                    string.Empty,
                    100,
                    100,
                    2000);
                var user = CreateBattleUnitWithStats(
                    "water_spout_user",
                    BattleSide.Player,
                    0,
                    1000,
                    skill.SkillId,
                    (PachimonStatType.Aqua, 100));
                var target = CreateBattleUnitWithStats(
                    "water_spout_target",
                    BattleSide.Enemy,
                    0,
                    2000,
                    1);
                var state = new BattleState(
                    123,
                    CreateTestSide(BattleSide.Player, user),
                    CreateTestSide(BattleSide.Enemy, target));

                var resolution = new WaterSpoutSkillLogic(skill)
                    .Resolve(new SkillExecutionContext(state, user, skill));

                Assert.That(resolution.Effects.Single().Damage, Is.EqualTo(250));
                Assert.That(target.CurrentHp, Is.EqualTo(1750));
            }
            finally
            {
                Object.DestroyImmediate(skill);
            }
        }

        [Test]
        public void WhalePassive_SupportsFractionalDerivedPercent()
        {
            var passive = CreateDerivedPassive(
                58,
                "クジラ",
                PachimonStatType.Aqua,
                PachimonStatType.MaxHp,
                percent: 1.5f);
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            try
            {
                catalog.SetPassivesForEditor(new PassiveAsset[] { passive });
                var stats = EffectivePachimonStats.Calculate(
                    CreateStats(
                        (PachimonStatType.MaxHp, 2000),
                        (PachimonStatType.Aqua, 100)),
                    new PassiveStatModifierRegistry(catalog)
                        .CreateModifiers(new[] { passive.PassiveId }));

                Assert.That(
                    stats.GetValue(PachimonStatType.Aqua),
                    Is.EqualTo(130));
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(passive);
            }
        }

        [Test]
        public void WaterCutting_ContinuesTurnAfterOwnerSkillDefeatsEnemy()
        {
            var passive = ScriptableObject
                .CreateInstance<WaterCuttingPassiveAsset>();
            var skill = ScriptableObject.CreateInstance<WaterCutterSkillAsset>();
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            try
            {
                passive.ConfigureForEditor(42, "水切り", string.Empty);
                skill.ConfigureForEditor(
                    42,
                    "ウォーターカッター",
                    100,
                    300,
                    100,
                    string.Empty,
                    100,
                    100,
                    25);
                catalog.SetPassivesForEditor(new PassiveAsset[] { passive });
                var owner = CreateBattleUnitWithPassive(
                    "water_cutting_owner",
                    BattleSide.Player,
                    0,
                    passive.PassiveId,
                    (PachimonStatType.Aqua, 100));
                var target = CreateBattleUnitWithStats(
                    "water_cutting_target",
                    BattleSide.Enemy,
                    0,
                    100,
                    1);
                var state = new BattleState(
                    123,
                    CreateTestSide(BattleSide.Player, owner),
                    CreateTestSide(BattleSide.Enemy, target),
                    new PassiveLogicRegistry(catalog));
                var resolution = new WaterCutterSkillLogic(skill)
                    .Resolve(new SkillExecutionContext(state, owner, skill));

                Assert.That(target.IsDefeated, Is.True);
                Assert.That(state.Passives.ShouldContinueTurn(state, resolution),
                    Is.True);

                Assert.That(state.Timeline.TryBeginNextTurn(out var actor), Is.True);
                state.Timeline.CompleteImmediateAction(
                    actor,
                    usedSkillSlotId: 1,
                    new BattleSkillTimingPlan(0, 100, 100),
                    continueTurn: true);
                Assert.That(state.Timeline.TryBeginNextTurn(out var nextActor), Is.True);
                Assert.That(nextActor, Is.SameAs(actor));
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(skill);
                Object.DestroyImmediate(passive);
            }
        }

        [Test]
        public void Evaporation_CombinesReferencesIntoOneFireDamageAndWeakness()
        {
            var weakness = ScriptableObject.CreateInstance<WeaknessStatusAsset>();
            var skill = ScriptableObject.CreateInstance<EvaporationSkillAsset>();
            try
            {
                weakness.ConfigureForEditor("弱点", string.Empty);
                ConfigureEvaporation(skill, weakness);
                var user = CreateBattleUnitWithStats(
                    "evaporation_user",
                    BattleSide.Player,
                    0,
                    2000,
                    skill.SkillId,
                    (PachimonStatType.Fire, 100),
                    (PachimonStatType.Aqua, 100));
                var target = CreateBattleUnitWithStats(
                    "evaporation_target",
                    BattleSide.Enemy,
                    0,
                    2000,
                    1,
                    (PachimonStatType.Fire, 100));
                var state = new BattleState(
                    123,
                    CreateTestSide(BattleSide.Player, user),
                    CreateTestSide(BattleSide.Enemy, target));

                var resolution = new EvaporationSkillLogic(skill)
                    .Resolve(new SkillExecutionContext(state, user, skill));

                Assert.That(resolution.Effects.Count, Is.EqualTo(1));
                Assert.That(resolution.Effects.Single().Damage, Is.EqualTo(168));
                Assert.That(target.GetStatus(BattleStatusId.Weakness)?.Value,
                    Is.EqualTo(40));
            }
            finally
            {
                Object.DestroyImmediate(skill);
                Object.DestroyImmediate(weakness);
            }
        }

        [Test]
        public void Weakness_IncreasesAndIsConsumedByNextAttributeDamage()
        {
            var weakness = ScriptableObject.CreateInstance<WeaknessStatusAsset>();
            try
            {
                weakness.ConfigureForEditor("弱点", string.Empty);
                var source = CreateBattleUnitWithStats(
                    "weakness_source",
                    BattleSide.Player,
                    0,
                    2000,
                    1);
                var target = CreateBattleUnitWithStats(
                    "weakness_target",
                    BattleSide.Enemy,
                    0,
                    2000,
                    1,
                    (PachimonStatType.Fire, 100));
                var state = new BattleState(
                    123,
                    CreateTestSide(BattleSide.Player, source),
                    CreateTestSide(BattleSide.Enemy, target));
                target.ApplyOrReplaceStatus(new BattleStatusInstance(
                    BattleStatusId.Weakness,
                    BattleStatusCategory.None,
                    source,
                    40,
                    definition: weakness));

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
                        applyAttackerAttributeMultiplier: false));

                Assert.That(result.AppliedDamage, Is.EqualTo(70));
                Assert.That(target.GetStatus(BattleStatusId.Weakness), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(weakness);
            }
        }

        [Test]
        public void Weakness_AppliesToEveryDamageComponentInOneSkillHit()
        {
            var weakness = ScriptableObject.CreateInstance<WeaknessStatusAsset>();
            var skill = CreateAquaShock();
            try
            {
                weakness.ConfigureForEditor("弱点", string.Empty);
                var source = CreateBattleUnitWithStats(
                    "weakness_multi_source",
                    BattleSide.Player,
                    0,
                    2000,
                    skill.SkillId);
                var target = CreateBattleUnitWithStats(
                    "weakness_multi_target",
                    BattleSide.Enemy,
                    0,
                    2000,
                    1);
                var state = new BattleState(
                    123,
                    CreateTestSide(BattleSide.Player, source),
                    CreateTestSide(BattleSide.Enemy, target));
                target.ApplyOrReplaceStatus(new BattleStatusInstance(
                    BattleStatusId.Weakness,
                    BattleStatusCategory.None,
                    source,
                    50,
                    definition: weakness));

                var resolution = new AquaShockSkillLogic(skill)
                    .Resolve(new SkillExecutionContext(state, source, skill));

                Assert.That(resolution.Effects.Single().Damage, Is.EqualTo(30));
                Assert.That(target.GetStatus(BattleStatusId.Weakness), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(weakness);
                Object.DestroyImmediate(skill);
            }
        }

        [Test]
        public void WeaklingBully_MultipliesDamageAndRefreshesSpeedStatus()
        {
            var weakness = ScriptableObject.CreateInstance<WeaknessStatusAsset>();
            var speed = ScriptableObject
                .CreateInstance<WeaklingBullySpeedStatusAsset>();
            var passive = ScriptableObject
                .CreateInstance<WeaklingBullyPassiveAsset>();
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            try
            {
                weakness.ConfigureForEditor("弱点", string.Empty);
                speed.ConfigureForEditor("弱いものイジメ", string.Empty);
                passive.ConfigureForEditor(
                    57,
                    "弱いものイジメ",
                    string.Empty,
                    130,
                    30,
                    100,
                    speed);
                catalog.SetPassivesForEditor(new PassiveAsset[] { passive });
                var owner = CreateBattleUnitWithPassive(
                    "weakling_bully_owner",
                    BattleSide.Player,
                    0,
                    passive.PassiveId);
                var target = CreateBattleUnitWithStats(
                    "weakling_bully_target",
                    BattleSide.Enemy,
                    0,
                    2000,
                    1);
                var state = new BattleState(
                    123,
                    CreateTestSide(BattleSide.Player, owner),
                    CreateTestSide(BattleSide.Enemy, target),
                    new PassiveLogicRegistry(catalog));
                target.ApplyOrReplaceStatus(new BattleStatusInstance(
                    BattleStatusId.Weakness,
                    BattleStatusCategory.None,
                    owner,
                    30,
                    definition: weakness));

                var result = ApplyUnscaledAttributeDamage(
                    state,
                    owner,
                    target,
                    PachimonAttribute.Fire);

                Assert.That(result.AppliedDamage, Is.EqualTo(169));
                Assert.That(
                    owner.GetBattleStatValue(PachimonStatType.Speed),
                    Is.EqualTo(30));
                Assert.That(
                    owner.GetStatus(BattleStatusId.WeaklingBullySpeed)
                        ?.RemainingTicks,
                    Is.EqualTo(100));

                state.Timeline.AdvanceToTick(50);
                target.ApplyOrReplaceStatus(new BattleStatusInstance(
                    BattleStatusId.Weakness,
                    BattleStatusCategory.None,
                    owner,
                    30,
                    definition: weakness));
                ApplyUnscaledAttributeDamage(
                    state,
                    owner,
                    target,
                    PachimonAttribute.Fire);

                Assert.That(
                    owner.GetStatus(BattleStatusId.WeaklingBullySpeed)
                        ?.RemainingTicks,
                    Is.EqualTo(100));
                Assert.That(
                    owner.GetBattleStatValue(PachimonStatType.Speed),
                    Is.EqualTo(30));
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(passive);
                Object.DestroyImmediate(speed);
                Object.DestroyImmediate(weakness);
            }
        }

        [Test]
        public void Plants_AreIndependentAndBotanicalGardenCountsOwnSide()
        {
            var beat = ScriptableObject.CreateInstance<BeatVineFieldEffectAsset>();
            var fire = ScriptableObject.CreateInstance<FireVineFieldEffectAsset>();
            var passive = ScriptableObject.CreateInstance<BotanicalGardenPassiveAsset>();
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            try
            {
                beat.ConfigureForEditor(
                    "Beat Vine",
                    string.Empty,
                    30,
                    100,
                    100);
                fire.ConfigureForEditor(
                    "Fire Vine",
                    string.Empty,
                    15,
                    100,
                    15,
                    100);
                passive.ConfigureForEditor(
                    51,
                    "Botanical Garden",
                    string.Empty,
                    15);
                catalog.SetPassivesForEditor(new PassiveAsset[] { passive });
                var owner = CreateBattleUnitWithPassive(
                    "garden_owner",
                    BattleSide.Player,
                    0,
                    passive.PassiveId);
                var state = new BattleState(
                    123,
                    CreateTestSide(BattleSide.Player, owner),
                    CreateTestSide(BattleSide.Enemy),
                    new PassiveLogicRegistry(catalog));

                var first = state.Fields.CreateBeatVine(owner, beat, 30);
                var second = state.Fields.CreateBeatVine(owner, beat, 30);
                state.Fields.CreateFireVine(owner, fire, 15, 15);
                state.Fields.CreateBeatVine(
                    state.Enemy.GetUnitAt(0),
                    beat,
                    30);

                Assert.That(second, Is.Not.SameAs(first));
                Assert.That(
                    state.Fields.CountEffects(
                        BattleSide.Player,
                        BattleFieldEffectCategory.Plant),
                    Is.EqualTo(3));
                Assert.That(
                    owner.GetBattleStatValue(PachimonStatType.DamageBonus),
                    Is.EqualTo(45));
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(passive);
                Object.DestroyImmediate(fire);
                Object.DestroyImmediate(beat);
            }
        }

        [Test]
        public void FireVine_ReactsToAlliedDamageWithoutRecursiveFieldTriggers()
        {
            var definition = ScriptableObject
                .CreateInstance<FireVineFieldEffectAsset>();
            try
            {
                definition.ConfigureForEditor(
                    "Fire Vine",
                    string.Empty,
                    15,
                    100,
                    15,
                    100);
                var state = new BattleState(
                    123,
                    CreateTestSide(BattleSide.Player),
                    CreateTestSide(BattleSide.Enemy));
                var source = state.Player.GetUnitAt(0);
                var target = state.Enemy.GetUnitAt(0);
                state.Fields.CreateFireVine(source, definition, 15, 15);

                ApplyUnscaledAttributeDamage(
                    state,
                    source,
                    target,
                    PachimonAttribute.Fire);

                Assert.That(target.CurrentHp, Is.EqualTo(1870));
                Assert.That(
                    state.LogEntries.Count(entry =>
                        entry.Contains("Fire Vine")),
                    Is.EqualTo(2),
                    "Creation and one reaction should be logged exactly once each.");
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void BurningFlower_GrowsFromFireAndLeafDamageAcrossAllSides()
        {
            var leafGrowth = ScriptableObject
                .CreateInstance<BurningFlowerGrowthStatusAsset>();
            var fireGrowth = ScriptableObject
                .CreateInstance<BurningFlowerGrowthStatusAsset>();
            var passive = ScriptableObject
                .CreateInstance<BurningFlowerPassiveAsset>();
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            try
            {
                leafGrowth.ConfigureForEditor(
                    BattleStatusId.BurningFlowerLeaf,
                    "Leaf Growth",
                    string.Empty);
                fireGrowth.ConfigureForEditor(
                    BattleStatusId.BurningFlowerFire,
                    "Fire Growth",
                    string.Empty);
                passive.ConfigureForEditor(
                    59,
                    "Burning Flower",
                    string.Empty,
                    5,
                    leafGrowth,
                    fireGrowth);
                catalog.SetPassivesForEditor(new PassiveAsset[] { passive });
                var owner = CreateBattleUnitWithPassive(
                    "flower_owner",
                    BattleSide.Player,
                    0,
                    passive.PassiveId);
                var state = new BattleState(
                    123,
                    CreateTestSide(BattleSide.Player, owner),
                    CreateTestSide(BattleSide.Enemy),
                    new PassiveLogicRegistry(catalog));
                var enemySource = state.Enemy.GetUnitAt(0);
                var enemyTarget = state.Enemy.GetUnitAt(1);

                ApplyUnscaledAttributeDamage(
                    state,
                    enemySource,
                    enemyTarget,
                    PachimonAttribute.Fire);
                ApplyUnscaledAttributeDamage(
                    state,
                    enemySource,
                    enemyTarget,
                    PachimonAttribute.Leaf);

                BattleStatusDamageService.ApplyAttribute(
                    state,
                    enemyTarget,
                    BattleStatusId.Burn,
                    PachimonAttribute.Fire,
                    baseDamage: 100m,
                    tags: DamageTag.DamageOverTime);
                BattleStatusDamageService.ApplyAttribute(
                    state,
                    enemyTarget,
                    BattleStatusId.Pollen,
                    PachimonAttribute.Leaf,
                    baseDamage: 100m,
                    tags: DamageTag.DamageOverTime);

                Assert.That(
                    owner.GetBattleStatValue(PachimonStatType.Leaf),
                    Is.EqualTo(5));
                Assert.That(
                    owner.GetBattleStatValue(PachimonStatType.Fire),
                    Is.EqualTo(5));
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(passive);
                Object.DestroyImmediate(fireGrowth);
                Object.DestroyImmediate(leafGrowth);
            }
        }

        [Test]
        public void Thunder_AmplifiesElectricAddsSpeedAndAttacksEveryone()
        {
            var thunder = ScriptableObject.CreateInstance<ThunderWeatherAsset>();
            try
            {
                thunder.ConfigureForEditor(
                    "Thunder", string.Empty,
                    electricRatioScalingPercent: 10,
                    speedFromElectricRatio: 10,
                    attackIntervalTicks: 1,
                    damageDivisor: 3);
                var source = CreateBattleUnitWithStats(
                    "thunder_source", BattleSide.Player, 0, 2000, 1,
                    (PachimonStatType.Electric, 100));
                var state = new BattleState(
                    123,
                    CreateTestSide(BattleSide.Player, source),
                    CreateTestSide(BattleSide.Enemy));
                state.Weather.CreateOrAdd(source, thunder, 400);

                Assert.That(
                    state.ResolveAttributeRatio(PachimonAttribute.Electric, 100m),
                    Is.EqualTo(140m));
                Assert.That(
                    source.GetBattleStatValue(PachimonStatType.Speed),
                    Is.EqualTo(10));

                state.Timeline.AdvanceToTick(state.CurrentTick + 1);

                Assert.That(source.CurrentHp, Is.EqualTo(1867));
                Assert.That(state.Enemy.GetUnitAt(0).CurrentHp, Is.EqualTo(1867));
            }
            finally
            {
                Object.DestroyImmediate(thunder);
            }
        }

        [Test]
        public void ThunderMan_AddsSpeedOnlyWhileThunderExists()
        {
            var thunder = ScriptableObject.CreateInstance<ThunderWeatherAsset>();
            var passive = ScriptableObject.CreateInstance<ThunderManPassiveAsset>();
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            try
            {
                thunder.ConfigureForEditor("Thunder", string.Empty, 10, 10, 150, 3);
                passive.ConfigureForEditor(52, "Thunder Man", string.Empty, 40);
                catalog.SetPassivesForEditor(new PassiveAsset[] { passive });
                var owner = CreateBattleUnitWithPassive(
                    "thunder_man", BattleSide.Player, 0, passive.PassiveId,
                    (PachimonStatType.Electric, 100));
                var state = new BattleState(
                    123,
                    CreateTestSide(BattleSide.Player, owner),
                    CreateTestSide(BattleSide.Enemy),
                    new PassiveLogicRegistry(catalog));

                Assert.That(owner.GetBattleStatValue(PachimonStatType.Speed), Is.Zero);
                state.Weather.CreateOrAdd(owner, thunder, 400);
                Assert.That(owner.GetBattleStatValue(PachimonStatType.Speed), Is.EqualTo(50));
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(passive);
                Object.DestroyImmediate(thunder);
            }
        }

        [Test]
        public void ElectricShield_CountersTheAttackThatBreaksItsOwnShield()
        {
            var paralysis = CreateSlowStatus(
                BattleStatusId.Paralysis,
                "Paralysis",
                usesAttributeDefense: true,
                PachimonAttribute.Electric);
            var shieldStatus = ScriptableObject.CreateInstance<ElectricShieldStatusAsset>();
            var skill = ScriptableObject.CreateInstance<ElectricShieldSkillAsset>();
            var passive = ScriptableObject
                .CreateInstance<ParalysisGenerationPassiveAsset>();
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            try
            {
                shieldStatus.ConfigureForEditor("Electric Shield", string.Empty, paralysis);
                skill.ConfigureForEditor(
                    60, "Electric Shield", 100, 300, 100, string.Empty,
                    100, 150, 100, 50, 100, 25, 100, 25,
                    paralysis, shieldStatus);
                passive.ConfigureForEditor(60, "Paralysis Generation", string.Empty, 50);
                catalog.SetPassivesForEditor(new PassiveAsset[] { passive });
                var owner = CreateBattleUnitWithPassive(
                    "shield_owner", BattleSide.Player, 0, passive.PassiveId);
                var state = new BattleState(
                    123,
                    CreateTestSide(BattleSide.Player, owner),
                    CreateTestSide(BattleSide.Enemy),
                    new PassiveLogicRegistry(catalog));
                new ElectricShieldSkillLogic(skill).Resolve(
                    new SkillExecutionContext(state, owner, skill));

                Assert.That(owner.TotalShield, Is.EqualTo(150));
                Assert.That(owner.GetStatus(BattleStatusId.Paralysis)?.Value, Is.EqualTo(50));
                Assert.That(
                    owner.GetStatus(BattleStatusId.Paralysis)?.RemainingTicks,
                    Is.EqualTo(100));
                Assert.That(owner.GetBattleStatValue(PachimonStatType.Electric), Is.EqualTo(25));

                var attacker = state.Enemy.GetUnitAt(0);
                BattleAttributeDamageService.Apply(
                    state,
                    attacker,
                    owner,
                    new DamageContext(
                        DamageOriginKind.Skill,
                        1,
                        200,
                        attacker.GetBattleStats(),
                        owner.GetBattleStats(),
                        PachimonAttribute.Fire,
                        isAttack: true,
                        applyAttackerAttributeMultiplier: false,
                        applyDamageBonusMultiplier: false));

                Assert.That(owner.TotalShield, Is.Zero);
                Assert.That(owner.CurrentHp, Is.EqualTo(1950));
                Assert.That(attacker.GetStatus(BattleStatusId.Paralysis)?.Value,
                    Is.EqualTo(25));
                Assert.That(
                    attacker.GetStatus(BattleStatusId.Paralysis)?.RemainingTicks,
                    Is.EqualTo(25));
                Assert.That(owner.GetStatus(BattleStatusId.ElectricShield), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(passive);
                Object.DestroyImmediate(skill);
                Object.DestroyImmediate(shieldStatus);
                Object.DestroyImmediate(paralysis);
            }
        }

        private static void ConfigureEvaporation(
            EvaporationSkillAsset skill,
            WeaknessStatusAsset weakness)
        {
            skill.ConfigureForEditor(
                57,
                "蒸発",
                120,
                300,
                120,
                string.Empty,
                70,
                100,
                70,
                100,
                25,
                25,
                10,
                100,
                10,
                100,
                weakness);
        }

        [Test]
        public void PoisonMist_UsesPoisonAquaAndWindForSeparateValues()
        {
            var field = ScriptableObject.CreateInstance<PoisonMistFieldEffectAsset>();
            var skill = ScriptableObject.CreateInstance<PoisonMistSkillAsset>();
            try
            {
                field.ConfigureForEditor("Poison Mist", string.Empty);
                skill.ConfigureForEditor(
                    53, "Poison Mist", 100, 300, 40, string.Empty,
                    100, 25, 75, 50, 20, 200, field);

                Assert.That(skill.PoisonValueRatio, Is.EqualTo(100));
                Assert.That(skill.AquaDurationRatio, Is.EqualTo(100));
                Assert.That(skill.WindMinimumValueRatio, Is.EqualTo(100));
                Assert.That(skill.CalculateMistValue(100), Is.EqualTo(200));
                Assert.That(skill.CalculateMistValue(200), Is.EqualTo(300));
                Assert.That(skill.CalculateDurationTicks(100), Is.EqualTo(150));
                Assert.That(skill.CalculateMinimumValue(100, 100), Is.EqualTo(40));
            }
            finally
            {
                Object.DestroyImmediate(skill);
                Object.DestroyImmediate(field);
            }
        }

        [Test]
        public void PoisonMist_EvadesQualifyingAttributeAndTrueSkillAttacks()
        {
            var mist = ScriptableObject.CreateInstance<PoisonMistFieldEffectAsset>();
            try
            {
                mist.ConfigureForEditor("Poison Mist", string.Empty);
                var defender = CreateBattleUnitWithStats(
                    "mist_defender", BattleSide.Player, 0, 2000, 1);
                var attacker = CreateBattleUnitWithStats(
                    "mist_attacker", BattleSide.Enemy, 0, 2000, 1);
                var state = new BattleState(
                    123,
                    CreateTestSide(BattleSide.Player, defender),
                    CreateTestSide(BattleSide.Enemy, attacker));
                state.Fields.CreatePoisonMist(defender, mist, 100, 300, 20);

                var attributeResult = BattleAttributeDamageService.Apply(
                    state,
                    attacker,
                    defender,
                    new DamageContext(
                        DamageOriginKind.Skill, 53, 100,
                        attacker.GetBattleStats(), defender.GetBattleStats(),
                        PachimonAttribute.Fire, isAttack: true,
                        applyAttackerAttributeMultiplier: false,
                        applyDamageBonusMultiplier: false));
                var trueResult = BattleTrueDamageService.Apply(
                    state,
                    attacker,
                    defender,
                    new TrueDamageContext(
                        DamageOriginKind.Skill, 53, 100, isAttack: true));
                var largeResult = BattleAttributeDamageService.Apply(
                    state,
                    attacker,
                    defender,
                    new DamageContext(
                        DamageOriginKind.Skill, 53, 101,
                        attacker.GetBattleStats(), defender.GetBattleStats(),
                        PachimonAttribute.Fire, isAttack: true,
                        applyAttackerAttributeMultiplier: false,
                        applyDamageBonusMultiplier: false));

                Assert.That(attributeResult.WasEvaded, Is.True);
                Assert.That(trueResult.WasEvaded, Is.True);
                Assert.That(largeResult.WasEvaded, Is.False);
                Assert.That(defender.CurrentHp, Is.EqualTo(1899));
            }
            finally
            {
                Object.DestroyImmediate(mist);
            }
        }

        [Test]
        public void PoisonMist_MergesValueDurationAndMinimumValue()
        {
            var mist = ScriptableObject.CreateInstance<PoisonMistFieldEffectAsset>();
            try
            {
                mist.ConfigureForEditor("Poison Mist", string.Empty);
                var defender = CreateBattleUnitWithStats(
                    "mist_defender", BattleSide.Player, 0, 2000, 1);
                var state = new BattleState(
                    123,
                    CreateTestSide(BattleSide.Player, defender),
                    CreateTestSide(BattleSide.Enemy,
                        CreateBattleUnitWithStats(
                            "mist_attacker", BattleSide.Enemy, 0, 2000, 1)));

                state.Fields.CreatePoisonMist(defender, mist, 100, 150, 20);
                state.Fields.CreatePoisonMist(defender, mist, 200, 300, 40);

                Assert.That(state.Fields.Effects.Count, Is.EqualTo(1));
                Assert.That(state.Fields.Effects[0].Value, Is.EqualTo(300));
                Assert.That(state.Fields.Effects[0].SecondaryValue, Is.EqualTo(60));
                Assert.That(state.Fields.Effects[0].RemainingTicks, Is.EqualTo(450));
            }
            finally
            {
                Object.DestroyImmediate(mist);
            }
        }

        [Test]
        public void PoisonMagician_GainsPoisonPerNonPoisonSkillDamageHit()
        {
            var growth = ScriptableObject
                .CreateInstance<PoisonMagicianGrowthStatusAsset>();
            var passive = ScriptableObject
                .CreateInstance<PoisonMagicianPassiveAsset>();
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            try
            {
                growth.ConfigureForEditor("Poison Magic", string.Empty);
                passive.ConfigureForEditor(53, "Poison Magician", string.Empty,
                    20, growth);
                catalog.SetPassivesForEditor(new PassiveAsset[] { passive });
                var owner = CreateBattleUnitWithPassive(
                    "poison_magician", BattleSide.Player, 0, 53,
                    (PachimonStatType.Poison, 10));
                var target = CreateBattleUnitWithStats(
                    "poison_target", BattleSide.Enemy, 0, 2000, 1);
                var state = new BattleState(
                    123,
                    CreateTestSide(BattleSide.Player, owner),
                    CreateTestSide(BattleSide.Enemy, target),
                    new PassiveLogicRegistry(catalog));

                ApplyUnscaledAttributeDamage(
                    state, owner, target, PachimonAttribute.Fire);
                ApplyUnscaledAttributeDamage(
                    state, owner, target, PachimonAttribute.Poison);

                Assert.That(
                    owner.GetBattleStatValue(PachimonStatType.Poison),
                    Is.EqualTo(30));
                Assert.That(
                    owner.GetStatus(BattleStatusId.PoisonMagicianGrowth)
                        ?.StackCount,
                    Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(passive);
                Object.DestroyImmediate(growth);
            }
        }

        [Test]
        public void FirstTouch_FullHpTargetReceivesEnhancedHitInsteadOfNormalHit()
        {
            var skill = ScriptableObject.CreateInstance<FirstTouchSkillAsset>();
            try
            {
                skill.ConfigureForEditor(
                    61, "First Touch", 100, 300, 0, string.Empty,
                    75, 50, 300, 150, 100, ToxinStatus);
                var user = CreateBattleUnitWithStats(
                    "first_touch_user", BattleSide.Player, 0, 2000, 61);
                var target = CreateBattleUnitWithStats(
                    "first_touch_target", BattleSide.Enemy, 0, 2000, 1);
                var state = new BattleState(
                    123,
                    CreateTestSide(BattleSide.Player, user),
                    CreateTestSide(BattleSide.Enemy, target));

                var resolution = new FirstTouchSkillLogic(skill).Resolve(
                    new SkillExecutionContext(state, user, skill));

                Assert.That(resolution.Effects.Count, Is.EqualTo(1));
                Assert.That(target.CurrentHp, Is.EqualTo(1700));
                Assert.That(target.GetStatus(BattleStatusId.Toxin)?.Value,
                    Is.EqualTo(150));
            }
            finally
            {
                Object.DestroyImmediate(skill);
            }
        }

        [Test]
        public void FirstTouch_DamagedTargetReceivesNormalHitAndSmallToxin()
        {
            var skill = ScriptableObject.CreateInstance<FirstTouchSkillAsset>();
            try
            {
                skill.ConfigureForEditor(
                    61, "First Touch", 100, 300, 0, string.Empty,
                    75, 50, 300, 150, 100, ToxinStatus);
                var user = CreateBattleUnitWithStats(
                    "first_touch_user", BattleSide.Player, 0, 2000, 61);
                var target = CreateBattleUnitWithStats(
                    "first_touch_target", BattleSide.Enemy, 0, 2000, 1);
                target.ApplyDamage(500);
                var state = new BattleState(
                    123,
                    CreateTestSide(BattleSide.Player, user),
                    CreateTestSide(BattleSide.Enemy, target));

                var resolution = new FirstTouchSkillLogic(skill).Resolve(
                    new SkillExecutionContext(state, user, skill));

                Assert.That(resolution.Effects.Count, Is.EqualTo(1));
                Assert.That(target.CurrentHp, Is.EqualTo(1425));
                Assert.That(target.GetStatus(BattleStatusId.Toxin)?.Value,
                    Is.EqualTo(50));
            }
            finally
            {
                Object.DestroyImmediate(skill);
            }
        }

        [Test]
        public void LastTouch_ExecutesLowHpTargetWithoutConsumingShieldFirst()
        {
            var passive = ScriptableObject.CreateInstance<LastTouchPassiveAsset>();
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            try
            {
                passive.ConfigureForEditor(61, "Last Touch", string.Empty, 4);
                catalog.SetPassivesForEditor(new PassiveAsset[] { passive });
                var owner = CreateBattleUnitWithPassive(
                    "last_touch_owner", BattleSide.Player, 0, 61,
                    (PachimonStatType.Poison, 100));
                var target = CreateBattleUnitWithStats(
                    "last_touch_target", BattleSide.Enemy, 0, 50, 1);
                var state = new BattleState(
                    123,
                    CreateTestSide(BattleSide.Player, owner),
                    CreateTestSide(BattleSide.Enemy, target),
                    new PassiveLogicRegistry(catalog));
                state.SupportEffects.ApplyShield(owner, target, 500, 500);

                ApplyUnscaledAttributeDamage(
                    state, owner, target, PachimonAttribute.Poison);

                Assert.That(target.IsDefeated, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(passive);
            }
        }

        [Test]
        public void IcePebble_DamagesChillsAndAppliesTimedShield()
        {
            var skill = ScriptableObject.CreateInstance<IcePebbleSkillAsset>();
            try
            {
                skill.ConfigureForEditor(54, "Ice Pebble", 100, 300, 100,
                    string.Empty, 70, 35, 70, 100, 100, ChillStatus);
                var user = CreateBattleUnitWithStats(
                    "ice_pebble_user", BattleSide.Player, 0, 2000, 54,
                    (PachimonStatType.Ice, 100));
                var target = CreateBattleUnitWithStats(
                    "ice_pebble_target", BattleSide.Enemy, 0, 2000, 1);
                var state = new BattleState(123,
                    CreateTestSide(BattleSide.Player, user),
                    CreateTestSide(BattleSide.Enemy, target));

                new IcePebbleSkillLogic(skill).Resolve(
                    new SkillExecutionContext(state, user, skill));

                Assert.That(target.CurrentHp, Is.EqualTo(1860));
                Assert.That(target.GetStatus(BattleStatusId.Chill)?.Value,
                    Is.EqualTo(70));
                Assert.That(user.Shields.Single().Value, Is.EqualTo(140));
                Assert.That(user.Shields.Single().RemainingTicks,
                    Is.EqualTo(100));
            }
            finally
            {
                Object.DestroyImmediate(skill);
            }
        }

        [Test]
        public void IceArmor_IncreasesOwnShieldValueAndDuration()
        {
            var passive = ScriptableObject.CreateInstance<IceArmorPassiveAsset>();
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            try
            {
                passive.ConfigureForEditor(54, "Ice Armor", string.Empty, 20);
                catalog.SetPassivesForEditor(new PassiveAsset[] { passive });
                var owner = CreateBattleUnitWithPassive(
                    "ice_armor_owner", BattleSide.Player, 0, 54,
                    (PachimonStatType.Ice, 100));
                var state = new BattleState(123,
                    CreateTestSide(BattleSide.Player, owner),
                    CreateTestSide(BattleSide.Enemy),
                    new PassiveLogicRegistry(catalog));

                state.SupportEffects.ApplyShield(owner, owner, 100, 100);

                Assert.That(owner.Shields.Single().Value, Is.EqualTo(120));
                Assert.That(owner.Shields.Single().RemainingTicks,
                    Is.EqualTo(120));
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(passive);
            }
        }

        [Test]
        public void FrostArrow_TargetsFrontmostLowestHpAndRefundsOnDefeat()
        {
            var skill = ScriptableObject.CreateInstance<FrostArrowSkillAsset>();
            try
            {
                skill.ConfigureForEditor(62, "Frost Arrow", 100, 300, 150,
                    string.Empty, 100, 30, 100, ChillStatus);
                var user = CreateBattleUnitWithStats(
                    "frost_arrow_user", BattleSide.Player, 0, 2000, 62);
                var first = CreateBattleUnitWithStats(
                    "frost_arrow_first", BattleSide.Enemy, 0, 500, 1);
                var second = CreateBattleUnitWithStats(
                    "frost_arrow_second", BattleSide.Enemy, 1, 100, 1);
                var third = CreateBattleUnitWithStats(
                    "frost_arrow_third", BattleSide.Enemy, 2, 100, 1);
                var state = new BattleState(123,
                    CreateTestSide(BattleSide.Player, user),
                    new BattleSideState(BattleSide.Enemy,
                        new[] { first, second, third }));
                Assert.That(user.TrySpendMn(150), Is.True);

                var resolution = new FrostArrowSkillLogic(skill).Resolve(
                    new SkillExecutionContext(state, user, skill,
                        actualManaSpent: 150));

                Assert.That(first.CurrentHp, Is.EqualTo(500));
                Assert.That(second.IsDefeated, Is.True);
                Assert.That(third.CurrentHp, Is.EqualTo(100));
                Assert.That(user.CurrentMn, Is.EqualTo(1000));
                Assert.That(resolution.RefundCooldown, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(skill);
            }
        }

        [Test]
        public void ChillSpread_UsesDefeatedTargetsPreDamageChill()
        {
            var passive = ScriptableObject.CreateInstance<ChillSpreadPassiveAsset>();
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            try
            {
                passive.ConfigureForEditor(
                    62, "Chill Spread", string.Empty, 150, ChillStatus);
                catalog.SetPassivesForEditor(new PassiveAsset[] { passive });
                var owner = CreateBattleUnitWithPassive(
                    "chill_spread_owner", BattleSide.Player, 0, 62);
                var defeated = CreateBattleUnitWithStats(
                    "chill_spread_defeated", BattleSide.Enemy, 0, 100, 1);
                var second = CreateBattleUnitWithStats(
                    "chill_spread_second", BattleSide.Enemy, 1, 2000, 1);
                var third = CreateBattleUnitWithStats(
                    "chill_spread_third", BattleSide.Enemy, 2, 2000, 1);
                var state = new BattleState(123,
                    CreateTestSide(BattleSide.Player, owner),
                    new BattleSideState(BattleSide.Enemy,
                        new[] { defeated, second, third }),
                    new PassiveLogicRegistry(catalog));
                state.Statuses.ApplyStatus(defeated,
                    BattleStatusFactory.CreateSlow(owner, 80, ChillStatus));

                ApplyUnscaledAttributeDamage(
                    state, owner, defeated, PachimonAttribute.Ice);

                Assert.That(defeated.IsDefeated, Is.True);
                Assert.That(second.GetStatus(BattleStatusId.Chill)?.Value,
                    Is.EqualTo(120));
                Assert.That(third.GetStatus(BattleStatusId.Chill)?.Value,
                    Is.EqualTo(120));
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(passive);
            }
        }

        [Test]
        public void CuttingDance_ChainsDamageAndErosionAndBuildsSpeed()
        {
            var erosion = ScriptableObject.CreateInstance<WindErosionStatusAsset>();
            var growth = ScriptableObject.CreateInstance<WindRiderGrowthStatusAsset>();
            var passive = ScriptableObject.CreateInstance<WindRiderPassiveAsset>();
            var skill = ScriptableObject.CreateInstance<CuttingDanceSkillAsset>();
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            try
            {
                growth.ConfigureForEditor("Wind Rider", string.Empty);
                passive.ConfigureForEditor(55, "Wind Rider", string.Empty,
                    20, growth);
                skill.ConfigureForEditor(55, "Cutting Dance", 100, 300, 100,
                    string.Empty, 100, 100, 20, 100, 2, 1, erosion);
                catalog.SetPassivesForEditor(new PassiveAsset[] { passive });
                var owner = CreateBattleUnitWithPassive(
                    "cutting_dance_owner", BattleSide.Player, 0, 55,
                    (PachimonStatType.Wind, 100));
                var enemies = CreateTestSide(BattleSide.Enemy);
                var state = new BattleState(123,
                    CreateTestSide(BattleSide.Player, owner), enemies,
                    new PassiveLogicRegistry(catalog));

                var resolution = new CuttingDanceSkillLogic(skill).Resolve(
                    new SkillExecutionContext(state, owner, skill));

                Assert.That(resolution.Effects.Count, Is.EqualTo(3));
                Assert.That(enemies.GetUnitAt(0).CurrentHp, Is.EqualTo(1800));
                Assert.That(enemies.GetUnitAt(1).CurrentHp, Is.EqualTo(1867));
                Assert.That(enemies.GetUnitAt(2).CurrentHp, Is.EqualTo(1934));
                Assert.That(enemies.GetUnitAt(0)
                    .GetStatus(BattleStatusId.WindErosion)?.Value, Is.EqualTo(40));
                Assert.That(enemies.GetUnitAt(1)
                    .GetStatus(BattleStatusId.WindErosion)?.Value, Is.EqualTo(26));
                Assert.That(enemies.GetUnitAt(2)
                    .GetStatus(BattleStatusId.WindErosion)?.Value, Is.EqualTo(13));
                Assert.That(owner.GetBattleStatValue(PachimonStatType.Speed),
                    Is.EqualTo(60));

                BattleAttributeDamageService.Apply(
                    state,
                    owner,
                    enemies.GetUnitAt(0),
                    new DamageContext(
                        DamageOriginKind.Field,
                        (int)BattleFieldEffectId.BeatVine,
                        100,
                        owner.StartingStats,
                        enemies.GetUnitAt(0).StartingStats,
                        PachimonAttribute.Wind,
                        isAttack: false,
                        applyAttackerAttributeMultiplier: false,
                        applyDamageBonusMultiplier: false,
                        applyOutgoingModifiers: false));
                Assert.That(owner.GetBattleStatValue(PachimonStatType.Speed),
                    Is.EqualTo(60));
                Assert.That(owner.GetStatus(
                    BattleStatusId.CuttingDanceChain)?.Value,
                    Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(skill);
                Object.DestroyImmediate(passive);
                Object.DestroyImmediate(growth);
                Object.DestroyImmediate(erosion);
            }
        }

        [Test]
        public void PachikageSpecies_UsesPachikageGraphics()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PachimonCatalog>(
                "Assets/GameData/Pachimon/PachimonCatalog.asset");
            var expectedFront = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Art/Pachimon/SpeciesFire_Pachikage/pachikage_front.png");
            var expectedBack = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Art/Pachimon/SpeciesFire_Pachikage/pachikage_back.png");

            Assert.That(catalog, Is.Not.Null);
            Assert.That(expectedFront, Is.Not.Null);
            Assert.That(expectedBack, Is.Not.Null);
            Assert.That(catalog.Get(1)?.FrontSprite, Is.SameAs(expectedFront));
            Assert.That(catalog.Get(1)?.BackSprite, Is.SameAs(expectedBack));
        }

        [Test]
        public void Kachofugetsu_DealsFourAttributesAndBuildsWindThreeTimes()
        {
            var growth = ScriptableObject.CreateInstance<WindMagicianGrowthStatusAsset>();
            var passive = ScriptableObject.CreateInstance<WindMagicianPassiveAsset>();
            var skill = ScriptableObject.CreateInstance<KachofugetsuSkillAsset>();
            var catalog = ScriptableObject.CreateInstance<PassiveCatalog>();
            try
            {
                growth.ConfigureForEditor("Wind Magician", string.Empty);
                passive.ConfigureForEditor(63, "Wind Magician", string.Empty,
                    10, growth);
                skill.ConfigureForEditor(63, "Kachofugetsu", 100, 300, 150,
                    string.Empty, 50, 100, 50, 100, 50, 100, 50, 100);
                catalog.SetPassivesForEditor(new PassiveAsset[] { passive });
                var owner = CreateBattleUnitWithPassive(
                    "kachofugetsu_owner", BattleSide.Player, 0, 63,
                    (PachimonStatType.Fire, 100),
                    (PachimonStatType.Aqua, 100),
                    (PachimonStatType.Leaf, 100),
                    (PachimonStatType.Wind, 100));
                var target = CreateBattleUnitWithStats(
                    "kachofugetsu_target", BattleSide.Enemy, 0, 2000, 1);
                var state = new BattleState(123,
                    CreateTestSide(BattleSide.Player, owner),
                    CreateTestSide(BattleSide.Enemy, target),
                    new PassiveLogicRegistry(catalog));

                var resolution = new KachofugetsuSkillLogic(skill).Resolve(
                    new SkillExecutionContext(state, owner, skill));

                Assert.That(resolution.Effects.Single().Damage, Is.EqualTo(415));
                Assert.That(target.CurrentHp, Is.EqualTo(1585));
                Assert.That(owner.GetBattleStatValue(PachimonStatType.Wind),
                    Is.EqualTo(130));
                Assert.That(owner.GetStatus(BattleStatusId.WindMagicianGrowth)
                    ?.StackCount, Is.EqualTo(3));

                BattleAttributeDamageService.Apply(
                    state,
                    owner,
                    target,
                    new DamageContext(
                        DamageOriginKind.Field,
                        (int)BattleFieldEffectId.FireVine,
                        100,
                        owner.StartingStats,
                        target.StartingStats,
                        PachimonAttribute.Fire,
                        isAttack: false,
                        applyAttackerAttributeMultiplier: false,
                        applyDamageBonusMultiplier: false,
                        applyOutgoingModifiers: false));
                Assert.That(owner.GetStatus(BattleStatusId.WindMagicianGrowth)
                    ?.StackCount, Is.EqualTo(3));
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(skill);
                Object.DestroyImmediate(passive);
                Object.DestroyImmediate(growth);
            }
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

        private static BattleUnitState CreateBattleUnitWithMaxHp(
            string instanceId,
            BattleSide side,
            int slotIndex,
            int maxHp,
            int currentHp,
            int skillId,
            params (PachimonStatType type, int value)[] stats)
        {
            var allStats = new[]
            {
                (PachimonStatType.MaxHp, maxHp),
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
        public void FireBarrier_RecastAddsValueAndUsesFixedDefense()
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
                    valueBurnRatio: 20,
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
                Assert.That(barrier.Source, Is.SameAs(second));
                Assert.That(
                    barrier.GetDefense(PachimonAttribute.Fire),
                    Is.EqualTo(200m));
                Assert.That(barrier.GetEffectiveResistBonus(), Is.Zero);

                state.Timeline.AdvanceToTick(state.CurrentTick + 1);

                Assert.That(barrier.Value, Is.EqualTo(199));
            }
            finally
            {
                Object.DestroyImmediate(barrierDefinition);
                Object.DestroyImmediate(burn);
            }
        }

        [Test]
        public void FireBarrier_ShieldRemovalRemovesUnitAndFieldShields()
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
                    valueBurnRatio: 20,
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
                state.SupportEffects.ApplyShield(
                    defender,
                    defender,
                    value: 50);
                state.Fields.CreateOrAddFireBarrier(
                    defender,
                    barrierDefinition,
                    value: 100);

                var removed = state.SupportEffects.RemoveAllShields(defender);

                Assert.That(removed, Is.EqualTo(150));
                Assert.That(defender.TotalShield, Is.Zero);
                Assert.That(state.Fields.Effects, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(barrierDefinition);
                Object.DestroyImmediate(burn);
            }
        }

        [Test]
        public void FireBarrier_ReceivesSupportedStatusesAndConsumesWeakness()
        {
            var burn = ScriptableObject.CreateInstance<BurnStatusAsset>();
            var toxinDefinition = ScriptableObject.CreateInstance<ToxinStatusAsset>();
            var weaknessDefinition = ScriptableObject
                .CreateInstance<WeaknessStatusAsset>();
            var barrierDefinition = ScriptableObject
                .CreateInstance<FireBarrierFieldEffectAsset>();
            var skill = CreateBasicElectricSkill();
            try
            {
                burn.ConfigureForEditor("火傷", "DamageBonusを減少する。");
                toxinDefinition.ConfigureForEditor(
                    "毒素",
                    "毎tickダメージを与える。",
                    damagePerTickRatio: 1,
                    decayPerTick: 1);
                weaknessDefinition.ConfigureForEditor(
                    "弱点",
                    "次のDamageを増幅する。");
                barrierDefinition.ConfigureForEditor(
                    "炎の障壁",
                    "攻撃を肩代わりする。",
                    valueBurnRatio: 0,
                    burn);
                var defender = CreateBattleUnitWithStats(
                    "defender", BattleSide.Player, 0, 2000, 1);
                var attacker = CreateBattleUnitWithStats(
                    "attacker", BattleSide.Enemy, 0, 2000, skill.SkillId);
                var state = new BattleState(
                    123,
                    new BattleSideState(BattleSide.Player, new[] { defender }),
                    new BattleSideState(BattleSide.Enemy, new[] { attacker }));
                var barrier = state.Fields.CreateOrAddFireBarrier(
                    defender,
                    barrierDefinition,
                    value: 1000);
                var context = new SkillExecutionContext(state, attacker, skill);

                var damageHit = context.BeginAttackHit(defender);
                BattleAttributeDamageService.Apply(
                    state,
                    attacker,
                    defender,
                    new DamageContext(
                        DamageOriginKind.Skill,
                        skill.SkillId,
                        100m,
                        attacker.GetBattleStats(),
                        defender.GetBattleStats(),
                        PachimonAttribute.Electric,
                        isAttack: true,
                        applyAttackerAttributeMultiplier: false,
                        applyDamageBonusMultiplier: false),
                    damageHit);
                var toxinApplied = damageHit.ApplyStatus(
                    BattleStatusFactory.CreateToxin(
                        attacker,
                        100,
                        toxinDefinition));
                var weaknessApplied = context.BeginStatusHit(defender).ApplyStatus(
                    new BattleStatusInstance(
                        BattleStatusId.Weakness,
                        BattleStatusCategory.None,
                        attacker,
                        100,
                        definition: weaknessDefinition));
                var slowApplied = context.BeginStatusHit(defender).ApplyStatus(
                    BattleStatusFactory.CreateSlow(
                        attacker,
                        100,
                        ParalysisStatus));

                Assert.That(toxinApplied, Is.True);
                Assert.That(weaknessApplied, Is.True);
                Assert.That(slowApplied, Is.False);
                Assert.That(barrier.GetStatus(BattleStatusId.Toxin)?.Value,
                    Is.EqualTo(50));
                Assert.That(barrier.GetStatus(BattleStatusId.Weakness)?.Value,
                    Is.EqualTo(100));
                Assert.That(barrier.GetStatus(BattleStatusId.Paralysis), Is.Null);
                Assert.That(defender.GetStatus(BattleStatusId.Toxin), Is.Null);

                BattleAttributeDamageService.Apply(
                    state,
                    attacker,
                    defender,
                    new DamageContext(
                        DamageOriginKind.Skill,
                        skill.SkillId,
                        100m,
                        attacker.GetBattleStats(),
                        defender.GetBattleStats(),
                        PachimonAttribute.Electric,
                        isAttack: true,
                        applyAttackerAttributeMultiplier: false,
                        applyDamageBonusMultiplier: false));

                Assert.That(barrier.Value, Is.EqualTo(850));
                Assert.That(barrier.GetStatus(BattleStatusId.Weakness), Is.Null);
                Assert.That(defender.CurrentHp, Is.EqualTo(2000));
            }
            finally
            {
                Object.DestroyImmediate(skill);
                Object.DestroyImmediate(barrierDefinition);
                Object.DestroyImmediate(weaknessDefinition);
                Object.DestroyImmediate(toxinDefinition);
                Object.DestroyImmediate(burn);
            }
        }

        [Test]
        public void FireBarrier_DamageTriggersFireVineAgainstProtectedUnit()
        {
            var burn = ScriptableObject.CreateInstance<BurnStatusAsset>();
            var barrierDefinition = ScriptableObject
                .CreateInstance<FireBarrierFieldEffectAsset>();
            var vineDefinition = ScriptableObject
                .CreateInstance<FireVineFieldEffectAsset>();
            try
            {
                burn.ConfigureForEditor("火傷", "DamageBonusを減少する。");
                barrierDefinition.ConfigureForEditor(
                    "炎の障壁",
                    "攻撃を肩代わりする。",
                    valueBurnRatio: 0,
                    burn);
                vineDefinition.ConfigureForEditor(
                    "ファイアヴァイン",
                    "炎・草Damageに呼応する。",
                    baseLeafValue: 10,
                    leafValueRatio: 100,
                    baseFireValue: 10,
                    fireValueRatio: 100);
                var defender = CreateBattleUnitWithStats(
                    "defender", BattleSide.Player, 0, 2000, 1);
                var attacker = CreateBattleUnitWithStats(
                    "attacker", BattleSide.Enemy, 0, 2000, 1);
                var state = new BattleState(
                    123,
                    new BattleSideState(BattleSide.Player, new[] { defender }),
                    new BattleSideState(BattleSide.Enemy, new[] { attacker }));
                var barrier = state.Fields.CreateOrAddFireBarrier(
                    defender,
                    barrierDefinition,
                    value: 100);
                state.Fields.CreateFireVine(
                    attacker,
                    vineDefinition,
                    leafValue: 10,
                    fireValue: 10);

                BattleAttributeDamageService.Apply(
                    state,
                    attacker,
                    defender,
                    new DamageContext(
                        DamageOriginKind.Skill,
                        originId: 1,
                        baseDamage: 10m,
                        attacker.GetBattleStats(),
                        defender.GetBattleStats(),
                        PachimonAttribute.Fire,
                        isAttack: true,
                        applyAttackerAttributeMultiplier: false,
                        applyDamageBonusMultiplier: false));

                Assert.That(barrier.Value, Is.EqualTo(97));
                Assert.That(defender.CurrentHp, Is.EqualTo(1980));
                Assert.That(state.LogEntries.Count(entry =>
                        entry.Contains("ファイアヴァインの攻撃")),
                    Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(vineDefinition);
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
                    valueBurnRatio: 20,
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

                Assert.That(result.FinalDamage, Is.EqualTo(12));
                Assert.That(defender.CurrentHp, Is.EqualTo(1988));
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
                    valueBurnRatio: 20,
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

        [Test]
        public void FireBarrier_FullAbsorptionBlocksStatusFromTheSameHit()
        {
            var burn = ScriptableObject.CreateInstance<BurnStatusAsset>();
            var barrierDefinition = ScriptableObject
                .CreateInstance<FireBarrierFieldEffectAsset>();
            var skill = CreateBasicElectricSkill();
            try
            {
                burn.ConfigureForEditor("Burn", "Reduces DamageBonus.");
                barrierDefinition.ConfigureForEditor(
                    "Fire Barrier",
                    "Intercepts attacks.",
                    valueBurnRatio: 20,
                    burn);
                var defender = CreateBattleUnitWithStats(
                    "defender", BattleSide.Player, 0, 2000, 1);
                var attacker = CreateBattleUnitWithStats(
                    "attacker", BattleSide.Enemy, 0, 2000, skill.SkillId);
                var state = new BattleState(
                    123,
                    new BattleSideState(BattleSide.Player, new[] { defender }),
                    new BattleSideState(BattleSide.Enemy, new[] { attacker }));
                var barrier = state.Fields.CreateOrAddFireBarrier(
                    defender,
                    barrierDefinition,
                    500);
                var context = new SkillExecutionContext(state, attacker, skill);
                var hit = context.BeginAttackHit(defender);

                BattleAttributeDamageService.Apply(
                    state,
                    attacker,
                    defender,
                    new DamageContext(
                        DamageOriginKind.Skill,
                        skill.SkillId,
                        100m,
                        attacker.GetBattleStats(),
                        defender.GetBattleStats(),
                        PachimonAttribute.Electric,
                        isAttack: true,
                        applyAttackerAttributeMultiplier: false,
                        applyDamageBonusMultiplier: false),
                    hit);
                var applied = hit.ApplyStatus(BattleStatusFactory.CreateSlow(
                    attacker,
                    100,
                    ParalysisStatus));

                Assert.That(applied, Is.False);
                Assert.That(hit.Outcome, Is.EqualTo(SkillHitOutcome.Blocked));
                Assert.That(defender.GetStatus(BattleStatusId.Paralysis), Is.Null);
                Assert.That(barrier.Value, Is.EqualTo(450));

                var statusOnlyHit = context.BeginStatusHit(defender);
                var statusOnlyApplied = statusOnlyHit.ApplyStatus(
                    BattleStatusFactory.CreateSlow(
                        attacker,
                        100,
                        ParalysisStatus));

                Assert.That(statusOnlyApplied, Is.False);
                Assert.That(
                    statusOnlyHit.Outcome,
                    Is.EqualTo(SkillHitOutcome.Blocked));
                Assert.That(barrier.Value, Is.EqualTo(450));
            }
            finally
            {
                Object.DestroyImmediate(skill);
                Object.DestroyImmediate(barrierDefinition);
                Object.DestroyImmediate(burn);
            }
        }

        [Test]
        public void FireBarrier_AllowsStatusWhenLaterDamageInSameHitOverflows()
        {
            var burn = ScriptableObject.CreateInstance<BurnStatusAsset>();
            var barrierDefinition = ScriptableObject
                .CreateInstance<FireBarrierFieldEffectAsset>();
            var skill = CreateBasicElectricSkill();
            try
            {
                burn.ConfigureForEditor("Burn", "Reduces DamageBonus.");
                barrierDefinition.ConfigureForEditor(
                    "Fire Barrier",
                    "Intercepts attacks.",
                    valueBurnRatio: 20,
                    burn);
                var defender = CreateBattleUnitWithStats(
                    "defender", BattleSide.Player, 0, 2000, 1);
                var attacker = CreateBattleUnitWithStats(
                    "attacker", BattleSide.Enemy, 0, 2000, skill.SkillId);
                var state = new BattleState(
                    123,
                    new BattleSideState(BattleSide.Player, new[] { defender }),
                    new BattleSideState(BattleSide.Enemy, new[] { attacker }));
                state.Fields.CreateOrAddFireBarrier(
                    defender,
                    barrierDefinition,
                    150);
                var context = new SkillExecutionContext(state, attacker, skill);
                var hit = context.BeginAttackHit(defender);
                var damageContext = new DamageContext(
                    DamageOriginKind.Skill,
                    skill.SkillId,
                    200m,
                    attacker.GetBattleStats(),
                    defender.GetBattleStats(),
                    PachimonAttribute.Electric,
                    isAttack: true,
                    applyAttackerAttributeMultiplier: false,
                    applyDamageBonusMultiplier: false);

                BattleAttributeDamageService.Apply(
                    state, attacker, defender, damageContext, hit);
                BattleAttributeDamageService.Apply(
                    state, attacker, defender, damageContext, hit);
                var applied = hit.ApplyStatus(BattleStatusFactory.CreateSlow(
                    attacker,
                    100,
                    ParalysisStatus));

                Assert.That(applied, Is.True);
                Assert.That(hit.Outcome, Is.EqualTo(SkillHitOutcome.Hit));
                Assert.That(defender.CurrentHp, Is.EqualTo(1950));
                Assert.That(
                    defender.GetStatus(BattleStatusId.Paralysis),
                    Is.Not.Null);
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
                    decayPerTick: 1);
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
                decayPerTick: statusId == BattleStatusId.Paralysis ? 0 : 1,
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
                    decayPerTick: 1,
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
            float percent)
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

        private static EffectivePachimonStats CreateEffectiveStatsWithoutBindings(
            params (PachimonStatType statType, int value)[] values)
        {
            return EffectivePachimonStats.Calculate(
                CreateStats(values),
                modifiers: null,
                bindings: null);
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
