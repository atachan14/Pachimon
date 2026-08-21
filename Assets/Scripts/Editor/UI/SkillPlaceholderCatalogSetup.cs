using System.Collections.Generic;
using System.Linq;
using Pachimon.Data;
using Pachimon.Battle;
using Pachimon.Skills;
using Pachimon.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Pachimon.Editor.UI
{
    public static class SkillPlaceholderCatalogSetup
    {
        private const string MenuRoot = "Tools/Pachimon/Data/";
        private const string DataFolder = "Assets/GameData/Skill";
        private const string PlaceholderFolder = DataFolder + "/Placeholder";
        private const string SystemFolder = DataFolder + "/System";
        private const string CatalogPath = DataFolder + "/SkillCatalog.asset";
        private const string ToxinStatusPath =
            "Assets/GameData/Battle/Status/ToxinStatus.asset";
        private const string SmogFieldEffectPath =
            "Assets/GameData/Battle/FieldEffect/SmogFieldEffect.asset";
        private const string WaterVeilFieldEffectPath =
            "Assets/GameData/Battle/FieldEffect/WaterVeilFieldEffect.asset";
        private const string StunStatusPath =
            "Assets/GameData/Battle/Status/StunStatus.asset";
        private const string ParalysisStatusPath =
            "Assets/GameData/Battle/Status/ParalysisStatus.asset";
        private const string ChillStatusPath =
            "Assets/GameData/Battle/Status/ChillStatus.asset";
        private const string ChargeStatusPath =
            "Assets/GameData/Battle/Status/ChargeStatus.asset";
        private const string LaunchCeremonyStatusPath =
            "Assets/GameData/Battle/Status/LaunchCeremonyStatus.asset";
        private const string FreezeStatusPath =
            "Assets/GameData/Battle/Status/FreezeStatus.asset";
        private const string FrozenBreakStatusPath =
            "Assets/GameData/Battle/Status/FrozenBreakStatus.asset";
        private const string SunnyWeatherPath =
            "Assets/GameData/Battle/Weather/SunnyWeather.asset";
        private const string RainWeatherPath =
            "Assets/GameData/Battle/Weather/RainWeather.asset";
        private const string WindWeatherPath =
            "Assets/GameData/Battle/Weather/WindWeather.asset";
        private const string FlyingStatusPath =
            "Assets/GameData/Battle/Status/FlyingStatus.asset";
        private const string WindErosionStatusPath =
            "Assets/GameData/Battle/Status/WindErosionStatus.asset";
        private const string HealingWindStatusPath =
            "Assets/GameData/Battle/Status/HealingWindStatus.asset";
        private const string StillAirStatusPath =
            "Assets/GameData/Battle/Status/StillAirStatus.asset";
        private const string OneTwoStatusPath =
            "Assets/GameData/Battle/Status/OneTwoStatus.asset";

        private static readonly AllocationType[] AllocationTypes =
        {
            AllocationType.Fire,
            AllocationType.Aqua,
            AllocationType.Leaf,
            AllocationType.Electric,
            AllocationType.Poison,
            AllocationType.Ice,
            AllocationType.Wind,
            AllocationType.Dragon,
        };

        private const int BasicRecovery = 100;
        private const int BasicCooldown = 200;

        [InitializeOnLoadMethod]
        private static void AssignExistingCatalogAfterReload()
        {
            EditorApplication.delayCall += TryAssignExistingCatalog;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        [MenuItem(MenuRoot + "Create Skill Placeholder Catalog")]
        private static void CreateCatalog()
        {
            EnsureAssetFolder(PlaceholderFolder);
            EnsureAssetFolder(SystemFolder);

            var catalog = GetOrCreateCatalog();
            var toxinStatus = AssetDatabase.LoadAssetAtPath<ToxinStatusAsset>(
                ToxinStatusPath);
            var smogFieldEffect = AssetDatabase
                .LoadAssetAtPath<SmogFieldEffectAsset>(SmogFieldEffectPath);
            var waterVeilFieldEffect = AssetDatabase
                .LoadAssetAtPath<WaterVeilFieldEffectAsset>(
                    WaterVeilFieldEffectPath);
            var stunStatus = AssetDatabase.LoadAssetAtPath<StunStatusAsset>(
                StunStatusPath);
            var paralysisStatus = AssetDatabase.LoadAssetAtPath<SlowStatusAsset>(
                ParalysisStatusPath);
            var chillStatus = AssetDatabase.LoadAssetAtPath<SlowStatusAsset>(
                ChillStatusPath);
            var chargeStatus = AssetDatabase.LoadAssetAtPath<ChargeStatusAsset>(
                ChargeStatusPath);
            var launchCeremonyStatus = AssetDatabase
                .LoadAssetAtPath<LaunchCeremonyStatusAsset>(
                    LaunchCeremonyStatusPath);
            var freezeStatus = AssetDatabase.LoadAssetAtPath<FreezeStatusAsset>(
                FreezeStatusPath);
            var frozenBreakStatus = AssetDatabase
                .LoadAssetAtPath<FrozenBreakStatusAsset>(FrozenBreakStatusPath);
            var sunnyWeather = AssetDatabase.LoadAssetAtPath<SunnyWeatherAsset>(
                SunnyWeatherPath);
            var rainWeather = AssetDatabase.LoadAssetAtPath<RainWeatherAsset>(
                RainWeatherPath);
            var windWeather = AssetDatabase.LoadAssetAtPath<WindWeatherAsset>(
                WindWeatherPath);
            var flyingStatus = AssetDatabase.LoadAssetAtPath<FlyingStatusAsset>(
                FlyingStatusPath);
            var windErosionStatus = AssetDatabase
                .LoadAssetAtPath<WindErosionStatusAsset>(WindErosionStatusPath);
            var healingWindStatus = AssetDatabase
                .LoadAssetAtPath<HealingWindStatusAsset>(HealingWindStatusPath);
            var stillAirStatus = AssetDatabase.LoadAssetAtPath<StillAirStatusAsset>(
                StillAirStatusPath);
            var oneTwoStatus = AssetDatabase.LoadAssetAtPath<OneTwoStatusAsset>(
                OneTwoStatusPath);
            if (toxinStatus == null
                || smogFieldEffect == null
                || stunStatus == null
                || paralysisStatus == null
                || chillStatus == null
                || chargeStatus == null
                || freezeStatus == null
                || frozenBreakStatus == null
                || sunnyWeather == null
                || rainWeather == null
                || windWeather == null
                || flyingStatus == null
                || windErosionStatus == null
                || healingWindStatus == null
                || stillAirStatus == null
                || oneTwoStatus == null)
            {
                Debug.LogError(
                    "Battle Status and Field Effect Definitions are required.");
                return;
            }
            var skillsById = catalog.Skills
                .Where(skill => skill != null)
                .GroupBy(skill => skill.SkillId)
                .ToDictionary(group => group.Key, group => group.First());

            for (var skillId = SkillIdRanges.FirstMapAssignableId;
                 skillId <= SkillIdRanges.LastMapAssignableId;
                 skillId++)
            {
                var allocationType = AllocationTypes[(skillId - 1) % AllocationTypes.Length];
                var path = $"{PlaceholderFolder}/Skill_{skillId:D3}.asset";
                var skill = skillsById.TryGetValue(skillId, out var existingSkill)
                    ? existingSkill
                    : AssetDatabase.LoadAssetAtPath<SkillAsset>(path);
                if (skill == null)
                {
                    skill = CreatePlaceholderSkill(
                        path,
                        skillId,
                        GetBasicSkillName(allocationType),
                        allocationType,
                        true,
                        BasicRecovery,
                        BasicCooldown,
                        GetBasicSkillDescription(allocationType));
                }
                else if (skill is BackfireSkillAsset backfire)
                {
                    Undo.RecordObject(backfire, "Update Backfire Skill");
                    backfire.ConfigureForEditor(
                        skillId: 9,
                        displayName: "バックファイア",
                        baseRecoveryTicks: 100,
                        baseCooldownTicks: 200,
                        baseManaCost: 100,
                        description:
                            "最後尾の敵へFireダメージを与える。"
                            + "Poisonに応じた貫通を持つ。",
                        basePower: 100,
                        fireScalingPercent: 100,
                        basePenetrationPercent: 10,
                        poisonScalingPercent: 100);
                    EditorUtility.SetDirty(backfire);
                }
                else if (skill is FireArrowSkillAsset fireArrow)
                {
                    Undo.RecordObject(fireArrow, "Update Fire Arrow Skill");
                    fireArrow.ConfigureForEditor(
                        skillId: 33,
                        displayName: "ファイアアロー",
                        baseRecoveryTicks: 100,
                        baseCooldownTicks: 250,
                        baseManaCost: 100,
                        description:
                            "CurrentHPが最も低い敵へFireダメージ。"
                            + "戦闘不能にした場合はMNを消費して再発動する。",
                        basePower: 100,
                        fireScalingPercent: 100);
                    EditorUtility.SetDirty(fireArrow);
                }
                else if (skill is CombustionSkillAsset combustion)
                {
                    Undo.RecordObject(combustion, "Update Combustion Skill");
                    combustion.ConfigureForEditor(
                        skillId: 41,
                        displayName: "燃焼",
                        baseRecoveryTicks: 100,
                        baseCooldownTicks: 300,
                        baseManaCost: 40,
                        description:
                            "先頭の敵と自身へFireダメージ。"
                            + "両者が生存している間はMNを追加消費せず再発動する。",
                        basePower: 100,
                        fireScalingPercent: 100);
                    EditorUtility.SetDirty(combustion);
                }
                else if (skill is ChainBurnSkillAsset chainBurn)
                {
                    Undo.RecordObject(chainBurn, "Update Chain Burn Skill");
                    chainBurn.ConfigureForEditor(
                        skillId: 17,
                        displayName: "チェインバーン",
                        baseRecoveryTicks: 130,
                        baseCooldownTicks: 250,
                        baseManaCost: 100,
                        description:
                            "先頭から後方へ往復するFire連鎖攻撃。"
                            + "使うたびにアドチェインが0.5増加する。",
                        basePower: 80,
                        fireScalingPercent: 100,
                        baseChainCount: 1,
                        addChainGainUnits: 50);
                    EditorUtility.SetDirty(chainBurn);
                }
                else if (skill is FireBarrierSkillAsset fireBarrier)
                {
                    var fieldEffect = AssetDatabase.LoadAssetAtPath<
                        FireBarrierFieldEffectAsset>(
                        "Assets/GameData/Battle/FieldEffect/FireBarrierFieldEffect.asset");
                    Undo.RecordObject(fireBarrier, "Update Fire Barrier Skill");
                    fireBarrier.ConfigureForEditor(
                        skillId: 25,
                        displayName: "炎の障壁",
                        baseRecoveryTicks: 100,
                        baseCooldownTicks: 300,
                        baseManaCost: 100,
                        description:
                            "自陣に、味方への攻撃を肩代わりする炎の障壁を生成する。",
                        baseValue: 400,
                        fireValueRatio: 100,
                        fieldEffect);
                    EditorUtility.SetDirty(fireBarrier);
                }
                else if (skill is SunnyDaySkillAsset sunnyDay)
                {
                    Undo.RecordObject(sunnyDay, "Update Sunny Day Skill");
                    sunnyDay.ConfigureForEditor(
                        skillId: 49,
                        displayName: "温暖化",
                        baseRecoveryTicks: 100,
                        baseCooldownTicks: 300,
                        baseManaCost: 100,
                        description:
                            "Fireに応じてBattle中の気温を恒久的に上昇させる。",
                        baseValue: 400,
                        fireValueRatio: 100,
                        temperatureDefinition: sunnyWeather);
                    EditorUtility.SetDirty(sunnyDay);
                }
                else if (skill is RainDanceSkillAsset rainDance)
                {
                    Undo.RecordObject(rainDance, "Update Rain Dance Skill");
                    rainDance.ConfigureForEditor(
                        skillId: 18,
                        displayName: "あまごい",
                        baseRecoveryTicks: 100,
                        baseCooldownTicks: 300,
                        baseManaCost: 100,
                        description:
                            "Aquaに応じたValueの雨を発生させる。",
                        baseValue: 400,
                        aquaValueRatio: 100,
                        rainDefinition: rainWeather);
                    EditorUtility.SetDirty(rainDance);
                }
                else if (skill is WaterPulseSkillAsset waterPulse)
                {
                    Undo.RecordObject(waterPulse, "Update Water Pulse Skill");
                    waterPulse.ConfigureForEditor(
                        skillId: 10,
                        displayName: "水の波動",
                        baseRecoveryTicks: 150,
                        baseCooldownTicks: 300,
                        description:
                            "原則CurrentMNをすべて消費し、消費量とAquaに応じた"
                            + "Aqua Damageを先頭の敵へ与える。"
                            + "本体だけで戦闘不能にできる場合は必要MNのみ消費する。",
                        aquaDamageRatio: 100);
                    EditorUtility.SetDirty(waterPulse);
                }
                else if (skill is LaunchCeremonySkillAsset launchCeremony)
                {
                    Undo.RecordObject(
                        launchCeremony,
                        "Update Launch Ceremony Skill");
                    launchCeremony.ConfigureForEditor(
                        skillId: 26,
                        displayName: "\u9032\u6C34\u5F0F",
                        baseRecoveryTicks: 20,
                        baseCooldownTicks: 120,
                        description:
                            "\u6B21\u306ESkill\u3092\u5F37\u5316\u3057\u3001MN\u6D88\u8CBB\u3092\u8EFD\u6E1B\u3059\u308B\u3002",
                        statusDefinition: launchCeremonyStatus);
                    EditorUtility.SetDirty(launchCeremony);
                }
                else if (skill is WaterVeilSkillAsset waterVeil)
                {
                    Undo.RecordObject(waterVeil, "Update Water Veil Skill");
                    waterVeil.ConfigureForEditor(
                        skillId: 34,
                        displayName: "\u6C34\u306E\u30D9\u30FC\u30EB",
                        baseRecoveryTicks: 120,
                        baseCooldownTicks: 300,
                        baseManaCost: 350,
                        description:
                            "\u5473\u65B9\u5074\u306B\u56DE\u5FA9\u3068Aqua/Fire\u8EFD\u6E1B\u3092\u884C\u3046\u30D9\u30FC\u30EB\u3092\u751F\u6210\u3059\u308B\u3002",
                        baseFieldValue: 300,
                        aquaValueRatio: 100,
                        fieldEffect: waterVeilFieldEffect);
                    EditorUtility.SetDirty(waterVeil);
                }
                else if (skill is HeavySnowSkillAsset heavySnow)
                {
                    Undo.RecordObject(heavySnow, "Update Heavy Snow Skill");
                    heavySnow.ConfigureForEditor(
                        skillId: 30,
                        displayName: "寒冷化",
                        baseRecoveryTicks: 100,
                        baseCooldownTicks: 300,
                        baseManaCost: 100,
                        description:
                            "Iceに応じてBattle中の気温を恒久的に低下させる。",
                        baseValue: 400,
                        iceValueRatio: 100,
                        temperatureDefinition: sunnyWeather);
                    EditorUtility.SetDirty(heavySnow);
                }
                else if (skill is IceShieldSkillAsset iceShield)
                {
                    Undo.RecordObject(iceShield, "Update Ice Shield Skill");
                    iceShield.ConfigureForEditor(
                        skillId: 14,
                        displayName: "\u6C37\u306E\u76FE",
                        baseRecoveryTicks: 100,
                        baseCooldownTicks: 300,
                        baseManaCost: 100,
                        description:
                            "\u5148\u982D\u306E\u751F\u5B58\u5473\u65B9\u3078Ice\u4F9D\u5B58\u306EShield\u3092\u4ED8\u4E0E\u3059\u308B\u3002",
                        baseShieldValue: 300,
                        iceShieldRatio: 100);
                    EditorUtility.SetDirty(iceShield);
                }
                else if (skill is IceShardSkillAsset iceShard)
                {
                    Undo.RecordObject(iceShard, "Update Ice Shard Skill");
                    iceShard.ConfigureForEditor(
                        skillId: 22,
                        displayName: "\u30A2\u30A4\u30B9\u30B7\u30E3\u30FC\u30C9",
                        baseRecoveryTicks: 100,
                        baseCooldownTicks: 300,
                        baseManaCost: 150,
                        description:
                            "\u6575\u5168\u4F53\u3078Ice Damage\u3068\u51B7\u6C17\u3092\u4E0E\u3048\u308B\u3002",
                        frontBaseDamage: 100,
                        frontDamageIceRatio: 100,
                        frontBaseChill: 75,
                        frontChillIceRatio: 100,
                        otherBaseDamage: 50,
                        otherDamageIceRatio: 100,
                        otherBaseChill: 50,
                        otherChillIceRatio: 100,
                        chillStatus: chillStatus);
                    EditorUtility.SetDirty(iceShard);
                }
                else if (skill is FrozenBreakSkillAsset frozenBreak)
                {
                    Undo.RecordObject(frozenBreak, "Update Frozen Break Skill");
                    frozenBreak.ConfigureForEditor(
                        skillId: 46,
                        displayName: "フローズンブレイク",
                        highHpRecoveryTicks: 200,
                        lowHpRecoveryTicks: 1,
                        baseCooldownTicks: 300,
                        baseManaCost: 100,
                        description:
                            "HPが半分以上なら敵を攻撃・凍結し、"
                            + "半分未満なら対象外になって毎tick回復する。",
                        baseIceDamage: 100,
                        iceDamageRatio: 100,
                        baseDuration: 70,
                        durationIceRatio: 40,
                        baseHealPerTick: 1,
                        healIceRatio: 50,
                        freezeStatus: freezeStatus,
                        selfStatus: frozenBreakStatus);
                    EditorUtility.SetDirty(frozenBreak);
                }
                else if (skill is WindStormSkillAsset windStorm)
                {
                    Undo.RecordObject(windStorm, "Update Wind Storm Skill");
                    windStorm.ConfigureForEditor(
                        skillId: 47,
                        displayName: "暴風",
                        baseRecoveryTicks: 100,
                        baseCooldownTicks: 300,
                        baseManaCost: 100,
                        description:
                            "Windに応じたValueの暴風を発生させる。",
                        baseValue: 400,
                        windValueRatio: 100,
                        windDefinition: windWeather);
                    EditorUtility.SetDirty(windStorm);
                }
                else if (skill is FlyingAttackSkillAsset flyingAttack)
                {
                    Undo.RecordObject(flyingAttack, "Update Flying Attack Skill");
                    flyingAttack.ConfigureForEditor(
                        15, "フライングアタック", 100, 100, 300, 100,
                        "発生中は飛行し、発動時に敵の先頭へWind Damageを与える。",
                        120, 100, flyingStatus);
                    EditorUtility.SetDirty(flyingAttack);
                }
                else if (skill is WindErosionSkillAsset windErosion)
                {
                    Undo.RecordObject(windErosion, "Update Wind Erosion Skill");
                    windErosion.ConfigureForEditor(
                        23, "風化の風", 100, 300, 100,
                        "敵全体へWind参照の風化を与える。",
                        20, 100, windErosionStatus);
                    EditorUtility.SetDirty(windErosion);
                }
                else if (skill is HealingWindSkillAsset healingWind)
                {
                    Undo.RecordObject(healingWind, "Update Healing Wind Skill");
                    healingWind.ConfigureForEditor(
                        31, "治癒の風", 100, 300, 100,
                        "HP割合が最も低い味方を回復し、WindとSpeedを増加させる。",
                        100, 50, 50, 100, 200, healingWindStatus);
                    EditorUtility.SetDirty(healingWind);
                }
                else if (skill is SecondWindSkillAsset secondWind)
                {
                    Undo.RecordObject(secondWind, "Update Second Wind Skill");
                    secondWind.ConfigureForEditor(
                        39, "セカンドウィンド", 100, 400, 100,
                        "Wind参照のShieldを得て、200tickの間Windが0になる。",
                        200, 200, stillAirStatus);
                    EditorUtility.SetDirty(secondWind);
                }
                else if (skill is DragonJabSkillAsset dragonJab)
                {
                    Undo.RecordObject(dragonJab, "Update Dragon Jab Skill");
                    dragonJab.ConfigureForEditor(
                        16,
                        "ドラゴンジャブ",
                        100,
                        250,
                        100,
                        "敵の先頭に竜ダメージを与え、ワン・ツーを獲得する。",
                        100,
                        100,
                        30,
                        oneTwoStatus);
                    EditorUtility.SetDirty(dragonJab);
                }
                else if (skill is AquaShockSkillAsset aquaShock)
                {
                    Undo.RecordObject(aquaShock, "Update Aqua Shock Skill");
                    aquaShock.ConfigureForEditor(
                        skillId: 12,
                        displayName: "アクアショック",
                        baseRecoveryTicks: 80,
                        baseCooldownTicks: 200,
                        baseManaCost: 80,
                        description:
                            "ElectricとAquaのダメージを与え、漏電を付与する。",
                        electricBasePower: 10,
                        aquaBasePower: 10,
                        leakBaseValue: 10);
                    EditorUtility.SetDirty(aquaShock);
                }
                else if (skill is ElectricExplosionSkillAsset electricExplosion)
                {
                    Undo.RecordObject(
                        electricExplosion,
                        "Update Electric Explosion Skill");
                    electricExplosion.ConfigureForEditor(
                        skillId: 20,
                        displayName: "電気爆発",
                        baseRecoveryTicks: 100,
                        baseCooldownTicks: 250,
                        baseManaCost: 130,
                        description:
                            "ElectricとFireを参照するElectricダメージ。"
                            + "Fireに応じた貫通を持つ。",
                        basePower: 50,
                        electricScalingPercent: 100,
                        fireScalingPercent: 100,
                        penetrationPercentAtFire100: 20);
                    EditorUtility.SetDirty(electricExplosion);
                }
                else if (skill is ElectricQuickAttackSkillAsset quickAttack)
                {
                    Undo.RecordObject(
                        quickAttack,
                        "Update Electric Quick Attack Skill");
                    quickAttack.ConfigureForEditor(
                        skillId: 28,
                        displayName: "電光石火",
                        baseRecoveryTicks: 60,
                        baseCooldownTicks: 100,
                        baseManaCost: 60,
                        description:
                            "ElectricとFireの複合攻撃。"
                            + "Windに応じて硬直とCDを軽減する。",
                        electricBasePower: 25,
                        fireBasePower: 10,
                        windTimingPercent: 100);
                    EditorUtility.SetDirty(quickAttack);
                }
                else if (skill is ElectromagneticCannonSkillAsset cannon)
                {
                    Undo.RecordObject(
                        cannon,
                        "Update Electromagnetic Cannon Skill");
                    cannon.ConfigureForEditor(
                        skillId: 44,
                        displayName: "電磁砲",
                        baseStartupTicks: 100,
                        baseRecoveryTicks: 100,
                        baseCooldownTicks: 500,
                        baseManaCost: 500,
                        description:
                            "300tick後にElectricダメージを与え、"
                            + "超過分を次の先頭へ引き継ぐ。",
                        basePower: 400);
                    EditorUtility.SetDirty(cannon);
                }
                else if (skill is ChargeSkillAsset charge)
                {
                    Undo.RecordObject(charge, "Update Charge Skill");
                    charge.ConfigureForEditor(
                        skillId: 36,
                        displayName: "充電",
                        baseStartupTicks: 100,
                        baseRecoveryTicks: 0,
                        baseCooldownTicks: 500,
                        baseManaCost: 400,
                        description:
                            "発生開始時のElectricを保存して充電中になり、"
                            + "発動時に同じValueの充電完了になる。",
                        chargeStatus: chargeStatus);
                    EditorUtility.SetDirty(charge);
                }
                else if (skill is SmogSkillAsset smog)
                {
                    Undo.RecordObject(smog, "Update Smog Skill");
                    smog.ConfigureForEditor(
                        skillId: 21,
                        displayName: "スモッグ",
                        baseRecoveryTicks: 100,
                        baseCooldownTicks: 300,
                        baseManaCost: 100,
                        description:
                            "敵陣に毒素を付与し続けるスモッグを生成する。",
                        baseFieldValue: 300,
                        poisonScalingPercent: 100,
                        fieldEffect: smogFieldEffect);
                    EditorUtility.SetDirty(smog);
                }
                else if (skill is NeurotoxinSkillAsset neurotoxin)
                {
                    Undo.RecordObject(neurotoxin, "Update Neurotoxin Skill");
                    neurotoxin.ConfigureForEditor(
                        skillId: 13,
                        displayName: "神経毒",
                        baseRecoveryTicks: 100,
                        baseCooldownTicks: 300,
                        baseManaCost: 100,
                        description:
                            "最後尾の敵へPoisonとElectricに応じたStunと、"
                            + "Poisonに応じた毒素を付与する。",
                        basePoisonStunTicks: 50,
                        poisonStunScalingPercent: 100,
                        baseElectricStunTicks: 50,
                        electricStunScalingPercent: 100,
                        baseToxinValue: 100,
                        toxinScalingPercent: 100,
                        toxinStatus: toxinStatus,
                        stunStatus: stunStatus);
                    EditorUtility.SetDirty(neurotoxin);
                }
                else if (skill is ToxinTransferSkillAsset toxinTransfer)
                {
                    Undo.RecordObject(toxinTransfer, "Update Toxin Transfer Skill");
                    toxinTransfer.ConfigureForEditor(
                        skillId: 29,
                        displayName: "毒渡し",
                        baseRecoveryTicks: 100,
                        baseCooldownTicks: 300,
                        baseManaCost: 100,
                        description:
                            "最も毒素が多い敵から50%を取り除き、"
                            + "別の最少対象へ除去量の200%を付与する。",
                        removalPercent: 50,
                        applicationPercent: 200);
                    EditorUtility.SetDirty(toxinTransfer);
                }
                else if (skill is ToxinExplosionSkillAsset toxinExplosion)
                {
                    Undo.RecordObject(toxinExplosion, "Update Toxin Explosion Skill");
                    toxinExplosion.ConfigureForEditor(
                        skillId: 37,
                        displayName: "毒爆破",
                        baseRecoveryTicks: 100,
                        baseCooldownTicks: 400,
                        baseManaCost: 200,
                        description:
                            "最も多い敵の毒素を全消費し、PoisonとFireを"
                            + "加えたPoisonダメージを敵全体へ与える。",
                        toxinConversionPercent: 100,
                        basePoisonPower: 50,
                        poisonScalingPercent: 100,
                        baseFirePower: 50,
                        fireScalingPercent: 100);
                    EditorUtility.SetDirty(toxinExplosion);
                }
                else if (skill is PoisonShieldSkillAsset poisonShield)
                {
                    Undo.RecordObject(poisonShield, "Update Poison Shield Skill");
                    poisonShield.ConfigureForEditor(
                        skillId: 45,
                        displayName: "ポイズンシールド",
                        baseRecoveryTicks: 100,
                        baseCooldownTicks: 300,
                        baseManaCost: 50,
                        description:
                            "自身へ80tickのPoison依存Shieldを付与し、"
                            + "自身の毒素をPoison依存の割合で取り除く。",
                        durationTicks: 80,
                        baseShieldValue: 100,
                        shieldPoisonScalingPercent: 100,
                        baseToxinReductionPercent: 30,
                        reductionPoisonScalingPercent: 100);
                    EditorUtility.SetDirty(poisonShield);
                }
                else
                {
                    Undo.RecordObject(skill, "Update Basic Skill Placeholder");
                    skill.ConfigureForEditor(
                        skillId,
                        GetBasicSkillName(allocationType),
                        allocationType,
                        true,
                        BasicRecovery,
                        BasicCooldown,
                        GetBasicSkillDescription(allocationType));
                    EditorUtility.SetDirty(skill);
                }

                if (skill is PlaceholderSkillAsset placeholder)
                {
                    placeholder.ConfigureBaseDamageForEditor(200);
                    placeholder.ConfigureStatusForEditor(
                        allocationType == AllocationType.Poison ? 100 : 0,
                        100,
                        allocationType == AllocationType.Poison
                            ? toxinStatus
                            : null,
                        allocationType == AllocationType.Electric
                            ? paralysisStatus
                            : null,
                        allocationType == AllocationType.Ice
                            ? chillStatus
                            : null);
                    EditorUtility.SetDirty(placeholder);
                }

                skillsById[skillId] = skill;
            }

            var strugglePath = $"{SystemFolder}/Skill_{SkillIdRanges.StruggleId}_Struggle.asset";
            var struggle = skillsById.TryGetValue(SkillIdRanges.StruggleId, out var existingStruggle)
                ? existingStruggle
                : AssetDatabase.LoadAssetAtPath<SkillAsset>(strugglePath);
            if (struggle == null)
            {
                struggle = CreatePlaceholderSkill(
                    strugglePath,
                    SkillIdRanges.StruggleId,
                    "わるあがき",
                    AllocationType.Unassigned,
                    false,
                    100,
                    0,
                    "System Skill used when no regular Skill is available.");
            }
            else
            {
                Undo.RecordObject(struggle, "Update Struggle Skill");
                struggle.ConfigureForEditor(
                    SkillIdRanges.StruggleId,
                    "わるあがき",
                    AllocationType.Unassigned,
                    false,
                    100,
                    0,
                    "System Skill used when no regular Skill is available.");
                EditorUtility.SetDirty(struggle);
            }

            skillsById[SkillIdRanges.StruggleId] = struggle;

            catalog.SetSkillsForEditor(skillsById.Values.OrderBy(skill => skill.SkillId));
            EditorUtility.SetDirty(catalog);
            AssignCatalogToSceneInstaller(catalog);
            AssetDatabase.SaveAssets();
            ValidateCatalog(catalog);
            Selection.activeObject = catalog;
        }

        [MenuItem(MenuRoot + "Validate Skill Catalog")]
        private static void ValidateCatalogFromMenu()
        {
            ValidateCatalog(AssetDatabase.LoadAssetAtPath<SkillCatalog>(CatalogPath));
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                TryAssignExistingCatalog();
            }
        }

        private static void TryAssignExistingCatalog()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var catalog = AssetDatabase.LoadAssetAtPath<SkillCatalog>(CatalogPath);
            if (catalog != null)
            {
                AssignCatalogToSceneInstaller(catalog);
            }
        }

        private static SkillCatalog GetOrCreateCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<SkillCatalog>(CatalogPath);
            if (catalog != null)
            {
                return catalog;
            }

            catalog = ScriptableObject.CreateInstance<SkillCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            return catalog;
        }

        private static SkillAsset CreatePlaceholderSkill(
            string path,
            int skillId,
            string displayName,
            AllocationType allocationType,
            bool isMapAssignable,
            int baseRecoveryTicks,
            int baseCooldownTicks,
            string description)
        {
            var skill = ScriptableObject.CreateInstance<PlaceholderSkillAsset>();
            skill.ConfigureForEditor(
                skillId,
                displayName,
                allocationType,
                isMapAssignable,
                baseRecoveryTicks,
                baseCooldownTicks,
                description);
            AssetDatabase.CreateAsset(skill, path);
            return skill;
        }

        private static string GetBasicSkillName(AllocationType allocationType)
        {
            return allocationType switch
            {
                AllocationType.Fire => "ひのこ",
                AllocationType.Aqua => "みずでっぽう",
                AllocationType.Leaf => "はっぱスライサー",
                AllocationType.Electric => "ビリビリショック",
                AllocationType.Poison => "どくばり",
                AllocationType.Wind => "かぜでっぽう",
                AllocationType.Ice => "冷たい手",
                AllocationType.Dragon => "ドラゴンストレート",
                _ => throw new System.ArgumentOutOfRangeException(nameof(allocationType)),
            };
        }

        private static string GetBasicSkillDescription(
            AllocationType allocationType)
        {
            return allocationType switch
            {
                AllocationType.Fire =>
                    "\u6575\u306E\u5148\u982D\u306B\u708E\u30C0\u30E1\u30FC\u30B8\u3092\u4E0E\u3048\u308B\u3002",
                AllocationType.Aqua =>
                    "\u6575\u306E\u5148\u982D\u306B\u6C34\u30C0\u30E1\u30FC\u30B8\u3092\u4E0E\u3048\u308B\u3002",
                AllocationType.Leaf =>
                    "\u6575\u306E\u5148\u982D\u306B\u8349\u30C0\u30E1\u30FC\u30B8\u3092\u4E0E\u3048\u308B\u3002",
                AllocationType.Electric =>
                    "\u6575\u306E\u5148\u982D\u306B\u96FB\u6C17\u30C0\u30E1\u30FC\u30B8\u3092\u4E0E\u3048\u3001\u9EBB\u75FA\u3092\u4ED8\u4E0E\u3059\u308B\u3002",
                AllocationType.Poison =>
                    "\u6575\u306E\u5148\u982D\u306B\u6BD2\u30C0\u30E1\u30FC\u30B8\u3092\u4E0E\u3048\u3001\u6BD2\u7D20\u3092\u4ED8\u4E0E\u3059\u308B\u3002",
                AllocationType.Ice =>
                    "\u6575\u306E\u5148\u982D\u306B\u6C37\u30C0\u30E1\u30FC\u30B8\u3092\u4E0E\u3048\u3001\u51B7\u6C17\u3092\u4ED8\u4E0E\u3059\u308B\u3002",
                AllocationType.Wind =>
                    "\u6575\u306E\u5148\u982D\u306B\u98A8\u30C0\u30E1\u30FC\u30B8\u3092\u4E0E\u3048\u308B\u3002",
                AllocationType.Dragon =>
                    "\u6575\u306E\u5148\u982D\u306B\u7ADC\u30C0\u30E1\u30FC\u30B8\u3092\u4E0E\u3048\u308B\u3002",
                _ => throw new System.ArgumentOutOfRangeException(
                    nameof(allocationType)),
            };
        }

        private static void ValidateCatalog(SkillCatalog catalog)
        {
            if (catalog == null)
            {
                Debug.LogError("SkillCatalog is missing. Create the placeholder catalog first.");
                return;
            }

            var errors = catalog.ValidateContent();
            if (errors.Count == 0)
            {
                Debug.Log($"SkillCatalog is valid: {catalog.Skills.Count} Skills.", catalog);
                return;
            }

            Debug.LogError("SkillCatalog validation failed:\n" + string.Join("\n", errors), catalog);
        }

        private static void AssignCatalogToSceneInstaller(SkillCatalog catalog)
        {
            var installer = Object.FindAnyObjectByType<GameSceneInstaller>(FindObjectsInactive.Include);
            if (installer == null)
            {
                Debug.LogWarning("GameSceneInstaller was not found. Assign SkillCatalog with GameScene open.");
                return;
            }

            Undo.RecordObject(installer, "Assign Skill Catalog");
            if (!installer.ConfigureSkillCatalog(catalog))
            {
                return;
            }

            EditorUtility.SetDirty(installer);
            EditorSceneManager.MarkSceneDirty(installer.gameObject.scene);
            Debug.Log("SkillCatalog assigned to GameSceneInstaller.", installer);
        }

        private static void EnsureAssetFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var next = $"{current}/{parts[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }

                current = next;
            }
        }
    }
}
