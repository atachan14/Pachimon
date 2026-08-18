using System;
using System.Collections.Generic;
using Pachimon.Items;
using Pachimon.Map;
using Pachimon.Run;
using UnityEngine;

namespace Pachimon.UI
{
    public sealed class RightPaneView : MonoBehaviour
    {
        [field: SerializeField]
        public NodeSelectionWindowView NodeSelectionWindow { get; private set; }

        public event Action ContentShown;
        public event Action ContentCleared;
        public event Action MainPaneRequested;

        public void Initialize(NodeSelectionWindowView nodeSelectionWindow)
        {
            NodeSelectionWindow = nodeSelectionWindow;
            ClearNodeSelection();
        }

        public void ShowBattleNodeSelection(
            TrainerPreviewContent trainerPreview,
            IReadOnlyList<PachimonPreviewContent> pachimonPreviews,
            Action onConfirm,
            Action onCancel)
        {
            NodeSelectionWindow?.ShowBattle(
                trainerPreview,
                pachimonPreviews,
                true,
                onConfirm,
                onCancel);
            ContentShown?.Invoke();
        }

        public void ShowBattleNodePreview(
            TrainerPreviewContent trainerPreview,
            IReadOnlyList<PachimonPreviewContent> pachimonPreviews)
        {
            NodeSelectionWindow?.ShowBattle(
                trainerPreview,
                pachimonPreviews,
                false,
                null,
                null);
            ContentShown?.Invoke();
        }

        public void ShowBattleStatus(
            TrainerPreviewContent trainerPreview,
            IReadOnlyList<PachimonPreviewContent> pachimonPreviews)
        {
            NodeSelectionWindow?.ShowBattle(
                trainerPreview,
                pachimonPreviews,
                false,
                null,
                null);
            ContentShown?.Invoke();
        }

        public void ShowStartCandidateSelection(
            IReadOnlyList<PachimonPreviewContent> previews,
            IReadOnlyList<bool> candidateSelections,
            int selectedIndex,
            Action<int> onTabSelected,
            string confirmLabel,
            Action onConfirm,
            Action onCancel)
        {
            NodeSelectionWindow?.ShowStartCandidates(
                previews,
                candidateSelections,
                selectedIndex,
                onTabSelected,
                confirmLabel,
                onConfirm,
                onCancel);
            ContentShown?.Invoke();
        }

        public void ShowSimpleNodeSelection(
            string title,
            string details,
            Action onConfirm,
            Action onCancel)
        {
            NodeSelectionWindow?.ShowSimple(title, details, true, onConfirm, onCancel);
            ContentShown?.Invoke();
        }

        public void ShowSimpleNodePreview(string title, string details)
        {
            NodeSelectionWindow?.ShowSimple(title, details, false, null, null);
            ContentShown?.Invoke();
        }

        public void ShowCityNodeSelection(
            CityNodeContent city,
            ItemCatalog itemCatalog,
            RunState runState,
            Action<CityStockEntry> onDetails,
            Action onConfirm,
            Action onCancel)
        {
            NodeSelectionWindow?.ShowCity(
                city,
                itemCatalog,
                runState,
                false,
                true,
                onDetails,
                null,
                onConfirm,
                onCancel);
            ContentShown?.Invoke();
        }

        public void ShowCityNodePreview(
            CityNodeContent city,
            ItemCatalog itemCatalog,
            RunState runState,
            Action<CityStockEntry> onDetails)
        {
            NodeSelectionWindow?.ShowCity(
                city,
                itemCatalog,
                runState,
                false,
                false,
                onDetails,
                null,
                null,
                null);
            ContentShown?.Invoke();
        }

        public void ShowCityShop(
            CityNodeContent city,
            ItemCatalog itemCatalog,
            RunState runState,
            Action<CityStockEntry> onDetails)
        {
            NodeSelectionWindow?.ShowCity(
                city,
                itemCatalog,
                runState,
                false,
                false,
                onDetails,
                null,
                null,
                null);
            ContentShown?.Invoke();
        }

        public void ClearNodeSelection()
        {
            NodeSelectionWindow?.Hide();
            ContentCleared?.Invoke();
        }

        public void RequestMainPane()
        {
            MainPaneRequested?.Invoke();
        }

        public void ApplyLayoutMode(LayoutMode layoutMode)
        {
            NodeSelectionWindow?.ApplyLayoutMode(layoutMode);
        }

        public void ConfigureItemDrop(
            Func<ItemInstance, int, bool> canUse,
            Func<ItemInstance, int, bool> tryUse)
        {
            var dropTarget = GetComponent<ItemDropTargetView>();
            if (dropTarget == null)
            {
                dropTarget = gameObject.AddComponent<ItemDropTargetView>();
            }

            dropTarget.Configure(
                item =>
                {
                    var selectedIndex =
                        (NodeSelectionWindow?.BattleWindow?.SelectedTabIndex ?? 0) - 1;
                    return selectedIndex >= 0
                        && canUse != null
                        && canUse(item, selectedIndex);
                },
                item =>
                {
                    var selectedIndex =
                        (NodeSelectionWindow?.BattleWindow?.SelectedTabIndex ?? 0) - 1;
                    return selectedIndex >= 0
                        && tryUse != null
                        && tryUse(item, selectedIndex);
                });
        }
    }
}
