using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Battle;
using Pachimon.Items;
using Pachimon.Passives;
using Pachimon.Skills;
using UnityEditor;
using UnityEngine;

namespace Pachimon.Editor.UI
{
    public static class ElectricContentSetup
    {
        private const string MenuPath = "Tools/Pachimon/Data/Setup Electric Content 7-8";
        private const string SkillFolder = "Assets/GameData/Skill/Placeholder";
        private const string PassiveFolder = "Assets/GameData/Passive";
        private const string ItemFolder = "Assets/GameData/Item";
        private const string WeatherFolder = "Assets/GameData/Battle/Weather";
        private const string StatusFolder = "Assets/GameData/Battle/Status";
        private const string SkillCatalogPath = "Assets/GameData/Skill/SkillCatalog.asset";
        private const string PassiveCatalogPath = "Assets/GameData/Passive/PassiveCatalog.asset";
        private const string ItemCatalogPath = "Assets/GameData/Item/ItemCatalog.asset";

        [InitializeOnLoadMethod]
        private static void SetupAfterReload()
        {
            EditorApplication.delayCall += TryAutoSetup;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += TryAutoSetup;
        }

        private static void TryAutoSetup()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode && !IsConfigured())
                Setup();
        }

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            var paralysis = LoadRequired<SlowStatusAsset>(
                $"{StatusFolder}/ParalysisStatus.asset");
            var thunder = GetOrCreate<ThunderWeatherAsset>(
                $"{WeatherFolder}/ThunderWeather.asset");
            thunder.ConfigureForEditor(
                "雷",
                "Electric RatioとSpeedを増加させ、150tickごとに全体を攻撃する。",
                10, 10, 150, 3);
            var shieldStatus = GetOrCreate<ElectricShieldStatusAsset>(
                $"{StatusFolder}/ElectricShieldStatus.asset");
            shieldStatus.ConfigureForEditor(
                "エレキシールド",
                "持続中に攻撃を受けると、攻撃者へ麻痺を付与する。",
                paralysis);

            var cloud = ReplaceWith<LightningCloudSkillAsset>(
                $"{SkillFolder}/Skill_052.asset");
            cloud.ConfigureForEditor(
                52, "雷雲", 100, 300, 100,
                "Valueが300+Electricの天気「雷」を生成する。",
                300, 100, thunder);
            var shield = ReplaceWith<ElectricShieldSkillAsset>(
                $"{SkillFolder}/Skill_060.asset");
            shield.ConfigureForEditor(
                60, "エレキシールド", 100, 300, 100,
                "自身へShieldと麻痺を付与し、Shield持続中は攻撃者へ麻痺を返す。",
                200, 50, 100, 50, 100, 25, 100, 25,
                paralysis, shieldStatus);

            var thunderMan = GetOrCreate<ThunderManPassiveAsset>(
                $"{PassiveFolder}/Passive_052_ThunderMan.asset");
            thunderMan.ConfigureForEditor(
                52, "雷男", "雷が存在する間、Speedが40増加する。", 40);
            var generation = GetOrCreate<ParalysisGenerationPassiveAsset>(
                $"{PassiveFolder}/Passive_060_ParalysisGeneration.asset");
            generation.ConfigureForEditor(
                60, "しびれ発電", "麻痺Valueの50%だけElectricが増加する。", 50);

            ReplaceCatalog(LoadRequired<SkillCatalog>(SkillCatalogPath),
                new SkillAsset[] { cloud, shield });
            ReplaceCatalog(LoadRequired<PassiveCatalog>(PassiveCatalogPath),
                new PassiveAsset[] { thunderMan, generation });
            var cloudMachine = ConfigureMachine(cloud);
            var shieldMachine = ConfigureMachine(shield);
            ReplaceCatalog(LoadRequired<ItemCatalog>(ItemCatalogPath),
                new ItemAsset[] { cloudMachine, shieldMachine });
            MarkDirtyAndSave(thunder, shieldStatus, cloud, shield,
                thunderMan, generation, cloudMachine, shieldMachine);
            AssetDatabase.Refresh();
            Debug.Log("Electric Content 7-8 setup completed.");
        }

        private static bool IsConfigured()
        {
            var skills = AssetDatabase.LoadAssetAtPath<SkillCatalog>(SkillCatalogPath);
            var passives = AssetDatabase.LoadAssetAtPath<PassiveCatalog>(PassiveCatalogPath);
            var items = AssetDatabase.LoadAssetAtPath<ItemCatalog>(ItemCatalogPath);
            return skills?.Get(52) is LightningCloudSkillAsset cloud
                && cloud.ThunderDefinition != null
                && skills.Get(60) is ElectricShieldSkillAsset shield
                && shield.ShieldStatus != null
                && passives?.Get(52) is ThunderManPassiveAsset
                && passives.Get(60) is ParalysisGenerationPassiveAsset
                && IsMachine(items, 52) && IsMachine(items, 60);
        }

        private static bool IsMachine(ItemCatalog catalog, int skillId) =>
            catalog?.Get(ItemIds.GetSkillMachineItemId(skillId))
                is SkillMachineItemAsset machine
            && machine.Skill?.SkillId == skillId;

        private static SkillMachineItemAsset ConfigureMachine(SkillAsset skill)
        {
            var typeName = skill.GetType().Name;
            const string suffix = "SkillAsset";
            if (typeName.EndsWith(suffix, StringComparison.Ordinal))
                typeName = typeName.Substring(0, typeName.Length - suffix.Length);
            var itemId = ItemIds.GetSkillMachineItemId(skill.SkillId);
            var item = GetOrCreate<SkillMachineItemAsset>(
                $"{ItemFolder}/Item_{itemId}_TM_{typeName}.asset");
            item.ConfigureForEditor(itemId, $"技マシーン[{skill.DisplayName}]", null,
                $"対象の味方パチモンが「{skill.DisplayName}」を習得する。",
                ItemCategory.SkillMachine, 1000);
            item.ConfigureSkillForEditor(skill);
            return item;
        }

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object =>
            AssetDatabase.LoadAssetAtPath<T>(path)
            ?? throw new InvalidOperationException($"{typeof(T).Name} is missing at {path}.");

        private static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static T ReplaceWith<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                AssetDatabase.DeleteAsset(path);
            return GetOrCreate<T>(path);
        }

        private static Dictionary<int, T> Unique<T>(IEnumerable<T> source, Func<T, int> id)
            where T : UnityEngine.Object
        {
            var result = new Dictionary<int, T>();
            foreach (var item in source ?? Enumerable.Empty<T>())
                if (item != null && id(item) > 0) result[id(item)] = item;
            return result;
        }

        private static void ReplaceCatalog(SkillCatalog catalog, IEnumerable<SkillAsset> values)
        {
            var map = Unique(catalog.Skills, item => item.SkillId);
            foreach (var value in values) map[value.SkillId] = value;
            catalog.SetSkillsForEditor(map.Values.OrderBy(item => item.SkillId));
            EditorUtility.SetDirty(catalog);
        }

        private static void ReplaceCatalog(PassiveCatalog catalog, IEnumerable<PassiveAsset> values)
        {
            var map = Unique(catalog.Passives, item => item.PassiveId);
            foreach (var value in values) map[value.PassiveId] = value;
            catalog.SetPassivesForEditor(map.Values.OrderBy(item => item.PassiveId));
            EditorUtility.SetDirty(catalog);
        }

        private static void ReplaceCatalog(ItemCatalog catalog, IEnumerable<ItemAsset> values)
        {
            var map = Unique(catalog.Items, item => item.ItemId);
            foreach (var value in values) map[value.ItemId] = value;
            catalog.SetItemsForEditor(map.Values.OrderBy(item => item.ItemId));
            EditorUtility.SetDirty(catalog);
        }

        private static void MarkDirtyAndSave(params UnityEngine.Object[] assets)
        {
            foreach (var asset in assets) if (asset != null) EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }
    }
}
