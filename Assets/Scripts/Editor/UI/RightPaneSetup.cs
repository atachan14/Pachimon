using Pachimon.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.Editor.UI
{
    public static class RightPaneSetup
    {
        private const string MenuPath = "Tools/Pachimon/UI/Setup Right Pane Windows";

        [MenuItem(MenuPath)]
        private static void SetupFromMenu()
        {
            var rightPane = Object.FindAnyObjectByType<RightPaneView>(FindObjectsInactive.Include);
            if (rightPane == null)
            {
                Debug.LogError("RightPaneView was not found in the open Scene.");
                return;
            }

            if (rightPane.NodeSelectionWindow != null)
            {
                Selection.activeGameObject = rightPane.NodeSelectionWindow.gameObject;
                Debug.Log("RightPane windows are already configured.", rightPane);
                return;
            }

            Undo.SetCurrentGroupName("Setup Right Pane Windows");
            var undoGroup = Undo.GetCurrentGroup();
            IsolatePaneWidths(rightPane.transform.parent);

            var selectionRoot = CreateObject(
                rightPane.transform,
                "NodeSelectionWindow",
                typeof(NodeSelectionWindowView),
                typeof(VerticalLayoutGroup));
            Stretch((RectTransform)selectionRoot.transform);
            var selectionLayout = selectionRoot.GetComponent<VerticalLayoutGroup>();
            ConfigureVerticalLayout(selectionLayout, 0f, 0);

            var windowHost = CreateObject(selectionRoot.transform, "WindowHost", typeof(LayoutElement));
            SetFlexibleHeight(windowHost, 1f);

            var battleWindow = CreateBattleWindow(windowHost.transform);
            var simpleWindow = CreateSimpleWindow(windowHost.transform);

            var footer = CreateObject(
                selectionRoot.transform,
                "SelectionFooter",
                typeof(HorizontalLayoutGroup),
                typeof(LayoutElement));
            SetPreferredHeight(footer, 64f);
            var footerLayout = footer.GetComponent<HorizontalLayoutGroup>();
            footerLayout.spacing = 12f;
            footerLayout.padding = new RectOffset(12, 12, 8, 8);
            footerLayout.childAlignment = TextAnchor.MiddleCenter;
            footerLayout.childControlWidth = true;
            footerLayout.childControlHeight = true;
            footerLayout.childForceExpandWidth = true;
            footerLayout.childForceExpandHeight = true;
            var cancelButton = CreateButton(footer.transform, "CancelButton", "キャンセル", false);
            var confirmButton = CreateButton(footer.transform, "ConfirmButton", "決定", true);

            var selectionView = selectionRoot.GetComponent<NodeSelectionWindowView>();
            selectionView.Configure(battleWindow, simpleWindow, footer, confirmButton, cancelButton);
            Undo.RecordObject(rightPane, "Configure Right Pane");
            rightPane.Initialize(selectionView);

            EditorUtility.SetDirty(rightPane);
            EditorUtility.SetDirty(selectionView);
            EditorSceneManager.MarkSceneDirty(rightPane.gameObject.scene);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = selectionRoot;
            Debug.Log("RightPane window hierarchy is ready.", rightPane);
        }

        private static BattleNodeWindowView CreateBattleWindow(Transform parent)
        {
            var root = CreateObject(parent, "BattleNodeWindow", typeof(BattleNodeWindowView), typeof(VerticalLayoutGroup));
            Stretch((RectTransform)root.transform);
            ConfigureVerticalLayout(root.GetComponent<VerticalLayoutGroup>(), 8f, 12);

            var tabBar = CreateObject(root.transform, "TabBar", typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            SetPreferredHeight(tabBar, 52f);
            var tabLayout = tabBar.GetComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 6f;
            tabLayout.childControlWidth = true;
            tabLayout.childControlHeight = true;
            tabLayout.childForceExpandWidth = true;
            tabLayout.childForceExpandHeight = true;

            var tabButtons = new[]
            {
                CreateButton(tabBar.transform, "TrainerTabButton", "Trainer", true),
                CreateButton(tabBar.transform, "Pachimon1TabButton", "P1", false),
                CreateButton(tabBar.transform, "Pachimon2TabButton", "P2", false),
                CreateButton(tabBar.transform, "Pachimon3TabButton", "P3", false),
            };

            var tabContent = CreateObject(root.transform, "TabContent", typeof(LayoutElement));
            SetFlexibleHeight(tabContent, 1f);
            var trainerPanel = CreateScrollTextPanel(tabContent.transform, "TrainerTab", out _);
            var trainerTab = Undo.AddComponent<TrainerTabView>(trainerPanel);
            TrainerTabLayoutSetup.Rebuild(trainerTab);

            var tabPanels = new GameObject[4];
            var pachimonTabs = new PachimonTabView[3];
            tabPanels[0] = trainerPanel;
            for (var index = 0; index < 3; index++)
            {
                var panel = CreateScrollTextPanel(
                    tabContent.transform,
                    $"Pachimon{index + 1}Tab",
                    out var detailsText);
                var tab = Undo.AddComponent<PachimonTabView>(panel);
                PachimonTabLayoutSetup.Rebuild(tab);
                tabPanels[index + 1] = panel;
                pachimonTabs[index] = tab;
            }

            var view = root.GetComponent<BattleNodeWindowView>();
            view.Configure(tabButtons, tabPanels, trainerTab, pachimonTabs);
            return view;
        }

        private static SimpleNodeWindowView CreateSimpleWindow(Transform parent)
        {
            var root = CreateObject(parent, "SimpleNodeWindow", typeof(SimpleNodeWindowView), typeof(VerticalLayoutGroup));
            Stretch((RectTransform)root.transform);
            ConfigureVerticalLayout(root.GetComponent<VerticalLayoutGroup>(), 8f, 12);

            var title = CreateText(root.transform, "Title", 26f, FontStyles.Bold);
            SetPreferredHeight(title.gameObject, 52f);
            var bodyPanel = CreateScrollTextPanel(root.transform, "DetailsScroll", out var details);
            SetFlexibleHeight(bodyPanel, 1f);

            var view = root.GetComponent<SimpleNodeWindowView>();
            view.Configure(title, details);
            return view;
        }

        private static GameObject CreateScrollTextPanel(
            Transform parent,
            string objectName,
            out TextMeshProUGUI detailsText)
        {
            var root = CreateObject(parent, objectName, typeof(ScrollRect));
            Stretch((RectTransform)root.transform);

            var viewport = CreateObject(root.transform, "Viewport", typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
            Stretch((RectTransform)viewport.transform);
            var viewportImage = viewport.GetComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.001f);
            viewportImage.raycastTarget = true;

            detailsText = CreateText(viewport.transform, "Content", 19f, FontStyles.Normal);
            detailsText.alignment = TextAlignmentOptions.TopLeft;
            detailsText.textWrappingMode = TextWrappingModes.Normal;
            detailsText.margin = new Vector4(12f, 12f, 12f, 12f);
            var contentRect = (RectTransform)detailsText.transform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = Vector2.one;
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;
            var fitter = Undo.AddComponent<ContentSizeFitter>(detailsText.gameObject);
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollRect = root.GetComponent<ScrollRect>();
            scrollRect.viewport = (RectTransform)viewport.transform;
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 24f;
            return root;
        }

        private static Button CreateButton(Transform parent, string objectName, string label, bool accent)
        {
            var target = CreateObject(parent, objectName, typeof(CanvasRenderer), typeof(Image), typeof(Button));
            var image = target.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.color = accent
                ? GameUiPalette.ButtonAccent
                : GameUiPalette.ButtonNeutral;
            var button = target.GetComponent<Button>();
            button.targetGraphic = image;
            var text = CreateText(target.transform, "Label", 18f, FontStyles.Bold);
            text.text = label;
            text.color = GameUiPalette.OnAccentText;
            text.alignment = TextAlignmentOptions.Center;
            Stretch((RectTransform)text.transform);
            return button;
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

        private static GameObject CreateObject(Transform parent, string objectName, params System.Type[] components)
        {
            var target = new GameObject(objectName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(target, $"Create {objectName}");
            Undo.SetTransformParent(target.transform, parent, $"Parent {objectName}");
            target.layer = parent.gameObject.layer;
            foreach (var component in components) Undo.AddComponent(target, component);
            return target;
        }

        private static void IsolatePaneWidths(Transform content)
        {
            if (content == null) return;
            foreach (Transform pane in content)
            {
                if (!pane.TryGetComponent<LayoutElement>(out var layout)) continue;
                Undo.RecordObject(layout, "Isolate Pane Width");
                layout.minWidth = 0f;
                layout.preferredWidth = 0f;
                EditorUtility.SetDirty(layout);
            }
        }

        private static void ConfigureVerticalLayout(VerticalLayoutGroup layout, float spacing, int padding)
        {
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private static void SetPreferredHeight(GameObject target, float height)
        {
            var layout = target.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(target);
            layout.minHeight = 0f;
            layout.preferredHeight = height;
            layout.flexibleHeight = 0f;
        }

        private static void SetFlexibleHeight(GameObject target, float flexibleHeight)
        {
            var layout = target.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(target);
            layout.minHeight = 0f;
            layout.preferredHeight = 0f;
            layout.flexibleHeight = flexibleHeight;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
