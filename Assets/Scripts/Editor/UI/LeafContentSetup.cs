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
    public static class LeafContentSetup
    {
        private const string MenuPath =
            "Tools/Pachimon/Data/Setup Leaf Content 7-8";
        private const string SkillFolder = "Assets/GameData/Skill/Placeholder";
        private const string PassiveFolder = "Assets/GameData/Passive";
        private const string ItemFolder = "Assets/GameData/Item";
        private const string FieldFolder = "Assets/GameData/Battle/FieldEffect";
        private const string StatusFolder = "Assets/GameData/Battle/Status";
        private const string SkillCatalogPath =
            "Assets/GameData/Skill/SkillCatalog.asset";
        private const string PassiveCatalogPath =
            "Assets/GameData/Passive/PassiveCatalog.asset";
        private const string ItemCatalogPath =
            "Assets/GameData/Item/ItemCatalog.asset";

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
            var beatVineField = GetOrCreate<BeatVineFieldEffectAsset>(
                $"{FieldFolder}/BeatVineFieldEffect.asset");
            beatVineField.ConfigureForEditor(
                "ビートヴァイン",
                "100tickごとに敵先頭へ草ダメージを与える植物。",
                baseValue: 30,
                leafValueRatio: 100,
                attackIntervalTicks: 100);

            var fireVineField = GetOrCreate<FireVineFieldEffectAsset>(
                $"{FieldFolder}/FireVineFieldEffect.asset");
            fireVineField.ConfigureForEditor(
                "ファイアヴァイン",
                "味方の炎・草ダメージに反応して同じ対象を攻撃する植物。",
                baseLeafValue: 15,
                leafValueRatio: 100,
                baseFireValue: 15,
                fireValueRatio: 100);

            var leafGrowth = GetOrCreate<BurningFlowerGrowthStatusAsset>(
                $"{StatusFolder}/BurningFlowerLeafGrowth.asset");
            leafGrowth.ConfigureForEditor(
                BattleStatusId.BurningFlowerLeaf,
                "燃える花・草",
                "炎ダメージが発生するたびに草が増加する。");
            var fireGrowth = GetOrCreate<BurningFlowerGrowthStatusAsset>(
                $"{StatusFolder}/BurningFlowerFireGrowth.asset");
            fireGrowth.ConfigureForEditor(
                BattleStatusId.BurningFlowerFire,
                "燃える花・炎",
                "草ダメージが発生するたびに炎が増加する。");

            var beatVine = ReplaceWith<BeatVineSkillAsset>(
                $"{SkillFolder}/Skill_051.asset");
            beatVine.ConfigureForEditor(
                51,
                "ビートヴァイン",
                100,
                300,
                100,
                "草を参照する植物「ビートヴァイン」を生成する。",
                beatVineField);
            var fireVine = ReplaceWith<FireVineSkillAsset>(
                $"{SkillFolder}/Skill_059.asset");
            fireVine.ConfigureForEditor(
                59,
                "ファイアヴァイン",
                100,
                300,
                100,
                "草と炎を参照する植物「ファイアヴァイン」を生成する。",
                fireVineField);

            var botanicalGarden = GetOrCreate<BotanicalGardenPassiveAsset>(
                $"{PassiveFolder}/Passive_051_BotanicalGarden.asset");
            botanicalGarden.ConfigureForEditor(
                51,
                "植物園",
                "自陣の植物1つにつきDamageBonusが15増加する。",
                damageBonusPerPlant: 15);
            var burningFlower = GetOrCreate<BurningFlowerPassiveAsset>(
                $"{PassiveFolder}/Passive_059_BurningFlower.asset");
            burningFlower.ConfigureForEditor(
                59,
                "燃える花",
                "全陣営で炎ダメージが発生するたび草が5、草ダメージが発生するたび炎が5増加する。",
                statGainPerDamage: 5,
                leafGrowth,
                fireGrowth);

            ReplaceCatalogEntries(
                LoadRequired<SkillCatalog>(SkillCatalogPath),
                new SkillAsset[] { beatVine, fireVine });
            ReplaceCatalogEntries(
                LoadRequired<PassiveCatalog>(PassiveCatalogPath),
                new PassiveAsset[] { botanicalGarden, burningFlower });

            var beatMachine = ConfigureSkillMachine(beatVine);
            var fireMachine = ConfigureSkillMachine(fireVine);
            ReplaceCatalogEntries(
                LoadRequired<ItemCatalog>(ItemCatalogPath),
                new ItemAsset[] { beatMachine, fireMachine });

            MarkDirtyAndSave(
                beatVineField,
                fireVineField,
                leafGrowth,
                fireGrowth,
                beatVine,
                fireVine,
                botanicalGarden,
                burningFlower,
                beatMachine,
                fireMachine);
            AssetDatabase.Refresh();
            Debug.Log("Leaf Content 7-8 setup completed.");
        }

        private static bool IsConfigured()
        {
            var skillCatalog = AssetDatabase.LoadAssetAtPath<SkillCatalog>(
                SkillCatalogPath);
            var passiveCatalog = AssetDatabase.LoadAssetAtPath<PassiveCatalog>(
                PassiveCatalogPath);
            var itemCatalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(
                ItemCatalogPath);
            return skillCatalog?.Get(51) is BeatVineSkillAsset beat
                && beat.FieldEffect != null
                && skillCatalog.Get(59) is FireVineSkillAsset fire
                && fire.FieldEffect != null
                && passiveCatalog?.Get(51) is BotanicalGardenPassiveAsset
                && passiveCatalog.Get(59) is BurningFlowerPassiveAsset burning
                && burning.LeafGrowthStatus != null
                && burning.FireGrowthStatus != null
                && IsMachineConfigured(itemCatalog, 51)
                && IsMachineConfigured(itemCatalog, 59);
        }

        private static bool IsMachineConfigured(ItemCatalog catalog, int skillId)
        {
            var itemId = ItemIds.GetSkillMachineItemId(skillId);
            return catalog?.Get(itemId) is SkillMachineItemAsset machine
                && machine.Skill?.SkillId == skillId;
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
                5000);
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

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path)
                ?? throw new InvalidOperationException($"{typeof(T).Name} is missing.");
        }

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
            var typed = AssetDatabase.LoadAssetAtPath<T>(path);
            if (typed != null) return typed;
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                AssetDatabase.DeleteAsset(path);
            return GetOrCreate<T>(path);
        }

        private static void ReplaceCatalogEntries(
            SkillCatalog catalog,
            IEnumerable<SkillAsset> replacements)
        {
            var byId = BuildUnique(catalog.Skills, item => item.SkillId);
            foreach (var item in replacements) byId[item.SkillId] = item;
            catalog.SetSkillsForEditor(byId.Values.OrderBy(item => item.SkillId));
            EditorUtility.SetDirty(catalog);
        }

        private static void ReplaceCatalogEntries(
            PassiveCatalog catalog,
            IEnumerable<PassiveAsset> replacements)
        {
            var byId = BuildUnique(catalog.Passives, item => item.PassiveId);
            foreach (var item in replacements) byId[item.PassiveId] = item;
            catalog.SetPassivesForEditor(byId.Values.OrderBy(item => item.PassiveId));
            EditorUtility.SetDirty(catalog);
        }

        private static void ReplaceCatalogEntries(
            ItemCatalog catalog,
            IEnumerable<ItemAsset> replacements)
        {
            var byId = BuildUnique(catalog.Items, item => item.ItemId);
            foreach (var item in replacements) byId[item.ItemId] = item;
            catalog.SetItemsForEditor(byId.Values.OrderBy(item => item.ItemId));
            EditorUtility.SetDirty(catalog);
        }

        private static Dictionary<int, T> BuildUnique<T>(
            IEnumerable<T> source,
            Func<T, int> getId)
            where T : UnityEngine.Object
        {
            var result = new Dictionary<int, T>();
            foreach (var item in source ?? Enumerable.Empty<T>())
            {
                if (item == null) continue;
                var id = getId(item);
                if (id > 0) result[id] = item;
            }
            return result;
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
