using System;
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

            if (skill is InitialAttributeDamageSkillAsset initial
                && TryGetAttribute(owner, skill.AllocationType, out var initialAttribute))
            {
                context.Set("damage", SignedStatMath.FloorNonNegative(
                        SignedStatMath.ScaleFromBase(
                            initial.BaseDamage,
                            initialAttribute,
                            initial.DamageRatio), 1))
                    .Set("baseDamage", initial.BaseDamage)
                    .Set("attribute", initialAttribute)
                    .Set("ratio", AttributeDamageRules.ScalingRatio);
                if (initial is ElectricShockSkillAsset electric)
                {
                    var ice = TryGetAttribute(
                        owner,
                        AllocationType.Ice,
                        out var electricIce)
                        ? electricIce
                        : 0;
                    context.Set("statusValue", SignedStatMath.FloorNonNegative(
                            SignedStatMath.ScaleFromBase(
                                electric.ParalysisBaseValue,
                                initialAttribute,
                                electric.ParalysisValueRatio)))
                        .Set("statusDuration", Math.Max(1,
                            SignedStatMath.FloorNonNegative(
                                SignedStatMath.ScaleFromBase(
                                    electric.ParalysisBaseDurationTicks,
                                    ice,
                                    electric.ParalysisDurationRatio))));
                    context.Set("statusBaseValue", electric.ParalysisBaseValue)
                        .Set("statusRatio", electric.ParalysisValueRatio)
                        .Set("statusDurationBase",
                            electric.ParalysisBaseDurationTicks)
                        .Set("statusDurationAttribute", ice)
                        .Set("statusDurationRatio",
                            electric.ParalysisDurationRatio);
                }
                else if (initial is PoisonNeedleSkillAsset poison)
                {
                    context.Set("statusValue", SignedStatMath.FloorNonNegative(
                        SignedStatMath.ScaleFromBase(
                             poison.ToxinBaseValue,
                             initialAttribute,
                             poison.ToxinRatio)))
                        .Set("statusBaseValue", poison.ToxinBaseValue)
                        .Set("statusRatio", poison.ToxinRatio);
                }
                else if (initial is ColdHandSkillAsset cold)
                {
                    context.Set("statusValue", SignedStatMath.FloorNonNegative(
                        SignedStatMath.ScaleFromBase(
                            cold.ChillBaseValue,
                            initialAttribute,
                            cold.ChillRatio)))
                        .Set("statusBaseValue", cold.ChillBaseValue)
                        .Set("statusRatio", cold.ChillRatio);
                }
                else if (initial is LeafSlicerSkillAsset leafSlicer
                    && TryGetAttribute(
                        owner,
                        AllocationType.Wind,
                        out var leafSlicerWind))
                {
                    context.Set("pollen", Scale(
                            leafSlicer.PollenBaseValue,
                            leafSlicerWind,
                            leafSlicer.PollenWindRatio))
                        .Set("pollenBaseValue", leafSlicer.PollenBaseValue)
                        .Set("pollenAttribute", leafSlicerWind)
                        .Set("pollenRatio", leafSlicer.PollenWindRatio);
                }
                return true;
            }

            if (skill is PlaceholderSkillAsset placeholder
                && TryGetAttribute(owner, skill.AllocationType, out var basicAttribute))
            {
                context.Set("damage", SignedStatMath.FloorNonNegative(
                        placeholder.BaseDamage
                        * SignedStatMath.AmplificationMultiplier(basicAttribute), 1))
                    .Set("baseDamage", placeholder.BaseDamage)
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
                    .Set("penetration", BackfireMath.CalculateAttributeFixedPenetration(
                        backfire, backfirePoison).ToString("0.##"))
                    .Set("baseDamage", backfire.BaseDamage)
                    .Set("damageRatio", backfire.FireScalingPercent)
                    .Set("basePenetration", backfire.BaseAttributeFixedPenetration)
                    .Set("penetrationRatio", backfire.PoisonPenetrationRatio)
                    .Set("fire", backfireFire)
                    .Set("poison", backfirePoison);
                return true;
            }

            if (skill is BurningStrikeSkillAsset burningStrike
                && TryGetStat(owner, PachimonDisplayStat.Fire, out var burningFire))
            {
                context.Set("selfDamage", Scale(
                        burningStrike.SelfBaseDamage,
                        burningFire,
                        burningStrike.SelfFireRatio))
                    .Set("enemyDamage", Scale(
                        burningStrike.EnemyBaseDamage,
                        burningFire,
                        burningStrike.EnemyFireRatio))
                    .Set("burn", Scale(
                        burningStrike.BaseBurnValue,
                        burningFire,
                        burningStrike.BurnFireRatio))
                    .Set("selfBaseDamage", burningStrike.SelfBaseDamage)
                    .Set("enemyBaseDamage", burningStrike.EnemyBaseDamage)
                    .Set("baseBurn", burningStrike.BaseBurnValue)
                    .Set("fire", burningFire);
                return true;
            }

            if (skill is WaterPulseReplacementSkillAsset regularWaterPulse
                && TryGetStat(owner, PachimonDisplayStat.Aqua, out var regularPulseAqua))
            {
                var manaCost = Math.Max(
                    1,
                    SignedStatMath.FloorNonNegative(
                        (owner?.MaxMn ?? 0)
                        * regularWaterPulse.MaxMnCostPercent / 100m));
                context.Set("manaCost", manaCost)
                    .Set("maxMn", owner?.MaxMn ?? 0)
                    .Set("maxMnCostPercent",
                        regularWaterPulse.MaxMnCostPercent)
                    .Set("damagePerMana", regularWaterPulse.DamagePerMana)
                    .Set("aqua", regularPulseAqua)
                    .Set("damageRatio", regularWaterPulse.AquaDamageRatio)
                    .Set("damage", SignedStatMath.FloorNonNegative(
                        manaCost
                        * regularWaterPulse.DamagePerMana
                        * SignedStatMath.AmplificationMultiplier(
                            regularPulseAqua
                            * regularWaterPulse.AquaDamageRatio / 100m),
                        1));
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
                            pulseAqua * waterPulse.AquaDamageRatio / 100m)
                        * waterPulse.DamagePercent / 100m,
                        1));
                return true;
            }

            if (skill is SunbathSkillAsset sunbath
                && TryGetStat(owner, PachimonDisplayStat.Leaf, out var sunbathLeaf))
            {
                context.Set("baseHealing", sunbath.BaseHealing)
                    .Set("leaf", sunbathLeaf)
                    .Set("healingRatio", sunbath.LeafHealingRatio)
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
                        aquaShock, shockAqua))
                    .Set("electricBaseDamage", aquaShock.ElectricBaseDamage)
                    .Set("aquaBaseDamage", aquaShock.AquaBaseDamage)
                    .Set("leakBaseValue", aquaShock.LeakBaseValue)
                    .Set("electric", shockElectric)
                    .Set("aqua", shockAqua)
                    .Set("electricRatio", AttributeDamageRules.ScalingRatio)
                    .Set("aquaRatio", AttributeDamageRules.ScalingRatio)
                    .Set("leakRatio", AttributeDamageRules.ScalingRatio);
                return true;
            }

            if (skill is NeurotoxinSkillAsset neurotoxin
                && TryGetStat(owner, PachimonDisplayStat.Poison, out var neuroPoison)
                && TryGetStat(owner, PachimonDisplayStat.Electric, out var neuroElectric))
            {
                context.Set("stunTicks", NeurotoxinMath.CalculateStunTicks(
                        neurotoxin, neuroElectric))
                    .Set("toxinValue", NeurotoxinMath.CalculateToxinValue(
                        neurotoxin, neuroPoison))
                    .Set("electricStun", SignedStatMath.FloorNonNegative(
                        SignedStatMath.ScaleFromBase(
                            neurotoxin.BaseElectricStunTicks,
                            neuroElectric,
                            neurotoxin.ElectricStunScalingPercent)))
                    .Set("baseElectricStun", neurotoxin.BaseElectricStunTicks)
                    .Set("baseToxin", neurotoxin.BaseToxinValue)
                    .Set("poison", neuroPoison)
                    .Set("electric", neuroElectric)
                    .Set("electricRatio", neurotoxin.ElectricStunScalingPercent)
                    .Set("toxinRatio", neurotoxin.ToxinScalingPercent);
                return true;
            }

            if (skill is IceShieldSkillAsset iceShield
                && TryGetStat(owner, PachimonDisplayStat.Ice, out var shieldIce))
            {
                context.Set("shield", IceShieldMath.CalculateShieldValue(
                        iceShield, shieldIce))
                    .Set("baseShield", iceShield.BaseShieldValue)
                    .Set("ice", shieldIce)
                    .Set("shieldRatio", iceShield.IceShieldRatio);
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
                        / 100m))
                    .Set("baseDamage", flyingAttack.BaseWindDamage)
                    .Set("wind", flyingWind)
                    .Set("damageRatio", flyingAttack.WindDamageRatio)
                    .Set("speedRatio",
                        flyingAttack.FlyingStatus?.WindSpeedRatio ?? 0);
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
                    .Set("oneTwoValue", dragonJab.OneTwoValue)
                    .Set("baseDamage", dragonJab.BaseDragonDamage)
                    .Set("dragon", jabDragon)
                    .Set("damageRatio", dragonJab.DragonDamageRatio);
                return true;
            }

            if (skill is ChainBurnSkillAsset chainBurn
                && TryGetStat(owner, PachimonDisplayStat.Fire, out var chainFire))
            {
                context.Set("damage", SignedStatMath.FloorNonNegative(
                        SignedStatMath.ScaleFromBase(
                            chainBurn.BaseDamage,
                            chainFire,
                            chainBurn.FireScalingPercent)))
                    .Set("hitCount", chainBurn.BaseChainCount + 1)
                    .Set("addChain", AddChainRuntime.FormatUnits(
                        chainBurn.AddChainGainUnits))
                    .Set("baseDamage", chainBurn.BaseDamage)
                    .Set("fire", chainFire)
                    .Set("damageRatio", chainBurn.FireScalingPercent);
                return true;
            }

            if (skill is RainDanceSkillAsset rainDance
                && TryGetStat(owner, PachimonDisplayStat.Aqua, out var rainAqua)
                && TryGetStat(owner, PachimonDisplayStat.GenerationPower, out var rainGeneration))
            {
                context.Set("rainValue", CalculateGeneratedWeatherValue(
                        rainDance.BaseValue,
                        rainAqua,
                        rainDance.AquaValueRatio,
                        rainGeneration))
                    .Set("baseValue", rainDance.BaseValue)
                    .Set("aqua", rainAqua)
                    .Set("aquaRatio", rainDance.AquaValueRatio)
                    .Set("generationPower", rainGeneration);
                return true;
            }

            if (skill is ChainVinesSkillAsset chainVines
                && TryGetStat(owner, PachimonDisplayStat.Leaf, out var vinesLeaf))
            {
                context.Set("damage", SignedStatMath.FloorNonNegative(
                        SignedStatMath.ScaleFromBase(
                            chainVines.BaseLeafDamage,
                            vinesLeaf,
                        AttributeDamageRules.ScalingRatio)))
                    .Set("slow", SignedStatMath.FloorNonNegative(
                        SignedStatMath.ScaleFromBase(
                            chainVines.BaseSlow,
                            vinesLeaf,
                            chainVines.SlowLeafRatio)))
                    .Set("hitCount", chainVines.BaseChainCount + 1)
                    .Set("addChain", AddChainRuntime.FormatUnits(
                        chainVines.AddChainGainUnits))
                    .Set("baseDamage", chainVines.BaseLeafDamage)
                    .Set("baseSlow", chainVines.BaseSlow)
                    .Set("leaf", vinesLeaf)
                    .Set("damageRatio", AttributeDamageRules.ScalingRatio)
                    .Set("slowRatio", chainVines.SlowLeafRatio);
                return true;
            }

            if (skill is ElectricExplosionSkillAsset explosion
                && TryGetStat(owner, PachimonDisplayStat.Electric, out var explosionElectric)
                && TryGetStat(owner, PachimonDisplayStat.Fire, out var explosionFire))
            {
                context.Set("damage", SignedStatMath.FloorNonNegative(
                        ElectricExplosionMath.CalculateBaseDamage(
                            explosion,
                            explosionElectric)))
                    .Set("penetration",
                        PenetrationMath.CalculateDiminishingPercentage(
                            ElectricExplosionMath.CalculateAttributePenetrationValue(
                                explosion,
                                explosionFire)).ToString("0.##"))
                    .Set("baseDamage", explosion.BaseDamage)
                    .Set("electric", explosionElectric)
                    .Set("fire", explosionFire)
                    .Set("electricRatio", explosion.ElectricScalingPercent)
                    .Set("penetrationValue",
                        ElectricExplosionMath.CalculateAttributePenetrationValue(
                            explosion,
                            explosionFire).ToString("0.##"))
                    .Set("penetrationRatio", explosion.FirePenetrationRatio);
                return true;
            }

            if (skill is SmogSkillAsset smog
                && TryGetStat(owner, PachimonDisplayStat.Poison, out var smogPoison))
            {
                context.Set("fieldValue", SignedStatMath.FloorNonNegative(
                    SignedStatMath.ScaleFromBase(
                        smog.BaseFieldValue,
                        smogPoison,
                        smog.PoisonScalingPercent)))
                    .Set("baseValue", smog.BaseFieldValue)
                    .Set("poison", smogPoison)
                    .Set("ratio", smog.PoisonScalingPercent);
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
                        iceShard.OtherChillIceRatio))
                    .Set("frontBaseDamage", iceShard.FrontBaseDamage)
                    .Set("frontBaseChill", iceShard.FrontBaseChill)
                    .Set("otherBaseDamage", iceShard.OtherBaseDamage)
                    .Set("otherBaseChill", iceShard.OtherBaseChill)
                    .Set("ice", shardIce)
                    .Set("frontDamageRatio", iceShard.FrontDamageIceRatio)
                    .Set("frontChillRatio", iceShard.FrontChillIceRatio)
                    .Set("otherDamageRatio", iceShard.OtherDamageIceRatio)
                    .Set("otherChillRatio", iceShard.OtherChillIceRatio);
                return true;
            }

            if (skill is WindErosionSkillAsset erosion
                && TryGetStat(owner, PachimonDisplayStat.Wind, out var erosionWind))
            {
                context.Set("erosionValue", Scale(
                    erosion.BaseErosionValue,
                    erosionWind,
                    erosion.WindValueRatio))
                    .Set("baseValue", erosion.BaseErosionValue)
                    .Set("wind", erosionWind)
                    .Set("ratio", erosion.WindValueRatio);
                return true;
            }

            if (skill is DragonFootworkSkillAsset footwork
                && TryGetStat(owner, PachimonDisplayStat.Dragon, out var footworkDragon))
            {
                context.Set("duration", System.Math.Max(1, Scale(
                    footwork.BaseDurationTicks,
                    footworkDragon,
                    footwork.DurationDragonRatio)))
                    .Set("baseDuration", footwork.BaseDurationTicks)
                    .Set("dragon", footworkDragon)
                    .Set("durationRatio", footwork.DurationDragonRatio);
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
                    .Set("burn", ScaleRatio(
                        value,
                        field?.ValueBurnRatio ?? 0))
                    .Set("baseValue", fireBarrier.BaseValue)
                    .Set("fire", barrierFire)
                    .Set("valueRatio", fireBarrier.FireValueRatio)
                    .Set("burnRatio", field?.ValueBurnRatio ?? 0);
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
                             / 100m).ToString("0.##"))
                        .Set("aqua", launchAqua);
                }
                return true;
            }

            if (skill is SolarBeamSkillAsset solarBeam
                && TryGetStat(owner, PachimonDisplayStat.Leaf, out var solarLeaf)
                && TryGetStat(owner, PachimonDisplayStat.Wind, out var solarWind))
            {
                context.Set("damage", Scale(
                        solarBeam.BaseLeafDamage,
                        solarLeaf,
                        AttributeDamageRules.ScalingRatio))
                    .Set("baseStartup", solarBeam.BaseStartupTicks)
                    .Set("baseDamage", solarBeam.BaseLeafDamage)
                    .Set("leaf", solarLeaf)
                    .Set("damageRatio", AttributeDamageRules.ScalingRatio)
                    .Set("pollen", Scale(
                        solarBeam.PollenBaseValue,
                        solarWind,
                        solarBeam.PollenWindRatio))
                    .Set("pollenBaseValue", solarBeam.PollenBaseValue)
                    .Set("wind", solarWind)
                    .Set("pollenRatio", solarBeam.PollenWindRatio)
                    .Set("temperatureRatio",
                        solarBeam.TemperatureStartupRatio);
                return true;
            }

            if (skill is ElectricQuickAttackSkillAsset quickAttack
                && TryGetStat(owner, PachimonDisplayStat.Electric, out var quickElectric)
                && TryGetStat(owner, PachimonDisplayStat.Fire, out var quickFire)
                && TryGetStat(owner, PachimonDisplayStat.Speed, out var quickSpeed)
                && TryGetStat(owner, PachimonDisplayStat.Haste, out var quickHaste))
            {
                var fireTimingMultiplier =
                    SkillTimingCalculator.CalculateFireTimingMultiplier(
                        quickAttack,
                        quickFire);
                context.Set("electricDamage", SignedStatMath.FloorNonNegative(
                        ElectricQuickAttackMath.CalculateElectricBaseDamage(
                            quickAttack,
                            quickElectric)))
                    .Set("recovery", BattleTickMath.GetEffectiveRecovery(
                        quickAttack.BaseRecoveryTicks,
                        quickSpeed,
                        fireTimingMultiplier))
                    .Set("cooldown", BattleTickMath.GetEffectiveCooldown(
                        quickAttack.BaseCooldownTicks,
                        quickHaste))
                    .Set("electricBaseDamage", quickAttack.ElectricBaseDamage)
                    .Set("electric", quickElectric)
                    .Set("fire", quickFire)
                    .Set("damageRatio", AttributeDamageRules.ScalingRatio)
                    .Set("fireTimingRatio", quickAttack.FireTimingPercent);
                return true;
            }

            if (skill is ToxinTransferSkillAsset toxinTransfer)
            {
                var baseToxin = toxinTransfer.BaseToxinValue;
                var transferPoison = 0;
                if (TryGetStat(
                        owner,
                        PachimonDisplayStat.Poison,
                        out transferPoison))
                {
                    baseToxin = ToxinTransferMath.CalculateBaseValue(
                        toxinTransfer,
                        transferPoison);
                }
                context.Set("removalPercent", toxinTransfer.RemovalPercent)
                    .Set("baseToxin", baseToxin)
                    .Set("rawBaseToxin", toxinTransfer.BaseToxinValue)
                    .Set("poison", transferPoison)
                    .Set("toxinRatio", toxinTransfer.PoisonScalingPercent)
                    .Set("applicationPercent",
                        toxinTransfer.ApplicationPercent);
                return true;
            }

            if (skill is HeavySnowSkillAsset heavySnow
                && TryGetStat(owner, PachimonDisplayStat.Ice, out var snowIce))
            {
                context.Set("temperatureReduction",
                        SignedStatMath.FloorNonNegative(
                            SignedStatMath.ScaleFromBase(
                                heavySnow.BaseValue,
                                snowIce,
                                heavySnow.IceValueRatio),
                            minimum: 1))
                    .Set("baseValue", heavySnow.BaseValue)
                    .Set("ice", snowIce)
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
                    .Set("duration", healingWind.DurationTicks)
                    .Set("baseHealing", healingWind.BaseHealing)
                    .Set("baseWindBonus", healingWind.BaseWindBonus)
                    .Set("baseSpeedBonus", healingWind.BaseSpeedBonus)
                    .Set("wind", healingWindStat)
                    .Set("windRatio", healingWind.WindRatio);
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
                        fireArrow.BaseDamage,
                        arrowFire,
                        fireArrow.FireScalingPercent))
                    .Set("repeatManaCost", fireArrow.BaseManaCost)
                    .Set("baseDamage", fireArrow.BaseDamage)
                    .Set("fire", arrowFire)
                    .Set("damageRatio", fireArrow.FireScalingPercent);
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
                        definition?.DamageReductionPercent ?? 0)
                    .Set("baseValue", waterVeil.BaseFieldValue)
                    .Set("aqua", veilAqua)
                    .Set("valueRatio", waterVeil.AquaValueRatio);
                return true;
            }

            if (skill is EntanglingVinesSkillAsset entanglingVines
                && TryGetStat(owner, PachimonDisplayStat.Leaf, out var entangleLeaf))
            {
                context.Set("stunTicks", System.Math.Max(1, Scale(
                    entanglingVines.BaseStun,
                    entangleLeaf,
                    entanglingVines.StunLeafRatio)))
                    .Set("baseStun", entanglingVines.BaseStun)
                    .Set("leaf", entangleLeaf)
                    .Set("stunRatio", entanglingVines.StunLeafRatio);
                return true;
            }

            if (skill is ChargeSkillAsset charge
                && TryGetStat(owner, PachimonDisplayStat.Electric, out var chargeElectric)
                && TryGetStat(owner, PachimonDisplayStat.Speed, out var chargeSpeed))
            {
                context.Set("chargeValue", System.Math.Max(1, chargeElectric))
                    .Set("startup", BattleTickMath.GetEffectiveStartup(
                        charge.BaseStartupTicks,
                        chargeSpeed))
                    .Set("electric", chargeElectric)
                    .Set("baseStartup", charge.BaseStartupTicks)
                    .Set("speed", chargeSpeed);
                return true;
            }

            if (skill is ToxinExplosionSkillAsset toxinExplosion)
            {
                context.Set("toxinConversion",
                        toxinExplosion.ToxinConversionPercent)
                    .Set("aoeFirePercent", toxinExplosion.AoeFirePercent)
                    .Set("poisonRatio", toxinExplosion.PoisonScalingPercent)
                    .Set("fireRatio", toxinExplosion.FireScalingPercent);
                if (TryGetStat(owner, PachimonDisplayStat.Poison, out var explosionPoison))
                {
                    context.Set("poison", explosionPoison);
                }
                if (TryGetStat(owner, PachimonDisplayStat.Fire, out var toxinExplosionFire))
                {
                    context.Set("fire", toxinExplosionFire);
                }
                return true;
            }

            if (skill is IceBladeSkillAsset iceBlade
                && TryGetStat(owner, PachimonDisplayStat.Ice, out var bladeIce))
            {
                context.Set("duration",
                        IceBladeSkillLogic.CalculateDuration(iceBlade, bladeIce))
                    .Set("damagePercent",
                        iceBlade.FieldEffect?.DamagePercent ?? 0)
                    .Set("baseDuration", iceBlade.BaseDurationTicks)
                    .Set("scalingDuration", iceBlade.ScalingDurationTicks)
                    .Set("ice", bladeIce)
                    .Set("durationRatio", iceBlade.IceDurationRatio);
                return true;
            }

            if (skill is SecondWindSkillAsset secondWind
                && TryGetStat(owner, PachimonDisplayStat.Wind, out var secondWindStat))
            {
                context.Set("shield", Scale(
                        secondWind.BaseShieldValue,
                        secondWindStat,
                        secondWind.WindShieldRatio))
                    .Set("baseShield", secondWind.BaseShieldValue)
                    .Set("duration", secondWind.DurationTicks)
                    .Set("wind", secondWindStat)
                    .Set("shieldRatio", secondWind.WindShieldRatio);
                return true;
            }

            if (skill is DragonBreakSkillAsset dragonBreak
                && TryGetStat(owner, PachimonDisplayStat.Dragon, out var breakDragon))
            {
                context.Set("damage", Scale(
                    dragonBreak.BaseDragonDamage,
                    breakDragon,
                    dragonBreak.DragonDamageRatio))
                    .Set("baseDamage", dragonBreak.BaseDragonDamage)
                    .Set("dragon", breakDragon)
                    .Set("damageRatio", dragonBreak.DragonDamageRatio);
                return true;
            }

            if (skill is CombustionSkillAsset combustion
                && TryGetStat(owner, PachimonDisplayStat.Fire, out var combustionFire))
            {
                context.Set("damage", Scale(
                        combustion.BaseDamage,
                        combustionFire,
                        combustion.FireScalingPercent))
                    .Set("baseDamage", combustion.BaseDamage)
                    .Set("fire", combustionFire)
                    .Set("damageRatio", combustion.FireScalingPercent);
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
                    .Set("penetration",
                        PenetrationMath.CalculateDiminishingPercentage(
                            cutterWind * waterCutter.WindPenetrationRatio / 100m)
                        .ToString("0.##"))
                    .Set("penetrationValue",
                        (cutterWind * waterCutter.WindPenetrationRatio / 100m)
                        .ToString("0.##"))
                    .Set("baseDamage", waterCutter.BaseAquaDamage)
                    .Set("aqua", cutterAqua)
                    .Set("damageRatio", waterCutter.AquaDamageRatio)
                    .Set("wind", cutterWind)
                    .Set("penetrationRatio", waterCutter.WindPenetrationRatio);
                return true;
            }

            if (skill is ParalysisPowderSkillAsset paralysisPowder
                && TryGetStat(owner, PachimonDisplayStat.Leaf, out var powderLeaf)
                && TryGetStat(owner, PachimonDisplayStat.Electric, out var powderElectric)
                && TryGetStat(owner, PachimonDisplayStat.Poison, out var powderPoison))
            {
                context.Set("paralysis", checked(
                        Scale(paralysisPowder.BaseElectricValue, powderElectric,
                            paralysisPowder.ElectricValueRatio)
                        + Scale(paralysisPowder.BasePoisonValue, powderPoison,
                            paralysisPowder.PoisonValueRatio)))
                    .Set("paralysisDuration", Math.Max(1,
                        Scale(paralysisPowder.BaseDurationTicks, powderLeaf,
                            paralysisPowder.DurationLeafRatio)))
                    .Set("baseDuration", paralysisPowder.BaseDurationTicks)
                    .Set("leaf", powderLeaf)
                    .Set("durationRatio", paralysisPowder.DurationLeafRatio)
                    .Set("electricValue", Scale(paralysisPowder.BaseElectricValue,
                        powderElectric, paralysisPowder.ElectricValueRatio))
                    .Set("baseElectricValue", paralysisPowder.BaseElectricValue)
                    .Set("electric", powderElectric)
                    .Set("electricRatio", paralysisPowder.ElectricValueRatio)
                    .Set("poisonValue", Scale(paralysisPowder.BasePoisonValue,
                        powderPoison, paralysisPowder.PoisonValueRatio))
                    .Set("basePoisonValue", paralysisPowder.BasePoisonValue)
                    .Set("poison", powderPoison)
                    .Set("poisonRatio", paralysisPowder.PoisonValueRatio)
                    .Set("pollen", Scale(
                        paralysisPowder.PollenBaseValue,
                        powderPoison,
                        paralysisPowder.PollenPoisonRatio))
                    .Set("pollenBaseValue", paralysisPowder.PollenBaseValue)
                    .Set("pollenRatio", paralysisPowder.PollenPoisonRatio);
                return true;
            }

            if (skill is ElectromagneticCannonSkillAsset cannon
                && TryGetStat(owner, PachimonDisplayStat.Electric, out var cannonElectric))
            {
                context.Set("damage", Scale(cannon.BaseDamage, cannonElectric, 100))
                    .Set("startup", cannon.BaseStartupTicks)
                    .Set("baseDamage", cannon.BaseDamage)
                    .Set("electric", cannonElectric)
                    .Set("damageRatio", AttributeDamageRules.ScalingRatio);
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
                            poisonShield, shieldPoison).ToString("0.##"))
                    .Set("baseShield", poisonShield.BaseShieldValue)
                    .Set("poison", shieldPoison)
                    .Set("shieldRatio", poisonShield.ShieldPoisonScalingPercent)
                    .Set("baseReduction", poisonShield.BaseToxinReductionPercent)
                    .Set("reductionRatio", poisonShield.ReductionPoisonScalingPercent);
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
                        frozenBreak.HealIceRatio))
                    .Set("baseDamage", frozenBreak.BaseIceDamage)
                    .Set("baseDuration", frozenBreak.BaseDuration)
                    .Set("baseHealing", frozenBreak.BaseHealPerTick)
                    .Set("ice", frozenIce)
                    .Set("damageRatio", frozenBreak.IceDamageRatio)
                    .Set("durationRatio", frozenBreak.DurationIceRatio)
                    .Set("healingRatio", frozenBreak.HealIceRatio);
                return true;
            }

            if (skill is WindStormSkillAsset windStorm
                && TryGetStat(owner, PachimonDisplayStat.Wind, out var stormWind)
                && TryGetStat(owner, PachimonDisplayStat.GenerationPower, out var stormGeneration))
            {
                context.Set("value", CalculateGeneratedWeatherValue(
                        windStorm.BaseValue,
                        stormWind,
                        windStorm.WindValueRatio,
                        stormGeneration))
                    .Set("baseValue", windStorm.BaseValue)
                    .Set("wind", stormWind)
                    .Set("valueRatio", windStorm.WindValueRatio)
                    .Set("generationPower", stormGeneration);
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
                            + hookDragon * dragonHook.CrankerDragonRatio / 100m)))
                    .Set("baseDamage", dragonHook.BaseDragonDamage)
                    .Set("baseCranker", dragonHook.BaseCrankerValue)
                    .Set("dragon", hookDragon)
                    .Set("damageRatio", dragonHook.DragonDamageRatio)
                    .Set("crankerRatio", dragonHook.CrankerDragonRatio);
                return true;
            }

            if (skill is SunnyDaySkillAsset sunnyDay
                && TryGetStat(owner, PachimonDisplayStat.Fire, out var sunnyFire))
            {
                context.Set("temperature", System.Math.Max(1,
                    SignedStatMath.FloorNonNegative(
                        SignedStatMath.ScaleFromBase(
                            sunnyDay.BaseValue,
                            sunnyFire,
                            sunnyDay.FireValueRatio))))
                    .Set("baseValue", sunnyDay.BaseValue)
                    .Set("fire", sunnyFire)
                    .Set("valueRatio", sunnyDay.FireValueRatio);
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
                        muddyWater.PoisonSlowRatio))
                    .Set("baseDamage", muddyWater.BaseAquaDamage)
                    .Set("baseSlow", muddyWater.BaseSlow)
                    .Set("aqua", muddyAqua)
                    .Set("poison", muddyPoison)
                    .Set("damageRatio", muddyWater.AquaDamageRatio)
                    .Set("slowRatio", muddyWater.PoisonSlowRatio);
                return true;
            }

            if (skill is BeatVineSkillAsset beatVine
                && TryGetStat(owner, PachimonDisplayStat.Leaf, out var beatLeaf))
            {
                context.Set("value", Scale(
                        beatVine.FieldEffect?.BaseValue ?? 0,
                        beatLeaf,
                        beatVine.FieldEffect?.LeafValueRatio ?? 0))
                    .Set("interval", beatVine.FieldEffect?.AttackIntervalTicks ?? 0)
                    .Set("baseValue", beatVine.FieldEffect?.BaseValue ?? 0)
                    .Set("leaf", beatLeaf)
                    .Set("valueRatio", beatVine.FieldEffect?.LeafValueRatio ?? 0)
                    .Set("pollen", ScaleRatio(
                        Scale(
                            beatVine.FieldEffect?.BaseValue ?? 0,
                            beatLeaf,
                            beatVine.FieldEffect?.LeafValueRatio ?? 0),
                        beatVine.FieldEffect?.PollenValueRatio ?? 0))
                    .Set("pollenRatio",
                        beatVine.FieldEffect?.PollenValueRatio ?? 0);
                return true;
            }

            if (skill is LightningCloudSkillAsset lightningCloud
                && TryGetStat(owner, PachimonDisplayStat.Electric, out var cloudElectric)
                && TryGetStat(owner, PachimonDisplayStat.GenerationPower, out var cloudGeneration))
            {
                context.Set("value", CalculateGeneratedWeatherValue(
                        lightningCloud.BaseValue,
                        cloudElectric,
                        lightningCloud.ElectricValueRatio,
                        cloudGeneration))
                    .Set("baseValue", lightningCloud.BaseValue)
                    .Set("electric", cloudElectric)
                    .Set("valueRatio", lightningCloud.ElectricValueRatio)
                    .Set("generationPower", cloudGeneration);
                return true;
            }

            if (skill is PoisonMistSkillAsset poisonMist
                && TryGetStat(owner, PachimonDisplayStat.Poison, out var mistPoison)
                && TryGetStat(owner, PachimonDisplayStat.Aqua, out var mistAqua)
                && TryGetStat(owner, PachimonDisplayStat.Wind, out var mistWind))
            {
                context.Set("value", poisonMist.CalculateMistValue(mistPoison))
                    .Set("duration", poisonMist.CalculateDurationTicks(mistAqua))
                    .Set("minimumValue",
                        poisonMist.CalculateMinimumValue(mistPoison, mistWind))
                    .Set("baseValue", poisonMist.BaseMistValue)
                    .Set("baseDuration", poisonMist.BaseDurationTicks)
                    .Set("baseMinimum", poisonMist.BaseMinimumValue)
                    .Set("poison", mistPoison)
                    .Set("aqua", mistAqua)
                    .Set("wind", mistWind)
                    .Set("valueRatio", poisonMist.PoisonValueRatio)
                    .Set("durationRatio", poisonMist.AquaDurationRatio)
                    .Set("minimumRatio", poisonMist.WindMinimumValueRatio);
                return true;
            }

            if (skill is IcePebbleSkillAsset icePebble
                && TryGetStat(owner, PachimonDisplayStat.Ice, out var pebbleIce))
            {
                context.Set("damage", Scale(icePebble.BaseDamage, pebbleIce))
                    .Set("chill", Scale(icePebble.BaseChill, pebbleIce, icePebble.IceRatio))
                    .Set("shield", Scale(icePebble.BaseShield, pebbleIce, icePebble.IceRatio))
                    .Set("duration", icePebble.ShieldDurationTicks)
                    .Set("baseDamage", icePebble.BaseDamage)
                    .Set("baseChill", icePebble.BaseChill)
                    .Set("baseShield", icePebble.BaseShield)
                    .Set("ice", pebbleIce)
                    .Set("ratio", icePebble.IceRatio);
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
                        cuttingDance.AddChainGainUnits))
                    .Set("baseDamage", cuttingDance.BaseWindDamage)
                    .Set("baseErosion", cuttingDance.BaseErosion)
                    .Set("wind", danceWind)
                    .Set("damageRatio", cuttingDance.WindDamageRatio)
                    .Set("erosionRatio", cuttingDance.ErosionWindRatio);
                return true;
            }

            if (skill is DragonUpperSkillAsset dragonUpper
                && TryGetStat(owner, PachimonDisplayStat.Dragon, out var upperDragon))
            {
                context.Set("damage", Scale(
                        dragonUpper.BaseDragonDamage,
                        upperDragon,
                        dragonUpper.DragonDamageRatio))
                    .Set("knockoutDuration", dragonUpper.KnockoutDurationTicks)
                    .Set("baseDamage", dragonUpper.BaseDragonDamage)
                    .Set("dragon", upperDragon)
                    .Set("damageRatio", dragonUpper.DragonDamageRatio);
                return true;
            }

            if (skill is EvaporationSkillAsset evaporation
                && TryGetStat(owner, PachimonDisplayStat.Fire, out var evaporationFire)
                && TryGetStat(owner, PachimonDisplayStat.Aqua, out var evaporationAqua))
            {
                context.Set("damage", checked(
                        Scale(evaporation.BaseFireDamage, evaporationFire, evaporation.FireDamageRatio)
                        + Scale(evaporation.BaseAquaDamage, evaporationAqua, evaporation.AquaDamageRatio)))
                    .Set("penetration",
                        PenetrationMath.CalculateDiminishingPercentage(
                            evaporationFire * evaporation.FirePenetrationRatio / 100m
                            + evaporationAqua * evaporation.AquaPenetrationRatio / 100m)
                        .ToString("0.##"))
                    .Set("penetrationValue",
                        (evaporationFire * evaporation.FirePenetrationRatio / 100m
                         + evaporationAqua * evaporation.AquaPenetrationRatio / 100m)
                        .ToString("0.##"))
                    .Set("weakness", checked(
                        Scale(evaporation.BaseFireWeakness, evaporationFire, evaporation.FireWeaknessRatio)
                        + Scale(evaporation.BaseAquaWeakness, evaporationAqua, evaporation.AquaWeaknessRatio)))
                    .Set("baseFireDamage", evaporation.BaseFireDamage).Set("baseAquaDamage", evaporation.BaseAquaDamage)
                    .Set("baseFireWeakness", evaporation.BaseFireWeakness).Set("baseAquaWeakness", evaporation.BaseAquaWeakness)
                    .Set("fire", evaporationFire).Set("aqua", evaporationAqua)
                    .Set("fireDamageRatio", evaporation.FireDamageRatio).Set("aquaDamageRatio", evaporation.AquaDamageRatio)
                    .Set("firePenetrationRatio", evaporation.FirePenetrationRatio).Set("aquaPenetrationRatio", evaporation.AquaPenetrationRatio)
                    .Set("fireWeaknessRatio", evaporation.FireWeaknessRatio).Set("aquaWeaknessRatio", evaporation.AquaWeaknessRatio);
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
                    .Set("hpDivisor", waterSpout.CurrentHpDivisor)
                    .Set("baseDamage", waterSpout.BaseAquaDamage).Set("aqua", spoutAqua)
                    .Set("damageRatio", waterSpout.AquaDamageRatio);
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
                        fireVine.FieldEffect?.FireValueRatio ?? 0))
                    .Set("baseLeafValue", fireVine.FieldEffect?.BaseLeafValue ?? 0).Set("baseFireValue", fireVine.FieldEffect?.BaseFireValue ?? 0)
                    .Set("leaf", vineLeaf).Set("fire", vineFire)
                    .Set("leafRatio", fireVine.FieldEffect?.LeafValueRatio ?? 0).Set("fireRatio", fireVine.FieldEffect?.FireValueRatio ?? 0);
                return true;
            }

            if (skill is ElectricShieldSkillAsset electricShield
                && TryGetStat(owner, PachimonDisplayStat.Electric, out var electricShieldStat))
            {
                var electricCounterIce = TryGetStat(owner, PachimonDisplayStat.Ice, out var foundCounterIce) ? foundCounterIce : 0;
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
                    .Set("counterParalysisDuration", Math.Max(1,
                        Scale(
                            electricShield.BaseCounterParalysisDurationTicks,
                            electricCounterIce,
                            electricShield.CounterParalysisDurationIceRatio)))
                    .Set("duration", electricShield.DurationTicks)
                    .Set("baseShield", electricShield.BaseShieldValue).Set("baseSelfParalysis", electricShield.BaseSelfParalysis)
                    .Set("baseCounterParalysis", electricShield.BaseCounterParalysis).Set("baseCounterDuration", electricShield.BaseCounterParalysisDurationTicks)
                    .Set("electric", electricShieldStat).Set("ice", electricCounterIce)
                    .Set("shieldRatio", electricShield.ShieldElectricRatio).Set("selfRatio", electricShield.SelfParalysisElectricRatio)
                    .Set("counterRatio", electricShield.CounterParalysisElectricRatio).Set("counterDurationRatio", electricShield.CounterParalysisDurationIceRatio);
                return true;
            }

            if (skill is FirstTouchSkillAsset firstTouch
                && TryGetStat(owner, PachimonDisplayStat.Poison, out var touchPoison))
            {
                context.Set("damage", Scale(firstTouch.BaseDamage, touchPoison))
                    .Set("normalToxin", Scale(firstTouch.BaseNormalToxinValue, touchPoison, firstTouch.PoisonRatio))
                    .Set("bonusDamage", Scale(firstTouch.BonusBaseDamage, touchPoison))
                    .Set("toxin", Scale(firstTouch.BaseToxinValue, touchPoison, firstTouch.PoisonRatio))
                    .Set("baseDamage", firstTouch.BaseDamage).Set("baseNormalToxin", firstTouch.BaseNormalToxinValue)
                    .Set("baseBonusDamage", firstTouch.BonusBaseDamage).Set("baseToxin", firstTouch.BaseToxinValue)
                    .Set("poison", touchPoison).Set("ratio", firstTouch.PoisonRatio);
                return true;
            }

            if (skill is FrostArrowSkillAsset frostArrow
                && TryGetStat(owner, PachimonDisplayStat.Ice, out var frostIce))
            {
                context.Set("damage", Scale(frostArrow.BaseDamage, frostIce))
                    .Set("chill", Scale(frostArrow.BaseChill, frostIce, frostArrow.IceRatio))
                    .Set("manaRefund", frostArrow.BaseManaCost)
                    .Set("cooldownRefund", frostArrow.BaseCooldownTicks)
                    .Set("baseDamage", frostArrow.BaseDamage).Set("baseChill", frostArrow.BaseChill)
                    .Set("ice", frostIce).Set("ratio", frostArrow.IceRatio);
                return true;
            }

            if (skill is KachofugetsuSkillAsset kachofugetsu
                && TryGetStat(owner, PachimonDisplayStat.Fire, out var kachoFire)
                && TryGetStat(owner, PachimonDisplayStat.Aqua, out var kachoAqua)
                && TryGetStat(owner, PachimonDisplayStat.Leaf, out var kachoLeaf)
                && TryGetStat(owner, PachimonDisplayStat.Wind, out var kachoWind))
            {
                context.Set("fireDamage", Scale(kachofugetsu.BaseFireDamage, kachoFire, kachofugetsu.FireDamageRatio))
                    .Set("aquaDamage", Scale(kachofugetsu.BaseAquaDamage, kachoAqua, kachofugetsu.AquaDamageRatio))
                    .Set("leafDamage", Scale(kachofugetsu.BaseLeafDamage, kachoLeaf, kachofugetsu.LeafDamageRatio))
                    .Set("windDamage", Scale(kachofugetsu.BaseWindDamage, kachoWind, kachofugetsu.WindDamageRatio))
                    .Set("baseFireDamage", kachofugetsu.BaseFireDamage).Set("baseAquaDamage", kachofugetsu.BaseAquaDamage).Set("baseLeafDamage", kachofugetsu.BaseLeafDamage).Set("baseWindDamage", kachofugetsu.BaseWindDamage)
                    .Set("fire", kachoFire).Set("aqua", kachoAqua).Set("leaf", kachoLeaf).Set("wind", kachoWind)
                    .Set("fireRatio", kachofugetsu.FireDamageRatio).Set("aquaRatio", kachofugetsu.AquaDamageRatio).Set("leafRatio", kachofugetsu.LeafDamageRatio).Set("windRatio", kachofugetsu.WindDamageRatio);
                return true;
            }

            if (skill is DragonDefenseSkillAsset dragonDefense
                && TryGetStat(owner, PachimonDisplayStat.Dragon, out var defenseDragon))
            {
                context.Set("shield", Scale(
                        dragonDefense.BaseShieldValue,
                        defenseDragon,
                        dragonDefense.DragonShieldRatio))
                    .Set("duration", dragonDefense.DurationTicks)
                    .Set("baseShield", dragonDefense.BaseShieldValue).Set("dragon", defenseDragon)
                    .Set("shieldRatio", dragonDefense.DragonShieldRatio);
                return true;
            }

            return false;
        }

        private static int Scale(
            int baseValue,
            int stat,
            int ratio = AttributeDamageRules.ScalingRatio) =>
            SignedStatMath.FloorNonNegative(
                SignedStatMath.ScaleFromBase(baseValue, stat, ratio));

        private static int ScaleRatio(int value, int ratio) =>
            SignedStatMath.FloorNonNegative(value * ratio / 100m);

        private static int CalculateGeneratedWeatherValue(
            int baseValue,
            int attribute,
            int attributeRatio,
            int generationPower)
        {
            var attributeScaled = SignedStatMath.FloorNonNegative(
                SignedStatMath.ScaleFromBase(
                    baseValue,
                    attribute,
                    attributeRatio),
                minimum: 1);
            return SignedStatMath.FloorNonNegative(
                attributeScaled
                * SignedStatMath.AmplificationMultiplier(generationPower),
                minimum: 1);
        }

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
                            staticElectricity.BaseValue)
                        .Set("iceBaseDuration",
                            staticElectricity.BaseDurationTicks);
                    if (TryGetStat(owner, PachimonDisplayStat.Electric, out var staticElectric)
                        && TryGetStat(owner, PachimonDisplayStat.Ice, out var staticIce))
                    {
                        context.Set("paralysisValue",
                            Scale(
                                staticElectricity.BaseValue,
                                staticElectric,
                                100))
                            .Set("paralysisDuration", Math.Max(1, Scale(
                                staticElectricity.BaseDurationTicks,
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
                        var penetrationValue = decimal.Floor(
                            rageDragon * dragonRage.PenetrationRatio / 100m);
                        context.Set("penetrationValue", penetrationValue)
                            .Set("currentPenetration",
                                PenetrationMath.CalculateDiminishingPercentage(
                                    penetrationValue).ToString("0.##"));
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
