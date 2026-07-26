using Pachimon.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.Editor.UI
{
    public static class PanePaletteSetup
    {
        private const string MenuPath = "Tools/Pachimon/UI/Apply Shared Pane Palette";

        [MenuItem(MenuPath)]
        private static void ApplyFromMenu()
        {
            var leftPanes = Object.FindObjectsByType<LeftPaneView>(FindObjectsInactive.Include);
            var rightPanes = Object.FindObjectsByType<RightPaneView>(FindObjectsInactive.Include);
            if (leftPanes.Length == 0 && rightPanes.Length == 0)
            {
                Debug.LogError("LeftPaneView and RightPaneView were not found in the open Scene.");
                return;
            }

            Undo.SetCurrentGroupName("Apply Shared Pane Palette");
            var undoGroup = Undo.GetCurrentGroup();
            foreach (var pane in leftPanes) ApplyLeftPane(pane);
            foreach (var pane in rightPanes) ApplyRightPane(pane);

            var scene = leftPanes.Length > 0
                ? leftPanes[0].gameObject.scene
                : rightPanes[0].gameObject.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = rightPanes.Length > 0
                ? rightPanes[0].gameObject
                : leftPanes[0].gameObject;
            Debug.Log(
                $"Applied shared palette to {leftPanes.Length} LeftPane and {rightPanes.Length} RightPane.",
                Selection.activeGameObject);
        }

        private static void ApplyLeftPane(LeftPaneView pane)
        {
            SetImageColor(pane.gameObject, GameUiPalette.LeftPaneBackground);
            SetTextColor(pane.TitleText, GameUiPalette.PrimaryText);
            SetTextColor(pane.BodyText, GameUiPalette.PrimaryText);
        }

        private static void ApplyRightPane(RightPaneView pane)
        {
            SetImageColor(pane.gameObject, GameUiPalette.RightPaneBackground);

            foreach (var trainerTab in pane.GetComponentsInChildren<TrainerTabView>(true))
            {
                ApplyTrainerTab(trainerTab);
            }

            foreach (var pachimonTab in pane.GetComponentsInChildren<PachimonTabView>(true))
            {
                ApplyPachimonTab(pachimonTab);
            }

            foreach (var simpleWindow in pane.GetComponentsInChildren<SimpleNodeWindowView>(true))
            {
                foreach (var text in simpleWindow.GetComponentsInChildren<TMP_Text>(true))
                {
                    SetTextColor(text, GameUiPalette.PrimaryText);
                }
            }

            foreach (var button in pane.GetComponentsInChildren<Button>(true))
            {
                ApplyButton(button, button.name == "ConfirmButton");
            }
        }

        private static void ApplyTrainerTab(TrainerTabView tab)
        {
            var content = tab.transform.Find("Viewport/Content");
            if (content == null) return;

            SetImageColor(content.Find("GraphicArea")?.gameObject, GameUiPalette.Transparent);
            SetTextColor(content.Find("TrainerName")?.GetComponent<TMP_Text>(), GameUiPalette.PrimaryText);

            var rewardSection = content.Find("RewardSection");
            SetImageColor(rewardSection?.gameObject, GameUiPalette.Card);
            SetTextChildren(rewardSection, GameUiPalette.PrimaryText);

            var goldSection = content.Find("GoldSection");
            SetImageColor(goldSection?.gameObject, GameUiPalette.GoldCard);
            SetTextChildren(goldSection, GameUiPalette.PrimaryText);
            var goldValue = goldSection?.Find("Value")?.GetComponent<TMP_Text>();
            if (goldValue != null)
            {
                Undo.RecordObject(goldValue, "Apply UI Palette");
                goldValue.alignment = TextAlignmentOptions.MidlineLeft;
                EditorUtility.SetDirty(goldValue);
            }
        }

        private static void ApplyPachimonTab(PachimonTabView tab)
        {
            var content = tab.transform.Find("Viewport/Content");
            if (content == null) return;

            SetTextColor(content.Find("Name")?.GetComponent<TMP_Text>(), GameUiPalette.PrimaryText);
            SetTextColor(content.Find("Hp")?.GetComponent<TMP_Text>(), GameUiPalette.PrimaryText);

            var statsGrid = content.Find("StatsGrid");
            if (statsGrid != null)
            {
                foreach (var slot in statsGrid.GetComponentsInChildren<PachimonStatSlotView>(true))
                {
                    SetImageColor(slot.gameObject, GameUiPalette.StatCard);
                    SetTextColor(
                        slot.transform.Find("Value")?.GetComponent<TMP_Text>(),
                        GameUiPalette.PrimaryText);
                }
            }

            ApplySection(content.Find("StatusSection"), GameUiPalette.StatusSection, GameUiPalette.StatusChip);
            ApplySection(content.Find("SkillSection"), GameUiPalette.SkillSection, GameUiPalette.SkillChip);
            ApplySection(content.Find("PassiveSection"), GameUiPalette.PassiveSection, GameUiPalette.PassiveChip);
        }

        private static void ApplySection(Transform section, Color sectionColor, Color chipColor)
        {
            if (section == null) return;
            SetImageColor(section.gameObject, sectionColor);
            SetTextColor(section.Find("Title")?.GetComponent<TMP_Text>(), GameUiPalette.PrimaryText);

            foreach (var chip in section.GetComponentsInChildren<TextChipView>(true))
            {
                SetImageColor(chip.gameObject, chipColor);
                var luminance = (0.299f * chipColor.r)
                    + (0.587f * chipColor.g)
                    + (0.114f * chipColor.b);
                SetTextChildren(
                    chip.transform,
                    luminance > 0.62f ? GameUiPalette.PrimaryText : GameUiPalette.OnAccentText);
            }
        }

        private static void ApplyButton(Button button, bool isAccent)
        {
            if (button == null) return;
            SetImageColor(
                button.gameObject,
                isAccent ? GameUiPalette.ButtonAccent : GameUiPalette.ButtonNeutral);
            SetTextChildren(button.transform, GameUiPalette.OnAccentText);
        }

        private static void SetImageColor(GameObject target, Color color)
        {
            if (target == null || !target.TryGetComponent<Image>(out var image)) return;
            Undo.RecordObject(image, "Apply UI Palette");
            image.color = color;
            EditorUtility.SetDirty(image);
        }

        private static void SetTextChildren(Transform root, Color color)
        {
            if (root == null) return;
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                SetTextColor(text, color);
            }
        }

        private static void SetTextColor(TMP_Text text, Color color)
        {
            if (text == null) return;
            Undo.RecordObject(text, "Apply UI Palette");
            text.color = color;
            EditorUtility.SetDirty(text);
        }
    }
}
