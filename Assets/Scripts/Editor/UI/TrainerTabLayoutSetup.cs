using System;
using Pachimon.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.Editor.UI
{
    public static class TrainerTabLayoutSetup
    {
        private const string MenuPath = "Tools/Pachimon/UI/Upgrade Trainer Tab Layout";

        [MenuItem(MenuPath)]
        private static void UpgradeFromMenu()
        {
            var battleWindows = UnityEngine.Object.FindObjectsByType<BattleNodeWindowView>(
                FindObjectsInactive.Include);
            if (battleWindows.Length == 0)
            {
                Debug.LogError("BattleNodeWindowView was not found in the open Scene.");
                return;
            }

            Undo.SetCurrentGroupName("Upgrade Trainer Tab Layout");
            var undoGroup = Undo.GetCurrentGroup();
            foreach (var battleWindow in battleWindows)
            {
                var trainerTabTransform = battleWindow.transform.Find("TabContent/TrainerTab");
                if (trainerTabTransform == null)
                {
                    Debug.LogError("TrainerTab was not found under BattleNodeWindow/TabContent.", battleWindow);
                    continue;
                }

                var trainerTab = trainerTabTransform.GetComponent<TrainerTabView>()
                    ?? Undo.AddComponent<TrainerTabView>(trainerTabTransform.gameObject);
                Rebuild(trainerTab);
                Undo.RecordObject(battleWindow, "Configure Trainer Tab");
                battleWindow.ConfigureTrainerTab(trainerTab);
                EditorUtility.SetDirty(battleWindow);
            }

            EditorSceneManager.MarkSceneDirty(battleWindows[0].gameObject.scene);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = battleWindows[0].gameObject;
            Debug.Log($"Upgraded {battleWindows.Length} Trainer tab layouts.", battleWindows[0]);
        }

        public static void Rebuild(TrainerTabView tab)
        {
            if (tab == null) throw new ArgumentNullException(nameof(tab));
            var scrollRect = tab.GetComponent<ScrollRect>();
            if (scrollRect == null)
            {
                Debug.LogError($"{tab.name} requires ScrollRect.", tab);
                return;
            }

            var viewport = scrollRect.viewport != null
                ? scrollRect.viewport
                : tab.transform.Find("Viewport") as RectTransform;
            if (viewport == null)
            {
                Debug.LogError($"{tab.name} requires Viewport.", tab);
                return;
            }

            for (var index = viewport.childCount - 1; index >= 0; index--)
            {
                Undo.DestroyObjectImmediate(viewport.GetChild(index).gameObject);
            }

            var content = CreateObject(
                viewport,
                "Content",
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            var contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = Vector2.one;
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;
            var contentLayout = content.GetComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(14, 14, 14, 20);
            contentLayout.spacing = 12f;
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.content = contentRect;

            var graphicArea = CreateObject(
                content.transform,
                "GraphicArea",
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement));
            graphicArea.GetComponent<Image>().color = GameUiPalette.Transparent;
            SetPreferredHeight(graphicArea, 300f);
            var graphic = CreateObject(
                graphicArea.transform,
                "TrainerGraphic",
                typeof(CanvasRenderer),
                typeof(Image));
            var graphicRect = (RectTransform)graphic.transform;
            graphicRect.anchorMin = Vector2.zero;
            graphicRect.anchorMax = Vector2.one;
            graphicRect.offsetMin = new Vector2(10f, 10f);
            graphicRect.offsetMax = new Vector2(-10f, -10f);
            var graphicImage = graphic.GetComponent<Image>();
            graphicImage.preserveAspect = true;
            graphicImage.raycastTarget = false;

            var name = CreateText(content.transform, "TrainerName", 24f, FontStyles.Bold);
            name.text = "TrainerのName";
            name.alignment = TextAlignmentOptions.Center;
            name.textWrappingMode = TextWrappingModes.Normal;
            SetPreferredHeight(name.gameObject, 48f);

            var rewardSection = CreateObject(
                content.transform,
                "RewardSection",
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(HorizontalLayoutGroup),
                typeof(LayoutElement));
            rewardSection.GetComponent<Image>().color = GameUiPalette.Card;
            SetPreferredHeight(rewardSection, 88f);
            var rewardLayout = rewardSection.GetComponent<HorizontalLayoutGroup>();
            rewardLayout.padding = new RectOffset(12, 12, 8, 8);
            rewardLayout.spacing = 10f;
            rewardLayout.childAlignment = TextAnchor.MiddleLeft;
            rewardLayout.childControlWidth = true;
            rewardLayout.childControlHeight = true;
            rewardLayout.childForceExpandWidth = false;
            rewardLayout.childForceExpandHeight = true;
            var rewardLabel = CreateText(rewardSection.transform, "Label", 18f, FontStyles.Bold);
            rewardLabel.text = "報酬：";
            rewardLabel.alignment = TextAlignmentOptions.MidlineLeft;
            SetPreferredWidth(rewardLabel.gameObject, 62f);

            var iconContainer = CreateObject(
                rewardSection.transform,
                "RewardIcons",
                typeof(HorizontalLayoutGroup),
                typeof(LayoutElement));
            iconContainer.GetComponent<LayoutElement>().flexibleWidth = 1f;
            var iconLayout = iconContainer.GetComponent<HorizontalLayoutGroup>();
            iconLayout.spacing = 8f;
            iconLayout.childAlignment = TextAnchor.MiddleLeft;
            iconLayout.childControlWidth = true;
            iconLayout.childControlHeight = true;
            iconLayout.childForceExpandWidth = false;
            iconLayout.childForceExpandHeight = false;
            var emptyReward = CreateText(iconContainer.transform, "EmptyReward", 20f, FontStyles.Bold);
            emptyReward.text = "---";
            emptyReward.alignment = TextAlignmentOptions.Center;
            SetPreferredSize(emptyReward.gameObject, 68f, 64f);
            var rewardTemplate = CreateRewardIcon(iconContainer.transform);
            rewardTemplate.gameObject.SetActive(false);

            var goldSection = CreateObject(
                content.transform,
                "GoldSection",
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(HorizontalLayoutGroup),
                typeof(LayoutElement));
            goldSection.GetComponent<Image>().color = GameUiPalette.GoldCard;
            SetPreferredHeight(goldSection, 54f);
            var goldLayout = goldSection.GetComponent<HorizontalLayoutGroup>();
            goldLayout.padding = new RectOffset(12, 14, 6, 6);
            goldLayout.spacing = 10f;
            goldLayout.childAlignment = TextAnchor.MiddleLeft;
            goldLayout.childControlWidth = true;
            goldLayout.childControlHeight = true;
            goldLayout.childForceExpandWidth = false;
            goldLayout.childForceExpandHeight = true;
            var goldLabel = CreateText(goldSection.transform, "Label", 19f, FontStyles.Bold);
            goldLabel.text = "Gold：";
            SetPreferredWidth(goldLabel.gameObject, 72f);
            var goldValue = CreateText(goldSection.transform, "Value", 22f, FontStyles.Bold);
            goldValue.text = "0";
            goldValue.alignment = TextAlignmentOptions.MidlineLeft;
            goldValue.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            Undo.RecordObject(tab, "Configure Trainer Tab");
            tab.Configure(
                graphicImage,
                name,
                iconContainer.transform,
                rewardTemplate,
                emptyReward,
                goldValue);
            EditorUtility.SetDirty(tab);
        }

        private static TrainerRewardIconView CreateRewardIcon(Transform parent)
        {
            var root = CreateObject(
                parent,
                "RewardIconTemplate",
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Outline),
                typeof(LayoutElement),
                typeof(TrainerRewardIconView));
            var background = root.GetComponent<Image>();
            background.color = new Color(0.35f, 0.39f, 0.44f, 1f);
            var outline = root.GetComponent<Outline>();
            outline.effectColor = GameUiPalette.Border;
            outline.effectDistance = new Vector2(1f, -1f);
            SetPreferredSize(root, 68f, 64f);

            var icon = CreateObject(root.transform, "Icon", typeof(CanvasRenderer), typeof(Image));
            Stretch((RectTransform)icon.transform, 6f);
            var iconImage = icon.GetComponent<Image>();
            iconImage.enabled = false;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            var label = CreateText(root.transform, "FallbackLabel", 12f, FontStyles.Bold);
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.Normal;
            Stretch((RectTransform)label.transform, 3f);

            var view = root.GetComponent<TrainerRewardIconView>();
            view.Configure(background, iconImage, label);
            return view;
        }

        private static TextMeshProUGUI CreateText(
            Transform parent,
            string objectName,
            float fontSize,
            FontStyles fontStyle)
        {
            var target = CreateObject(parent, objectName, typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            var text = target.GetComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = GameUiPalette.PrimaryText;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreateObject(Transform parent, string objectName, params Type[] components)
        {
            var target = new GameObject(objectName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(target, $"Create {objectName}");
            Undo.SetTransformParent(target.transform, parent, $"Parent {objectName}");
            target.layer = parent.gameObject.layer;
            foreach (var component in components) Undo.AddComponent(target, component);
            return target;
        }

        private static void SetPreferredHeight(GameObject target, float height)
        {
            var layout = target.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(target);
            layout.minHeight = 0f;
            layout.preferredHeight = height;
            layout.flexibleHeight = 0f;
        }

        private static void SetPreferredWidth(GameObject target, float width)
        {
            var layout = target.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(target);
            layout.minWidth = 0f;
            layout.preferredWidth = width;
            layout.flexibleWidth = 0f;
        }

        private static void SetPreferredSize(GameObject target, float width, float height)
        {
            SetPreferredWidth(target, width);
            SetPreferredHeight(target, height);
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }
    }
}
