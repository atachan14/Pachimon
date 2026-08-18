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
    public static class PoisonContentSetup
    {
        private const string MenuPath = "Tools/Pachimon/Data/Setup Poison Content 7-8";
        private const string SkillFolder = "Assets/GameData/Skill/Placeholder";
        private const string PassiveFolder = "Assets/GameData/Passive";
        private const string ItemFolder = "Assets/GameData/Item";
        private const string FieldFolder = "Assets/GameData/Battle/FieldEffect";
        private const string StatusFolder = "Assets/GameData/Battle/Status";
        private const string SkillCatalogPath = "Assets/GameData/Skill/SkillCatalog.asset";
        private const string PassiveCatalogPath = "Assets/GameData/Passive/PassiveCatalog.asset";
        private const string ItemCatalogPath = "Assets/GameData/Item/ItemCatalog.asset";

        [InitializeOnLoadMethod]
        private static void SetupAfterReload()
        {
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
            var toxin = LoadRequired<ToxinStatusAsset>(
                $"{StatusFolder}/ToxinStatus.asset");
            var mistDefinition = GetOrCreate<PoisonMistFieldEffectAsset>(
                $"{FieldFolder}/PoisonMistFieldEffect.asset");
            mistDefinition.ConfigureForEditor(
                "毒の霧",
                "Value以下の軽減前ダメージとなる敵Skill攻撃を回避する。");
            var growthDefinition = GetOrCreate<PoisonMagicianGrowthStatusAsset>(
                $"{StatusFolder}/PoisonMagicianGrowthStatus.asset");
            growthDefinition.ConfigureForEditor(
                "毒の魔術",
                "毒以外の属性Skillダメージを与えるたびにPoisonが増加する。");

            var mist = ReplaceWith<PoisonMistSkillAsset>(
                $"{SkillFolder}/Skill_053.asset");
            mist.ConfigureForEditor(
                53, "毒の霧", 100, 300, 0,
                "自陣に、弱い敵Skill攻撃を回避する毒の霧を生成する。",
                100, 100, 75, 25, mistDefinition);
            var firstTouch = ReplaceWith<FirstTouchSkillAsset>(
                $"{SkillFolder}/Skill_061.asset");
            firstTouch.ConfigureForEditor(
                61, "ファーストタッチ", 100, 300, 0,
                "先頭へ毒ダメージ。HP最大の対象には追加ダメージと毒素を与える。",
                75, 50, 300, 150, 100, toxin);

            var magician = GetOrCreate<PoisonMagicianPassiveAsset>(
                $"{PassiveFolder}/Passive_053_PoisonMagician.asset");
            magician.ConfigureForEditor(
                53, "毒の魔術師",
                "自身のSkillで毒以外の属性ダメージを与えるたびPoisonが20増加する。",
                20, growthDefinition);
            var lastTouch = GetOrCreate<LastTouchPassiveAsset>(
                $"{PassiveFolder}/Passive_061_LastTouch.asset");
            lastTouch.ConfigureForEditor(
                61, "ラストタッチ",
                "自身のSkillダメージ後、対象HPがPoison×4%以下なら戦闘不能にする。",
                4);

            ReplaceCatalog(LoadRequired<SkillCatalog>(SkillCatalogPath),
                new SkillAsset[] { mist, firstTouch });
            ReplaceCatalog(LoadRequired<PassiveCatalog>(PassiveCatalogPath),
                new PassiveAsset[] { magician, lastTouch });
            var mistMachine = ConfigureMachine(mist);
            var touchMachine = ConfigureMachine(firstTouch);
            ReplaceCatalog(LoadRequired<ItemCatalog>(ItemCatalogPath),
                new ItemAsset[] { mistMachine, touchMachine });
            MarkDirtyAndSave(mistDefinition, growthDefinition, mist, firstTouch,
                magician, lastTouch, mistMachine, touchMachine);
            AssetDatabase.Refresh();
            Debug.Log("Poison Content 7-8 setup completed.");
        }

        private static bool IsConfigured()
        {
            var skills = AssetDatabase.LoadAssetAtPath<SkillCatalog>(SkillCatalogPath);
            var passives = AssetDatabase.LoadAssetAtPath<PassiveCatalog>(PassiveCatalogPath);
            var items = AssetDatabase.LoadAssetAtPath<ItemCatalog>(ItemCatalogPath);
            return skills?.Get(53) is PoisonMistSkillAsset mist
                && mist.FieldEffect != null
                && skills.Get(61) is FirstTouchSkillAsset touch
                && touch.ToxinStatus != null
                && passives?.Get(53) is PoisonMagicianPassiveAsset magician
                && magician.GrowthStatus != null
                && passives.Get(61) is LastTouchPassiveAsset
                && IsMachine(items, 53)
                && IsMachine(items, 61);
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
