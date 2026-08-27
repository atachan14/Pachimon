using System;
using UnityEngine;

namespace Pachimon.Reward
{
    [CreateAssetMenu(
        fileName = "ModValueSettings",
        menuName = "Pachimon/Reward/Mod Value Settings")]
    public sealed class ModValueSettings : ScriptableObject
    {
        [SerializeField, Min(0)] private int _attributeAmount = 30;
        [SerializeField, Min(0)] private int _maxHpAmount = 240;
        [SerializeField, Min(0)] private int _maxMnAmount = 240;
        [SerializeField, Min(0)] private int _speedAmount = 20;
        [SerializeField, Min(0)] private int _damageBonusAmount = 20;
        [SerializeField, Min(0)] private int _resistBonusAmount = 20;
        [SerializeField, Min(0)] private int _bonusGoldAmount = 4000;
        [SerializeField, Range(0, 100)] private int _secondSlotPercent = 50;

        private static ModValueSettings _runtimeDefault;

        public static ModValueSettings RuntimeDefault
        {
            get
            {
                if (_runtimeDefault != null)
                {
                    return _runtimeDefault;
                }

                _runtimeDefault = CreateInstance<ModValueSettings>();
                _runtimeDefault.hideFlags = HideFlags.HideAndDontSave;
                return _runtimeDefault;
            }
        }

        public int SecondSlotPercent => _secondSlotPercent;

        public int GetAmount(RewardElementKind kind, bool isSecondSlot)
        {
            var firstSlotAmount = kind switch
            {
                RewardElementKind.Attribute => _attributeAmount,
                RewardElementKind.MaxHp => _maxHpAmount,
                RewardElementKind.MaxMn => _maxMnAmount,
                RewardElementKind.Speed => _speedAmount,
                RewardElementKind.DamageBonus => _damageBonusAmount,
                RewardElementKind.ResistBonus => _resistBonusAmount,
                RewardElementKind.BonusGold => _bonusGoldAmount,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
            };

            return isSecondSlot
                ? checked(firstSlotAmount * _secondSlotPercent / 100)
                : firstSlotAmount;
        }
    }
}
