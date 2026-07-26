using Pachimon.UI;
using Pachimon.Trainer;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.Editor.UI
{
    public static class MapScrollViewSetup
    {
        private const string MenuPath = "Tools/Pachimon/UI/Setup Map Scroll View";
        private const string PrefabFolder = "Assets/Prefabs/UI/Map";
        private const string NodePrefabPath = PrefabFolder + "/MapNodeView.prefab";
        private const string CityPrefabPath = PrefabFolder + "/CityMapNodeView.prefab";
        private const string EdgePrefabPath = PrefabFolder + "/MapEdgeView.prefab";
        private const string CityMapIconPath =
            "Assets/Art/Map/Nodes/City/city_map_icon_112.png";
        private const string EventRingIconPath =
            "Assets/Art/Map/Nodes/Event/event_ring_256.png";
        private const string RestSpotRingIconPath =
            "Assets/Art/Map/Nodes/RestSpot/rest_spot_ring_256.png";
        private const string TrainerMapIconSetPath =
            "Assets/GameData/Trainer/TrainerMapIconSet.asset";
        private const string TrainerMapIconCatalogPath =
            "Assets/GameData/Trainer/TrainerMapIconCatalog.asset";

        [MenuItem(MenuPath)]
        private static void SetupSelectedMapOverlay()
        {
            var mapOverlayView = FindMapOverlayView();
            if (mapOverlayView == null)
            {
                EditorUtility.DisplayDialog(
                    "Map Scroll View Setup",
                    "MapOverlayViewがScene内に見つかりませんでした。",
                    "OK");
                return;
            }

            Undo.SetCurrentGroupName("Setup Map Scroll View");
            var undoGroup = Undo.GetCurrentGroup();

            var scrollRoot = GetOrCreateChild(mapOverlayView.transform, "MapScrollRect");
            SetStretch(scrollRoot);

            var scrollViewport = GetOrCreateChild(scrollRoot, "ScrollViewport");
            SetStretch(scrollViewport);
            EnsureComponent<RectMask2D>(scrollViewport.gameObject);
            var viewportImage = EnsureComponent<Image>(scrollViewport.gameObject);
            viewportImage.color = Color.clear;
            viewportImage.raycastTarget = true;

            var mapContent = GetOrCreateChild(scrollViewport, "MapContent");
            SetBottomStretch(mapContent);

            var edgeLayer = GetOrCreateChild(mapContent, "EdgeLayer");
            SetStretch(edgeLayer);

            var nodeLayer = GetOrCreateChild(mapContent, "NodeLayer");
            SetStretch(nodeLayer);

            var scrollRect = EnsureComponent<ScrollRect>(scrollRoot.gameObject);
            scrollRect.content = mapContent;
            scrollRect.viewport = scrollViewport;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;
            scrollRect.scrollSensitivity = 30f;

            EnsureAssetFolder(PrefabFolder);
            var nodePrefab = GetOrCreateNodePrefab();
            var cityPrefab = GetOrCreateCityPrefab();
            var edgePrefab = GetOrCreateEdgePrefab();
            var trainerMapIconSet = AssetDatabase.LoadAssetAtPath<TrainerMapIconSet>(
                TrainerMapIconSetPath);
            var trainerMapIconCatalog = AssetDatabase.LoadAssetAtPath<TrainerMapIconCatalog>(
                TrainerMapIconCatalogPath);

            Undo.RecordObject(mapOverlayView, "Configure Map Overlay View");
            mapOverlayView.ConfigureMapScrollView(
                scrollRect,
                scrollViewport,
                mapContent,
                edgeLayer,
                nodeLayer);
            mapOverlayView.ConfigureMapPrefabs(
                nodePrefab,
                edgePrefab,
                cityPrefab,
                trainerMapIconSet,
                trainerMapIconCatalog);

            EditorUtility.SetDirty(mapOverlayView);
            EditorUtility.SetDirty(scrollRect);
            EditorUtility.SetDirty(viewportImage);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = scrollRoot.gameObject;

            Debug.Log("Map Scroll View hierarchy is ready.", mapOverlayView);
        }

        private static MapNodeView GetOrCreateNodePrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<MapNodeView>(NodePrefabPath);
            var isExistingPrefab = existing != null;
            var root = isExistingPrefab
                ? PrefabUtility.LoadPrefabContents(NodePrefabPath)
                : new GameObject(
                    "MapNodeView",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button),
                    typeof(Outline),
                    typeof(MapNodeView));
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.zero;
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(56f, 56f);

            var background = EnsureComponent<Image>(root);
            background.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            background.type = Image.Type.Sliced;

            var button = EnsureComponent<Button>(root);
            button.targetGraphic = background;

            var outline = EnsureComponent<Outline>(root);
            outline.useGraphicAlpha = true;
            outline.enabled = false;

            var labelTransform = root.transform.Find("Label");
            var labelObject = labelTransform != null
                ? labelTransform.gameObject
                : new GameObject(
                    "Label",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI));
            if (labelTransform == null)
            {
                labelObject.transform.SetParent(root.transform, false);
            }

            var labelRect = (RectTransform)labelObject.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var label = EnsureComponent<TextMeshProUGUI>(labelObject);
            label.text = "B";
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 22f;
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(1f, 0.96f, 0.84f, 1f);
            label.raycastTarget = false;

            var gymRoleFrame = GetOrCreateGymRoleFrame(root.transform);
            var selectionFrame = GetOrCreateTrainerSelectionFrame(root.transform);
            var trainerIcon = GetOrCreateTrainerIcon(root.transform);
            GetOrCreateSymbolIcon(
                root.transform,
                out var symbolRing,
                out var symbolOutline,
                out var eventRingSprite,
                out var restSpotRingSprite);
            EnsureComponent<MapNodeView>(root).Configure(
                background,
                label,
                button,
                outline,
                trainerIcon,
                selectionFrame,
                gymRoleFrame,
                symbolRing,
                symbolOutline,
                eventRingSprite,
                restSpotRingSprite);
            PrefabUtility.SaveAsPrefabAsset(root, NodePrefabPath);

            if (isExistingPrefab)
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            else
            {
                Object.DestroyImmediate(root);
            }

            return AssetDatabase.LoadAssetAtPath<MapNodeView>(NodePrefabPath);
        }

        private static void GetOrCreateSymbolIcon(
            Transform parent,
            out Image symbolRing,
            out Outline symbolOutline,
            out Sprite eventRingSprite,
            out Sprite restSpotRingSprite)
        {
            PrepareMapIconSprite(EventRingIconPath, 256f, FilterMode.Bilinear);
            PrepareMapIconSprite(RestSpotRingIconPath, 256f, FilterMode.Bilinear);

            var iconTransform = parent.Find("SymbolRing");
            if (iconTransform == null)
            {
                iconTransform = parent.Find("EventRing");
                if (iconTransform != null)
                {
                    iconTransform.name = "SymbolRing";
                }
            }
            var iconObject = iconTransform != null
                ? iconTransform.gameObject
                : new GameObject(
                    "SymbolRing",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Outline));
            if (iconTransform == null)
            {
                iconObject.transform.SetParent(parent, false);
            }

            SetStretch((RectTransform)iconObject.transform);
            iconObject.transform.SetAsFirstSibling();

            eventRingSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EventRingIconPath);
            restSpotRingSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RestSpotRingIconPath);
            symbolRing = EnsureComponent<Image>(iconObject);
            symbolRing.sprite = eventRingSprite;
            symbolRing.type = Image.Type.Simple;
            symbolRing.preserveAspect = true;
            symbolRing.color = Color.white;
            symbolRing.raycastTarget = false;

            symbolOutline = EnsureComponent<Outline>(iconObject);
            symbolOutline.useGraphicAlpha = true;
            symbolOutline.enabled = false;
            iconObject.SetActive(false);
        }

        private static Image[] GetOrCreateGymRoleFrame(Transform parent)
        {
            var frameTransform = parent.Find("GymRoleFrame");
            var frameObject = frameTransform != null
                ? frameTransform.gameObject
                : new GameObject("GymRoleFrame", typeof(RectTransform));
            if (frameTransform == null)
            {
                frameObject.transform.SetParent(parent, false);
            }

            SetStretch((RectTransform)frameObject.transform);
            frameObject.transform.SetAsFirstSibling();
            return new[]
            {
                GetOrCreateOctagonPart(frameObject.transform, "Top", new Vector2(0f, 25f), new Vector2(28f, 2f), 0f),
                GetOrCreateOctagonPart(frameObject.transform, "TopRight", new Vector2(18f, 18f), new Vector2(16f, 2f), -45f),
                GetOrCreateOctagonPart(frameObject.transform, "Right", new Vector2(25f, 0f), new Vector2(28f, 2f), 90f),
                GetOrCreateOctagonPart(frameObject.transform, "BottomRight", new Vector2(18f, -18f), new Vector2(16f, 2f), 45f),
                GetOrCreateOctagonPart(frameObject.transform, "Bottom", new Vector2(0f, -25f), new Vector2(28f, 2f), 0f),
                GetOrCreateOctagonPart(frameObject.transform, "BottomLeft", new Vector2(-18f, -18f), new Vector2(16f, 2f), -45f),
                GetOrCreateOctagonPart(frameObject.transform, "Left", new Vector2(-25f, 0f), new Vector2(28f, 2f), 90f),
                GetOrCreateOctagonPart(frameObject.transform, "TopLeft", new Vector2(-18f, 18f), new Vector2(16f, 2f), 45f),
            };
        }

        private static Image GetOrCreateOctagonPart(
            Transform parent,
            string partName,
            Vector2 position,
            Vector2 size,
            float rotation)
        {
            var partTransform = parent.Find(partName);
            var partObject = partTransform != null
                ? partTransform.gameObject
                : new GameObject(
                    partName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Outline));
            if (partTransform == null)
            {
                partObject.transform.SetParent(parent, false);
            }

            var rect = (RectTransform)partObject.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localEulerAngles = new Vector3(0f, 0f, rotation);

            var image = EnsureComponent<Image>(partObject);
            image.color = new Color32(210, 151, 45, 255);
            image.raycastTarget = false;
            image.enabled = false;

            var outline = EnsureComponent<Outline>(partObject);
            outline.effectColor = new Color32(58, 42, 24, 255);
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
            return image;
        }

        private static Image[] GetOrCreateTrainerSelectionFrame(Transform parent)
        {
            var frameTransform = parent.Find("TrainerSelectionFrame");
            var frameObject = frameTransform != null
                ? frameTransform.gameObject
                : new GameObject("TrainerSelectionFrame", typeof(RectTransform));
            if (frameTransform == null)
            {
                frameObject.transform.SetParent(parent, false);
            }

            SetStretch((RectTransform)frameObject.transform);
            frameObject.transform.SetAsFirstSibling();
            return new[]
            {
                GetOrCreateFramePart(frameObject.transform, "Top", true, true),
                GetOrCreateFramePart(frameObject.transform, "Bottom", true, false),
                GetOrCreateFramePart(frameObject.transform, "Left", false, true),
                GetOrCreateFramePart(frameObject.transform, "Right", false, false),
            };
        }

        private static Image GetOrCreateFramePart(
            Transform parent,
            string partName,
            bool isHorizontal,
            bool isMinimumSide)
        {
            var partTransform = parent.Find(partName);
            var partObject = partTransform != null
                ? partTransform.gameObject
                : new GameObject(partName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            if (partTransform == null)
            {
                partObject.transform.SetParent(parent, false);
            }

            const float inset = 1f;
            const float thickness = 2f;
            var rect = (RectTransform)partObject.transform;
            if (isHorizontal)
            {
                rect.anchorMin = new Vector2(0f, isMinimumSide ? 1f : 0f);
                rect.anchorMax = new Vector2(1f, isMinimumSide ? 1f : 0f);
                rect.pivot = new Vector2(0.5f, isMinimumSide ? 1f : 0f);
                rect.offsetMin = new Vector2(inset, isMinimumSide ? -inset - thickness : inset);
                rect.offsetMax = new Vector2(-inset, isMinimumSide ? -inset : inset + thickness);
            }
            else
            {
                rect.anchorMin = new Vector2(isMinimumSide ? 0f : 1f, 0f);
                rect.anchorMax = new Vector2(isMinimumSide ? 0f : 1f, 1f);
                rect.pivot = new Vector2(isMinimumSide ? 0f : 1f, 0.5f);
                rect.offsetMin = new Vector2(isMinimumSide ? inset : -inset - thickness, inset);
                rect.offsetMax = new Vector2(isMinimumSide ? inset + thickness : -inset, -inset);
            }

            var image = EnsureComponent<Image>(partObject);
            image.raycastTarget = false;
            image.enabled = false;
            return image;
        }

        private static TrainerMapIconView GetOrCreateTrainerIcon(Transform parent)
        {
            var iconTransform = parent.Find("TrainerMapIcon");
            var iconObject = iconTransform != null
                ? iconTransform.gameObject
                : new GameObject("TrainerMapIcon", typeof(RectTransform), typeof(TrainerMapIconView));
            if (iconTransform == null)
            {
                iconObject.transform.SetParent(parent, false);
            }

            SetStretch((RectTransform)iconObject.transform);
            var baseImage = GetOrCreateIconLayer(iconObject.transform, "Base");
            var secondary = GetOrCreateIconLayer(iconObject.transform, "Secondary", "Bottoms");
            var primary = GetOrCreateIconLayer(iconObject.transform, "Primary", "Tops");
            var detail = GetOrCreateIconLayer(iconObject.transform, "Detail");
            var iconView = EnsureComponent<TrainerMapIconView>(iconObject);
            iconView.Configure(baseImage, primary, secondary, detail);
            iconObject.SetActive(false);
            return iconView;
        }

        private static Image GetOrCreateIconLayer(
            Transform parent,
            string layerName,
            string legacyLayerName = null)
        {
            var layerTransform = parent.Find(layerName);
            if (layerTransform == null && legacyLayerName != null)
            {
                layerTransform = parent.Find(legacyLayerName);
                if (layerTransform != null)
                {
                    layerTransform.name = layerName;
                }
            }
            var layerObject = layerTransform != null
                ? layerTransform.gameObject
                : new GameObject(layerName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            if (layerTransform == null)
            {
                layerObject.transform.SetParent(parent, false);
            }

            var layerRect = (RectTransform)layerObject.transform;
            SetStretch(layerRect);
            layerRect.offsetMin = Vector2.zero;
            layerRect.offsetMax = Vector2.zero;

            var image = EnsureComponent<Image>(layerObject);
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static MapEdgeView GetOrCreateEdgePrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<MapEdgeView>(EdgePrefabPath);
            if (existing != null)
            {
                return existing;
            }

            var root = new GameObject(
                "MapEdgeView",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(MapEdgeView));
            var rectTransform = (RectTransform)root.transform;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0f, 0.5f);
            rectTransform.sizeDelta = new Vector2(100f, 5f);

            var image = root.GetComponent<Image>();
            image.color = new Color(0.18f, 0.22f, 0.21f, 0.48f);
            image.raycastTarget = false;
            root.GetComponent<MapEdgeView>().Configure(image);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, EdgePrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<MapEdgeView>();
        }

        private static CityMapNodeView GetOrCreateCityPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<CityMapNodeView>(CityPrefabPath);
            var isExistingPrefab = existing != null;
            var root = isExistingPrefab
                ? PrefabUtility.LoadPrefabContents(CityPrefabPath)
                : new GameObject(
                    "CityMapNodeView",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button),
                    typeof(Outline),
                    typeof(CityMapNodeView));
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.zero;
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(112f, 56f);

            PrepareMapIconSprite(CityMapIconPath, 112f);
            var background = EnsureComponent<Image>(root);
            background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CityMapIconPath);
            background.type = Image.Type.Simple;
            background.preserveAspect = true;
            background.color = Color.white;
            background.raycastTarget = true;

            var button = EnsureComponent<Button>(root);
            button.targetGraphic = background;

            var outline = EnsureComponent<Outline>(root);
            outline.useGraphicAlpha = true;
            outline.enabled = false;

            var labelTransform = root.transform.Find("Label");
            var labelObject = labelTransform != null
                ? labelTransform.gameObject
                : new GameObject(
                    "Label",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI));
            if (labelTransform == null)
            {
                labelObject.transform.SetParent(root.transform, false);
            }
            var labelRect = (RectTransform)labelObject.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = string.Empty;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 18f;
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(1f, 0.96f, 0.84f, 1f);
            label.raycastTarget = false;
            labelObject.SetActive(false);

            root.GetComponent<CityMapNodeView>().Configure(background, label, button, outline);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, CityPrefabPath);
            if (isExistingPrefab)
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            else
            {
                Object.DestroyImmediate(root);
            }
            return prefab.GetComponent<CityMapNodeView>();
        }

        private static void PrepareMapIconSprite(
            string assetPath,
            float pixelsPerUnit,
            FilterMode filterMode = FilterMode.Point)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = filterMode;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static void EnsureAssetFolder(string path)
        {
            var parts = path.Split('/');
            var currentPath = parts[0];

            for (var index = 1; index < parts.Length; index++)
            {
                var nextPath = $"{currentPath}/{parts[index]}";
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, parts[index]);
                }

                currentPath = nextPath;
            }
        }

        private static MapOverlayView FindMapOverlayView()
        {
            var selected = Selection.activeGameObject;
            if (selected != null)
            {
                var selectedOverlay = selected.GetComponentInParent<MapOverlayView>(true);
                if (selectedOverlay != null)
                {
                    return selectedOverlay;
                }
            }

            return Object.FindAnyObjectByType<MapOverlayView>(FindObjectsInactive.Include);
        }

        private static RectTransform GetOrCreateChild(Transform parent, string childName)
        {
            var existing = parent.Find(childName) as RectTransform;
            if (existing != null)
            {
                return existing;
            }

            var child = new GameObject(childName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(child, $"Create {childName}");
            Undo.SetTransformParent(child.transform, parent, $"Parent {childName}");
            child.layer = parent.gameObject.layer;
            return (RectTransform)child.transform;
        }

        private static T EnsureComponent<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            return component != null ? component : Undo.AddComponent<T>(target);
        }

        private static void SetStretch(RectTransform rectTransform)
        {
            Undo.RecordObject(rectTransform, "Stretch RectTransform");
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.localScale = Vector3.one;
        }

        private static void SetBottomStretch(RectTransform rectTransform)
        {
            Undo.RecordObject(rectTransform, "Configure Map Content RectTransform");
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.right;
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(0f, Mathf.Max(100f, rectTransform.sizeDelta.y));
            rectTransform.localScale = Vector3.one;
        }
    }
}
