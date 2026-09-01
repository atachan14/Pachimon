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
    public static class WindContentSetup
    {
        private const string SkillFolder = "Assets/GameData/Skill/Placeholder";
        private const string PassiveFolder = "Assets/GameData/Passive";
        private const string ItemFolder = "Assets/GameData/Item";
        private const string StatusFolder = "Assets/GameData/Battle/Status";
        private const string SkillCatalogPath = "Assets/GameData/Skill/SkillCatalog.asset";
        private const string PassiveCatalogPath = "Assets/GameData/Passive/PassiveCatalog.asset";
        private const string ItemCatalogPath = "Assets/GameData/Item/ItemCatalog.asset";

        [InitializeOnLoadMethod]
        private static void SetupAfterReload() =>
            EditorApplication.delayCall += TryAutoSetup;

        private static void TryAutoSetup()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode && !IsConfigured())
                Setup();
        }

        [MenuItem("Tools/Pachimon/Data/Setup Wind Content 7-8")]
        public static void Setup()
        {
            var erosion = LoadRequired<WindErosionStatusAsset>(
                $"{StatusFolder}/WindErosionStatus.asset");
            var riderGrowth = GetOrCreate<WindRiderGrowthStatusAsset>(
                $"{StatusFolder}/WindRiderGrowthStatus.asset");
            riderGrowth.ConfigureForEditor("\u98A8\u4E57\u308A",
                "\u98A8\u30C0\u30E1\u30FC\u30B8\u3092\u4E0E\u3048\u308B\u305F\u3073Speed\u304C\u5897\u52A0\u3059\u308B\u3002");
            var magicianGrowth = GetOrCreate<WindMagicianGrowthStatusAsset>(
                $"{StatusFolder}/WindMagicianGrowthStatus.asset");
            magicianGrowth.ConfigureForEditor("\u98A8\u306E\u9B54\u8853\u5E2B",
                "\u98A8\u4EE5\u5916\u306E\u5C5E\u6027\u30C0\u30E1\u30FC\u30B8\u3092\u4E0E\u3048\u308B\u305F\u3073Wind\u304C\u5897\u52A0\u3059\u308B\u3002");

            var dance = ReplaceWith<CuttingDanceSkillAsset>(
                $"{SkillFolder}/Skill_055.asset");
            dance.ConfigureForEditor(55, "\u304D\u308A\u304D\u308A\u821E\u3044", 100, 300, 100,
                "連鎖しながら風ダメージと風化を与え、きりきり舞いの追加連鎖数を得る。",
                100, 100, 20, 100, 2, 1, erosion);
            var kachofugetsu = ReplaceWith<KachofugetsuSkillAsset>(
                $"{SkillFolder}/Skill_063.asset");
            kachofugetsu.ConfigureForEditor(63, "\u82B1\u9CE5\u98A8\u6708", 100, 300, 150,
                "\u5148\u982D\u306E\u6575\u306BFire\u30FBAqua\u30FBLeaf\u30FBWind\u306E4\u5C5E\u6027\u30C0\u30E1\u30FC\u30B8\u3092\u4E0E\u3048\u308B\u3002",
                76, 100, 76, 100, 76, 100, 76, 100);

            var rider = GetOrCreate<WindRiderPassiveAsset>(
                $"{PassiveFolder}/Passive_055_WindRider.asset");
            rider.ConfigureForEditor(55, "\u98A8\u4E57\u308A",
                "\u98A8\u30C0\u30E1\u30FC\u30B8\u3092\u4E0E\u3048\u308B\u305F\u3073Speed\u304C\u5897\u52A0\u3059\u308B\u3002",
                20, riderGrowth);
            var magician = GetOrCreate<WindMagicianPassiveAsset>(
                $"{PassiveFolder}/Passive_063_WindMagician.asset");
            magician.ConfigureForEditor(63, "\u98A8\u306E\u9B54\u8853\u5E2B",
                "\u98A8\u4EE5\u5916\u306E\u5C5E\u6027\u30C0\u30E1\u30FC\u30B8\u3092\u4E0E\u3048\u308B\u305F\u3073Wind\u304C10\u5897\u52A0\u3059\u308B\u3002",
                10, magicianGrowth);

            ReplaceCatalog(LoadRequired<SkillCatalog>(SkillCatalogPath),
                new SkillAsset[] { dance, kachofugetsu });
            ReplaceCatalog(LoadRequired<PassiveCatalog>(PassiveCatalogPath),
                new PassiveAsset[] { rider, magician });
            var danceMachine = ConfigureMachine(dance);
            var kachofugetsuMachine = ConfigureMachine(kachofugetsu);
            ReplaceCatalog(LoadRequired<ItemCatalog>(ItemCatalogPath),
                new ItemAsset[] { danceMachine, kachofugetsuMachine });
            MarkDirtyAndSave(riderGrowth, magicianGrowth, dance, kachofugetsu,
                rider, magician, danceMachine, kachofugetsuMachine);
            AssetDatabase.Refresh();
            Debug.Log("Wind Content 7-8 setup completed.");
        }

        private static bool IsConfigured()
        {
            var skills = AssetDatabase.LoadAssetAtPath<SkillCatalog>(SkillCatalogPath);
            var passives = AssetDatabase.LoadAssetAtPath<PassiveCatalog>(PassiveCatalogPath);
            var items = AssetDatabase.LoadAssetAtPath<ItemCatalog>(ItemCatalogPath);
            return skills?.Get(55) is CuttingDanceSkillAsset dance
                && dance.ErosionStatus != null
                && skills.Get(63) is KachofugetsuSkillAsset
                && passives?.Get(55) is WindRiderPassiveAsset rider
                && rider.GrowthStatus != null
                && passives.Get(63) is WindMagicianPassiveAsset magician
                && magician.GrowthStatus != null
                && IsMachine(items, 55) && IsMachine(items, 63);
        }

        private static bool IsMachine(ItemCatalog catalog, int skillId) =>
            catalog?.Get(ItemIds.GetSkillMachineItemId(skillId))
                is SkillMachineItemAsset machine
            && machine.Skill?.SkillId == skillId;

        private static SkillMachineItemAsset ConfigureMachine(SkillAsset skill)
        {
            var typeName = skill.GetType().Name.Replace("SkillAsset", string.Empty);
            var itemId = ItemIds.GetSkillMachineItemId(skill.SkillId);
            var item = GetOrCreate<SkillMachineItemAsset>(
                $"{ItemFolder}/Item_{itemId}_TM_{typeName}.asset");
            item.ConfigureForEditor(itemId,
                $"\u6280\u30DE\u30B7\u30FC\u30F3[{skill.DisplayName}]", null,
                $"\u5BFE\u8C61\u306E\u5473\u65B9\u30D1\u30C1\u30E2\u30F3\u304C[{skill.DisplayName}]\u3092\u7FD2\u5F97\u3059\u308B\u3002",
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

        private static Dictionary<int, T> Unique<T>(IEnumerable<T> source,
            Func<T, int> id) where T : UnityEngine.Object
        {
            var result = new Dictionary<int, T>();
            foreach (var item in source ?? Enumerable.Empty<T>())
                if (item != null && id(item) > 0) result[id(item)] = item;
            return result;
        }

        private static void ReplaceCatalog(SkillCatalog catalog,
            IEnumerable<SkillAsset> values)
        {
            var map = Unique(catalog.Skills, value => value.SkillId);
            foreach (var value in values) map[value.SkillId] = value;
            catalog.SetSkillsForEditor(map.Values.OrderBy(value => value.SkillId));
            EditorUtility.SetDirty(catalog);
        }

        private static void ReplaceCatalog(PassiveCatalog catalog,
            IEnumerable<PassiveAsset> values)
        {
            var map = Unique(catalog.Passives, value => value.PassiveId);
            foreach (var value in values) map[value.PassiveId] = value;
            catalog.SetPassivesForEditor(map.Values.OrderBy(value => value.PassiveId));
            EditorUtility.SetDirty(catalog);
        }

        private static void ReplaceCatalog(ItemCatalog catalog,
            IEnumerable<ItemAsset> values)
        {
            var map = Unique(catalog.Items, value => value.ItemId);
            foreach (var value in values) map[value.ItemId] = value;
            catalog.SetItemsForEditor(map.Values.OrderBy(value => value.ItemId));
            EditorUtility.SetDirty(catalog);
        }

        private static void MarkDirtyAndSave(params UnityEngine.Object[] assets)
        {
            foreach (var asset in assets)
                if (asset != null) EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }
    }
}
