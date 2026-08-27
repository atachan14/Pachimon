using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Battle;
using Pachimon.Items;
using Pachimon.Passives;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;
using UnityEditor;
using UnityEngine;

namespace Pachimon.Editor.UI
{
    public static class AquaContentSetup
    {
        private const string MenuPath =
            "Tools/Pachimon/Data/Setup Aqua Content 6-8";
        private const string SkillFolder = "Assets/GameData/Skill/Placeholder";
        private const string PassiveFolder = "Assets/GameData/Passive";
        private const string ItemFolder = "Assets/GameData/Item";
        private const string SkillCatalogPath =
            "Assets/GameData/Skill/SkillCatalog.asset";
        private const string PassiveCatalogPath =
            "Assets/GameData/Passive/PassiveCatalog.asset";
        private const string ItemCatalogPath =
            "Assets/GameData/Item/ItemCatalog.asset";
        private const string SlowStatusPath =
            "Assets/GameData/Battle/Status/SlowStatus.asset";

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
            if (EditorApplication.isPlayingOrWillChangePlaymode || IsConfigured())
                return;
            Setup();
        }

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            var slowStatus = AssetDatabase.LoadAssetAtPath<SlowStatusAsset>(
                SlowStatusPath)
                ?? throw new InvalidOperationException("Slow Status is missing.");

            var waterCutter = ReplaceWith<WaterCutterSkillAsset>(
                $"{SkillFolder}/Skill_042.asset");
            waterCutter.ConfigureForEditor(
                42, "ウォーターカッター", 100, 300, 100,
                "先頭の敵に、Windに応じた貫通を持つAqua Damageを与える。",
                100, 100, 25);

            var muddyWater = ReplaceWith<MuddyWaterSkillAsset>(
                $"{SkillFolder}/Skill_050.asset");
            muddyWater.ConfigureForEditor(
                50, "泥水", 100, 300, 100,
                "先頭の敵にAqua Damageと、Poisonに応じたSlowを与える。",
                100, 100, 100, 100, slowStatus);

            var waterSpout = ReplaceWith<WaterSpoutSkillAsset>(
                $"{SkillFolder}/Skill_058.asset");
            waterSpout.ConfigureForEditor(
                58, "しおふき", 120, 350, 120,
                "先頭の敵に、CurrentHPが高いほど増加するAqua Damageを与える。",
                100, 100, 2000);

            var waterCutting = GetOrCreate<WaterCuttingPassiveAsset>(
                $"{PassiveFolder}/Passive_042_WaterCutting.asset");
            waterCutting.ConfigureForEditor(
                42,
                "水切り",
                "自身のSkillで敵を戦闘不能にしたとき、硬直せず続けてTurnを行う。");

            var filteredWater = GetOrCreate<DerivedAdditivePassiveAsset>(
                $"{PassiveFolder}/Passive_050_FilteredWater.asset");
            filteredWater.ConfigureForEditor(
                50,
                "ろ過水",
                "AquaがPoisonの30%増加する。",
                PachimonStatType.Aqua,
                PachimonStatType.Poison,
                30f,
                0);

            var whale = GetOrCreate<DerivedAdditivePassiveAsset>(
                $"{PassiveFolder}/Passive_058_Whale.asset");
            whale.ConfigureForEditor(
                58,
                "クジラ",
                "AquaがMaxHPの1.5%増加する。",
                PachimonStatType.Aqua,
                PachimonStatType.MaxHp,
                1.5f,
                0);

            MarkDirtyAndSave(
                waterCutter,
                muddyWater,
                waterSpout,
                waterCutting,
                filteredWater,
                whale);

            ReplaceCatalogEntries(
                AssetDatabase.LoadAssetAtPath<SkillCatalog>(SkillCatalogPath),
                new SkillAsset[] { waterCutter, muddyWater, waterSpout });
            ReplaceCatalogEntries(
                AssetDatabase.LoadAssetAtPath<PassiveCatalog>(PassiveCatalogPath),
                new PassiveAsset[] { waterCutting, filteredWater, whale });

            var cutterMachine = ConfigureSkillMachine(waterCutter);
            var muddyMachine = ConfigureSkillMachine(muddyWater);
            var spoutMachine = ConfigureSkillMachine(waterSpout);
            ReplaceCatalogEntries(
                AssetDatabase.LoadAssetAtPath<ItemCatalog>(ItemCatalogPath),
                new ItemAsset[]
                {
                    cutterMachine,
                    muddyMachine,
                    spoutMachine,
                });
            MarkDirtyAndSave(cutterMachine, muddyMachine, spoutMachine);
            AssetDatabase.Refresh();
            Debug.Log("Aqua Content 6-8 setup completed.");
        }

        private static bool IsConfigured()
        {
            return IsSkillConfigured<WaterCutterSkillAsset>(42)
                && IsSkillConfigured<MuddyWaterSkillAsset>(50)
                && IsSkillConfigured<WaterSpoutSkillAsset>(58)
                && IsPassiveConfigured<WaterCuttingPassiveAsset>(42)
                && IsPassiveConfigured<DerivedAdditivePassiveAsset>(50)
                && IsPassiveConfigured<DerivedAdditivePassiveAsset>(58)
                && IsMachineConfigured(42)
                && IsMachineConfigured(50)
                && IsMachineConfigured(58);
        }

        private static bool IsSkillConfigured<T>(int skillId)
            where T : SkillAsset
        {
            var skill = AssetDatabase.LoadAssetAtPath<T>(
                $"{SkillFolder}/Skill_{skillId:000}.asset");
            return skill != null
                && skill.SkillId == skillId
                && !string.IsNullOrWhiteSpace(skill.DisplayName);
        }

        private static bool IsPassiveConfigured<T>(int passiveId)
            where T : PassiveAsset
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PassiveCatalog>(
                PassiveCatalogPath);
            return catalog?.Get(passiveId) is T;
        }

        private static bool IsMachineConfigured(int skillId)
        {
            var itemId = ItemIds.GetSkillMachineItemId(skillId);
            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(
                ItemCatalogPath);
            return catalog?.Get(itemId) is SkillMachineItemAsset machine
                && machine.Skill?.SkillId == skillId
                && AssetDatabase.GetAssetPath(machine)
                    == GetSkillMachinePath(machine.Skill);
        }

        private static T GetOrCreate<T>(string path)
            where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static T ReplaceWith<T>(string path)
            where T : ScriptableObject
        {
            var typed = AssetDatabase.LoadAssetAtPath<T>(path);
            if (typed != null) return typed;
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                AssetDatabase.DeleteAsset(path);
            return GetOrCreate<T>(path);
        }

        private static SkillMachineItemAsset ConfigureSkillMachine(
            SkillAsset skill)
        {
            var item = GetOrCreate<SkillMachineItemAsset>(
                GetSkillMachinePath(skill));
            item.ConfigureForEditor(
                ItemIds.GetSkillMachineItemId(skill.SkillId),
                $"技マシーン[{skill.DisplayName}]",
                null,
                $"対象の味方パチモンが「{skill.DisplayName}」を習得する。",
                ItemCategory.SkillMachine,
                1000);
            item.ConfigureSkillForEditor(skill);
            return item;
        }

        private static string GetSkillMachinePath(SkillAsset skill)
        {
            const string suffix = "SkillAsset";
            var englishName = skill.GetType().Name;
            if (englishName.EndsWith(suffix, StringComparison.Ordinal))
            {
                englishName = englishName.Substring(
                    0,
                    englishName.Length - suffix.Length);
            }
            return $"{ItemFolder}/Item_{ItemIds.GetSkillMachineItemId(skill.SkillId)}"
                + $"_TM_{englishName}.asset";
        }

        private static void ReplaceCatalogEntries(
            SkillCatalog catalog,
            IEnumerable<SkillAsset> replacements)
        {
            if (catalog == null) throw new InvalidOperationException("SkillCatalog is missing.");
            var byId = BuildUniqueCatalog(catalog.Skills, item => item.SkillId);
            foreach (var replacement in replacements)
                byId[replacement.SkillId] = replacement;
            catalog.SetSkillsForEditor(byId.Values.OrderBy(item => item.SkillId));
            EditorUtility.SetDirty(catalog);
        }

        private static void ReplaceCatalogEntries(
            PassiveCatalog catalog,
            IEnumerable<PassiveAsset> replacements)
        {
            if (catalog == null) throw new InvalidOperationException("PassiveCatalog is missing.");
            var byId = BuildUniqueCatalog(catalog.Passives, item => item.PassiveId);
            foreach (var replacement in replacements)
                byId[replacement.PassiveId] = replacement;
            catalog.SetPassivesForEditor(byId.Values.OrderBy(item => item.PassiveId));
            EditorUtility.SetDirty(catalog);
        }

        private static void ReplaceCatalogEntries(
            ItemCatalog catalog,
            IEnumerable<ItemAsset> replacements)
        {
            if (catalog == null) throw new InvalidOperationException("ItemCatalog is missing.");
            var byId = BuildUniqueCatalog(catalog.Items, item => item.ItemId);
            foreach (var replacement in replacements)
                byId[replacement.ItemId] = replacement;
            catalog.SetItemsForEditor(byId.Values.OrderBy(item => item.ItemId));
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        private static Dictionary<int, T> BuildUniqueCatalog<T>(
            IEnumerable<T> entries,
            Func<T, int> getId)
            where T : UnityEngine.Object
        {
            var byId = new Dictionary<int, T>();
            foreach (var entry in entries ?? Enumerable.Empty<T>())
            {
                if (entry == null) continue;
                var id = getId(entry);
                if (id <= 0 || byId.ContainsKey(id)) continue;
                byId.Add(id, entry);
            }
            return byId;
        }

        private static void MarkDirtyAndSave(params UnityEngine.Object[] assets)
        {
            foreach (var asset in assets)
            {
                if (asset != null) EditorUtility.SetDirty(asset);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
