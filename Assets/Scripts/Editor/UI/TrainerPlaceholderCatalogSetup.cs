using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Pachimon.Trainer;
using Pachimon.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Pachimon.Editor.UI
{
    public static class TrainerPlaceholderCatalogSetup
    {
        private const string MenuRoot = "Tools/Pachimon/Data/";
        private const string DataFolder = "Assets/GameData/Trainer";
        private const string SpriteFolder = DataFolder + "/Placeholder";
        private const string NormalMapIconSpriteFolder = "Assets/Art/Trainers/MapIcon/Layers56";
        private const string GymLeaderMapIconSpriteFolder =
            "Assets/Art/Trainers/GymLeaderMapIcon/Layers56";
        private const string StyleCatalogPath = DataFolder + "/TrainerStyleCatalog.asset";
        private const string NameCatalogPath = DataFolder + "/TrainerNameCatalog.asset";
        private const string MapIconSetPath = DataFolder + "/TrainerMapIconSet.asset";
        private const string GymLeaderMapIconSetPath = DataFolder + "/GymLeaderMapIconSet.asset";
        private const string MapIconCatalogPath = DataFolder + "/TrainerMapIconCatalog.asset";

        private static readonly ThemeTitles[] ContentTitles =
        {
            new(TrainerTheme.Fire,
                new[] { "炎上中", "晴れ男" },
                new[] { "燃える女", "放火犯" }),
            new(TrainerTheme.Aqua,
                new[] { "ビーチボーイ", "トイレ掃除" },
                new[] { "スクール水着", "トイレ掃除" }),
            new(TrainerTheme.Leaf,
                new[] { "植物学者", "庭師" },
                new[] { "森ガール", "ヴィーガン" }),
            new(TrainerTheme.Electric,
                new[] { "電気工事士", "ビリビリマン" },
                new[] { "避雷針", "ビリビリガール" }),
            new(TrainerTheme.Poison,
                new[] { "虫取り少年", "ツイッタラー" },
                new[] { "地雷系", "オカルト研究部" }),
            new(TrainerTheme.Ice,
                new[] { "スキーヤー", "冷蔵庫" },
                new[] { "スキーヤー", "雪だるま" }),
            new(TrainerTheme.Wind,
                new[] { "旅人", "パラグライダー" },
                new[] { "旅人", "ジェットコースター" }),
            new(TrainerTheme.Dragon,
                new[] { "ドラゴン使い", "マニア" },
                new[] { "ドラゴンの母", "生き物係" }),
            new(TrainerTheme.Speed,
                new[] { "せっかち男", "陸上部" },
                new[] { "せっかち女", "スプリンター" }),
            new(TrainerTheme.MaxMn,
                new[] { "時短マスター", "時計職人" },
                new[] { "タイパ女子", "時計職人" }),
            new(TrainerTheme.MaxHp,
                new[] { "ボディビルダー", "大食い" },
                new[] { "フィットネス女子", "大食い" }),
            new(TrainerTheme.DamageBonus,
                new[] { "格闘家", "破壊王" },
                new[] { "格闘家", "クラッシャー" }),
            new(TrainerTheme.ResistBonus,
                new[] { "警備員", "我慢強い男" },
                new[] { "警備員", "鉄壁ガール" }),
            new(TrainerTheme.Gold,
                new[] { "ジェントルマン", "御曹司" },
                new[] { "お嬢様", "セレブ" }),
        };

        private static readonly string[] MaleNames =
        {
            "タクヤ", "ケンタ", "ショウ", "ユウタ", "リョウ", "ダイキ", "ハルト", "ソウタ",
            "コウタ", "レン", "カズキ", "ヒロト", "アキラ", "シンジ", "マサル", "ツヨシ",
            "タケル", "ナオキ", "ユウジ", "コウジ", "ケンジ", "シゲル", "ノボル", "ミノル",
            "イサム", "マコト", "トオル", "ジュン", "レオ", "カイ", "リク", "ソラ",
            "ゲン", "ゴウ", "ジョージ", "マイケル", "ジョン", "トム", "ボブ", "ケビン",
            "デイビッド", "ダニエル", "ルイス", "カルロス", "パブロ", "マルコ", "ルカ", "ニコ",
            "オスカー", "エリック", "アレックス", "サム", "ロビン", "クリス", "ミゲル", "ラウル",
            "イワン", "ユーリ", "アーロン", "ノア", "リアム", "フィン", "テオ", "ロイド",
        };

        private static readonly string[] FemaleNames =
        {
            "ヨシコ", "サヤカ", "アヤカ", "ミサキ", "ユイ", "アオイ", "サクラ", "ナナ",
            "ミオ", "リン", "メイ", "ヒナ", "ユナ", "カナ", "マリ", "エリ",
            "ユリ", "リナ", "レイ", "アイ", "マイ", "ナオ", "アキ", "ハルカ",
            "チヒロ", "カオリ", "ミドリ", "アスカ", "ノゾミ", "ミライ", "ルナ", "ソラ",
            "エマ", "オリビア", "ソフィア", "ミア", "エミリー", "アリス", "ルーシー", "アンナ",
            "マリア", "ローラ", "サラ", "リリー", "クロエ", "ゾーイ", "エラ", "グレース",
            "ハンナ", "クレア", "ジュリア", "エレナ", "カルラ", "ニーナ", "ナタリア", "イリーナ",
            "レイラ", "アミラ", "ノラ", "エヴァ", "フレイヤ", "ステラ", "テレサ", "リタ",
        };

        [InitializeOnLoadMethod]
        private static void AssignExistingCatalogsAfterScriptReload()
        {
            EditorApplication.delayCall += TryAssignExistingCatalogs;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                TryAssignExistingCatalogs();
            }
        }

        private static void TryAssignExistingCatalogs()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var styleCatalog = AssetDatabase.LoadAssetAtPath<TrainerStyleCatalog>(StyleCatalogPath);
            var nameCatalog = AssetDatabase.LoadAssetAtPath<TrainerNameCatalog>(NameCatalogPath);
            if (styleCatalog != null && nameCatalog != null)
            {
                AssignCatalogsToSceneInstaller(styleCatalog, nameCatalog);
            }
        }

        [MenuItem(MenuRoot + "Create Trainer Placeholder Catalogs")]
        private static void CreateCatalogs()
        {
            EnsureAssetFolder(SpriteFolder);
            CreateOrUpdateMapIconAssets();
            var battleGraphic = GetOrCreateSprite("trainer_battle.png", PlaceholderLayer.Battle);
            var styleCatalog = GetOrCreateStyleCatalog(battleGraphic);
            var nameCatalog = GetOrCreateNameCatalog();
            AssignCatalogsToSceneInstaller(styleCatalog, nameCatalog);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidateCatalog(styleCatalog, nameCatalog);
            Selection.activeObject = styleCatalog;
        }

        [MenuItem(MenuRoot + "Apply Trainer Map Icons")]
        private static void ApplyTrainerMapIcons()
        {
            var catalog = CreateOrUpdateMapIconAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = catalog;
            Debug.Log("Applied the 56px Trainer Map Icon layers by role.", catalog);
        }

        [MenuItem(MenuRoot + "Validate Trainer Catalogs")]
        private static void ValidateCatalogs()
        {
            var styleCatalog = AssetDatabase.LoadAssetAtPath<TrainerStyleCatalog>(StyleCatalogPath);
            var nameCatalog = AssetDatabase.LoadAssetAtPath<TrainerNameCatalog>(NameCatalogPath);
            ValidateCatalog(styleCatalog, nameCatalog);
        }

        [MenuItem(MenuRoot + "Apply Trainer Content Data")]
        private static void ApplyContentDataFromMenu()
        {
            var styleCatalog = AssetDatabase.LoadAssetAtPath<TrainerStyleCatalog>(StyleCatalogPath);
            var nameCatalog = AssetDatabase.LoadAssetAtPath<TrainerNameCatalog>(NameCatalogPath);
            if (styleCatalog == null || nameCatalog == null)
            {
                Debug.LogError("Trainer catalogs are missing. Create placeholder catalogs first.");
                return;
            }

            EnsureAssetFolder(SpriteFolder);
            CreateOrUpdateMapIconAssets();
            var fallbackBattleGraphic = GetOrCreateSprite(
                "trainer_battle.png",
                PlaceholderLayer.Battle);
            ApplyContentData(styleCatalog, nameCatalog, fallbackBattleGraphic);
            AssetDatabase.SaveAssets();
            ValidateCatalog(styleCatalog, nameCatalog);
            Selection.activeObject = styleCatalog;
        }

        private static void ApplyContentData(
            TrainerStyleCatalog styleCatalog,
            TrainerNameCatalog nameCatalog,
            Sprite fallbackBattleGraphic)
        {
            var previousStyles = styleCatalog.Styles.Where(style => style != null).ToArray();
            var fallbackGraphic = previousStyles
                .Select(style => style.BattleGraphic)
                .FirstOrDefault(graphic => graphic != null)
                ?? fallbackBattleGraphic;
            var styles = previousStyles
                .Where(style => style.StyleCategory == TrainerStyleCategory.League)
                .Select(style => new TrainerStyleDefinition(
                    style.StyleId,
                    style.Theme,
                    style.Gender,
                    style.StyleCategory,
                    style.NormalTitle,
                    style.BattleGraphic != null ? style.BattleGraphic : fallbackGraphic))
                .ToList();

            foreach (var titles in ContentTitles)
            {
                AddNormalStyles(
                    styles,
                    previousStyles,
                    fallbackGraphic,
                    titles.Theme,
                    TrainerGender.Male,
                    titles.Male);
                AddNormalStyles(
                    styles,
                    previousStyles,
                    fallbackGraphic,
                    titles.Theme,
                    TrainerGender.Female,
                    titles.Female);
            }

            var names = MaleNames.Select((displayName, index) => new TrainerNameDefinition(
                    $"male_{index + 1:D2}",
                    TrainerGender.Male,
                    displayName))
                .Concat(FemaleNames.Select((displayName, index) => new TrainerNameDefinition(
                    $"female_{index + 1:D2}",
                    TrainerGender.Female,
                    displayName)))
                .ToArray();

            Undo.RecordObject(styleCatalog, "Apply Trainer Style Content");
            Undo.RecordObject(nameCatalog, "Apply Trainer Name Content");
            styleCatalog.SetStylesForEditor(styles);
            nameCatalog.SetNamesForEditor(names);
            EditorUtility.SetDirty(styleCatalog);
            EditorUtility.SetDirty(nameCatalog);
            Debug.Log(
                $"Applied Trainer content: {styles.Count} styles / {names.Length} names.",
                styleCatalog);
        }

        private static void AddNormalStyles(
            ICollection<TrainerStyleDefinition> destination,
            IReadOnlyList<TrainerStyleDefinition> previousStyles,
            Sprite fallbackGraphic,
            TrainerTheme theme,
            TrainerGender gender,
            IReadOnlyList<string> titles)
        {
            var graphicCandidates = previousStyles
                .Where(style => style.StyleCategory == TrainerStyleCategory.Normal
                    && style.Theme == theme
                    && style.Gender == gender
                    && style.BattleGraphic != null)
                .Select(style => style.BattleGraphic)
                .ToArray();
            if (graphicCandidates.Length == 0)
            {
                graphicCandidates = previousStyles
                    .Where(style => style.StyleCategory == TrainerStyleCategory.Normal
                        && style.Gender == gender
                        && style.BattleGraphic != null)
                    .Select(style => style.BattleGraphic)
                    .ToArray();
            }

            for (var index = 0; index < titles.Count; index++)
            {
                var battleGraphic = graphicCandidates.Length > 0
                    ? graphicCandidates[index % graphicCandidates.Length]
                    : fallbackGraphic;
                destination.Add(new TrainerStyleDefinition(
                    $"normal_{GetIdPart(theme)}_{gender.ToString().ToLowerInvariant()}_{index + 1:D2}",
                    theme,
                    gender,
                    TrainerStyleCategory.Normal,
                    titles[index],
                    battleGraphic));
            }
        }

        private static TrainerStyleCatalog GetOrCreateStyleCatalog(Sprite battleGraphic)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TrainerStyleCatalog>(StyleCatalogPath);
            if (existing != null)
            {
                Debug.Log($"TrainerStyleCatalog already exists at {StyleCatalogPath}.", existing);
                return existing;
            }

            var styles = new List<TrainerStyleDefinition>();
            var allThemes = (TrainerTheme[])Enum.GetValues(typeof(TrainerTheme));
            for (var index = 0; index < allThemes.Length; index++)
            {
                var theme = allThemes[index];
                styles.Add(new TrainerStyleDefinition(
                    $"normal_{GetIdPart(theme)}_01",
                    theme,
                    index % 2 == 0 ? TrainerGender.Male : TrainerGender.Female,
                    TrainerStyleCategory.Normal,
                    $"{theme} Trainer",
                    battleGraphic));
            }

            foreach (var theme in TrainerThemeUtility.AttributeThemes)
            {
                for (var index = 0; index < 4; index++)
                {
                    styles.Add(new TrainerStyleDefinition(
                        $"league_{GetIdPart(theme)}_{index + 1:D2}",
                        theme,
                        index % 2 == 0 ? TrainerGender.Male : TrainerGender.Female,
                        TrainerStyleCategory.League,
                        string.Empty,
                        battleGraphic));
                }
            }

            var catalog = ScriptableObject.CreateInstance<TrainerStyleCatalog>();
            catalog.SetStylesForEditor(styles);
            AssetDatabase.CreateAsset(catalog, StyleCatalogPath);
            return catalog;
        }

        private static TrainerNameCatalog GetOrCreateNameCatalog()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TrainerNameCatalog>(NameCatalogPath);
            if (existing != null)
            {
                Debug.Log($"TrainerNameCatalog already exists at {NameCatalogPath}.", existing);
                return existing;
            }

            var names = new List<TrainerNameDefinition>();
            for (var index = 1; index <= 64; index++)
            {
                names.Add(new TrainerNameDefinition(
                    $"male_{index:D2}",
                    TrainerGender.Male,
                    $"Male {index:D2}"));
                names.Add(new TrainerNameDefinition(
                    $"female_{index:D2}",
                    TrainerGender.Female,
                    $"Female {index:D2}"));
            }

            var catalog = ScriptableObject.CreateInstance<TrainerNameCatalog>();
            catalog.SetNamesForEditor(names);
            AssetDatabase.CreateAsset(catalog, NameCatalogPath);
            return catalog;
        }

        private static TrainerMapIconCatalog CreateOrUpdateMapIconAssets()
        {
            var normalLayers = LoadRuntimeMapIconLayers(
                NormalMapIconSpriteFolder,
                "trainer_map_icon");
            var gymLeaderLayers = LoadRuntimeMapIconLayers(
                GymLeaderMapIconSpriteFolder,
                "gym_leader_map_icon");
            var normal = GetOrCreateMapIconSet(MapIconSetPath, normalLayers);
            var gymLeader = GetOrCreateMapIconSet(GymLeaderMapIconSetPath, gymLeaderLayers);
            return GetOrCreateMapIconCatalog(normal, gymLeader);
        }

        private static TrainerVisualLayers LoadRuntimeMapIconLayers(
            string spriteFolder,
            string filePrefix)
        {
            var baseSprite = LoadMapIconSprite(spriteFolder, $"{filePrefix}_base.png");
            var primary = LoadMapIconSprite(spriteFolder, $"{filePrefix}_primary.png");
            var secondary = LoadMapIconSprite(spriteFolder, $"{filePrefix}_secondary.png");
            var detail = LoadMapIconSprite(spriteFolder, $"{filePrefix}_detail.png");
            return new TrainerVisualLayers(baseSprite, primary, secondary, detail);
        }

        private static TrainerMapIconSet GetOrCreateMapIconSet(
            string assetPath,
            TrainerVisualLayers layers)
        {
            var iconSet = AssetDatabase.LoadAssetAtPath<TrainerMapIconSet>(assetPath);
            if (iconSet != null)
            {
                Undo.RecordObject(iconSet, "Apply Trainer Map Icon");
                iconSet.ConfigureForEditor(layers);
                EditorUtility.SetDirty(iconSet);
                return iconSet;
            }

            iconSet = ScriptableObject.CreateInstance<TrainerMapIconSet>();
            iconSet.ConfigureForEditor(layers);
            AssetDatabase.CreateAsset(iconSet, assetPath);
            return iconSet;
        }

        private static TrainerMapIconCatalog GetOrCreateMapIconCatalog(
            TrainerMapIconSet normal,
            TrainerMapIconSet gymLeader)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<TrainerMapIconCatalog>(MapIconCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<TrainerMapIconCatalog>();
                AssetDatabase.CreateAsset(catalog, MapIconCatalogPath);
            }

            Undo.RecordObject(catalog, "Apply Trainer Map Icon Catalog");
            catalog.ConfigureForEditor(normal, gymLeader, catalog.Elite);
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static Sprite LoadMapIconSprite(string spriteFolder, string fileName)
        {
            var assetPath = $"{spriteFolder}/{fileName}";
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new FileNotFoundException($"Trainer Map Icon layer was not found: {assetPath}");
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 56;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static Sprite GetOrCreateSprite(string fileName, PlaceholderLayer layer)
        {
            var assetPath = $"{SpriteFolder}/{fileName}";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = Enumerable.Repeat(Color.clear, size * size).ToArray();

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    pixels[(y * size) + x] = GetPixel(layer, x, y);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(assetPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
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

        private static Color GetPixel(PlaceholderLayer layer, int x, int y)
        {
            var isHead = x >= 11 && x <= 20 && y >= 21 && y <= 29;
            var isTorso = x >= 8 && x <= 23 && y >= 10 && y <= 21;
            var isLeftLeg = x >= 10 && x <= 14 && y >= 2 && y <= 11;
            var isRightLeg = x >= 17 && x <= 21 && y >= 2 && y <= 11;

            return layer switch
            {
                PlaceholderLayer.Battle when IsOutlinePixel(x, y) => new Color(0.12f, 0.12f, 0.12f, 1f),
                PlaceholderLayer.Battle when isHead => new Color(0.92f, 0.72f, 0.56f, 1f),
                PlaceholderLayer.Battle when isTorso => new Color(0.20f, 0.56f, 0.52f, 1f),
                PlaceholderLayer.Battle when isLeftLeg || isRightLeg => new Color(0.18f, 0.25f, 0.36f, 1f),
                _ => Color.clear,
            };
        }

        private static bool IsOutlinePixel(int x, int y)
        {
            var headOutline = (x == 10 || x == 21) && y >= 21 && y <= 29
                || (y == 20 || y == 30) && x >= 11 && x <= 20;
            var torsoOutline = (x == 7 || x == 24) && y >= 10 && y <= 21
                || (y == 9 || y == 22) && x >= 8 && x <= 23;
            var legOutline = (x == 9 || x == 15 || x == 16 || x == 22) && y >= 2 && y <= 11
                || (y == 1 && (x >= 10 && x <= 14 || x >= 17 && x <= 21));
            return headOutline || torsoOutline || legOutline;
        }

        private static void ValidateCatalog(
            TrainerStyleCatalog styleCatalog,
            TrainerNameCatalog nameCatalog)
        {
            if (styleCatalog == null || nameCatalog == null)
            {
                Debug.LogError("Trainer catalogs are missing. Create placeholder catalogs first.");
                return;
            }

            var errors = new List<string>(styleCatalog.ValidateMinimumContent());
            var mapIconCatalog = AssetDatabase.LoadAssetAtPath<TrainerMapIconCatalog>(
                MapIconCatalogPath);
            if (!HasCompleteLayers(mapIconCatalog?.Normal)
                || !HasCompleteLayers(mapIconCatalog?.GymLeader))
            {
                errors.Add(
                    "TrainerMapIconCatalog requires complete Normal and GymLeader icon sets.");
            }

            foreach (TrainerGender gender in Enum.GetValues(typeof(TrainerGender)))
            {
                var genderNames = nameCatalog.GetByGender(gender);
                if (genderNames.Count < 64)
                {
                    errors.Add(
                        $"Trainer names require at least 64 entries for {gender}, "
                        + $"but contain {genderNames.Count}.");
                }
            }

            foreach (var duplicateId in nameCatalog.Names
                         .Where(name => name != null)
                         .GroupBy(name => name.NameId)
                         .Where(group => group.Count() > 1)
                         .Select(group => group.Key))
            {
                errors.Add($"Duplicate TrainerName ID: {duplicateId}");
            }

            if (errors.Count == 0)
            {
                ValidateProfileFactory(styleCatalog, nameCatalog);
                Debug.Log(
                    $"Trainer catalogs are valid: {styleCatalog.Styles.Count} styles / "
                    + $"{nameCatalog.Names.Count} names.",
                    styleCatalog);
                return;
            }

            Debug.LogError("Trainer catalog validation failed:\n" + string.Join("\n", errors), styleCatalog);
        }

        private static bool HasCompleteLayers(TrainerMapIconSet iconSet)
        {
            return iconSet?.Layers != null
                && iconSet.Layers.Base != null
                && iconSet.Layers.Primary != null
                && iconSet.Layers.Secondary != null
                && iconSet.Layers.Detail != null;
        }

        private static void ValidateProfileFactory(
            TrainerStyleCatalog styleCatalog,
            TrainerNameCatalog nameCatalog)
        {
            for (var seed = 0; seed < 100; seed++)
            {
                var factory = new TrainerProfileFactory(
                    styleCatalog,
                    nameCatalog,
                    new System.Random(seed));

                foreach (TrainerTheme theme in Enum.GetValues(typeof(TrainerTheme)))
                {
                    ValidateProfile(factory.Create(TrainerRole.Normal, theme), styleCatalog, nameCatalog);
                }

                foreach (var theme in TrainerThemeUtility.AttributeThemes)
                {
                    for (var count = 0; count < 3; count++)
                    {
                        ValidateProfile(
                            factory.Create(TrainerRole.GymLeader, theme),
                            styleCatalog,
                            nameCatalog);
                    }
                }

                foreach (var theme in TrainerThemeUtility.AttributeThemes.Take(4))
                {
                    ValidateProfile(
                        factory.Create(TrainerRole.Elite, theme),
                        styleCatalog,
                        nameCatalog);
                }
            }
        }

        private static void ValidateProfile(
            TrainerProfile profile,
            TrainerStyleCatalog styleCatalog,
            TrainerNameCatalog nameCatalog)
        {
            var style = styleCatalog.Get(profile.StyleId)
                ?? throw new InvalidOperationException($"Style {profile.StyleId} was not found.");
            var name = nameCatalog.Get(profile.NameId)
                ?? throw new InvalidOperationException($"Name {profile.NameId} was not found.");
            if (style.Gender != name.Gender)
            {
                throw new InvalidOperationException(
                    $"Style {style.StyleId} and name {name.NameId} have different genders.");
            }
        }

        private static string GetIdPart(TrainerTheme theme) => theme.ToString().ToLowerInvariant();

        private static void AssignCatalogsToSceneInstaller(
            TrainerStyleCatalog styleCatalog,
            TrainerNameCatalog nameCatalog)
        {
            var installer = UnityEngine.Object.FindAnyObjectByType<GameSceneInstaller>(
                FindObjectsInactive.Include);
            if (installer == null)
            {
                Debug.LogWarning(
                    "GameSceneInstaller was not found. Assign Trainer catalogs when GameScene is open.");
                return;
            }

            Undo.RecordObject(installer, "Assign Trainer Catalogs");
            if (!installer.ConfigureTrainerCatalogs(styleCatalog, nameCatalog))
            {
                return;
            }

            EditorUtility.SetDirty(installer);
            EditorSceneManager.MarkSceneDirty(installer.gameObject.scene);
            Debug.Log("Trainer catalogs assigned to GameSceneInstaller.", installer);
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

        private enum PlaceholderLayer { Battle }

        private readonly struct ThemeTitles
        {
            public ThemeTitles(
                TrainerTheme theme,
                IReadOnlyList<string> male,
                IReadOnlyList<string> female)
            {
                Theme = theme;
                Male = male;
                Female = female;
            }

            public TrainerTheme Theme { get; }
            public IReadOnlyList<string> Male { get; }
            public IReadOnlyList<string> Female { get; }
        }
    }
}
