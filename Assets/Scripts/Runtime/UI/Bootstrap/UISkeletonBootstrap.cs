using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;

namespace Pachimon.UI
{
    public static class UISkeletonBootstrap
    {
        private const float CompactBreakpoint = 1100f;

        public static void EnsureGameUiBuilt()
        {
            if (UnityEngine.Object.FindFirstObjectByType<GameRootView>() != null)
            {
                return;
            }

            EnsureEventSystem();
            CreateGameRoot();
        }

        private static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            _ = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private static void CreateGameRoot()
        {
            var canvasObject = new GameObject("PachimonUiCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var root = canvasObject.AddComponent<GameRootView>();
            var canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.one;
            canvasRect.offsetMin = Vector2.zero;
            canvasRect.offsetMax = Vector2.zero;

            var header = CreateHeader(canvasRect);
            var headerRect = header.GetComponent<RectTransform>();
            var content = CreateContentArea(canvasRect);

            var leftPane = CreatePane<LeftPaneView>(content, "LeftPane", new Color(0.12f, 0.16f, 0.2f), "Left Pane", "Party info\n- party list\n- selected ally detail");
            var leftPaneRect = leftPane.GetComponent<RectTransform>();
            SetEdgePaneLayout(leftPaneRect, true, 260f);

            var rightPane = CreatePane<RightPaneView>(content, "RightPane", new Color(0.16f, 0.14f, 0.18f), "Right Pane", "Enemy / node detail\n- selected enemy\n- node info");
            var rightPaneRect = rightPane.GetComponent<RectTransform>();
            SetEdgePaneLayout(rightPaneRect, false, 260f);

            var mainPane = CreateMainPane(content);
            var mainRect = mainPane.GetComponent<RectTransform>();
            mainRect.anchorMin = new Vector2(0f, 0f);
            mainRect.anchorMax = new Vector2(1f, 1f);
            mainRect.offsetMin = new Vector2(280f, 0f);
            mainRect.offsetMax = new Vector2(-280f, 0f);

            var mapOverlay = CreateMapOverlay(content);
            mapOverlay.Close();

            var startScreen = CreateSimpleScreen<StartScreen>(mainPane.transform, "StartScreen", "Start Screen", "Initial pachimon selection");
            var cityScreen = CreateSimpleScreen<CityScreen>(mainPane.transform, "CityScreen", "City Screen", "Shop and town actions");
            var restSpotScreen = CreateSimpleScreen<RestSpotScreen>(mainPane.transform, "RestSpotScreen", "Rest Spot", "Recovery and return to map");
            var leagueGateScreen = CreateSimpleScreen<LeagueGateScreen>(mainPane.transform, "LeagueGateScreen", "League Gate", "League check and special defeat branch");
            var defeatScreen = CreateSimpleScreen<DefeatScreen>(mainPane.transform, "DefeatScreen", "Defeat Screen", "Defeat direction placeholder");
            var hallOfFameScreen = CreateSimpleScreen<HallOfFameScreen>(mainPane.transform, "HallOfFameScreen", "Hall Of Fame", "Hall of fame direction placeholder");
            var battleScreen = CreateBattleScreen(mainPane.transform);

            mainPane.RegisterScreen(startScreen);
            mainPane.RegisterScreen(battleScreen);
            mainPane.RegisterScreen(cityScreen);
            mainPane.RegisterScreen(restSpotScreen);
            mainPane.RegisterScreen(leagueGateScreen);
            mainPane.RegisterScreen(defeatScreen);
            mainPane.RegisterScreen(hallOfFameScreen);
            mainPane.Show(battleScreen);

            header.MapButton.onClick.AddListener(root.ToggleMapOverlay);

            root.Initialize(
                header,
                leftPane,
                mainPane,
                rightPane,
                mapOverlay,
                headerRect,
                content,
                leftPaneRect,
                mainRect,
                rightPaneRect,
                CompactBreakpoint);
        }

        private static HeaderView CreateHeader(RectTransform parent)
        {
            var headerObject = CreatePanel("Header", parent, new Color(0.09f, 0.11f, 0.14f));
            var rect = headerObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, 96f);
            rect.anchoredPosition = Vector2.zero;

            var header = headerObject.AddComponent<HeaderView>();

            var leftGroup = CreateLayoutGroup("HeaderLeft", rect, TextAnchor.MiddleLeft);
            leftGroup.anchorMin = new Vector2(0f, 0f);
            leftGroup.anchorMax = new Vector2(1f, 1f);
            leftGroup.offsetMin = new Vector2(12f, 12f);
            leftGroup.offsetMax = new Vector2(-456f, -12f);

            var rightGroup = CreateLayoutGroup("HeaderRight", rect, TextAnchor.MiddleRight);
            rightGroup.anchorMin = new Vector2(1f, 0f);
            rightGroup.anchorMax = new Vector2(1f, 1f);
            rightGroup.pivot = new Vector2(1f, 0.5f);
            rightGroup.sizeDelta = new Vector2(420f, 0f);
            rightGroup.anchoredPosition = new Vector2(-12f, 0f);

            var goldText = CreateText("GoldText", leftGroup, "Gold: 123", 24, TextAnchor.MiddleLeft);
            var stageText = CreateText("StageText", leftGroup, "Stage: 12", 24, TextAnchor.MiddleLeft);
            var badgeText = CreateText("BadgeText", leftGroup, "Badges: 3", 24, TextAnchor.MiddleLeft);

            var mapButton = CreateButton("MapButton", rightGroup, "Map");
            var itemButton = CreateButton("ItemButton", rightGroup, "Items");
            var settingsButton = CreateButton("SettingsButton", rightGroup, "Settings");

            header.Initialize(goldText, stageText, badgeText, mapButton, itemButton, settingsButton);
            return header;
        }

        private static RectTransform CreateContentArea(RectTransform parent)
        {
            var contentObject = new GameObject("Content", typeof(RectTransform));
            contentObject.transform.SetParent(parent, false);
            var rect = contentObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = new Vector2(0f, -96f);
            return rect;
        }

        private static T CreatePane<T>(RectTransform parent, string name, Color color, string title, string body)
            where T : MonoBehaviour
        {
            var paneObject = CreatePanel(name, parent, color);
            var titleText = CreateText("Title", paneObject.GetComponent<RectTransform>(), title, 30, TextAnchor.UpperLeft);
            var bodyText = CreateText("Body", paneObject.GetComponent<RectTransform>(), body, 22, TextAnchor.UpperLeft);

            var titleRect = titleText.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.offsetMin = new Vector2(16f, -56f);
            titleRect.offsetMax = new Vector2(-16f, -16f);

            var bodyRect = bodyText.rectTransform;
            bodyRect.anchorMin = new Vector2(0f, 0f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.offsetMin = new Vector2(16f, 16f);
            bodyRect.offsetMax = new Vector2(-16f, -72f);

            var pane = paneObject.AddComponent<T>();
            switch (pane)
            {
                case LeftPaneView leftPaneView:
                    leftPaneView.Initialize(titleText, bodyText);
                    break;
                case RightPaneView rightPaneView:
                    rightPaneView.Initialize(titleText, bodyText);
                    break;
            }

            return pane;
        }

        private static MainPaneView CreateMainPane(RectTransform parent)
        {
            var paneObject = CreatePanel("MainPane", parent, new Color(0.11f, 0.13f, 0.16f));
            return paneObject.AddComponent<MainPaneView>();
        }

        private static MapOverlayView CreateMapOverlay(RectTransform parent)
        {
            var overlayObject = CreatePanel("MapOverlay", parent, new Color(0.06f, 0.08f, 0.11f, 0.96f));
            var rect = overlayObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var overlay = overlayObject.AddComponent<MapOverlayView>();
            var titleText = CreateText("Title", rect, "Map Overlay", 36, TextAnchor.UpperCenter);
            var bodyText = CreateText(
                "Body",
                rect,
                "MainPane almost fully covered\n- node list\n- node detail\n- go button",
                24,
                TextAnchor.MiddleCenter);

            SetVerticalBlock(titleText.rectTransform, 1f, 72f, 16f);
            bodyText.rectTransform.anchorMin = new Vector2(0.1f, 0.22f);
            bodyText.rectTransform.anchorMax = new Vector2(0.9f, 0.78f);
            bodyText.rectTransform.offsetMin = Vector2.zero;
            bodyText.rectTransform.offsetMax = Vector2.zero;

            overlay.Initialize(titleText, bodyText);
            return overlay;
        }

        private static T CreateSimpleScreen<T>(Transform parent, string objectName, string title, string body)
            where T : NodeScreen
        {
            var screenObject = CreatePanel(objectName, parent as RectTransform, new Color(0.15f, 0.17f, 0.2f));
            var screen = screenObject.AddComponent<T>();
            screen.SetScreenName(objectName);

            var titleText = CreateText("Title", screenObject.GetComponent<RectTransform>(), title, 34, TextAnchor.UpperLeft);
            var bodyText = CreateText("Body", screenObject.GetComponent<RectTransform>(), body, 24, TextAnchor.MiddleCenter);

            titleText.rectTransform.anchorMin = new Vector2(0f, 1f);
            titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
            titleText.rectTransform.offsetMin = new Vector2(24f, -64f);
            titleText.rectTransform.offsetMax = new Vector2(-24f, -16f);

            bodyText.rectTransform.anchorMin = new Vector2(0.15f, 0.2f);
            bodyText.rectTransform.anchorMax = new Vector2(0.85f, 0.8f);
            bodyText.rectTransform.offsetMin = Vector2.zero;
            bodyText.rectTransform.offsetMax = Vector2.zero;

            return screen;
        }

        private static BattleScreen CreateBattleScreen(Transform parent)
        {
            var screenObject = CreatePanel("BattleScreen", parent as RectTransform, new Color(0.13f, 0.15f, 0.18f));
            var screen = screenObject.AddComponent<BattleScreen>();
            screen.SetScreenName("BattleScreen");
            var rect = screenObject.GetComponent<RectTransform>();

            var titleText = CreateText("Title", rect, "Battle Screen", 34, TextAnchor.UpperLeft);
            titleText.rectTransform.anchorMin = new Vector2(0f, 1f);
            titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
            titleText.rectTransform.offsetMin = new Vector2(24f, -64f);
            titleText.rectTransform.offsetMax = new Vector2(-24f, -16f);

            var battleMainObject = CreatePanel("BattleMain", rect, new Color(0.13f, 0.15f, 0.18f, 0f));
            var battleMainRect = battleMainObject.GetComponent<RectTransform>();
            battleMainRect.anchorMin = new Vector2(0f, 0f);
            battleMainRect.anchorMax = new Vector2(1f, 1f);
            battleMainRect.offsetMin = new Vector2(0f, 0f);
            battleMainRect.offsetMax = new Vector2(0f, 0f);
            var battleMainView = battleMainObject.AddComponent<BattleMainView>();

            var graphicWindow = CreatePanel("GraphicWindow", battleMainRect, new Color(0.19f, 0.21f, 0.26f));
            var graphicRect = graphicWindow.GetComponent<RectTransform>();
            graphicRect.anchorMin = new Vector2(0.04f, 0.42f);
            graphicRect.anchorMax = new Vector2(0.96f, 0.82f);
            graphicRect.offsetMin = Vector2.zero;
            graphicRect.offsetMax = Vector2.zero;

            var enemyAreaObject = CreatePanel("EnemyArea", graphicRect, new Color(0.17f, 0.19f, 0.23f, 0.55f));
            var enemyAreaRect = enemyAreaObject.GetComponent<RectTransform>();
            enemyAreaRect.anchorMin = new Vector2(0.02f, 0.08f);
            enemyAreaRect.anchorMax = new Vector2(0.47f, 0.92f);
            enemyAreaRect.offsetMin = Vector2.zero;
            enemyAreaRect.offsetMax = Vector2.zero;
            var enemyAreaView = CreateBattleUnitArea(enemyAreaObject, "Enemy");

            var spaceObject = CreatePanel("Space", graphicRect, new Color(1f, 1f, 1f, 0.04f));
            var spaceRect = spaceObject.GetComponent<RectTransform>();
            spaceRect.anchorMin = new Vector2(0.48f, 0.08f);
            spaceRect.anchorMax = new Vector2(0.52f, 0.92f);
            spaceRect.offsetMin = Vector2.zero;
            spaceRect.offsetMax = Vector2.zero;

            var allyAreaObject = CreatePanel("AllyArea", graphicRect, new Color(0.17f, 0.19f, 0.23f, 0.55f));
            var allyAreaRect = allyAreaObject.GetComponent<RectTransform>();
            allyAreaRect.anchorMin = new Vector2(0.53f, 0.08f);
            allyAreaRect.anchorMax = new Vector2(0.98f, 0.92f);
            allyAreaRect.offsetMin = Vector2.zero;
            allyAreaRect.offsetMax = Vector2.zero;
            var allyAreaView = CreateBattleUnitArea(allyAreaObject, "Ally");

            var battleLogWindow = CreatePanel("BattleLogWindow", battleMainRect, new Color(0.17f, 0.18f, 0.22f));
            var battleLogRect = battleLogWindow.GetComponent<RectTransform>();
            battleLogRect.anchorMin = new Vector2(0.04f, 0.06f);
            battleLogRect.anchorMax = new Vector2(0.96f, 0.34f);
            battleLogRect.offsetMin = Vector2.zero;
            battleLogRect.offsetMax = Vector2.zero;

            var outerObject = new GameObject("Outer", typeof(RectTransform));
            outerObject.transform.SetParent(battleLogRect, false);
            var outerRect = outerObject.GetComponent<RectTransform>();
            outerRect.anchorMin = Vector2.zero;
            outerRect.anchorMax = Vector2.one;
            outerRect.offsetMin = new Vector2(16f, 16f);
            outerRect.offsetMax = new Vector2(-16f, -16f);

            var battleLogRoot = CreatePanel("BattleLog", outerRect, new Color(0.14f, 0.15f, 0.19f, 0.6f));
            var battleLogRootRect = battleLogRoot.GetComponent<RectTransform>();
            battleLogRootRect.anchorMin = new Vector2(0f, 0.34f);
            battleLogRootRect.anchorMax = new Vector2(1f, 1f);
            battleLogRootRect.offsetMin = Vector2.zero;
            battleLogRootRect.offsetMax = Vector2.zero;

            var skillSelectorRoot = CreatePanel("SkillSelector", outerRect, new Color(0.14f, 0.15f, 0.19f, 0.6f));
            var skillSelectorRect = skillSelectorRoot.GetComponent<RectTransform>();
            skillSelectorRect.anchorMin = new Vector2(0f, 0f);
            skillSelectorRect.anchorMax = new Vector2(1f, 0.28f);
            skillSelectorRect.offsetMin = Vector2.zero;
            skillSelectorRect.offsetMax = Vector2.zero;

            battleMainView.Initialize(graphicRect, enemyAreaView, allyAreaView, battleLogRootRect, skillSelectorRect);

            var rewardOverlayObject = CreatePanel("RewardOverlay", rect, new Color(0.1f, 0.08f, 0.12f, 0.96f));
            var rewardRect = rewardOverlayObject.GetComponent<RectTransform>();
            rewardRect.anchorMin = new Vector2(0.18f, 0.18f);
            rewardRect.anchorMax = new Vector2(0.82f, 0.72f);
            rewardRect.offsetMin = Vector2.zero;
            rewardRect.offsetMax = Vector2.zero;

            var rewardOverlay = rewardOverlayObject.AddComponent<RewardOverlayView>();
            var rewardTitle = CreateText("Title", rewardRect, "Reward Overlay", 30, TextAnchor.UpperCenter);
            var rewardBody = CreateText(
                "Body",
                rewardRect,
                "Battle-only overlay\n- reward candidates\n- target selection\n- accept flow",
                22,
                TextAnchor.MiddleCenter);

            rewardTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
            rewardTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
            rewardTitle.rectTransform.offsetMin = new Vector2(16f, -52f);
            rewardTitle.rectTransform.offsetMax = new Vector2(-16f, -12f);

            rewardBody.rectTransform.anchorMin = new Vector2(0.08f, 0.24f);
            rewardBody.rectTransform.anchorMax = new Vector2(0.92f, 0.76f);
            rewardBody.rectTransform.offsetMin = Vector2.zero;
            rewardBody.rectTransform.offsetMax = Vector2.zero;

            rewardOverlay.Initialize(rewardTitle, rewardBody);
            rewardOverlay.Close();

            screen.Initialize(battleMainView, rewardOverlay);
            return screen;
        }

        private static BattleUnitAreaView CreateBattleUnitArea(GameObject areaObject, string prefix)
        {
            var rect = areaObject.GetComponent<RectTransform>();
            var areaView = areaObject.AddComponent<BattleUnitAreaView>();

            var barsObject = CreatePanel(prefix + "Bars", rect, new Color(0.22f, 0.24f, 0.29f, 0.65f));
            var barsRect = barsObject.GetComponent<RectTransform>();
            barsRect.anchorMin = new Vector2(0f, 0.52f);
            barsRect.anchorMax = new Vector2(1f, 1f);
            barsRect.offsetMin = new Vector2(8f, -8f);
            barsRect.offsetMax = new Vector2(-8f, -8f);
            CreateText(prefix + "BarsLabel", barsRect, prefix + " Bars", 20, TextAnchor.MiddleCenter);

            var graphicsObject = CreatePanel(prefix + "Graphics", rect, new Color(0.14f, 0.16f, 0.2f, 0.65f));
            var graphicsRect = graphicsObject.GetComponent<RectTransform>();
            graphicsRect.anchorMin = new Vector2(0f, 0f);
            graphicsRect.anchorMax = new Vector2(1f, 0.48f);
            graphicsRect.offsetMin = new Vector2(8f, 8f);
            graphicsRect.offsetMax = new Vector2(-8f, 8f);
            CreateText(prefix + "GraphicsLabel", graphicsRect, prefix + " Graphics", 22, TextAnchor.MiddleCenter);

            areaView.Initialize(barsRect, graphicsRect);
            return areaView;
        }

        private static RectTransform CreateLayoutGroup(string name, RectTransform parent, TextAnchor childAlignment)
        {
            var groupObject = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            groupObject.transform.SetParent(parent, false);
            var rect = groupObject.GetComponent<RectTransform>();
            var layout = groupObject.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 16f;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            layout.childControlHeight = true;
            layout.childControlWidth = false;
            layout.childAlignment = childAlignment;

            var fitter = groupObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            return rect;
        }

        private static GameObject CreatePanel(string name, RectTransform parent, Color color)
        {
            var panelObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(parent, false);
            var image = panelObject.GetComponent<Image>();
            image.color = color;
            return panelObject;
        }

        private static TMP_Text CreateText(string name, RectTransform parent, string text, int fontSize, TextAnchor alignment)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            var label = textObject.GetComponent<TextMeshProUGUI>();
            label.fontSize = fontSize;
            label.alignment = ToTextAlignment(alignment);
            label.color = Color.white;
            label.richText = false;
            label.enableWordWrapping = true;
            label.overflowMode = TextOverflowModes.Overflow;
            label.text = text;

            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return label;
        }

        private static Button CreateButton(string name, RectTransform parent, string label)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.24f, 0.31f, 0.42f);

            var button = buttonObject.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = new Color(0.32f, 0.4f, 0.52f);
            colors.pressedColor = new Color(0.18f, 0.25f, 0.35f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.2f, 0.2f, 0.2f);
            button.colors = colors;

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(120f, 44f);

            var labelText = CreateText("Label", rect, label, 20, TextAnchor.MiddleCenter);
            labelText.rectTransform.offsetMin = new Vector2(10f, 8f);
            labelText.rectTransform.offsetMax = new Vector2(-10f, -8f);
            return button;
        }

        private static void SetEdgePaneLayout(RectTransform rect, bool isLeftPane, float width)
        {
            if (isLeftPane)
            {
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.sizeDelta = new Vector2(width, 0f);
                rect.offsetMin = new Vector2(0f, 0f);
                rect.offsetMax = new Vector2(width, 0f);
                return;
            }

            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(width, 0f);
            rect.offsetMin = new Vector2(-width, 0f);
            rect.offsetMax = new Vector2(0f, 0f);
        }

        private static void SetVerticalBlock(RectTransform rect, float anchorY, float height, float topOffset)
        {
            rect.anchorMin = new Vector2(0f, anchorY);
            rect.anchorMax = new Vector2(1f, anchorY);
            rect.pivot = new Vector2(0.5f, anchorY);
            rect.sizeDelta = new Vector2(0f, height);
            rect.offsetMin = new Vector2(24f, -topOffset - height);
            rect.offsetMax = new Vector2(-24f, -topOffset);
        }

        private static TextAlignmentOptions ToTextAlignment(TextAnchor alignment)
        {
            return alignment switch
            {
                TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
                TextAnchor.UpperCenter => TextAlignmentOptions.Top,
                TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
                TextAnchor.MiddleLeft => TextAlignmentOptions.MidlineLeft,
                TextAnchor.MiddleCenter => TextAlignmentOptions.Center,
                TextAnchor.MiddleRight => TextAlignmentOptions.MidlineRight,
                TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
                TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
                TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
                _ => TextAlignmentOptions.Center,
            };
        }
    }
}
