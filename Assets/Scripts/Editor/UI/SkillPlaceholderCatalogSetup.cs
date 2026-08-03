using System.Collections.Generic;
using System.Linq;
using Pachimon.Data;
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
                        "v0.3 basic attribute damage Skill placeholder.");
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
                        baseManaCost: 100,
                        description:
                            "先頭の敵と自身へFireダメージ。"
                            + "両者が生存しMNを消費できる間は再発動する。",
                        basePower: 100,
                        fireScalingPercent: 100);
                    EditorUtility.SetDirty(combustion);
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
                        baseStartupTicks: 300,
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
                        baseRecoveryTicks: 200,
                        baseCooldownTicks: 500,
                        baseManaCost: 400,
                        description:
                            "自身のElectricを保存して充電中になる。"
                            + "終了後、同じValueの充電完了になる。",
                        chargingDurationPercent: 400,
                        chargingResistBonusPercent: 40,
                        chargingElectricPercent: 50,
                        chargedDurationPercent: 200,
                        chargedElectricPercent: 150,
                        chargedSpeedPercent: 100);
                    EditorUtility.SetDirty(charge);
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
                        "v0.3 basic attribute damage Skill placeholder.");
                    EditorUtility.SetDirty(skill);
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
