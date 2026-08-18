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
    public static class FireContentSetup
    {
        private const string MenuPath =
            "Tools/Pachimon/Data/Setup Fire Content 8";
        private const string SkillPath =
            "Assets/GameData/Skill/Placeholder/Skill_057.asset";
        private const string PassivePath =
            "Assets/GameData/Passive/Passive_057_WeaklingBully.asset";
        private const string WeaknessStatusPath =
            "Assets/GameData/Battle/Status/WeaknessStatus.asset";
        private const string SpeedStatusPath =
            "Assets/GameData/Battle/Status/WeaklingBullySpeedStatus.asset";
        private const string SkillCatalogPath =
            "Assets/GameData/Skill/SkillCatalog.asset";
        private const string PassiveCatalogPath =
            "Assets/GameData/Passive/PassiveCatalog.asset";
        private const string ItemCatalogPath =
            "Assets/GameData/Item/ItemCatalog.asset";
        private const string MachinePath =
            "Assets/GameData/Item/Item_10057_TM_Evaporation.asset";

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
            var weakness = GetOrCreate<WeaknessStatusAsset>(WeaknessStatusPath);
            weakness.ConfigureForEditor(
                "弱点",
                "次に受けるAttribute DamageがValue%増加する。");
            var speed = GetOrCreate<WeaklingBullySpeedStatusAsset>(
                SpeedStatusPath);
            speed.ConfigureForEditor(
                "弱いものイジメ",
                "Speedが増加する。");

            var skill = ReplaceWith<EvaporationSkillAsset>(SkillPath);
            skill.ConfigureForEditor(
                57,
                "蒸発",
                120,
                300,
                120,
                "先頭の敵にFireとAquaを参照するFire Damageと弱点を与える。",
                70,
                100,
                70,
                100,
                20,
                100,
                20,
                100,
                10,
                100,
                10,
                100,
                weakness);

            var passive = GetOrCreate<WeaklingBullyPassiveAsset>(PassivePath);
            passive.ConfigureForEditor(
                57,
                "弱いものイジメ",
                "弱点を持つ敵へのDamageが増加し、Speedが一時的に増加する。",
                130,
                30,
                100,
                speed);

            var machine = GetOrCreate<SkillMachineItemAsset>(MachinePath);
            machine.ConfigureForEditor(
                10057,
                "技マシーン[蒸発]",
                null,
                "対象の味方パチモンが「蒸発」を習得する。",
                ItemCategory.SkillMachine,
                5000);
            machine.ConfigureSkillForEditor(skill);

            ReplaceCatalogEntry(
                AssetDatabase.LoadAssetAtPath<SkillCatalog>(SkillCatalogPath),
                skill);
            ReplaceCatalogEntry(
                AssetDatabase.LoadAssetAtPath<PassiveCatalog>(PassiveCatalogPath),
                passive);
            ReplaceCatalogEntry(
                AssetDatabase.LoadAssetAtPath<ItemCatalog>(ItemCatalogPath),
                machine);
            MarkDirtyAndSave(weakness, speed, skill, passive, machine);
            AssetDatabase.Refresh();
            Debug.Log("Fire Content 8 setup completed.");
        }

        private static bool IsConfigured()
        {
            var skill = AssetDatabase.LoadAssetAtPath<EvaporationSkillAsset>(
                SkillPath);
            var passiveCatalog = AssetDatabase.LoadAssetAtPath<PassiveCatalog>(
                PassiveCatalogPath);
            var itemCatalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(
                ItemCatalogPath);
            return skill?.SkillId == 57
                && skill.WeaknessStatus != null
                && passiveCatalog?.Get(57) is WeaklingBullyPassiveAsset passive
                && passive.SpeedStatus != null
                && itemCatalog?.Get(10057) is SkillMachineItemAsset machine
                && machine.Skill?.SkillId == 57;
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

        private static T GetOrCreate<T>(string path)
            where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void ReplaceCatalogEntry(
            SkillCatalog catalog,
            SkillAsset replacement)
        {
            if (catalog == null) throw new InvalidOperationException("SkillCatalog is missing.");
            var entries = BuildUnique(catalog.Skills, entry => entry.SkillId);
            entries[replacement.SkillId] = replacement;
            catalog.SetSkillsForEditor(entries.Values.OrderBy(entry => entry.SkillId));
            EditorUtility.SetDirty(catalog);
        }

        private static void ReplaceCatalogEntry(
            PassiveCatalog catalog,
            PassiveAsset replacement)
        {
            if (catalog == null) throw new InvalidOperationException("PassiveCatalog is missing.");
            var entries = BuildUnique(catalog.Passives, entry => entry.PassiveId);
            entries[replacement.PassiveId] = replacement;
            catalog.SetPassivesForEditor(entries.Values.OrderBy(entry => entry.PassiveId));
            EditorUtility.SetDirty(catalog);
        }

        private static void ReplaceCatalogEntry(
            ItemCatalog catalog,
            ItemAsset replacement)
        {
            if (catalog == null) throw new InvalidOperationException("ItemCatalog is missing.");
            var entries = BuildUnique(catalog.Items, entry => entry.ItemId);
            entries[replacement.ItemId] = replacement;
            catalog.SetItemsForEditor(entries.Values.OrderBy(entry => entry.ItemId));
            EditorUtility.SetDirty(catalog);
        }

        private static Dictionary<int, T> BuildUnique<T>(
            IEnumerable<T> source,
            Func<T, int> getId)
            where T : UnityEngine.Object
        {
            var result = new Dictionary<int, T>();
            foreach (var entry in source ?? Enumerable.Empty<T>())
            {
                if (entry == null) continue;
                var id = getId(entry);
                if (id > 0 && !result.ContainsKey(id)) result.Add(id, entry);
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
