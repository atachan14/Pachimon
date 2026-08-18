using System.Collections.Generic;
using System.IO;
using System.Linq;
using Pachimon.Data;
using Pachimon.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Pachimon.Editor.UI
{
    public static class PachimonCatalogSetup
    {
        private const string MenuRoot = "Tools/Pachimon/Data/";
        private const string DataFolder = "Assets/GameData/Pachimon";
        private const string PlaceholderFolder = DataFolder + "/Placeholder";
        private const string CatalogPath = DataFolder + "/PachimonCatalog.asset";
        private const string PachigidaneFrontPath =
            "Assets/Art/Pachimon/Species001_Pachigidane/pachigidane_front.png";
        private const string PachigidaneBackPath =
            "Assets/Art/Pachimon/Species001_Pachigidane/pachigidane_back.png";
        private const string PachikageFrontPath =
            "Assets/Art/Pachimon/SpeciesFire_Pachikage/pachikage_front.png";
        private const string PachikageBackPath =
            "Assets/Art/Pachimon/SpeciesFire_Pachikage/pachikage_back.png";
        private const string PachigameFrontPath =
            "Assets/Art/Pachimon/SpeciesAqua_Pachigame/pachigame_front.png";
        private const string PachigameBackPath =
            "Assets/Art/Pachimon/SpeciesAqua_Pachigame/pachigame_back.png";
        private const string PachichuFrontPath =
            "Assets/Art/Pachimon/SpeciesElectric_Pachichu/pachichu_front.png";
        private const string PachichuBackPath =
            "Assets/Art/Pachimon/SpeciesElectric_Pachichu/pachichu_back.png";
        private const string PachimushiFrontPath =
            "Assets/Art/Pachimon/SpeciesPoison_Pachimushi/pachimushi_front.png";
        private const string PachimushiBackPath =
            "Assets/Art/Pachimon/SpeciesPoison_Pachimushi/pachimushi_back.png";
        private const string PachigooriFrontPath =
            "Assets/Art/Pachimon/SpeciesIce_Pachigoori/pachigoori_front.png";
        private const string PachigooriBackPath =
            "Assets/Art/Pachimon/SpeciesIce_Pachigoori/pachigoori_back.png";
        private const string PachikazeFrontPath =
            "Assets/Art/Pachimon/SpeciesWind_Pachikaze/pachikaze_front.png";
        private const string PachikazeBackPath =
            "Assets/Art/Pachimon/SpeciesWind_Pachikaze/pachikaze_back.png";
        private const string PachidragonFrontPath =
            "Assets/Art/Pachimon/SpeciesDragon_Pachidragon/pachidragon_front.png";
        private const string PachidragonBackPath =
            "Assets/Art/Pachimon/SpeciesDragon_Pachidragon/pachidragon_back.png";

        [InitializeOnLoadMethod]
        private static void AssignExistingCatalogAfterReload()
        {
            EditorApplication.delayCall += TryAssignExistingCatalog;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        [MenuItem(MenuRoot + "Create Pachimon Placeholder Catalog")]
        private static void CreateCatalog()
        {
            EnsureAssetFolder(PlaceholderFolder);
            var front = GetOrCreatePlaceholderSprite("pachimon_front.png", false);
            var back = GetOrCreatePlaceholderSprite("pachimon_back.png", true);
            var catalog = GetOrCreateCatalog(front, back);
            ApplyAvailableTypeGraphics(catalog);
            MigrateMissingLogicIds(catalog);
            MigrateMissingAllocationTypes(catalog);
            MigrateGeneratedDisplayNames(catalog);
            AssignCatalogToSceneInstaller(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidateCatalog(catalog);
            Selection.activeObject = catalog;
        }

        [MenuItem(MenuRoot + "Validate Pachimon Catalog")]
        private static void ValidateCatalogFromMenu()
        {
            ValidateCatalog(AssetDatabase.LoadAssetAtPath<PachimonCatalog>(CatalogPath));
        }

        [MenuItem(MenuRoot + "Apply Available Pachimon Graphics")]
        private static void ApplyAvailableTypeGraphicsFromMenu()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PachimonCatalog>(CatalogPath);
            if (catalog == null)
            {
                Debug.LogError("PachimonCatalog is missing. Create the placeholder catalog first.");
                return;
            }

            ApplyAvailableTypeGraphics(catalog);
            AssetDatabase.SaveAssets();
            ValidateCatalog(catalog);
            Selection.activeObject = catalog;
        }

        [MenuItem(MenuRoot + "Migrate Missing Pachimon Logic IDs")]
        private static void MigrateMissingLogicIdsFromMenu()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PachimonCatalog>(CatalogPath);
            MigrateMissingLogicIds(catalog);
            AssetDatabase.SaveAssets();
            ValidateCatalog(catalog);
        }

        [MenuItem(MenuRoot + "Migrate Missing Pachimon Allocation Types")]
        private static void MigrateMissingAllocationTypesFromMenu()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PachimonCatalog>(CatalogPath);
            MigrateMissingAllocationTypes(catalog);
            AssetDatabase.SaveAssets();
            ValidateCatalog(catalog);
        }

        [MenuItem(MenuRoot + "Reset Pachimon Display Names")]
        private static void ResetDisplayNamesFromMenu()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PachimonCatalog>(CatalogPath);
            ResetDefaultDisplayNames(catalog);
            AssetDatabase.SaveAssets();
            ValidateCatalog(catalog);
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

            var catalog = AssetDatabase.LoadAssetAtPath<PachimonCatalog>(CatalogPath);
            if (catalog != null)
            {
                ApplyAvailableTypeGraphics(catalog);
                MigrateMissingLogicIds(catalog);
                MigrateMissingAllocationTypes(catalog);
                MigrateGeneratedDisplayNames(catalog);
                AssetDatabase.SaveAssets();
                AssignCatalogToSceneInstaller(catalog);
            }
        }

        private static PachimonCatalog GetOrCreateCatalog(Sprite front, Sprite back)
        {
            var existing = AssetDatabase.LoadAssetAtPath<PachimonCatalog>(CatalogPath);
            if (existing != null)
            {
                Debug.Log($"PachimonCatalog already exists at {CatalogPath}.", existing);
                return existing;
            }

            var species = new List<PachimonSpeciesDefinition>(PachimonCatalog.RequiredSpeciesCount);
            for (var speciesId = 1; speciesId <= PachimonCatalog.RequiredSpeciesCount; speciesId++)
            {
                species.Add(new PachimonSpeciesDefinition(
                    speciesId,
                    $"パチモン{speciesId:D3}",
                    front,
                    back,
                    (AllocationType)(((speciesId - 1) % 8) + 1),
                    speciesId,
                    speciesId));
            }

            var catalog = ScriptableObject.CreateInstance<PachimonCatalog>();
            catalog.SetSpeciesForEditor(species);
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            return catalog;
        }

        private static Sprite GetOrCreatePlaceholderSprite(string fileName, bool isBack)
        {
            var assetPath = $"{PlaceholderFolder}/{fileName}";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            const int size = 48;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = Enumerable.Repeat(Color.clear, size * size).ToArray();
            var bodyColor = isBack
                ? new Color(0.24f, 0.52f, 0.47f, 1f)
                : new Color(0.88f, 0.48f, 0.20f, 1f);
            var detailColor = new Color(0.12f, 0.15f, 0.14f, 1f);

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x - 24f;
                    var dy = y - 23f;
                    if ((dx * dx) / 225f + (dy * dy) / 289f <= 1f)
                    {
                        pixels[(y * size) + x] = bodyColor;
                    }

                    if (!isBack && y >= 25 && y <= 29 && (x == 18 || x == 30))
                    {
                        pixels[(y * size) + x] = detailColor;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(assetPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = size;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static void ApplyAvailableTypeGraphics(PachimonCatalog catalog)
        {
            PrepareIllustrationSprite(PachigidaneFrontPath);
            PrepareIllustrationSprite(PachigidaneBackPath);
            PrepareIllustrationSprite(PachikageFrontPath);
            PrepareIllustrationSprite(PachikageBackPath);
            PrepareIllustrationSprite(PachigameFrontPath);
            PrepareIllustrationSprite(PachigameBackPath);
            PrepareIllustrationSprite(PachichuFrontPath);
            PrepareIllustrationSprite(PachichuBackPath);
            PrepareIllustrationSprite(PachimushiFrontPath);
            PrepareIllustrationSprite(PachimushiBackPath);
            PrepareIllustrationSprite(PachigooriFrontPath);
            PrepareIllustrationSprite(PachigooriBackPath);
            PrepareIllustrationSprite(PachikazeFrontPath);
            PrepareIllustrationSprite(PachikazeBackPath);
            PrepareIllustrationSprite(PachidragonFrontPath);
            PrepareIllustrationSprite(PachidragonBackPath);

            var pachigidaneFront = AssetDatabase.LoadAssetAtPath<Sprite>(PachigidaneFrontPath);
            var pachigidaneBack = AssetDatabase.LoadAssetAtPath<Sprite>(PachigidaneBackPath);
            var pachikageFront = AssetDatabase.LoadAssetAtPath<Sprite>(PachikageFrontPath);
            var pachikageBack = AssetDatabase.LoadAssetAtPath<Sprite>(PachikageBackPath);
            var pachigameFront = AssetDatabase.LoadAssetAtPath<Sprite>(PachigameFrontPath);
            var pachigameBack = AssetDatabase.LoadAssetAtPath<Sprite>(PachigameBackPath);
            var pachichuFront = AssetDatabase.LoadAssetAtPath<Sprite>(PachichuFrontPath);
            var pachichuBack = AssetDatabase.LoadAssetAtPath<Sprite>(PachichuBackPath);
            var pachimushiFront = AssetDatabase.LoadAssetAtPath<Sprite>(PachimushiFrontPath);
            var pachimushiBack = AssetDatabase.LoadAssetAtPath<Sprite>(PachimushiBackPath);
            var pachigooriFront = AssetDatabase.LoadAssetAtPath<Sprite>(PachigooriFrontPath);
            var pachigooriBack = AssetDatabase.LoadAssetAtPath<Sprite>(PachigooriBackPath);
            var pachikazeFront = AssetDatabase.LoadAssetAtPath<Sprite>(PachikazeFrontPath);
            var pachikazeBack = AssetDatabase.LoadAssetAtPath<Sprite>(PachikazeBackPath);
            var pachidragonFront = AssetDatabase.LoadAssetAtPath<Sprite>(PachidragonFrontPath);
            var pachidragonBack = AssetDatabase.LoadAssetAtPath<Sprite>(PachidragonBackPath);
            if (pachigidaneFront == null || pachigidaneBack == null)
            {
                Debug.LogError("Pachigidane Front / Back graphics are missing.");
                return;
            }

            var fallbackGraphics = new Dictionary<AllocationType, (Sprite Front, Sprite Back)>
            {
                [AllocationType.Fire] = (pachikageFront, pachikageBack),
                [AllocationType.Aqua] = (pachigameFront, pachigameBack),
                [AllocationType.Electric] = (pachichuFront, pachichuBack),
                [AllocationType.Poison] = (pachimushiFront, pachimushiBack),
                [AllocationType.Ice] = (pachigooriFront, pachigooriBack),
                [AllocationType.Wind] = (pachikazeFront, pachikazeBack),
                [AllocationType.Dragon] = (pachidragonFront, pachidragonBack),
            };

            var changed = false;
            foreach (var definition in catalog.Species.Where(item => item != null))
            {
                if (!TryLoadSpeciesGraphics(
                        definition.SpeciesId,
                        out var front,
                        out var back))
                {
                    if (!fallbackGraphics.TryGetValue(
                            definition.AllocationType,
                            out var fallback)
                        || fallback.Front == null
                        || fallback.Back == null)
                    {
                        fallback = (pachigidaneFront, pachigidaneBack);
                    }

                    front = fallback.Front;
                    back = fallback.Back;
                }

                changed |= catalog.SetSpeciesGraphicsForEditor(
                    definition.SpeciesId,
                    front,
                    back);
            }

            if (!changed)
            {
                return;
            }

            EditorUtility.SetDirty(catalog);
            Debug.Log(
                "Available species graphics and type placeholders were assigned.",
                catalog);
        }

        private static bool TryLoadSpeciesGraphics(
            int speciesId,
            out Sprite front,
            out Sprite back)
        {
            front = null;
            back = null;
            const string artFolder = "Assets/Art/Pachimon";
            var folderPrefix = $"Species{speciesId:D3}_";
            var speciesFolder = Directory
                .GetDirectories(artFolder, folderPrefix + "*", SearchOption.TopDirectoryOnly)
                .OrderBy(path => path)
                .FirstOrDefault();
            if (string.IsNullOrEmpty(speciesFolder))
            {
                return false;
            }

            var frontPath = Directory
                .GetFiles(speciesFolder, "*_front.png", SearchOption.TopDirectoryOnly)
                .OrderBy(path => path)
                .FirstOrDefault();
            var backPath = Directory
                .GetFiles(speciesFolder, "*_back.png", SearchOption.TopDirectoryOnly)
                .OrderBy(path => path)
                .FirstOrDefault();
            if (string.IsNullOrEmpty(frontPath) || string.IsNullOrEmpty(backPath))
            {
                return false;
            }

            frontPath = frontPath.Replace('\\', '/');
            backPath = backPath.Replace('\\', '/');
            PrepareIllustrationSprite(frontPath);
            PrepareIllustrationSprite(backPath);
            front = AssetDatabase.LoadAssetAtPath<Sprite>(frontPath);
            back = AssetDatabase.LoadAssetAtPath<Sprite>(backPath);
            return front != null && back != null;
        }

        private static void PrepareIllustrationSprite(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            var requiresReimport = importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single
                || !Mathf.Approximately(importer.spritePixelsPerUnit, 512f)
                || !importer.alphaIsTransparency
                || importer.mipmapEnabled
                || importer.filterMode != FilterMode.Bilinear
                || importer.textureCompression != TextureImporterCompression.Uncompressed
                || importer.maxTextureSize != 1024;
            if (!requiresReimport)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 512f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 1024;
            importer.SaveAndReimport();
        }

        private static void ValidateCatalog(PachimonCatalog catalog)
        {
            if (catalog == null)
            {
                Debug.LogError("PachimonCatalog is missing. Create the placeholder catalog first.");
                return;
            }

            var errors = catalog.ValidateContent();
            if (errors.Count == 0)
            {
                Debug.Log($"PachimonCatalog is valid: {catalog.Species.Count} species.", catalog);
                return;
            }

            Debug.LogError("PachimonCatalog validation failed:\n" + string.Join("\n", errors), catalog);
        }

        private static void MigrateMissingLogicIds(PachimonCatalog catalog)
        {
            if (catalog == null || !catalog.PopulateMissingLogicIdsForEditor())
            {
                return;
            }

            EditorUtility.SetDirty(catalog);
            Debug.Log(
                "Missing fixed Skill / Passive IDs were initialized from Species IDs.",
                catalog);
        }

        private static void MigrateMissingAllocationTypes(PachimonCatalog catalog)
        {
            if (catalog == null || !catalog.PopulateMissingAllocationTypesForEditor())
            {
                return;
            }

            EditorUtility.SetDirty(catalog);
            Debug.Log(
                "Missing Allocation Types were distributed evenly from Species IDs.",
                catalog);
        }

        private static void ResetDefaultDisplayNames(PachimonCatalog catalog)
        {
            if (catalog == null || !catalog.ResetDefaultDisplayNamesForEditor())
            {
                return;
            }

            EditorUtility.SetDirty(catalog);
            Debug.Log(
                "Pachimon display names were reset to their defaults.",
                catalog);
        }

        private static void MigrateGeneratedDisplayNames(PachimonCatalog catalog)
        {
            if (catalog == null || !catalog.MigrateGeneratedDisplayNamesForEditor())
            {
                return;
            }

            EditorUtility.SetDirty(catalog);
            Debug.Log(
                "Generated Pachimon display names were migrated without changing custom names.",
                catalog);
        }

        private static void AssignCatalogToSceneInstaller(PachimonCatalog catalog)
        {
            var installer = Object.FindAnyObjectByType<GameSceneInstaller>(FindObjectsInactive.Include);
            if (installer == null)
            {
                Debug.LogWarning("GameSceneInstaller was not found. Assign PachimonCatalog with GameScene open.");
                return;
            }

            Undo.RecordObject(installer, "Assign Pachimon Catalog");
            if (!installer.ConfigurePachimonCatalog(catalog))
            {
                return;
            }

            EditorUtility.SetDirty(installer);
            EditorSceneManager.MarkSceneDirty(installer.gameObject.scene);
            Debug.Log("PachimonCatalog assigned to GameSceneInstaller.", installer);
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
