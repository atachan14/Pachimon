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
    public static class DragonContentSetup
    {
        private const string MenuPath =
            "Tools/Pachimon/Data/Setup Dragon Content 3-8";
        private const string SkillFolder = "Assets/GameData/Skill/Placeholder";
        private const string PassiveFolder = "Assets/GameData/Passive";
        private const string StatusFolder = "Assets/GameData/Battle/Status";
        private const string ItemFolder = "Assets/GameData/Item";
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
            if (EditorApplication.isPlayingOrWillChangePlaymode || IsConfigured())
                return;

            Setup();
        }

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            var footwork = GetOrCreate<FootworkStatusAsset>(
                $"{StatusFolder}/FootworkStatus.asset");
            footwork.ConfigureForEditor(
                "フットワーク",
                "次に受ける攻撃と付随する状態を回避する。");

            var sweetScience = GetOrCreate<SweetScienceStatusAsset>(
                $"{StatusFolder}/SweetScienceStatus.asset");
            sweetScience.ConfigureForEditor(
                "スイートサイエンス",
                "回避に成功するたびにSpeedが恒久的に増加する。");

            var dragonDance = GetOrCreate<DragonDanceStatusAsset>(
                $"{StatusFolder}/DragonDanceStatus.asset");
            dragonDance.ConfigureForEditor(
                "龍の舞",
                "Battle中、DragonとSpeedが恒久的に増加する。");

            var dragonCranker = GetOrCreate<DragonCrankerStatusAsset>(
                $"{StatusFolder}/DragonCrankerStatus.asset");
            dragonCranker.ConfigureForEditor(
                "ドラゴンクランカー",
                "次に受けるDragon DamageがValue%増加する。");

            var knockout = GetOrCreate<KnockoutStatusAsset>(
                $"{StatusFolder}/KnockoutStatus.asset");
            knockout.ConfigureForEditor(
                "ノックアウト",
                "Stunとして扱い、Damageを受けるたびに残り時間が延長する。",
                10);

            var dragonDefense = GetOrCreate<DragonDefenseStatusAsset>(
                $"{StatusFolder}/DragonDefenseStatus.asset");
            dragonDefense.ConfigureForEditor(
                "ドラゴンディフェンス",
                "期間中、味方が受ける攻撃Damageを代わりに受ける。");

            var footworkSkill = ReplaceWith<DragonFootworkSkillAsset>(
                $"{SkillFolder}/Skill_024.asset");
            footworkSkill.ConfigureForEditor(
                24,
                "ドラゴンフットワーク",
                80,
                63,
                80,
                "{value:duration}tickの間、次に受ける攻撃と付随する状態を回避する。",
                80,
                100,
                footwork);

            var danceSkill = ReplaceWith<DragonDanceSkillAsset>(
                $"{SkillFolder}/Skill_032.asset");
            danceSkill.ConfigureForEditor(
                32,
                "龍の舞",
                100,
                400,
                120,
                "Battle中、DragonとSpeedを恒久的に増加する。",
                50,
                20,
                dragonDance);

            var breakSkill = ReplaceWith<DragonBreakSkillAsset>(
                $"{SkillFolder}/Skill_040.asset");
            breakSkill.ConfigureForEditor(
                40,
                "ドラゴンブレイク",
                120,
                350,
                100,
                "先頭の敵のShieldを全て破壊して、Dragon Damageを与える。",
                100,
                100);

            var hookSkill = ReplaceWith<DragonHookSkillAsset>(
                $"{SkillFolder}/Skill_048.asset");
            hookSkill.ConfigureForEditor(
                48,
                "ドラゴンフック",
                100,
                300,
                80,
                "先頭の敵にDragon Damageとドラゴンクランカーを与える。",
                100,
                100,
                30,
                10,
                dragonCranker);

            var upperSkill = ReplaceWith<DragonUpperSkillAsset>(
                $"{SkillFolder}/Skill_056.asset");
            upperSkill.ConfigureForEditor(
                56,
                "ドラゴンアッパー",
                120,
                400,
                120,
                "先頭の敵にDragon Damageとノックアウトを与える。",
                100,
                100,
                200,
                knockout);

            var defenseSkill = ReplaceWith<DragonDefenseSkillAsset>(
                $"{SkillFolder}/Skill_064.asset");
            defenseSkill.ConfigureForEditor(
                64,
                "ドラゴンディフェンス",
                100,
                400,
                120,
                "Shieldを得て、期間中は味方が受ける攻撃Damageを肩代わりする。",
                300,
                100,
                500,
                dragonDefense);

            var sweetSciencePassive = GetOrCreate<SweetSciencePassiveAsset>(
                $"{PassiveFolder}/Passive_024_SweetScience.asset");
            sweetSciencePassive.ConfigureForEditor(
                24,
                "スイートサイエンス",
                "回避に成功するたびにSpeedが増加する。",
                20,
                sweetScience);

            var skeletonPassive = GetOrCreate<DragonSkeletonPassiveAsset>(
                $"{PassiveFolder}/Passive_032_DragonSkeleton.asset");
            skeletonPassive.ConfigureForEditor(
                32,
                "龍の骨格",
                "Speedに応じてDragonが増加し、Dragonに応じてSpeedが増加する。",
                20,
                20);

            var ragePassive = GetOrCreate<DragonRagePassiveAsset>(
                $"{PassiveFolder}/Passive_040_DragonRage.asset");
            ragePassive.ConfigureForEditor(
                40,
                "龍の怒り",
                "Dragonに応じて、自身が与えるAttribute DamageのResistBonus貫通率が増加する。",
                25);

            var manyHitsPassive = GetOrCreate<ManyHitsPassiveAsset>(
                $"{PassiveFolder}/Passive_048_ManyHits.asset");
            manyHitsPassive.ConfigureForEditor(
                48,
                "滅多打ち",
                "ドラゴンクランカーを受けている敵へのDamageが増加する。",
                150);

            var guardPassive = GetOrCreate<DragonGuardPassiveAsset>(
                $"{PassiveFolder}/Passive_064_DragonGuard.asset");
            guardPassive.ConfigureForEditor(
                64,
                "龍の守り",
                "Dragonの20%だけResistBonusが増加する。",
                20);

            // Persist configured assets before catalog repair so a later
            // catalog error cannot leave newly-created assets blank.
            MarkDirtyAndSave(
                footwork,
                sweetScience,
                dragonDance,
                dragonCranker,
                knockout,
                dragonDefense,
                footworkSkill,
                danceSkill,
                breakSkill,
                hookSkill,
                upperSkill,
                defenseSkill,
                sweetSciencePassive,
                skeletonPassive,
                ragePassive,
                manyHitsPassive,
                guardPassive);

            ReplaceCatalogEntries(
                AssetDatabase.LoadAssetAtPath<SkillCatalog>(SkillCatalogPath),
                new SkillAsset[]
                {
                    footworkSkill, danceSkill, breakSkill, hookSkill,
                    upperSkill, defenseSkill,
                });
            ReplaceCatalogEntries(
                AssetDatabase.LoadAssetAtPath<PassiveCatalog>(PassiveCatalogPath),
                new PassiveAsset[]
                {
                    sweetSciencePassive, skeletonPassive,
                    ragePassive, manyHitsPassive,
                    guardPassive,
                });

            var footworkMachine = ConfigureSkillMachine(footworkSkill);
            var danceMachine = ConfigureSkillMachine(danceSkill);
            var breakMachine = ConfigureSkillMachine(breakSkill);
            var hookMachine = ConfigureSkillMachine(hookSkill);
            var upperMachine = ConfigureSkillMachine(upperSkill);
            var defenseMachine = ConfigureSkillMachine(defenseSkill);
            ReplaceCatalogEntries(
                AssetDatabase.LoadAssetAtPath<ItemCatalog>(ItemCatalogPath),
                new ItemAsset[]
                {
                    footworkMachine, danceMachine, breakMachine, hookMachine,
                    upperMachine, defenseMachine,
                });

            MarkDirtyAndSave(
                footworkMachine,
                danceMachine,
                breakMachine,
                hookMachine,
                upperMachine,
                defenseMachine);
            AssetDatabase.Refresh();
            Debug.Log("Dragon Content 3-8 setup completed.");
        }

        private static bool IsConfigured()
        {
            return IsSkillConfigured<DragonFootworkSkillAsset>(24)
                && IsSkillConfigured<DragonDanceSkillAsset>(32)
                && IsSkillConfigured<DragonBreakSkillAsset>(40)
                && IsSkillConfigured<DragonHookSkillAsset>(48)
                && IsSkillConfigured<DragonUpperSkillAsset>(56)
                && IsSkillConfigured<DragonDefenseSkillAsset>(64)
                && IsPassiveConfigured<SweetSciencePassiveAsset>(24)
                && IsPassiveConfigured<DragonSkeletonPassiveAsset>(32)
                && IsPassiveConfigured<DragonRagePassiveAsset>(40)
                && IsPassiveConfigured<ManyHitsPassiveAsset>(48)
                && IsPassiveConfigured<TargetStatusDamagePassiveAsset>(56)
                && IsPassiveConfigured<DragonGuardPassiveAsset>(64)
                && IsMachineConfigured(24)
                && IsMachineConfigured(32)
                && IsMachineConfigured(40)
                && IsMachineConfigured(48)
                && IsMachineConfigured(56)
                && IsMachineConfigured(64);
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
                && machine.ItemId == itemId
                && machine.Skill?.SkillId == skillId
                && AssetDatabase.GetAssetPath(machine)
                    == GetSkillMachinePath(machine.Skill)
                && !string.IsNullOrWhiteSpace(machine.DisplayName);
        }

        private static void MarkDirtyAndSave(params Object[] assets)
        {
            foreach (var asset in assets)
            {
                if (asset != null)
                    EditorUtility.SetDirty(asset);
            }
            AssetDatabase.SaveAssets();
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

        private static SkillMachineItemAsset ConfigureSkillMachine(SkillAsset skill)
        {
            var path = GetSkillMachinePath(skill);
            var legacyPath = $"{ItemFolder}/Item_{ItemIds.GetSkillMachineItemId(skill.SkillId)}"
                             + $"_TM_{skill.name}.asset";
            if (path != legacyPath
                && AssetDatabase.LoadMainAssetAtPath(path) == null
                && AssetDatabase.LoadMainAssetAtPath(legacyPath) != null)
            {
                var moveError = AssetDatabase.MoveAsset(legacyPath, path);
                if (!string.IsNullOrEmpty(moveError))
                {
                    throw new System.InvalidOperationException(
                        $"Failed to rename Skill Machine Asset: {moveError}");
                }
            }

            var item = GetOrCreate<SkillMachineItemAsset>(path);
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
            if (skill == null)
                throw new System.ArgumentNullException(nameof(skill));

            const string suffix = "SkillAsset";
            var englishName = skill.GetType().Name;
            if (englishName.EndsWith(
                    suffix,
                    System.StringComparison.Ordinal))
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
            if (catalog == null) throw new System.InvalidOperationException("SkillCatalog is missing.");
            var byId = BuildUniqueCatalog(
                catalog.Skills,
                item => item.SkillId);
            foreach (var replacement in replacements)
                byId[replacement.SkillId] = replacement;
            catalog.SetSkillsForEditor(byId.Values.OrderBy(item => item.SkillId));
            EditorUtility.SetDirty(catalog);
        }

        private static void ReplaceCatalogEntries(
            PassiveCatalog catalog,
            IEnumerable<PassiveAsset> replacements)
        {
            if (catalog == null) throw new System.InvalidOperationException("PassiveCatalog is missing.");
            var byId = BuildUniqueCatalog(
                catalog.Passives,
                item => item.PassiveId);
            foreach (var replacement in replacements)
                byId[replacement.PassiveId] = replacement;
            catalog.SetPassivesForEditor(byId.Values.OrderBy(item => item.PassiveId));
            EditorUtility.SetDirty(catalog);
        }

        private static void ReplaceCatalogEntries(
            ItemCatalog catalog,
            IEnumerable<ItemAsset> replacements)
        {
            if (catalog == null) throw new System.InvalidOperationException("ItemCatalog is missing.");
            var byId = BuildUniqueCatalog(
                catalog.Items,
                item => item.ItemId);
            foreach (var replacement in replacements)
                byId[replacement.ItemId] = replacement;
            catalog.SetItemsForEditor(byId.Values.OrderBy(item => item.ItemId));
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        private static Dictionary<int, T> BuildUniqueCatalog<T>(
            IEnumerable<T> entries,
            System.Func<T, int> getId)
            where T : Object
        {
            var byId = new Dictionary<int, T>();
            foreach (var entry in entries ?? Enumerable.Empty<T>())
            {
                if (entry == null)
                    continue;

                var id = getId(entry);
                if (id <= 0 || byId.ContainsKey(id))
                    continue;
                byId.Add(id, entry);
            }
            return byId;
        }
    }
}
