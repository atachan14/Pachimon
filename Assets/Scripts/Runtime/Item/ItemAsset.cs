using System;
using System.Collections.Generic;
using Pachimon.Run;
using UnityEngine;

namespace Pachimon.Items
{
    public enum ItemCategory
    {
        Pharmacy = 0,
        Other = 1,
        SkillMachine = 2,
        Engraving = 3,
        Equipment = 4,
    }

    public enum EquipmentSlot
    {
        Head = 0,
        Body = 1,
        Feet = 2,
    }

    public static class StatUnitValue
    {
        public static int Get(PachimonStatType statType)
        {
            if (PachimonStatTypeUtility.TryGetAttribute(statType, out _))
            {
                return 15;
            }

            return statType switch
            {
                PachimonStatType.MaxHp or PachimonStatType.MaxMn =>
                    PachimonStatValueUnits.ToDisplayedAmount(statType, 15),
                PachimonStatType.Speed
                    or PachimonStatType.Haste
                    or PachimonStatType.DamageBonus
                    or PachimonStatType.ResistBonus
                    or PachimonStatType.GenerationPower
                    or PachimonStatType.StatusMastery
                    or PachimonStatType.SustainPower
                    or PachimonStatType.StatusResistance => 10,
                _ => throw new ArgumentOutOfRangeException(nameof(statType)),
            };
        }
    }

    public abstract class ItemAsset : ScriptableObject
    {
        [SerializeField] private int _itemId;
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _icon;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private ItemCategory _category;
        [SerializeField, Min(1)] private int _basePrice;

        public int ItemId => _itemId;
        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public string Description => _description;
        public ItemCategory Category => _category;
        public int BasePrice => _basePrice;

        public virtual void CollectValidationErrors(ICollection<string> errors)
        {
            if (errors == null) throw new ArgumentNullException(nameof(errors));
            if (_itemId <= 0) errors.Add($"{name}: Item ID must be positive.");
            if (string.IsNullOrWhiteSpace(_displayName))
            {
                errors.Add($"Item {_itemId}: display name is missing.");
            }

            if (_basePrice <= 0)
            {
                errors.Add($"Item {_itemId}: base price must be positive.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int itemId,
            string displayName,
            Sprite icon,
            string description,
            ItemCategory category,
            int basePrice)
        {
            _itemId = itemId;
            _displayName = displayName;
            _icon = icon;
            _description = description;
            _category = category;
            _basePrice = basePrice;
        }
#endif
    }

    public static class EngravingStatName
    {
        public static string Get(PachimonStatType statType)
        {
            return statType switch
            {
                PachimonStatType.MaxHp => "MaxHP",
                PachimonStatType.MaxMn => "MaxMN",
                PachimonStatType.Fire => "炎",
                PachimonStatType.Aqua => "水",
                PachimonStatType.Leaf => "草",
                PachimonStatType.Electric => "電",
                PachimonStatType.Poison => "毒",
                PachimonStatType.Ice => "氷",
                PachimonStatType.Wind => "風",
                PachimonStatType.Dragon => "竜",
                PachimonStatType.Speed => "SPD",
                PachimonStatType.Haste => "HST",
                PachimonStatType.DamageBonus => "DB",
                PachimonStatType.ResistBonus => "RB",
                PachimonStatType.GenerationPower => "GEN",
                PachimonStatType.StatusMastery => "SM",
                PachimonStatType.SustainPower => "SUS",
                PachimonStatType.StatusResistance => "SR",
                _ => throw new ArgumentOutOfRangeException(nameof(statType)),
            };
        }
    }
}
