using System.Linq;
using Pachimon.Battle;
using Pachimon.Data;
using Pachimon.Passives;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.UI
{
    public static class SkillDescriptionValueProviderRegistry
    {
        public static bool TryCreateContext(
            SkillAsset skill,
            PachimonPreviewContent owner,
            out DescriptionTemplateContext context)
        {
            context = new DescriptionTemplateContext();
            if (skill == null)
            {
                return false;
            }

            if (skill is PlaceholderSkillAsset placeholder
                && TryGetAttribute(owner, skill.AllocationType, out var basicAttribute))
            {
                context.Set("damage", SignedStatMath.FloorNonNegative(
                        BasicAttributeDamageSkillLogic.BaseDamage
                        * SignedStatMath.AmplificationMultiplier(basicAttribute), 1))
                    .Set("baseDamage", BasicAttributeDamageSkillLogic.BaseDamage)
                    .Set("attribute", basicAttribute)
                    .Set("ratio", 100);
                if (placeholder.StatusBaseValue > 0)
                {
                    context.Set("statusValue", SignedStatMath.FloorNonNegative(
                        SignedStatMath.ScaleFromBase(
                            placeholder.StatusBaseValue,
                            basicAttribute,
                            placeholder.StatusScalingPercent), 1));
                }
                return true;
            }

            if (skill is BackfireSkillAsset backfire
                && TryGetStat(owner, PachimonDisplayStat.Fire, out var backfireFire)
                && TryGetStat(owner, PachimonDisplayStat.Poison, out var backfirePoison))
            {
                context.Set("damage", SignedStatMath.FloorNonNegative(
                        BackfireMath.CalculateBaseDamage(backfire, backfireFire)))
                    .Set("penetration", BackfireMath.CalculatePenetrationPercent(
                        backfire, backfirePoison).ToString("0.##"))
                    .Set("fire", backfireFire)
                    .Set("poison", backfirePoison);
                return true;
            }

            if (skill is WaterPulseSkillAsset waterPulse
                && TryGetStat(owner, PachimonDisplayStat.Aqua, out var pulseAqua))
            {
                var mana = owner?.CurrentMn ?? 0;
                context.Set("mana", mana)
                    .Set("aqua", pulseAqua)
                    .Set("damage", SignedStatMath.FloorNonNegative(
                        mana * SignedStatMath.AmplificationMultiplier(
                            pulseAqua * waterPulse.AquaDamageRatio / 100m), 1));
                return true;
            }

            if (skill is SunbathSkillAsset sunbath
                && TryGetStat(owner, PachimonDisplayStat.Leaf, out var sunbathLeaf))
            {
                context.Set("baseHealing", sunbath.BaseHealing)
                    .Set("leaf", sunbathLeaf)
                    .Set("healingBeforeWeather", SignedStatMath.FloorNonNegative(
                        SignedStatMath.ScaleFromBase(
                            sunbath.BaseHealing,
                            sunbathLeaf,
                            sunbath.LeafHealingRatio)))
                    .Set("temperatureRatio", sunbath.TemperatureHealingRatio)
                    .Set("rainReductionRatio", sunbath.RainHealingReductionRatio);
                return true;
            }

            if (skill is AquaShockSkillAsset aquaShock
                && TryGetStat(owner, PachimonDisplayStat.Electric, out var shockElectric)
                && TryGetStat(owner, PachimonDisplayStat.Aqua, out var shockAqua))
            {
                context.Set("electricDamage", SignedStatMath.FloorNonNegative(
                        AquaShockMath.CalculateElectricBaseDamage(
                            aquaShock, shockElectric)))
                    .Set("aquaDamage", SignedStatMath.FloorNonNegative(
                        AquaShockMath.CalculateAquaBaseDamage(aquaShock, shockAqua)))
                    .Set("leakValue", AquaShockMath.CalculateLeakValue(
                        aquaShock, shockAqua));
                return true;
            }

            if (skill is NeurotoxinSkillAsset neurotoxin
                && TryGetStat(owner, PachimonDisplayStat.Poison, out var neuroPoison)
                && TryGetStat(owner, PachimonDisplayStat.Electric, out var neuroElectric))
            {
                context.Set("stunTicks", NeurotoxinMath.CalculateStunTicks(
                        neurotoxin, neuroPoison, neuroElectric))
                    .Set("toxinValue", NeurotoxinMath.CalculateToxinValue(
                        neurotoxin, neuroPoison));
                return true;
            }

            if (skill is IceShieldSkillAsset iceShield
                && TryGetStat(owner, PachimonDisplayStat.Ice, out var shieldIce))
            {
                context.Set("shield", IceShieldMath.CalculateShieldValue(
                    iceShield, shieldIce));
                return true;
            }

            if (skill is FlyingAttackSkillAsset flyingAttack
                && TryGetStat(owner, PachimonDisplayStat.Wind, out var flyingWind))
            {
                context.Set("damage", SignedStatMath.FloorNonNegative(
                        SignedStatMath.ScaleFromBase(
                            flyingAttack.BaseWindDamage,
                            flyingWind,
                            flyingAttack.WindDamageRatio)))
                    .Set("speed", SignedStatMath.FloorNonNegative(
                        flyingWind
                        * (flyingAttack.FlyingStatus?.WindSpeedRatio ?? 0)
                        / 100m));
                return true;
            }

            if (skill is DragonJabSkillAsset dragonJab
                && TryGetStat(owner, PachimonDisplayStat.Dragon, out var jabDragon))
            {
                context.Set("damage", SignedStatMath.FloorNonNegative(
                        SignedStatMath.ScaleFromBase(
                            dragonJab.BaseDragonDamage,
                            jabDragon,
                            dragonJab.DragonDamageRatio)))
                    .Set("oneTwoValue", dragonJab.OneTwoValue);
                return true;
            }

            if (skill is ChainBurnSkillAsset chainBurn
                && TryGetStat(owner, PachimonDisplayStat.Fire, out var chainFire))
            {
                context.Set("damage", SignedStatMath.FloorNonNegative(
                        SignedStatMath.ScaleFromBase(
                            chainBurn.BasePower,
                            chainFire,
                            chainBurn.FireScalingPercent)))
                    .Set("hitCount", chainBurn.BaseChainCount + 1)
                    .Set("addChain", AddChainRuntime.FormatUnits(
                        chainBurn.AddChainGainUnits));
                return true;
            }

            if (skill is RainDanceSkillAsset rainDance
                && TryGetStat(owner, PachimonDisplayStat.Aqua, out var rainAqua))
            {
                context.Set("rainValue", SignedStatMath.FloorNonNegative(
                        rainDance.BaseValue
                        + rainAqua * rainDance.AquaValueRatio / 100m,
                        minimum: 1))
                    .Set("baseValue", rainDance.BaseValue)
                    .Set("aquaRatio", rainDance.AquaValueRatio);
                return true;
            }

            if (skill is ChainVinesSkillAsset chainVines
                && TryGetStat(owner, PachimonDisplayStat.Leaf, out var vinesLeaf))
            {
                context.Set("damage", SignedStatMath.FloorNonNegative(
                        SignedStatMath.ScaleFromBase(
                            chainVines.BaseLeafDamage,
                            vinesLeaf,
                            chainVines.LeafDamageRatio)))
                    .Set("slow", SignedStatMath.FloorNonNegative(
                        SignedStatMath.ScaleFromBase(
                            chainVines.BaseSlow,
                            vinesLeaf,
                            chainVines.SlowLeafRatio)))
                    .Set("hitCount", chainVines.BaseChainCount + 1)
                    .Set("addChain", AddChainRuntime.FormatUnits(
                        chainVines.AddChainGainUnits));
                return true;
            }

            if (skill is ElectricExplosionSkillAsset explosion
                && TryGetStat(owner, PachimonDisplayStat.Electric, out var explosionElectric)
                && TryGetStat(owner, PachimonDisplayStat.Fire, out var explosionFire))
            {
                context.Set("damage", SignedStatMath.FloorNonNegative(
                        ElectricExplosionMath.CalculateBaseDamage(
                            explosion,
                            explosionElectric,
                            explosionFire)))
                    .Set("penetration",
                        ElectricExplosionMath.CalculatePenetrationPercent(
                            explosion,
                            explosionFire).ToString("0.##"));
                return true;
            }

            if (skill is SmogSkillAsset smog
                && TryGetStat(owner, PachimonDisplayStat.Poison, out var smogPoison))
            {
                context.Set("fieldValue", SignedStatMath.FloorNonNegative(
                    SignedStatMath.ScaleFromBase(
                        smog.BaseFieldValue,
                        smogPoison,
                        smog.PoisonScalingPercent)));
                return true;
            }

            if (skill is IceShardSkillAsset iceShard
                && TryGetStat(owner, PachimonDisplayStat.Ice, out var shardIce))
            {
                context.Set("frontDamage", Scale(
                        iceShard.FrontBaseDamage,
                        shardIce,
                        iceShard.FrontDamageIceRatio))
                    .Set("frontChill", Scale(
                        iceShard.FrontBaseChill,
                        shardIce,
                        iceShard.FrontChillIceRatio))
                    .Set("otherDamage", Scale(
                        iceShard.OtherBaseDamage,
                        shardIce,
                        iceShard.OtherDamageIceRatio))
                    .Set("otherChill", Scale(
                        iceShard.OtherBaseChill,
                        shardIce,
                        iceShard.OtherChillIceRatio));
                return true;
            }

            if (skill is WindErosionSkillAsset erosion
                && TryGetStat(owner, PachimonDisplayStat.Wind, out var erosionWind))
            {
                context.Set("erosionValue", Scale(
                    erosion.BaseErosionValue,
                    erosionWind,
                    erosion.WindValueRatio));
                return true;
            }

            if (skill is DragonFootworkSkillAsset footwork
                && TryGetStat(owner, PachimonDisplayStat.Dragon, out var footworkDragon))
            {
                context.Set("duration", System.Math.Max(1, Scale(
                    footwork.BaseDurationTicks,
                    footworkDragon,
                    footwork.DurationDragonRatio)));
                return true;
            }

            if (skill is FireBarrierSkillAsset fireBarrier
                && TryGetStat(owner, PachimonDisplayStat.Fire, out var barrierFire))
            {
                var value = Scale(
                    fireBarrier.BaseValue,
                    barrierFire,
                    fireBarrier.FireValueRatio);
                var field = fireBarrier.FieldEffect;
                context.Set("value", value)
                    .Set("hp", System.Math.Max(1, ScaleRatio(
                        value,
                        field?.ValueHpRatio ?? 0)))
                    .Set("duration", System.Math.Max(1,
                        SignedStatMath.CeilPositive(
                            value * (field?.ValueDurationRatio ?? 0) / 100m)))
                    .Set("burn", ScaleRatio(
                        value,
                        field?.ValueBurnRatio ?? 0))
                    .Set("defenseRatio", field?.DefenseSnapshotRatio ?? 0);
                return true;
            }

            if (skill is LaunchCeremonySkillAsset launchCeremony)
            {
                var definition = launchCeremony.StatusDefinition;
                context.Set("aquaMultiplier",
                        definition?.AquaMultiplierPercent ?? 0)
                    .Set("manaReductionRatio",
                        definition?.ManaReductionAquaRatio ?? 0);
                if (TryGetStat(owner, PachimonDisplayStat.Aqua, out var launchAqua))
                {
                    context.Set("currentManaMultiplier",
                        SignedStatMath.ReductionMultiplier(
                            launchAqua
                            * (definition?.ManaReductionAquaRatio ?? 0)
                            / 100m).ToString("0.##"));
                }
                return true;
            }

            if (skill is SolarBeamSkillAsset solarBeam
                && TryGetStat(owner, PachimonDisplayStat.Leaf, out var solarLeaf))
            {
                context.Set("damage", Scale(
                        solarBeam.BaseLeafDamage,
                        solarLeaf,
                        solarBeam.LeafDamageRatio))
                    .Set("baseStartup", solarBeam.BaseStartupTicks)
                    .Set("temperatureRatio",
                        solarBeam.TemperatureStartupRatio);
                return true;
            }

            if (skill is ElectricQuickAttackSkillAsset quickAttack
                && TryGetStat(owner, PachimonDisplayStat.Electric, out var quickElectric)
                && TryGetStat(owner, PachimonDisplayStat.Fire, out var quickFire)
                && TryGetStat(owner, PachimonDisplayStat.Wind, out var quickWind)
                && TryGetStat(owner, PachimonDisplayStat.Speed, out var quickSpeed)
                && TryGetStat(owner, PachimonDisplayStat.Haste, out var quickHaste))
            {
                var windMultiplier =
                    SkillTimingCalculator.CalculateWindMultiplier(
                        quickAttack,
                        quickWind);
                context.Set("electricDamage", SignedStatMath.FloorNonNegative(
                        ElectricQuickAttackMath.CalculateElectricBaseDamage(
                            quickAttack,
                            quickElectric)))
                    .Set("fireDamage", SignedStatMath.FloorNonNegative(
                        ElectricQuickAttackMath.CalculateFireBaseDamage(
                            quickAttack,
                            quickFire)))
                    .Set("recovery", BattleTickMath.GetEffectiveRecovery(
                        quickAttack.BaseRecoveryTicks,
                        quickSpeed,
                        windMultiplier))
                    .Set("cooldown", BattleTickMath.GetEffectiveCooldown(
                        quickAttack.BaseCooldownTicks,
                        quickHaste,
                        windMultiplier));
                return true;
            }

            if (skill is ToxinTransferSkillAsset toxinTransfer)
            {
                context.Set("removalPercent", toxinTransfer.RemovalPercent)
                    .Set("applicationPercent",
                        toxinTransfer.ApplicationPercent);
                return true;
            }

            if (skill is HeavySnowSkillAsset heavySnow
                && TryGetStat(owner, PachimonDisplayStat.Ice, out var snowIce))
            {
                context.Set("temperatureReduction",
                        SignedStatMath.FloorNonNegative(
                            heavySnow.BaseValue
                            + snowIce * heavySnow.IceValueRatio / 100m,
                            minimum: 1))
                    .Set("baseValue", heavySnow.BaseValue)
                    .Set("iceRatio", heavySnow.IceValueRatio);
                return true;
            }

            if (skill is HealingWindSkillAsset healingWind
                && TryGetStat(owner, PachimonDisplayStat.Wind, out var healingWindStat))
            {
                context.Set("healing", Scale(
                        healingWind.BaseHealing,
                        healingWindStat,
                        healingWind.WindRatio))
                    .Set("windBonus", Scale(
                        healingWind.BaseWindBonus,
                        healingWindStat,
                        healingWind.WindRatio))
                    .Set("speedBonus", Scale(
                        healingWind.BaseSpeedBonus,
                        healingWindStat,
                        healingWind.WindRatio))
                    .Set("duration", healingWind.DurationTicks);
                return true;
            }

            if (skill is DragonDanceSkillAsset dragonDance)
            {
                context.Set("dragonBonus", dragonDance.DragonBonus)
                    .Set("speedBonus", dragonDance.SpeedBonus);
                return true;
            }

            if (skill is FireArrowSkillAsset fireArrow
                && TryGetStat(owner, PachimonDisplayStat.Fire, out var arrowFire))
            {
                context.Set("damage", Scale(
                        fireArrow.BasePower,
                        arrowFire,
                        fireArrow.FireScalingPercent))
                    .Set("repeatManaCost", fireArrow.BaseManaCost);
                return true;
            }

            if (skill is WaterVeilSkillAsset waterVeil
                && TryGetStat(owner, PachimonDisplayStat.Aqua, out var veilAqua))
            {
                var definition = waterVeil.FieldEffect;
                context.Set("fieldValue", Scale(
                        waterVeil.BaseFieldValue,
                        veilAqua,
                        waterVeil.AquaValueRatio))
                    .Set("healingPerTick", definition?.HealingPerTick ?? 0)
                    .Set("decayPerTick", definition?.DecayPerTick ?? 0)
                    .Set("reductionPercent",
                        definition?.DamageReductionPercent ?? 0);
                return true;
            }

            if (skill is EntanglingVinesSkillAsset entanglingVines
                && TryGetStat(owner, PachimonDisplayStat.Leaf, out var entangleLeaf))
            {
                context.Set("stunTicks", System.Math.Max(1, Scale(
                    entanglingVines.BaseStun,
                    entangleLeaf,
                    entanglingVines.StunLeafRatio)));
                return true;
            }

            if (skill is ChargeSkillAsset charge
                && TryGetStat(owner, PachimonDisplayStat.Electric, out var chargeElectric)
                && TryGetStat(owner, PachimonDisplayStat.Speed, out var chargeSpeed))
            {
                context.Set("chargeValue", System.Math.Max(1, chargeElectric))
                    .Set("startup", BattleTickMath.GetEffectiveStartup(
                        charge.BaseStartupTicks,
                        chargeSpeed));
                return true;
            }

            if (skill is ToxinExplosionSkillAsset toxinExplosion
                && TryGetStat(owner, PachimonDisplayStat.Poison, out var toxinPoison)
                && TryGetStat(owner, PachimonDisplayStat.Fire, out var toxinFire))
            {
                context.Set("fixedDamage", SignedStatMath.FloorNonNegative(
                        ToxinExplosionMath.CalculateBaseDamage(
                            toxinExplosion,
                            consumedToxin: 0,
                            toxinPoison,
                            toxinFire)))
                    .Set("toxinConversion",
                        toxinExplosion.ToxinConversionPercent);
                return true;
            }

            if (skill is IceBladeSkillAsset iceBlade
                && TryGetStat(owner, PachimonDisplayStat.Ice, out var bladeIce))
            {
                context.Set("duration",
                    IceBladeSkillLogic.CalculateDuration(iceBlade, bladeIce));
                return true;
            }

            if (skill is SecondWindSkillAsset secondWind
                && TryGetStat(owner, PachimonDisplayStat.Wind, out var secondWindStat))
            {
                context.Set("shield", ScaleRatio(
                        secondWindStat,
                        secondWind.WindShieldRatio))
                    .Set("duration", secondWind.DurationTicks);
                return true;
            }

            if (skill is DragonBreakSkillAsset dragonBreak
                && TryGetStat(owner, PachimonDisplayStat.Dragon, out var breakDragon))
            {
                context.Set("damage", Scale(
                    dragonBreak.BaseDragonDamage,
                    breakDragon,
                    dragonBreak.DragonDamageRatio));
                return true;
            }

            if (skill is CombustionSkillAsset combustion
                && TryGetStat(owner, PachimonDisplayStat.Fire, out var combustionFire))
            {
                context.Set("damage", Scale(
                        combustion.BasePower,
                        combustionFire,
                        combustion.FireScalingPercent));
                return true;
            }

            if (skill is WaterCutterSkillAsset waterCutter
                && TryGetStat(owner, PachimonDisplayStat.Aqua, out var cutterAqua)
                && TryGetStat(owner, PachimonDisplayStat.Wind, out var cutterWind))
            {
                context.Set("damage", Scale(
                        waterCutter.BaseAquaDamage,
                        cutterAqua,
                        waterCutter.AquaDamageRatio))
                    .Set("penetration", Scale(
                        waterCutter.BasePenetrationPercent,
                        cutterWind,
                        waterCutter.WindPenetrationRatio));
                return true;
            }

            if (skill is ParalysisPowderSkillAsset paralysisPowder
                && TryGetStat(owner, PachimonDisplayStat.Leaf, out var powderLeaf)
                && TryGetStat(owner, PachimonDisplayStat.Poison, out var powderPoison))
            {
                context.Set("paralysis", checked(
                    Scale(paralysisPowder.BaseLeafParalysis, powderLeaf, paralysisPowder.LeafRatio)
                    + Scale(paralysisPowder.BasePoisonParalysis, powderPoison, paralysisPowder.PoisonRatio)));
                return true;
            }

            if (skill is ElectromagneticCannonSkillAsset cannon
                && TryGetStat(owner, PachimonDisplayStat.Electric, out var cannonElectric))
            {
                context.Set("damage", Scale(cannon.BasePower, cannonElectric, 100))
                    .Set("startup", cannon.BaseStartupTicks);
                return true;
            }

            if (skill is PoisonShieldSkillAsset poisonShield
                && TryGetStat(owner, PachimonDisplayStat.Poison, out var shieldPoison))
            {
                context.Set("shield", PoisonShieldMath.CalculateShieldValue(
                        poisonShield, shieldPoison))
                    .Set("duration", poisonShield.DurationTicks)
                    .Set("toxinReductionPercent",
                        PoisonShieldMath.CalculateToxinReductionPercent(
                            poisonShield, shieldPoison).ToString("0.##"));
                return true;
            }

            if (skill is FrozenBreakSkillAsset frozenBreak
                && TryGetStat(owner, PachimonDisplayStat.Ice, out var frozenIce))
            {
                context.Set("damage", Scale(
                        frozenBreak.BaseIceDamage,
                        frozenIce,
                        frozenBreak.IceDamageRatio))
                    .Set("duration", System.Math.Max(1,
                        SignedStatMath.FloorNonNegative(
                            frozenBreak.BaseDuration
                            + frozenIce * frozenBreak.DurationIceRatio / 100m)))
                    .Set("healingPerTick", Scale(
                        frozenBreak.BaseHealPerTick,
                        frozenIce,
                        frozenBreak.HealIceRatio));
                return true;
            }

            if (skill is WindStormSkillAsset windStorm
                && TryGetStat(owner, PachimonDisplayStat.Wind, out var stormWind))
            {
                context.Set("value", System.Math.Max(1,
                    SignedStatMath.FloorNonNegative(
                        windStorm.BaseValue
                        + stormWind * windStorm.WindValueRatio / 100m)));
                return true;
            }

            if (skill is DragonHookSkillAsset dragonHook
                && TryGetStat(owner, PachimonDisplayStat.Dragon, out var hookDragon))
            {
                context.Set("damage", Scale(
                        dragonHook.BaseDragonDamage,
                        hookDragon,
                        dragonHook.DragonDamageRatio))
                    .Set("crankerValue", System.Math.Max(1,
                        SignedStatMath.FloorNonNegative(
                            dragonHook.BaseCrankerValue
                            + hookDragon * dragonHook.CrankerDragonRatio / 100m)));
                return true;
            }

            if (skill is SunnyDaySkillAsset sunnyDay
                && TryGetStat(owner, PachimonDisplayStat.Fire, out var sunnyFire))
            {
                context.Set("temperature", System.Math.Max(1,
                    SignedStatMath.FloorNonNegative(
                        sunnyDay.BaseValue
                        + sunnyFire * sunnyDay.FireValueRatio / 100m)));
                return true;
            }

            if (skill is MuddyWaterSkillAsset muddyWater
                && TryGetStat(owner, PachimonDisplayStat.Aqua, out var muddyAqua)
                && TryGetStat(owner, PachimonDisplayStat.Poison, out var muddyPoison))
            {
                context.Set("damage", Scale(
                        muddyWater.BaseAquaDamage,
                        muddyAqua,
                        muddyWater.AquaDamageRatio))
                    .Set("slow", Scale(
                        muddyWater.BaseSlow,
                        muddyPoison,
                        muddyWater.PoisonSlowRatio));
                return true;
            }

            if (skill is BeatVineSkillAsset beatVine
                && TryGetStat(owner, PachimonDisplayStat.Leaf, out var beatLeaf))
            {
                context.Set("value", Scale(
                        beatVine.FieldEffect?.BaseValue ?? 0,
                        beatLeaf,
                        beatVine.FieldEffect?.LeafValueRatio ?? 0))
                    .Set("interval", beatVine.FieldEffect?.AttackIntervalTicks ?? 0);
                return true;
            }

            if (skill is LightningCloudSkillAsset lightningCloud
                && TryGetStat(owner, PachimonDisplayStat.Electric, out var cloudElectric))
            {
                context.Set("value", System.Math.Max(1,
                    SignedStatMath.FloorNonNegative(
                        lightningCloud.BaseValue
                        + cloudElectric * lightningCloud.ElectricValueRatio / 100m)));
                return true;
            }

            if (skill is PoisonMistSkillAsset poisonMist
                && TryGetStat(owner, PachimonDisplayStat.Poison, out var mistPoison)
                && TryGetStat(owner, PachimonDisplayStat.Aqua, out var mistAqua)
                && TryGetStat(owner, PachimonDisplayStat.Wind, out var mistWind))
            {
                context.Set("value", Scale(
                        poisonMist.BaseMistValue,
                        mistPoison,
                        poisonMist.PoisonValueRatio))
                    .Set("duration", System.Math.Max(1,
                        SignedStatMath.FloorNonNegative(
                            mistAqua * poisonMist.AquaDurationRatio / 100m
                            + mistWind * poisonMist.WindDurationRatio / 100m)));
                return true;
            }

            if (skill is IcePebbleSkillAsset icePebble
                && TryGetStat(owner, PachimonDisplayStat.Ice, out var pebbleIce))
            {
                context.Set("damage", Scale(icePebble.BaseDamage, pebbleIce, icePebble.IceRatio))
                    .Set("chill", Scale(icePebble.BaseChill, pebbleIce, icePebble.IceRatio))
                    .Set("shield", Scale(icePebble.BaseShield, pebbleIce, icePebble.IceRatio))
                    .Set("duration", icePebble.ShieldDurationTicks);
                return true;
            }

            if (skill is CuttingDanceSkillAsset cuttingDance
                && TryGetStat(owner, PachimonDisplayStat.Wind, out var danceWind))
            {
                context.Set("damage", Scale(
                        cuttingDance.BaseWindDamage,
                        danceWind,
                        cuttingDance.WindDamageRatio))
                    .Set("erosion", Scale(
                        cuttingDance.BaseErosion,
                        danceWind,
                        cuttingDance.ErosionWindRatio))
                    .Set("hitCount", cuttingDance.BaseChainCount + 1)
                    .Set("addChain", AddChainRuntime.FormatUnits(
                        cuttingDance.AddChainGainUnits));
                return true;
            }

            if (skill is DragonUpperSkillAsset dragonUpper
                && TryGetStat(owner, PachimonDisplayStat.Dragon, out var upperDragon))
            {
                context.Set("damage", Scale(
                        dragonUpper.BaseDragonDamage,
                        upperDragon,
                        dragonUpper.DragonDamageRatio))
                    .Set("knockoutDuration", dragonUpper.KnockoutDurationTicks);
                return true;
            }

            if (skill is EvaporationSkillAsset evaporation
                && TryGetStat(owner, PachimonDisplayStat.Fire, out var evaporationFire)
                && TryGetStat(owner, PachimonDisplayStat.Aqua, out var evaporationAqua))
            {
                context.Set("damage", checked(
                        Scale(evaporation.BaseFireDamage, evaporationFire, evaporation.FireDamageRatio)
                        + Scale(evaporation.BaseAquaDamage, evaporationAqua, evaporation.AquaDamageRatio)))
                    .Set("penetration", checked(
                        Scale(evaporation.BaseFirePenetration, evaporationFire, evaporation.FirePenetrationRatio)
                        + Scale(evaporation.BaseAquaPenetration, evaporationAqua, evaporation.AquaPenetrationRatio)))
                    .Set("weakness", checked(
                        Scale(evaporation.BaseFireWeakness, evaporationFire, evaporation.FireWeaknessRatio)
                        + Scale(evaporation.BaseAquaWeakness, evaporationAqua, evaporation.AquaWeaknessRatio)));
                return true;
            }

            if (skill is WaterSpoutSkillAsset waterSpout
                && TryGetStat(owner, PachimonDisplayStat.Aqua, out var spoutAqua))
            {
                var hp = owner?.CurrentHp ?? 0;
                context.Set("damage", SignedStatMath.FloorNonNegative(
                        waterSpout.BaseAquaDamage
                        * (SignedStatMath.AmplificationMultiplier(
                            spoutAqua * waterSpout.AquaDamageRatio / 100m)
                           + (decimal)hp / waterSpout.CurrentHpDivisor)))
                    .Set("currentHp", hp)
                    .Set("hpDivisor", waterSpout.CurrentHpDivisor);
                return true;
            }

            if (skill is FireVineSkillAsset fireVine
                && TryGetStat(owner, PachimonDisplayStat.Leaf, out var vineLeaf)
                && TryGetStat(owner, PachimonDisplayStat.Fire, out var vineFire))
            {
                context.Set("leafValue", Scale(
                        fireVine.FieldEffect?.BaseLeafValue ?? 0,
                        vineLeaf,
                        fireVine.FieldEffect?.LeafValueRatio ?? 0))
                    .Set("fireValue", Scale(
                        fireVine.FieldEffect?.BaseFireValue ?? 0,
                        vineFire,
                        fireVine.FieldEffect?.FireValueRatio ?? 0));
                return true;
            }

            if (skill is ElectricShieldSkillAsset electricShield
                && TryGetStat(owner, PachimonDisplayStat.Electric, out var electricShieldStat))
            {
                context.Set("shield", Scale(
                        electricShield.BaseShieldValue,
                        electricShieldStat,
                        electricShield.ShieldElectricRatio))
                    .Set("selfParalysis", Scale(
                        electricShield.BaseSelfParalysis,
                        electricShieldStat,
                        electricShield.SelfParalysisElectricRatio))
                    .Set("counterParalysis", Scale(
                        electricShield.BaseCounterParalysis,
                        electricShieldStat,
                        electricShield.CounterParalysisElectricRatio))
                    .Set("duration", electricShield.DurationTicks);
                return true;
            }

            if (skill is FirstTouchSkillAsset firstTouch
                && TryGetStat(owner, PachimonDisplayStat.Poison, out var touchPoison))
            {
                context.Set("damage", Scale(firstTouch.BaseDamage, touchPoison, firstTouch.PoisonRatio))
                    .Set("normalToxin", Scale(firstTouch.BaseNormalToxinValue, touchPoison, firstTouch.PoisonRatio))
                    .Set("bonusDamage", Scale(firstTouch.BonusBaseDamage, touchPoison, firstTouch.PoisonRatio))
                    .Set("toxin", Scale(firstTouch.BaseToxinValue, touchPoison, firstTouch.PoisonRatio));
                return true;
            }

            if (skill is FrostArrowSkillAsset frostArrow
                && TryGetStat(owner, PachimonDisplayStat.Ice, out var frostIce))
            {
                context.Set("damage", Scale(frostArrow.BaseDamage, frostIce, frostArrow.IceRatio))
                    .Set("chill", Scale(frostArrow.BaseChill, frostIce, frostArrow.IceRatio))
                    .Set("manaRefund", frostArrow.BaseManaCost)
                    .Set("cooldownRefund", frostArrow.BaseCooldownTicks);
                return true;
            }

            if (skill is KachofugetsuSkillAsset kachofugetsu
                && TryGetStat(owner, PachimonDisplayStat.Fire, out var kachoFire)
                && TryGetStat(owner, PachimonDisplayStat.Aqua, out var kachoAqua)
                && TryGetStat(owner, PachimonDisplayStat.Wind, out var kachoWind))
            {
                context.Set("fireDamage", Scale(kachofugetsu.BaseFireDamage, kachoFire, kachofugetsu.FireDamageRatio))
                    .Set("aquaDamage", Scale(kachofugetsu.BaseAquaDamage, kachoAqua, kachofugetsu.AquaDamageRatio))
                    .Set("windDamage", Scale(kachofugetsu.BaseWindDamage, kachoWind, kachofugetsu.WindDamageRatio));
                return true;
            }

            if (skill is DragonDefenseSkillAsset dragonDefense
                && TryGetStat(owner, PachimonDisplayStat.Dragon, out var defenseDragon))
            {
                context.Set("shield", Scale(
                        dragonDefense.BaseShieldValue,
                        defenseDragon,
                        dragonDefense.DragonShieldRatio))
                    .Set("duration", dragonDefense.DurationTicks);
                return true;
            }

            return false;
        }

        private static int Scale(int baseValue, int stat, int ratio) =>
            SignedStatMath.FloorNonNegative(
                SignedStatMath.ScaleFromBase(baseValue, stat, ratio));

        private static int ScaleRatio(int value, int ratio) =>
            SignedStatMath.FloorNonNegative(value * ratio / 100m);

        private static bool TryGetAttribute(
            PachimonPreviewContent owner,
            AllocationType type,
            out int value)
        {
            value = 0;
            return owner?.IsRevealed == true
                && AttributeRichText.TryGetDisplayStat(type, out var stat)
                && owner.TryGetStat(stat, out value);
        }

        private static bool TryGetStat(
            PachimonPreviewContent owner,
            PachimonDisplayStat stat,
            out int value)
        {
            value = 0;
            return owner?.IsRevealed == true && owner.TryGetStat(stat, out value);
        }
    }

    public static class PassiveDescriptionValueProviderRegistry
    {
        public static bool TryCreateContext(
            PassiveAsset passive,
            PachimonPreviewContent owner,
            out DescriptionTemplateContext context)
        {
            context = new DescriptionTemplateContext();
            switch (passive)
            {
                case OutgoingAttributeDamagePassiveAsset outgoing:
                    context.Set("increasePercent", outgoing.DamagePercent - 100);
                    return true;
                case DarkFlamePassiveAsset darkFlame:
                    context.Set("baseConversion", darkFlame.BaseConversionPercent)
                        .Set("poisonRatio", darkFlame.PoisonScalingPercent);
                    if (TryGetStat(owner, PachimonDisplayStat.Poison, out var poison))
                    {
                        context.Set("conversion", (
                            darkFlame.BaseConversionPercent
                            * SignedStatMath.AmplificationMultiplier(
                                poison * darkFlame.PoisonScalingPercent / 100m))
                            .ToString("0.##"));
                    }
                    return true;
                case LifeWaterPassiveAsset lifeWater:
                    context.Set("baseRecoveryRatio", lifeWater.BaseRecoveryRatio)
                        .Set("aquaRecoveryRatio", lifeWater.AquaRecoveryRatio);
                    return true;
                case HealthyPlantPassiveAsset healthyPlant:
                    context.Set("baseHealingRatio", healthyPlant.BaseHealingRatio)
                        .Set("leafHealingRatio", healthyPlant.LeafHealingRatio);
                    return true;
                case DerivedAdditivePassiveAsset derived:
                    context.Set("percent", derived.Percent.ToString("0.##"))
                        .Set("referenceStat", derived.ReferenceStat)
                        .Set("targetStat", derived.TargetStat);
                    var contribution = owner?.StatCalculation?
                        .GetContributions(derived.TargetStat)
                        .FirstOrDefault(item =>
                            item.Source.SourceId == $"passive:{derived.PassiveId}")?
                        .Value;
                    if (contribution.HasValue)
                    {
                        context.Set("contribution", contribution.Value.ToString("0.##"));
                    }
                    return true;
                case ToxinGrowthPassiveAsset toxinGrowth:
                    context.Set("poisonPercent", toxinGrowth.PoisonPercentPerApplication);
                    return true;
                case IncomingAttributeDamagePassiveAsset incoming:
                    context.Set("reductionPercent", 100 - incoming.DamagePercent);
                    return true;
                case RunningStartPassiveAsset runningStart:
                    context.Set("startupRatio", runningStart.StartupDamageBonusRatio);
                    return true;
                case DragonBoxerPassiveAsset dragonBoxer:
                    context.Set("stackGain", dragonBoxer.StackGain)
                        .Set("damagePerStack", dragonBoxer.DamagePercentPerStack);
                    return true;
                case ComboMasterPassiveAsset comboMaster:
                    context.Set("damageBonusPerChain",
                        comboMaster.DamageBonusPerChain);
                    return true;
                case RainManPassiveAsset rainMan:
                    context.Set("baseSpeedPercent", rainMan.BaseSpeedPercent)
                        .Set("rainValueRatio", rainMan.RainValueRatio);
                    return true;
                case EntanglingVinePassiveAsset entanglingVine:
                    context.Set("leafSlowRatio", entanglingVine.LeafSlowRatio);
                    return true;
                case FieldValueAmplificationPassiveAsset fieldAmplification:
                    context.Set("poisonScalingPercent",
                        fieldAmplification.PoisonScalingPercent);
                    if (TryGetStat(
                        owner,
                        PachimonDisplayStat.Poison,
                        out var fieldPoison))
                    {
                        context.Set("currentMultiplier",
                            SignedStatMath.AmplificationMultiplier(
                                fieldPoison
                                * fieldAmplification.PoisonScalingPercent
                                / 100m).ToString("0.##"));
                    }
                    return true;
                case TargetSlowDamagePassiveAsset targetSlow:
                    context.Set("slowRatio", targetSlow.SlowRatio);
                    return true;
                case ResistAdvantageDamagePassiveAsset resistAdvantage:
                    context.Set("resistDifferenceRatio",
                        resistAdvantage.ResistDifferenceRatio);
                    return true;
                case SweetSciencePassiveAsset sweetScience:
                    context.Set("speedGain", sweetScience.SpeedGain);
                    return true;
                case BurnPursuitPassiveAsset burnPursuit:
                    context.Set("increasePercent",
                        burnPursuit.DamagePercent - 100);
                    return true;
                case WarmPlantPassiveAsset warmPlant:
                    context.Set("temperatureSpeedRatio",
                        warmPlant.TemperatureSpeedRatio);
                    return true;
                case FrozenGroundPassiveAsset:
                    return true;
                case TeamAttributeDamagePassiveAsset teamDamage:
                    context.Set("increasePercent",
                        teamDamage.DamagePercent - 100);
                    return true;
                case DragonSkeletonPassiveAsset dragonSkeleton:
                    context.Set("dragonFromSpeedRatio",
                            dragonSkeleton.DragonFromSpeedRatio)
                        .Set("speedFromDragonRatio",
                            dragonSkeleton.SpeedFromDragonRatio);
                    return true;
                case FireArcherPassiveAsset fireArcher:
                    context.Set("missingHpPercent",
                            fireArcher.MissingHpPercent)
                        .Set("fireScalingPercent",
                            fireArcher.FireScalingPercent);
                    if (TryGetStat(owner, PachimonDisplayStat.Fire, out var archerFire))
                    {
                        context.Set("currentMissingHpPercent",
                            (fireArcher.MissingHpPercent
                             * SignedStatMath.AmplificationMultiplier(
                                 archerFire
                                 * fireArcher.FireScalingPercent
                                 / 100m)).ToString("0.##"));
                    }
                    return true;
                case WaterBlessingPassiveAsset waterBlessing:
                    context.Set("baseHealingRatio",
                            waterBlessing.BaseHealingRatio)
                        .Set("aquaHealingRatio",
                            waterBlessing.AquaHealingRatio);
                    if (TryGetStat(owner, PachimonDisplayStat.Aqua, out var blessingAqua))
                    {
                        context.Set("currentHealingPercent",
                            System.Math.Max(
                                0m,
                                waterBlessing.BaseHealingRatio
                                + blessingAqua
                                * waterBlessing.AquaHealingRatio
                                / 100m).ToString("0.##"));
                    }
                    return true;
                case SturdyPlantPassiveAsset sturdyPlant:
                    context.Set("leafResistRatio",
                        sturdyPlant.LeafResistBonusRatio);
                    if (TryGetStat(owner, PachimonDisplayStat.Leaf, out var sturdyLeaf))
                    {
                        context.Set("currentResistBonus", decimal.Floor(
                            sturdyLeaf
                            * sturdyPlant.LeafResistBonusRatio
                            / 100m));
                    }
                    return true;
                case StaticElectricityPassiveAsset staticElectricity:
                    context.Set("electricBaseValue",
                            staticElectricity.ElectricBaseValue)
                        .Set("iceBaseValue",
                            staticElectricity.IceBaseValue);
                    if (TryGetStat(owner, PachimonDisplayStat.Electric, out var staticElectric)
                        && TryGetStat(owner, PachimonDisplayStat.Ice, out var staticIce))
                    {
                        context.Set("paralysisValue", checked(
                            Scale(
                                staticElectricity.ElectricBaseValue,
                                staticElectric,
                                100)
                            + Scale(
                                staticElectricity.IceBaseValue,
                                staticIce,
                                100)));
                    }
                    return true;
                case IceGrowthOnDamagePassiveAsset iceGrowth:
                    context.Set("iceIncrease",
                        iceGrowth.IceIncreasePerDamage);
                    return true;
                case WindBlessingPassiveAsset windBlessing:
                    context.Set("shieldPercent",
                            windBlessing.SharedShieldPercent)
                        .Set("durationPercent",
                            windBlessing.DurationPercent);
                    return true;
                case DragonRagePassiveAsset dragonRage:
                    context.Set("penetrationRatio",
                        dragonRage.PenetrationRatio);
                    if (TryGetStat(owner, PachimonDisplayStat.Dragon, out var rageDragon))
                    {
                        context.Set("currentPenetration", decimal.Floor(
                            rageDragon * dragonRage.PenetrationRatio / 100m));
                    }
                    return true;
                case FireGrowthOnDamagePassiveAsset burningMan:
                    context.Set("fireIncrease", burningMan.FireIncreasePerDamage);
                    return true;
                case WaterCuttingPassiveAsset:
                    return true;
                case PowderPlantPassiveAsset powderPlant:
                    context.Set("leafIncrease", powderPlant.LeafIncreasePerApplication);
                    return true;
                case StoredChargePassiveAsset storedCharge:
                    context.Set("increasePercent", storedCharge.DamagePercentPerStack);
                    return true;
                case PoisonKnightPassiveAsset poisonKnight:
                    context.Set("baseSharePercent", poisonKnight.BaseSharePercent)
                        .Set("poisonScalingPercent", poisonKnight.PoisonScalingPercent);
                    if (TryGetStat(owner, PachimonDisplayStat.Poison, out var knightPoison))
                    {
                        context.Set("sharePercent", (
                            poisonKnight.BaseSharePercent
                            * SignedStatMath.AmplificationMultiplier(
                                knightPoison * poisonKnight.PoisonScalingPercent / 100m))
                            .ToString("0.##"));
                    }
                    return true;
                case IceWitchPassiveAsset iceWitch:
                    context.Set("baseDamage", iceWitch.BaseIceDamage)
                        .Set("iceRatio", iceWitch.IceDamageRatio);
                    if (TryGetStat(owner, PachimonDisplayStat.Ice, out var witchIce))
                    {
                        context.Set("damage", Scale(
                            iceWitch.BaseIceDamage,
                            witchIce,
                            iceWitch.IceDamageRatio));
                    }
                    return true;
                case WeatherChildPassiveAsset weatherChild:
                    context.Set("damageBonusPerWeather", weatherChild.DamageBonusPerWeather);
                    return true;
                case ManyHitsPassiveAsset manyHits:
                    context.Set("increasePercent", manyHits.DamagePercent - 100);
                    return true;
                case SunnyManPassiveAsset sunnyMan:
                    context.Set("increasePercent", sunnyMan.SpeedPercent - 100);
                    return true;
                case BotanicalGardenPassiveAsset botanicalGarden:
                    context.Set("damageBonusPerPlant", botanicalGarden.DamageBonusPerPlant);
                    return true;
                case ThunderManPassiveAsset thunderMan:
                    context.Set("speedBonus", thunderMan.SpeedBonus);
                    return true;
                case PoisonMagicianPassiveAsset poisonMagician:
                    context.Set("poisonGain", poisonMagician.PoisonGainPerHit);
                    return true;
                case IceArmorPassiveAsset iceArmor:
                    context.Set("iceScalingPercent", iceArmor.IceScalingPercent);
                    return true;
                case WindRiderPassiveAsset windRider:
                    context.Set("speedGain", windRider.SpeedGainPerHit);
                    return true;
                case TargetStatusDamagePassiveAsset statusDamage:
                    context.Set("increasePercent", statusDamage.DamagePercent - 100);
                    return true;
                case WeaklingBullyPassiveAsset weaklingBully:
                    context.Set("increasePercent", weaklingBully.DamagePercent - 100)
                        .Set("speedBonus", weaklingBully.SpeedBonus)
                        .Set("duration", weaklingBully.SpeedDurationTicks);
                    return true;
                case BurningFlowerPassiveAsset burningFlower:
                    context.Set("statGain", burningFlower.StatGainPerDamage);
                    return true;
                case ParalysisGenerationPassiveAsset paralysisGeneration:
                    context.Set("electricRatio", paralysisGeneration.ElectricFromParalysisRatio);
                    return true;
                case LastTouchPassiveAsset lastTouch:
                    context.Set("executionRatio", lastTouch.PoisonExecutionRatio);
                    if (TryGetStat(owner, PachimonDisplayStat.Poison, out var lastTouchPoison))
                    {
                        context.Set("executionPercent", (
                            lastTouchPoison * lastTouch.PoisonExecutionRatio / 100m)
                            .ToString("0.##"));
                    }
                    return true;
                case ChillSpreadPassiveAsset chillSpread:
                    context.Set("spreadPercent", chillSpread.SpreadPercent);
                    return true;
                case WindMagicianPassiveAsset windMagician:
                    context.Set("windGain", windMagician.WindGainPerHit);
                    return true;
                case DragonGuardPassiveAsset dragonGuard:
                    context.Set("dragonRatio", dragonGuard.ResistFromDragonRatio);
                    if (TryGetStat(owner, PachimonDisplayStat.Dragon, out var guardDragon))
                    {
                        context.Set("resistBonus", decimal.Floor(
                            guardDragon * dragonGuard.ResistFromDragonRatio / 100m));
                    }
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryGetStat(
            PachimonPreviewContent owner,
            PachimonDisplayStat stat,
            out int value)
        {
            value = 0;
            return owner?.IsRevealed == true && owner.TryGetStat(stat, out value);
        }

        private static int Scale(int baseValue, int stat, int ratio) =>
            SignedStatMath.FloorNonNegative(
                SignedStatMath.ScaleFromBase(baseValue, stat, ratio));
    }
}
