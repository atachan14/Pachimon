using Pachimon.Items;
using Pachimon.Skills;
using Pachimon.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Pachimon.Editor.UI
{
    public static class ItemCatalogSetup
    {
        private const string MenuRoot = "Tools/Pachimon/Data/";
        private const string DataFolder = "Assets/GameData/Item";
        private const string CatalogPath = DataFolder + "/ItemCatalog.asset";
        private const string PotionPath = DataFolder + "/Item_001_Potion.asset";
        private const string StonePath = DataFolder + "/Item_002_Stone.asset";
        private const string BackfireMachinePath =
            DataFolder + "/Item_10009_TM_Backfire.asset";
        private const string FireArrowMachinePath =
            DataFolder + "/Item_10033_TM_FireArrow.asset";
        private const string CombustionMachinePath =
            DataFolder + "/Item_10041_TM_Combustion.asset";
        private const string ElectricExplosionMachinePath =
            DataFolder + "/Item_10020_TM_ElectricExplosion.asset";
        private const string AquaShockMachinePath =
            DataFolder + "/Item_10012_TM_AquaShock.asset";
        private const string ElectricQuickAttackMachinePath =
            DataFolder + "/Item_10028_TM_ElectricQuickAttack.asset";
        private const string ElectromagneticCannonMachinePath =
            DataFolder + "/Item_10044_TM_ElectromagneticCannon.asset";
        private const string ChargeMachinePath =
            DataFolder + "/Item_10036_TM_Charge.asset";
        private const string BackfireSkillPath =
            "Assets/GameData/Skill/Placeholder/Skill_009.asset";
        private const string FireArrowSkillPath =
            "Assets/GameData/Skill/Placeholder/Skill_033.asset";
        private const string CombustionSkillPath =
            "Assets/GameData/Skill/Placeholder/Skill_041.asset";
        private const string ElectricExplosionSkillPath =
            "Assets/GameData/Skill/Placeholder/Skill_020.asset";
        private const string AquaShockSkillPath =
            "Assets/GameData/Skill/Placeholder/Skill_012.asset";
        private const string ElectricQuickAttackSkillPath =
            "Assets/GameData/Skill/Placeholder/Skill_028.asset";
        private const string ElectromagneticCannonSkillPath =
            "Assets/GameData/Skill/Placeholder/Skill_044.asset";
        private const string ChargeSkillPath =
            "Assets/GameData/Skill/Placeholder/Skill_036.asset";
        private const string PotionIconPath = "Assets/Art/Items/Icons/Potion.png";
        private const string StoneIconPath = "Assets/Art/Items/Icons/Stone.png";

        [InitializeOnLoadMethod]
        private static void AssignExistingCatalogAfterReload()
        {
            EditorApplication.delayCall += TryAssignExistingCatalog;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        [MenuItem(MenuRoot + "Create Item Sample Catalog")]
        private static void CreateCatalog()
        {
            EnsureAssetFolder(DataFolder);
            ConfigureIconImporter(PotionIconPath);
            ConfigureIconImporter(StoneIconPath);
            var potionIcon = AssetDatabase.LoadAssetAtPath<Sprite>(PotionIconPath);
            var stoneIcon = AssetDatabase.LoadAssetAtPath<Sprite>(StoneIconPath);
            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ItemCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var potion = AssetDatabase.LoadAssetAtPath<HealingItemAsset>(PotionPath);
            if (potion == null)
            {
                potion = ScriptableObject.CreateInstance<HealingItemAsset>();
                AssetDatabase.CreateAsset(potion, PotionPath);
            }

            Undo.RecordObject(potion, "Configure Potion Item");
            potion.ConfigureForEditor(
                ItemIds.Potion,
                "きずぐすり",
                potionIcon,
                "対象の味方パチモンのHPを300回復する。",
                ItemCategory.Pharmacy,
                300);
            potion.ConfigureHealingForEditor(300, false);
            EditorUtility.SetDirty(potion);

            var stone = AssetDatabase.LoadAssetAtPath<DamageItemAsset>(StonePath);
            if (stone == null)
            {
                stone = ScriptableObject.CreateInstance<DamageItemAsset>();
                AssetDatabase.CreateAsset(stone, StonePath);
            }

            Undo.RecordObject(stone, "Configure Stone Item");
            stone.ConfigureForEditor(
                ItemIds.Stone,
                "石ころ",
                stoneIcon,
                "対象の敵パチモンに100の確定ダメージを与える。",
                ItemCategory.Other,
                200);
            stone.ConfigureDamageForEditor(100);
            EditorUtility.SetDirty(stone);

            var backfireMachine = ConfigureSkillMachine(
                BackfireMachinePath,
                BackfireSkillPath,
                "技マシーン[バックファイア]",
                stoneIcon);
            var fireArrowMachine = ConfigureSkillMachine(
                FireArrowMachinePath,
                FireArrowSkillPath,
                "技マシーン[ファイアアロー]",
                stoneIcon);
            var combustionMachine = ConfigureSkillMachine(
                CombustionMachinePath,
                CombustionSkillPath,
                "技マシーン[燃焼]",
                stoneIcon);
            var aquaShockMachine = ConfigureSkillMachine(
                AquaShockMachinePath,
                AquaShockSkillPath,
                "技マシーン[アクアショック]",
                stoneIcon);
            var electricExplosionMachine = ConfigureSkillMachine(
                ElectricExplosionMachinePath,
                ElectricExplosionSkillPath,
                "技マシーン[電気爆発]",
                stoneIcon);
            var electricQuickAttackMachine = ConfigureSkillMachine(
                ElectricQuickAttackMachinePath,
                ElectricQuickAttackSkillPath,
                "技マシーン[電光石火]",
                stoneIcon);
            var chargeMachine = ConfigureSkillMachine(
                ChargeMachinePath,
                ChargeSkillPath,
                "技マシーン[充電]",
                stoneIcon);
            var electromagneticCannonMachine = ConfigureSkillMachine(
                ElectromagneticCannonMachinePath,
                ElectromagneticCannonSkillPath,
                "技マシーン[電磁砲]",
                stoneIcon);

            Undo.RecordObject(catalog, "Configure Item Catalog");
            catalog.SetItemsForEditor(new ItemAsset[]
            {
                potion,
                stone,
                backfireMachine,
                fireArrowMachine,
                combustionMachine,
                aquaShockMachine,
                electricExplosionMachine,
                electricQuickAttackMachine,
                chargeMachine,
                electromagneticCannonMachine,
            });
            EditorUtility.SetDirty(catalog);
            AssignCatalogToSceneInstaller(catalog);
            AssetDatabase.SaveAssets();
            ValidateCatalog(catalog);
            Selection.activeObject = catalog;
        }

        [MenuItem(MenuRoot + "Validate Item Catalog")]
        private static void ValidateCatalogFromMenu()
        {
            ValidateCatalog(AssetDatabase.LoadAssetAtPath<ItemCatalog>(CatalogPath));
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

            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(CatalogPath);
            if (catalog != null)
            {
                AssignCatalogToSceneInstaller(catalog);
            }
        }

        private static void AssignCatalogToSceneInstaller(ItemCatalog catalog)
        {
            var installer = Object.FindAnyObjectByType<GameSceneInstaller>(
                FindObjectsInactive.Include);
            if (installer == null)
            {
                Debug.LogWarning(
                    "GameSceneInstaller was not found. Assign ItemCatalog with GameScene open.");
                return;
            }

            Undo.RecordObject(installer, "Assign Item Catalog");
            if (!installer.ConfigureItemCatalog(catalog))
            {
                return;
            }

            EditorUtility.SetDirty(installer);
            EditorSceneManager.MarkSceneDirty(installer.gameObject.scene);
            Debug.Log("ItemCatalog assigned to GameSceneInstaller.", installer);
        }

        private static void ValidateCatalog(ItemCatalog catalog)
        {
            if (catalog == null)
            {
                Debug.LogError("ItemCatalog is missing. Create the sample catalog first.");
                return;
            }

            var errors = catalog.ValidateContent();
            if (errors.Count == 0)
            {
                Debug.Log($"ItemCatalog is valid: {catalog.Items.Count} Items.", catalog);
                return;
            }

            Debug.LogError(
                "ItemCatalog validation failed:\n" + string.Join("\n", errors),
                catalog);
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

        private static void ConfigureIconImporter(string assetPath)
        {
            AssetDatabase.ImportAsset(
                assetPath,
                ImportAssetOptions.ForceSynchronousImport);
            if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer)
            {
                Debug.LogError($"Item Icon could not be imported: {assetPath}");
                return;
            }

            var changed = importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single
                || importer.filterMode != FilterMode.Point
                || importer.textureCompression != TextureImporterCompression.Uncompressed
                || importer.mipmapEnabled
                || !importer.alphaIsTransparency
                || !Mathf.Approximately(importer.spritePixelsPerUnit, 64f);
            if (!changed)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 64f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        private static SkillMachineItemAsset ConfigureSkillMachine(
            string itemPath,
            string skillPath,
            string displayName,
            Sprite icon)
        {
            var skill = AssetDatabase.LoadAssetAtPath<SkillAsset>(skillPath);
            if (skill == null)
            {
                throw new System.InvalidOperationException(
                    $"Skill Machine source Skill is missing: {skillPath}");
            }

            var item =
                AssetDatabase.LoadAssetAtPath<SkillMachineItemAsset>(itemPath);
            if (item == null)
            {
                item = ScriptableObject
                    .CreateInstance<SkillMachineItemAsset>();
                AssetDatabase.CreateAsset(item, itemPath);
            }

            Undo.RecordObject(item, "Configure Skill Machine Item");
            item.ConfigureForEditor(
                ItemIds.GetSkillMachineItemId(skill.SkillId),
                displayName,
                icon,
                $"対象の味方パチモンが「{skill.DisplayName}」を習得する。",
                ItemCategory.SkillMachine,
                5000);
            item.ConfigureSkillForEditor(skill);
            EditorUtility.SetDirty(item);
            return item;
        }
    }
}
