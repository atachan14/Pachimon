using System;
using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using Pachimon.Items;
using UnityEngine;

namespace Pachimon.UI
{
    public sealed class BattleUnitAreaView : MonoBehaviour
    {
        [SerializeField] private BattleUnitSlotView[] _slots =
            Array.Empty<BattleUnitSlotView>();

        public void ConfigureItemDrops(
            Func<ItemInstance, int, bool> canUse,
            Func<ItemInstance, int, bool> tryUse)
        {
            for (var slotIndex = 0; slotIndex < _slots.Length; slotIndex++)
            {
                var capturedIndex = slotIndex;
                _slots[slotIndex]?.ConfigureItemDrop(
                    item => canUse != null && canUse(item, capturedIndex),
                    item => tryUse != null && tryUse(item, capturedIndex));
            }
        }

        public void ConfigureUnitClicks(Action<int> onClicked)
        {
            for (var slotIndex = 0; slotIndex < _slots.Length; slotIndex++)
            {
                var capturedIndex = slotIndex;
                _slots[slotIndex]?.ConfigureClick(
                    () => onClicked?.Invoke(capturedIndex));
            }
        }

        public void RenderUnits(
            IReadOnlyList<BattleUnitState> units,
            string sideLabel,
            PachimonCatalog pachimonCatalog = null,
            bool useBackSprite = false)
        {
            for (var slotIndex = 0; slotIndex < _slots.Length; slotIndex++)
            {
                var unit = units != null && slotIndex < units.Count
                    ? units[slotIndex]
                    : null;
                _slots[slotIndex]?.Render(
                    unit,
                    $"{sideLabel} {slotIndex + 1}",
                    pachimonCatalog,
                    useBackSprite);
            }
        }

        public void ShowSkillPreview(
            IReadOnlyList<BattleUnitState> units,
            IReadOnlyList<SkillPreviewEffect> effects)
        {
            for (var slotIndex = 0; slotIndex < _slots.Length; slotIndex++)
            {
                var unit = units != null && slotIndex < units.Count
                    ? units[slotIndex]
                    : null;
                var hpDelta = 0;
                var mnDelta = 0;
                if (unit != null && effects != null)
                {
                    foreach (var effect in effects)
                    {
                        if (!ReferenceEquals(effect.Target, unit))
                        {
                            continue;
                        }

                        hpDelta += effect.HpDelta;
                        mnDelta += effect.MnDelta;
                    }
                }

                _slots[slotIndex]?.ShowResourcePreview(unit, hpDelta, mnDelta);
            }
        }

        public void ClearSkillPreview()
        {
            foreach (var slot in _slots)
            {
                slot?.ClearResourcePreview();
            }
        }

        public void PresentResourceSnapshot(
            BattleUnitState unit,
            int currentHp,
            int currentMn)
        {
            if (unit == null
                || unit.SlotIndex < 0
                || unit.SlotIndex >= _slots.Length)
            {
                return;
            }

            _slots[unit.SlotIndex]?.PresentResourceSnapshot(
                unit,
                currentHp,
                currentMn);
        }

        public void ShowPendingToxinDamage(
            BattleResourceTransition transition)
        {
            if (!TryGetSlot(transition, out var slot))
            {
                return;
            }

            slot.ShowPendingToxinDamage(
                transition.Unit,
                transition.HpBefore,
                transition.HpAfter,
                transition.MnAfter);
        }

        public void CommitToxinDamage(BattleResourceTransition transition)
        {
            if (!TryGetSlot(transition, out var slot))
            {
                return;
            }

            slot.CommitToxinDamage(
                transition.Unit,
                transition.HpAfter,
                transition.MnAfter);
        }

        private bool TryGetSlot(
            BattleResourceTransition transition,
            out BattleUnitSlotView slot)
        {
            slot = null;
            if (transition?.Unit == null
                || transition.Unit.SlotIndex < 0
                || transition.Unit.SlotIndex >= _slots.Length)
            {
                return false;
            }

            slot = _slots[transition.Unit.SlotIndex];
            return slot != null;
        }
    }
}
