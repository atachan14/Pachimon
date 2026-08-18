using System.Linq;
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
        private const string MnPotionPath = DataFolder + "/Item_003_MnPotion.asset";
        private const string BackfireMachinePath =
            DataFolder + "/Item_10009_TM_Backfire.asset";
        private const string FireArrowMachinePath =
            DataFolder + "/Item_10033_TM_FireArrow.asset";
        private const string CombustionMachinePath =
            DataFolder + "/Item_10041_TM_BurningStrike.asset";
        private const string ChainBurnMachinePath =
            DataFolder + "/Item_10017_TM_ChainBurn.asset";
        private const string FireBarrierMachinePath =
            DataFolder + "/Item_10025_TM_FireBarrier.asset";
        private const string SunnyDayMachinePath =
            DataFolder + "/Item_10049_TM_SunnyDay.asset";
        private const string RainDanceMachinePath =
            DataFolder + "/Item_10018_TM_RainDance.asset";
        private const string WaterPulseMachinePath =
            DataFolder + "/Item_10010_TM_WaterPulse.asset";
        private const string LaunchCeremonyMachinePath =
            DataFolder + "/Item_10026_TM_LaunchCeremony.asset";
        private const string WaterVeilMachinePath =
            DataFolder + "/Item_10034_TM_WaterVeil.asset";
        private const string SunbathMachinePath = DataFolder + "/Item_10011_TM_Sunbath.asset";
        private const string ChainVinesMachinePath = DataFolder + "/Item_10019_TM_ChainVines.asset";
        private const string SolarBeamMachinePath = DataFolder + "/Item_10027_TM_SolarBeam.asset";
        private const string EntanglingVinesMachinePath = DataFolder + "/Item_10035_TM_EntanglingVines.asset";
        private const string ParalysisPowderMachinePath = DataFolder + "/Item_10043_TM_ParalysisPowder.asset";
        private const string HeavySnowMachinePath =
            DataFolder + "/Item_10030_TM_HeavySnow.asset";
        private const string WindStormMachinePath =
            DataFolder + "/Item_10047_TM_WindStorm.asset";
        private const string FlyingAttackMachinePath =
            DataFolder + "/Item_10015_TM_FlyingAttack.asset";
        private const string WindErosionMachinePath =
            DataFolder + "/Item_10023_TM_WindErosion.asset";
        private const string HealingWindMachinePath =
            DataFolder + "/Item_10031_TM_HealingWind.asset";
        private const string SecondWindMachinePath =
            DataFolder + "/Item_10039_TM_SecondWind.asset";
        private const string DragonJabMachinePath =
            DataFolder + "/Item_10016_TM_DragonJab.asset";
        private const string IceShieldMachinePath =
            DataFolder + "/Item_10014_TM_IceShield.asset";
        private const string IceShardMachinePath =
            DataFolder + "/Item_10022_TM_IceShard.asset";
        private const string ElectricExplosionMachinePath =
            DataFolder + "/Item_10020_TM_ElectricExplosion.asset";
        private const string SmogMachinePath =
            DataFolder + "/Item_10021_TM_Smog.asset";
        private const string NeurotoxinMachinePath =
            DataFolder + "/Item_10013_TM_Neurotoxin.asset";
        private const string ToxinTransferMachinePath =
            DataFolder + "/Item_10029_TM_ToxinTransfer.asset";
        private const string ToxinExplosionMachinePath =
            DataFolder + "/Item_10037_TM_ToxinExplosion.asset";
        private const string PoisonShieldMachinePath =
            DataFolder + "/Item_10045_TM_PoisonShield.asset";
        private const string FrozenBreakMachinePath =
            DataFolder + "/Item_10046_TM_FrozenBreak.asset";
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
        private const string ChainBurnSkillPath =
            "Assets/GameData/Skill/Placeholder/Skill_017.asset";
        private const string FireBarrierSkillPath =
            "Assets/GameData/Skill/Placeholder/Skill_025.asset";
        private const string SunnyDaySkillPath =
            "Assets/GameData/Skill/Placeholder/Skill_049.asset";
        private const string RainDanceSkillPath =
            "Assets/GameData/Skill/Placeholder/Skill_018.asset";
        private const string WaterPulseSkillPath =
            "Assets/GameData/Skill/Placeholder/Skill_010.asset";
        private const string LaunchCeremonySkillPath =
            "Assets/GameData/Skill/Placeholder/Skill_026.asset";
        private const string WaterVeilSkillPath =
            "Assets/GameData/Skill/Placeholder/Skill_034.asset";
        private const string SunbathSkillPath = "Assets/GameData/Skill/Placeholder/Skill_011.asset";
        private const string ChainVinesSkillPath = "Assets/GameData/Skill/Placeholder/Skill_019.asset";
        private const string SolarBeamSkillPath = "Assets/GameData/Skill/Placeholder/Skill_027.asset";
        private const string EntanglingVinesSkillPath = "Assets/GameData/Skill/Placeholder/Skill_035.asset";
        private const string ParalysisPowderSkillPath = "Assets/GameData/Skill/Placeholder/Skill_043.asset";
        private const string HeavySnowSkillPath =
            "Assets/GameData/Skill/Placeholder/Skill_030.asset";
        private const string WindStormSkillPath =
            "Assets/GameData/Skill/Placeholder/Skill_047.asset";
        private const string FlyingAttackSkillPath =
            "Assets/GameData/Skill/Placeholder/Skill_015.asset";
        private const string WindErosionSkillPath =
            "Assets/GameData/Skill/Placeholder/Skill_023.asset";
        private const string HealingWindSkillPath =
            "Assets/GameData/Skill/Placeholder/Skill_031.asset";
        private const string SecondWindSkillPath =
            "Assets/GameData/Skill/Placeholder/Skill_039.asset";
        private const string DragonJabSkillPath =
            "Assets/GameData/Skill/Placeholder/Skill_016.asset";
        private const string IceShieldSkillPath =
            "Assets/GameData/Skill/Placeholder/Skill_014.asset";
        private const string IceShardSkillPath =
            "Assets/GameData/Skill/Placeholder/Skill_022.asset";
        private const string ElectricExplosionSkillPath =
            "Assets/GameData/Skill/Placeholder/Skill_020.asset";
        private const string SmogSkillPath =
            "Assets/GameData/Skill/Placeholder/Skill_021.asset";
        private const string NeurotoxinSkillPath =
            "Assets/GameData/Skill/Placeholder/Skill_013.asset";
        private const string ToxinTransferSkillPath =
            "Assets/GameData/Skill/Placeholder/Skill_029.asset";
        private const string ToxinExplosionSkillPath =
            "Assets/GameData/Skill/Placeholder/Skill_037.asset";
        private const string PoisonShieldSkillPath =
            "Assets/GameData/Skill/Placeholder/Skill_045.asset";
        private const string FrozenBreakSkillPath =
            "Assets/GameData/Skill/Placeholder/Skill_046.asset";
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
                "対象の味方パチモンの最大HPの50%を回復する。",
                ItemCategory.Pharmacy,
                300);
            potion.ConfigureHealingForEditor(
                RecoveryResourceType.Hp,
                50,
                false);
            EditorUtility.SetDirty(potion);

            var mnPotion = AssetDatabase.LoadAssetAtPath<HealingItemAsset>(MnPotionPath);
            if (mnPotion == null)
            {
                mnPotion = ScriptableObject.CreateInstance<HealingItemAsset>();
                AssetDatabase.CreateAsset(mnPotion, MnPotionPath);
            }

            Undo.RecordObject(mnPotion, "Configure MN Potion Item");
            mnPotion.ConfigureForEditor(
                ItemIds.MnPotion,
                "MNポーション",
                potionIcon,
                "対象の味方パチモンの最大MNの50%を回復する。",
                ItemCategory.Pharmacy,
                300);
            mnPotion.ConfigureHealingForEditor(
                RecoveryResourceType.Mn,
                50,
                false);
            EditorUtility.SetDirty(mnPotion);

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
                "技マシーン[燃える一撃]",
                stoneIcon);
            var chainBurnMachine = ConfigureSkillMachine(
                ChainBurnMachinePath,
                ChainBurnSkillPath,
                "技マシーン[チェインバーン]",
                stoneIcon);
            var fireBarrierMachine = ConfigureSkillMachine(
                FireBarrierMachinePath,
                FireBarrierSkillPath,
                "技マシーン[炎の障壁]",
                stoneIcon);
            var sunnyDayMachine = ConfigureSkillMachine(
                SunnyDayMachinePath,
                SunnyDaySkillPath,
                "技マシーン[温暖化]",
                stoneIcon);
            var rainDanceMachine = ConfigureSkillMachine(
                RainDanceMachinePath,
                RainDanceSkillPath,
                "技マシーン[あまごい]",
                stoneIcon);
            var waterPulseMachine = ConfigureSkillMachine(
                WaterPulseMachinePath,
                WaterPulseSkillPath,
                "\u6280\u30DE\u30B7\u30FC\u30F3[\u6C34\u306E\u6CE2\u52D5]",
                stoneIcon);
            var launchCeremonyMachine = ConfigureSkillMachine(
                LaunchCeremonyMachinePath,
                LaunchCeremonySkillPath,
                "\u6280\u30DE\u30B7\u30FC\u30F3[\u9032\u6C34\u5F0F]",
                stoneIcon);
            var waterVeilMachine = ConfigureSkillMachine(
                WaterVeilMachinePath,
                WaterVeilSkillPath,
                "\u6280\u30DE\u30B7\u30FC\u30F3[\u6C34\u306E\u30D9\u30FC\u30EB]",
                stoneIcon);
            var sunbathMachine = ConfigureSkillMachine(SunbathMachinePath, SunbathSkillPath, "技マシーン[日光浴]", stoneIcon);
            var chainVinesMachine = ConfigureSkillMachine(ChainVinesMachinePath, ChainVinesSkillPath, "技マシーン[連鎖する蔦]", stoneIcon);
            var solarBeamMachine = ConfigureSkillMachine(SolarBeamMachinePath, SolarBeamSkillPath, "技マシーン[ソーラービーム]", stoneIcon);
            var entanglingVinesMachine = ConfigureSkillMachine(EntanglingVinesMachinePath, EntanglingVinesSkillPath, "技マシーン[絡み合う蔓]", stoneIcon);
            var paralysisPowderMachine = ConfigureSkillMachine(ParalysisPowderMachinePath, ParalysisPowderSkillPath, "技マシーン[しびれ粉]", stoneIcon);
            var heavySnowMachine = ConfigureSkillMachine(
                HeavySnowMachinePath,
                HeavySnowSkillPath,
                "技マシーン[寒冷化]",
                stoneIcon);
            var windStormMachine = ConfigureSkillMachine(
                WindStormMachinePath,
                WindStormSkillPath,
                "技マシーン[暴風]",
                stoneIcon);
            var flyingAttackMachine = ConfigureSkillMachine(
                FlyingAttackMachinePath,
                FlyingAttackSkillPath,
                "技マシーン[フライングアタック]",
                stoneIcon);
            var windErosionMachine = ConfigureSkillMachine(
                WindErosionMachinePath,
                WindErosionSkillPath,
                "技マシーン[風化の風]",
                stoneIcon);
            var healingWindMachine = ConfigureSkillMachine(
                HealingWindMachinePath,
                HealingWindSkillPath,
                "技マシーン[治癒の風]",
                stoneIcon);
            var secondWindMachine = ConfigureSkillMachine(
                SecondWindMachinePath,
                SecondWindSkillPath,
                "技マシーン[セカンドウィンド]",
                stoneIcon);
            var dragonJabMachine = ConfigureSkillMachine(
                DragonJabMachinePath,
                DragonJabSkillPath,
                "技マシーン[ドラゴンジャブ]",
                stoneIcon);
            var iceShieldMachine = ConfigureSkillMachine(
                IceShieldMachinePath,
                IceShieldSkillPath,
                "技マシーン[氷の盾]",
                stoneIcon);
            var iceShardMachine = ConfigureSkillMachine(
                IceShardMachinePath,
                IceShardSkillPath,
                "技マシーン[アイスシャード]",
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
            var smogMachine = ConfigureSkillMachine(
                SmogMachinePath,
                SmogSkillPath,
                "技マシーン[スモッグ]",
                stoneIcon);
            var neurotoxinMachine = ConfigureSkillMachine(
                NeurotoxinMachinePath,
                NeurotoxinSkillPath,
                "技マシーン[神経毒]",
                stoneIcon);
            var toxinTransferMachine = ConfigureSkillMachine(
                ToxinTransferMachinePath,
                ToxinTransferSkillPath,
                "技マシーン[毒渡し]",
                stoneIcon);
            var toxinExplosionMachine = ConfigureSkillMachine(
                ToxinExplosionMachinePath,
                ToxinExplosionSkillPath,
                "技マシーン[毒爆破]",
                stoneIcon);
            var poisonShieldMachine = ConfigureSkillMachine(
                PoisonShieldMachinePath,
                PoisonShieldSkillPath,
                "技マシーン[ポイズンシールド]",
                stoneIcon);
            var frozenBreakMachine = ConfigureSkillMachine(
                FrozenBreakMachinePath,
                FrozenBreakSkillPath,
                "技マシーン[フローズンブレイク]",
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
            var configuredItems = new ItemAsset[]
            {
                potion,
                stone,
                mnPotion,
                backfireMachine,
                fireArrowMachine,
                combustionMachine,
                chainBurnMachine,
                fireBarrierMachine,
                sunnyDayMachine,
                rainDanceMachine,
                waterPulseMachine,
                launchCeremonyMachine,
                waterVeilMachine,
                heavySnowMachine,
                windStormMachine,
                iceShieldMachine,
                iceShardMachine,
                aquaShockMachine,
                electricExplosionMachine,
                neurotoxinMachine,
                smogMachine,
                toxinTransferMachine,
                toxinExplosionMachine,
                poisonShieldMachine,
                frozenBreakMachine,
                electricQuickAttackMachine,
                chargeMachine,
                electromagneticCannonMachine,
                sunbathMachine,
                chainVinesMachine,
                solarBeamMachine,
                entanglingVinesMachine,
                paralysisPowderMachine,
                flyingAttackMachine,
                windErosionMachine,
                healingWindMachine,
                secondWindMachine,
                dragonJabMachine,
            };
            var mergedItems = catalog.Items
                .Where(item => item != null)
                .Concat(configuredItems)
                .GroupBy(item => item.ItemId)
                .Select(group => group.Last())
                .OrderBy(item => item.ItemId);
            catalog.SetItemsForEditor(mergedItems);
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

namespace Pachimon.Editor.UI
{
    public static class EngravingItemSetup
    {
        private const string DataFolder = "Assets/GameData/Item/Engraving";
        private const string CatalogPath = "Assets/GameData/Item/ItemCatalog.asset";
        private const int BasePrice = 500;

        private static readonly (Pachimon.Run.PachimonStatType Stat, string Name, int Value)[]
            Definitions =
            {
                (Pachimon.Run.PachimonStatType.MaxHp, "生命の刻印", 50),
                (Pachimon.Run.PachimonStatType.MaxMn, "活力の刻印", 50),
                (Pachimon.Run.PachimonStatType.Fire, "炎の刻印", 30),
                (Pachimon.Run.PachimonStatType.Aqua, "水の刻印", 30),
                (Pachimon.Run.PachimonStatType.Leaf, "草の刻印", 30),
                (Pachimon.Run.PachimonStatType.Electric, "電の刻印", 30),
                (Pachimon.Run.PachimonStatType.Poison, "毒の刻印", 30),
                (Pachimon.Run.PachimonStatType.Ice, "氷の刻印", 30),
                (Pachimon.Run.PachimonStatType.Wind, "風の刻印", 30),
                (Pachimon.Run.PachimonStatType.Dragon, "竜の刻印", 30),
                (Pachimon.Run.PachimonStatType.Speed, "俊足の刻印", 10),
                (Pachimon.Run.PachimonStatType.Haste, "加速の刻印", 10),
                (Pachimon.Run.PachimonStatType.DamageBonus, "攻勢の刻印", 10),
                (Pachimon.Run.PachimonStatType.ResistBonus, "守勢の刻印", 10),
            };

        [UnityEditor.InitializeOnLoadMethod]
        private static void ScheduleSetup()
        {
            UnityEditor.EditorApplication.delayCall += TryAutoSetup;
        }

        [UnityEditor.MenuItem("Tools/Pachimon/Data/Create Engraving Items")]
        public static void Setup()
        {
            EnsureAssetFolder(DataFolder);
            var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<Pachimon.Items.ItemCatalog>(
                CatalogPath);
            if (catalog == null)
            {
                UnityEngine.Debug.LogWarning(
                    "ItemCatalog is missing. Create the Item sample catalog first.");
                return;
            }

            var items = Definitions.Select((definition, index) =>
            {
                var itemId = Pachimon.Items.ItemIds.FirstEngraving + index;
                var path = $"{DataFolder}/Item_{itemId:D3}_Engraving_{definition.Stat}.asset";
                var item = UnityEditor.AssetDatabase
                    .LoadAssetAtPath<Pachimon.Items.EngravingItemAsset>(path);
                if (!HasValidScriptReference(item))
                {
                    UnityEditor.AssetDatabase.DeleteAsset(path);
                    item = UnityEngine.ScriptableObject
                        .CreateInstance<Pachimon.Items.EngravingItemAsset>();
                    UnityEditor.AssetDatabase.CreateAsset(item, path);
                }

                item.ConfigureForEditor(
                    itemId,
                    definition.Name,
                    null,
                    $"対象の{Pachimon.Items.EngravingStatName.Get(definition.Stat)}を恒久的に増加させる。",
                    Pachimon.Items.ItemCategory.Engraving,
                    BasePrice);
                item.ConfigureEngravingForEditor(
                    definition.Stat,
                    Pachimon.Items.StatUnitValue.Get(definition.Stat));
                UnityEditor.EditorUtility.SetDirty(item);
                return item;
            }).ToArray();

            catalog.SetItemsForEditor(catalog.Items
                .Where(item => item != null)
                .Concat(items)
                .GroupBy(item => item.ItemId)
                .Select(group => group.Last())
                .OrderBy(item => item.ItemId));
            UnityEditor.EditorUtility.SetDirty(catalog);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEngine.Debug.Log("Engraving Items were created and registered.", catalog);
        }

        private static void TryAutoSetup()
        {
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<Pachimon.Items.ItemCatalog>(
                CatalogPath);
            if (catalog == null)
            {
                return;
            }

            var engravings = catalog.Items
                .OfType<Pachimon.Items.EngravingItemAsset>()
                .Where(item => item.ItemId >= Pachimon.Items.ItemIds.FirstEngraving
                    && item.ItemId <= Pachimon.Items.ItemIds.LastEngraving)
                .ToArray();
            var count = engravings
                .Select(item => item.TargetStat)
                .Distinct()
                .Count();
            if (count != (int)Pachimon.Run.PachimonStatType.Count
                || engravings.Any(item => item.BasePrice != BasePrice
                    || !HasValidScriptReference(item)))
            {
                Setup();
            }
        }

        private static void EnsureAssetFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var next = $"{current}/{parts[index]}";
                if (!UnityEditor.AssetDatabase.IsValidFolder(next))
                {
                    UnityEditor.AssetDatabase.CreateFolder(current, parts[index]);
                }
                current = next;
            }
        }

        private static bool HasValidScriptReference(UnityEngine.ScriptableObject asset)
        {
            if (asset == null)
            {
                return false;
            }

            return new UnityEditor.SerializedObject(asset)
                .FindProperty("m_Script")?.objectReferenceValue != null;
        }
    }
}

namespace Pachimon.Editor.UI
{
    public static class EquipmentItemSetup
    {
        private const string DataFolder = "Assets/GameData/Item/Equipment";
        private const string CatalogPath = "Assets/GameData/Item/ItemCatalog.asset";
        private const int BasePrice = 2000;

        private static readonly (Pachimon.Reward.PachimonAttribute Attribute, string Name)[]
            Attributes =
            {
                (Pachimon.Reward.PachimonAttribute.Fire, "炎"),
                (Pachimon.Reward.PachimonAttribute.Aqua, "水"),
                (Pachimon.Reward.PachimonAttribute.Leaf, "草"),
                (Pachimon.Reward.PachimonAttribute.Electric, "電"),
                (Pachimon.Reward.PachimonAttribute.Poison, "毒"),
                (Pachimon.Reward.PachimonAttribute.Ice, "氷"),
                (Pachimon.Reward.PachimonAttribute.Wind, "風"),
                (Pachimon.Reward.PachimonAttribute.Dragon, "竜"),
            };

        [UnityEditor.InitializeOnLoadMethod]
        private static void ScheduleSetup()
        {
            UnityEditor.EditorApplication.delayCall += TryAutoSetup;
        }

        [UnityEditor.MenuItem("Tools/Pachimon/Data/Create Equipment Items")]
        public static void Setup()
        {
            EnsureAssetFolder(DataFolder);
            var catalog = UnityEditor.AssetDatabase
                .LoadAssetAtPath<Pachimon.Items.ItemCatalog>(CatalogPath);
            if (catalog == null)
            {
                UnityEngine.Debug.LogWarning(
                    "ItemCatalog is missing. Create the Item sample catalog first.");
                return;
            }

            var items = new System.Collections.Generic.List<Pachimon.Items.EquipmentItemAsset>();
            var itemId = Pachimon.Items.ItemIds.FirstEquipment;
            foreach (Pachimon.Items.EquipmentSlot slot in System.Enum.GetValues(
                         typeof(Pachimon.Items.EquipmentSlot)))
            {
                foreach (var attribute in Attributes)
                {
                    var slotName = GetSlotName(slot);
                    var path = $"{DataFolder}/Item_{itemId:D3}_Equipment_"
                        + $"{slot}_{attribute.Attribute}.asset";
                    var item = UnityEditor.AssetDatabase
                        .LoadAssetAtPath<Pachimon.Items.EquipmentItemAsset>(path);
                    if (!HasValidScriptReference(item))
                    {
                        UnityEditor.AssetDatabase.DeleteAsset(path);
                        item = UnityEngine.ScriptableObject
                            .CreateInstance<Pachimon.Items.EquipmentItemAsset>();
                        UnityEditor.AssetDatabase.CreateAsset(item, path);
                    }

                    item.ConfigureForEditor(
                        itemId,
                        $"{attribute.Name}の{slotName}",
                        null,
                        $"{attribute.Name}属性を主効果とする{slotName}。",
                        Pachimon.Items.ItemCategory.Equipment,
                        BasePrice);
                    item.ConfigureEquipmentForEditor(slot, attribute.Attribute);
                    UnityEditor.EditorUtility.SetDirty(item);
                    items.Add(item);
                    itemId++;
                }
            }

            catalog.SetItemsForEditor(catalog.Items
                .Where(item => item != null)
                .Concat(items)
                .GroupBy(item => item.ItemId)
                .Select(group => group.Last())
                .OrderBy(item => item.ItemId));
            UnityEditor.EditorUtility.SetDirty(catalog);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEngine.Debug.Log("Equipment Items were created and registered.", catalog);
        }

        private static void TryAutoSetup()
        {
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var catalog = UnityEditor.AssetDatabase
                .LoadAssetAtPath<Pachimon.Items.ItemCatalog>(CatalogPath);
            if (catalog == null)
            {
                return;
            }

            var equipment = catalog.Items
                .OfType<Pachimon.Items.EquipmentItemAsset>()
                .Where(item => item.ItemId >= Pachimon.Items.ItemIds.FirstEquipment
                    && item.ItemId <= Pachimon.Items.ItemIds.LastEquipment)
                .ToArray();
            var uniqueDefinitions = equipment
                .Select(item => (item.Slot, item.MainAttribute))
                .Distinct()
                .Count();
            if (equipment.Length != 24
                || uniqueDefinitions != 24
                || equipment.Any(item => item.BasePrice != BasePrice
                    || !HasValidScriptReference(item)))
            {
                Setup();
            }
        }

        private static string GetSlotName(Pachimon.Items.EquipmentSlot slot)
        {
            return slot switch
            {
                Pachimon.Items.EquipmentSlot.Head => "冠",
                Pachimon.Items.EquipmentSlot.Body => "勾玉",
                Pachimon.Items.EquipmentSlot.Feet => "靴",
                _ => throw new System.ArgumentOutOfRangeException(nameof(slot)),
            };
        }

        private static void EnsureAssetFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var next = $"{current}/{parts[index]}";
                if (!UnityEditor.AssetDatabase.IsValidFolder(next))
                {
                    UnityEditor.AssetDatabase.CreateFolder(current, parts[index]);
                }

                current = next;
            }
        }

        private static bool HasValidScriptReference(UnityEngine.ScriptableObject asset)
        {
            if (asset == null)
            {
                return false;
            }

            return new UnityEditor.SerializedObject(asset)
                .FindProperty("m_Script")?.objectReferenceValue != null;
        }
    }
}
