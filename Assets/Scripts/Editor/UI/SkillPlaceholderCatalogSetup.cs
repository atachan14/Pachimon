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

        private const int BasicTurnCost = 100;
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
                        BasicTurnCost,
                        BasicCooldown,
                        "v0.3 basic attribute damage Skill placeholder.");
                }
                else
                {
                    Undo.RecordObject(skill, "Update Basic Skill Placeholder");
                    skill.ConfigureForEditor(
                        skillId,
                        GetBasicSkillName(allocationType),
                        allocationType,
                        true,
                        BasicTurnCost,
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
            int baseTurnCostTicks,
            int baseCooldownTicks,
            string description)
        {
            var skill = ScriptableObject.CreateInstance<PlaceholderSkillAsset>();
            skill.ConfigureForEditor(
                skillId,
                displayName,
                allocationType,
                isMapAssignable,
                baseTurnCostTicks,
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
