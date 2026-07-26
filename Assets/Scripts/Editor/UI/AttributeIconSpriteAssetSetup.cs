using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.TextCore;

namespace Pachimon.Editor.UI
{
    public static class AttributeIconSpriteAssetSetup
    {
        private const string AtlasPath =
            "Assets/Art/UI/AttributeIcons/AttributeIcons.png";
        private const string AssetFolder =
            "Assets/Resources/Sprite Assets";
        private const string AssetPath =
            AssetFolder + "/AttributeIcons.asset";
        private const int CellSize = 256;
        private const float IconScale = 1.3f;
        private const float VerticalBearingRatio = 0.82f;

        private static readonly string[] IconNames =
        {
            "Fire",
            "Aqua",
            "Leaf",
            "Electric",
            "Poison",
            "Ice",
            "Wind",
            "Dragon",
        };

        [InitializeOnLoadMethod]
        private static void CreateMissingAssetAfterReload()
        {
            EditorApplication.delayCall += () =>
            {
                var existing =
                    AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(AssetPath);
                if (!EditorApplication.isPlayingOrWillChangePlaymode
                    && (existing == null
                        || existing.spriteCharacterTable.Count != IconNames.Length
                        || existing.spriteGlyphTable.Count != IconNames.Length
                        || HasLegacyCenteredMetrics(existing)))
                {
                    Rebuild();
                }
            };
        }

        private static bool HasLegacyCenteredMetrics(TMP_SpriteAsset spriteAsset)
        {
            return spriteAsset.spriteGlyphTable.Any(glyph =>
                !Mathf.Approximately(glyph.metrics.horizontalBearingX, 0f)
                || !Mathf.Approximately(
                    glyph.metrics.horizontalBearingY,
                    glyph.metrics.height * VerticalBearingRatio)
                || !Mathf.Approximately(glyph.scale, IconScale));
        }

        [MenuItem("Tools/Pachimon/UI/Rebuild Attribute Icon Sprite Asset")]
        public static void Rebuild()
        {
            EnsureAssetFolder(AssetFolder);
            ConfigureAtlasImporter();

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
            var sprites = AssetDatabase.LoadAllAssetsAtPath(AtlasPath)
                .OfType<Sprite>()
                .ToDictionary(sprite => sprite.name);
            if (texture == null || IconNames.Any(name => !sprites.ContainsKey(name)))
            {
                Debug.LogError(
                    "Attribute icon atlas could not be imported with all eight sprites.");
                return;
            }

            var spriteAsset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(AssetPath);
            var isNewAsset = spriteAsset == null;
            if (isNewAsset)
            {
                spriteAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
                AssetDatabase.CreateAsset(spriteAsset, AssetPath);
            }

            SetCurrentSpriteAssetVersion(spriteAsset);
            spriteAsset.hashCode = TMP_TextUtilities.GetSimpleHashCode("AttributeIcons");
            spriteAsset.spriteSheet = texture;
            var glyphs = BuildGlyphTable(sprites);
            spriteAsset.spriteGlyphTable.Clear();
            spriteAsset.spriteGlyphTable.AddRange(glyphs);
            spriteAsset.spriteCharacterTable.Clear();
            spriteAsset.spriteCharacterTable.AddRange(
                BuildCharacterTable(glyphs));
            ConfigureMaterial(spriteAsset, texture);
            spriteAsset.UpdateLookupTables();

            EditorUtility.SetDirty(spriteAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(AssetPath);
            Debug.Log($"Rebuilt TMP Attribute Icon Sprite Asset: {AssetPath}", spriteAsset);
        }

        private static void SetCurrentSpriteAssetVersion(
            TMP_SpriteAsset spriteAsset)
        {
            var serializedAsset = new SerializedObject(spriteAsset);
            var version = serializedAsset.FindProperty("m_Version");
            if (version != null)
            {
                version.stringValue = "1.1.0";
                serializedAsset.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void ConfigureAtlasImporter()
        {
            AssetDatabase.ImportAsset(AtlasPath, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(AtlasPath) is not TextureImporter importer)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
            ConfigureSpriteRects(importer);
        }

        private static void ConfigureSpriteRects(TextureImporter importer)
        {
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            var provider =
                factories.GetSpriteEditorDataProviderFromObject(importer);
            if (provider == null)
            {
                throw new MissingReferenceException(
                    "Sprite Editor data provider was not available.");
            }

            provider.InitSpriteEditorDataProvider();
            var existingIds = provider.GetSpriteRects()
                .ToDictionary(rect => rect.name, rect => rect.spriteID);
            var spriteRects = new SpriteRect[IconNames.Length];
            for (var index = 0; index < IconNames.Length; index++)
            {
                var column = index % 4;
                var topRow = index / 4;
                spriteRects[index] = new SpriteRect
                {
                    name = IconNames[index],
                    rect = new Rect(
                        column * CellSize,
                        (1 - topRow) * CellSize,
                        CellSize,
                        CellSize),
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    spriteID = existingIds.TryGetValue(
                        IconNames[index],
                        out var existingId)
                        ? existingId
                        : GUID.Generate(),
                };
            }

            provider.SetSpriteRects(spriteRects);
            provider.Apply();
            AssetDatabase.ImportAsset(AtlasPath, ImportAssetOptions.ForceUpdate);
        }

        private static List<TMP_SpriteGlyph> BuildGlyphTable(
            IReadOnlyDictionary<string, Sprite> sprites)
        {
            var glyphs = new List<TMP_SpriteGlyph>(IconNames.Length);
            for (var index = 0; index < IconNames.Length; index++)
            {
                var sprite = sprites[IconNames[index]];
                glyphs.Add(new TMP_SpriteGlyph
                {
                    index = (uint)index,
                    metrics = new GlyphMetrics(
                        sprite.rect.width,
                        sprite.rect.height,
                        0f,
                        sprite.rect.height * VerticalBearingRatio,
                        sprite.rect.width),
                    glyphRect = new GlyphRect(sprite.rect),
                    scale = IconScale,
                    sprite = sprite,
                });
            }

            return glyphs;
        }

        private static List<TMP_SpriteCharacter> BuildCharacterTable(
            IReadOnlyList<TMP_SpriteGlyph> glyphs)
        {
            var characters = new List<TMP_SpriteCharacter>(IconNames.Length);
            for (var index = 0; index < IconNames.Length; index++)
            {
                characters.Add(new TMP_SpriteCharacter(0xFFFE, glyphs[index])
                {
                    name = IconNames[index],
                    scale = 1f,
                });
            }

            return characters;
        }

        private static void ConfigureMaterial(
            TMP_SpriteAsset spriteAsset,
            Texture2D texture)
        {
            var material = spriteAsset.material;
            if (material == null)
            {
                material = new Material(Shader.Find("TextMeshPro/Sprite"))
                {
                    name = "AttributeIcons Material",
                };
                AssetDatabase.AddObjectToAsset(material, spriteAsset);
                spriteAsset.material = material;
            }

            material.SetTexture(ShaderUtilities.ID_MainTex, texture);
            EditorUtility.SetDirty(material);
        }

        private static void EnsureAssetFolder(string folderPath)
        {
            var parts = folderPath.Split('/');
            var current = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }

                current = next;
            }
        }
    }
}
