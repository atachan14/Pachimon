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
    public static class IceContentSetup
    {
        private const string MenuPath = "Tools/Pachimon/Data/Setup Ice Content 7-8";
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

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            var chill = LoadRequired<SlowStatusAsset>($"{StatusFolder}/ChillStatus.asset");

            var pebble = ReplaceWith<IcePebbleSkillAsset>($"{SkillFolder}/Skill_054.asset");
            pebble.ConfigureForEditor(54, "\u6C37\u306E\u792B", 100, 300, 100,
                "\u5148\u982D\u306E\u6575\u306B\u6C37\u30C0\u30E1\u30FC\u30B8\u3068\u51B7\u6C17\u3092\u4E0E\u3048\u3001\u81EA\u8EAB\u306B\u6642\u9593\u5236Shield\u3092\u4ED8\u4E0E\u3059\u308B\u3002",
                70, 35, 70, 100, 100, chill);
            var arrow = ReplaceWith<FrostArrowSkillAsset>($"{SkillFolder}/Skill_062.asset");
            arrow.ConfigureForEditor(62, "\u30D5\u30ED\u30B9\u30C8\u30A2\u30ED\u30FC", 100, 300, 150,
                "\u73FE\u5728HP\u304C\u6700\u3082\u4F4E\u3044\u6575\u306B\u6C37\u30C0\u30E1\u30FC\u30B8\u3068\u51B7\u6C17\u3002\u6483\u7834\u6642\u306FMN\u3068CD\u3092\u9084\u5143\u3059\u308B\u3002",
                100, 30, 100, chill);

            var armor = GetOrCreate<IceArmorPassiveAsset>(
                $"{PassiveFolder}/Passive_054_IceArmor.asset");
            armor.ConfigureForEditor(54, "\u6C37\u306E\u93A7",
                "\u81EA\u8EAB\u304C\u5F97\u308BShield\u306EValue\u3068\u52B9\u679C\u6642\u9593\u3092Ice\u306B\u5FDC\u3058\u3066\u5897\u52A0\u3055\u305B\u308B\u3002", 20);
            var spread = GetOrCreate<ChillSpreadPassiveAsset>(
                $"{PassiveFolder}/Passive_062_ChillSpread.asset");
            spread.ConfigureForEditor(62, "\u51B7\u6C17\u62E1\u6563",
                "\u81EA\u8EAB\u306ESkill\u3067\u6575\u3092\u6483\u7834\u3057\u305F\u3068\u304D\u3001\u305D\u306E\u51B7\u6C17\u306E150%\u3092\u6B8B\u308A\u306E\u6575\u5168\u54E1\u306B\u4ED8\u4E0E\u3059\u308B\u3002",
                150, chill);

            ReplaceCatalog(LoadRequired<SkillCatalog>(SkillCatalogPath),
                new SkillAsset[] { pebble, arrow });
            ReplaceCatalog(LoadRequired<PassiveCatalog>(PassiveCatalogPath),
                new PassiveAsset[] { armor, spread });
            var pebbleMachine = ConfigureMachine(pebble);
            var arrowMachine = ConfigureMachine(arrow);
            ReplaceCatalog(LoadRequired<ItemCatalog>(ItemCatalogPath),
                new ItemAsset[] { pebbleMachine, arrowMachine });
            MarkDirtyAndSave(pebble, arrow, armor, spread,
                pebbleMachine, arrowMachine);
            AssetDatabase.Refresh();
            Debug.Log("Ice Content 7-8 setup completed.");
        }

        private static bool IsConfigured()
        {
            var skills = AssetDatabase.LoadAssetAtPath<SkillCatalog>(SkillCatalogPath);
            var passives = AssetDatabase.LoadAssetAtPath<PassiveCatalog>(PassiveCatalogPath);
            var items = AssetDatabase.LoadAssetAtPath<ItemCatalog>(ItemCatalogPath);
            return skills?.Get(54) is IcePebbleSkillAsset pebble
                && pebble.ChillStatus != null
                && skills.Get(62) is FrostArrowSkillAsset arrow
                && arrow.ChillStatus != null
                && passives?.Get(54) is IceArmorPassiveAsset
                && passives.Get(62) is ChillSpreadPassiveAsset spread
                && spread.ChillStatus != null
                && IsMachine(items, 54) && IsMachine(items, 62);
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
                ItemCategory.SkillMachine, 5000);
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
            var map = Unique(catalog.Skills, item => item.SkillId);
            foreach (var value in values) map[value.SkillId] = value;
            catalog.SetSkillsForEditor(map.Values.OrderBy(item => item.SkillId));
            EditorUtility.SetDirty(catalog);
        }

        private static void ReplaceCatalog(PassiveCatalog catalog,
            IEnumerable<PassiveAsset> values)
        {
            var map = Unique(catalog.Passives, item => item.PassiveId);
            foreach (var value in values) map[value.PassiveId] = value;
            catalog.SetPassivesForEditor(map.Values.OrderBy(item => item.PassiveId));
            EditorUtility.SetDirty(catalog);
        }

        private static void ReplaceCatalog(ItemCatalog catalog,
            IEnumerable<ItemAsset> values)
        {
            var map = Unique(catalog.Items, item => item.ItemId);
            foreach (var value in values) map[value.ItemId] = value;
            catalog.SetItemsForEditor(map.Values.OrderBy(item => item.ItemId));
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
