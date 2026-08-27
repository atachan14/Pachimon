using System.Collections.Generic;
using Pachimon.Reward;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(
        fileName = "FireBarrierFieldEffect",
        menuName = "Pachimon/Field Effect/Fire Barrier")]
    public sealed class FireBarrierFieldEffectAsset : BattleFieldEffectAsset
    {
        [SerializeField, Min(0)] private int _valueBurnRatio = 20;
        [SerializeField] private int _fireDefense = 200;
        [SerializeField] private int _aquaDefense;
        [SerializeField] private int _leafDefense = 100;
        [SerializeField] private int _electricDefense = 100;
        [SerializeField] private int _poisonDefense = 100;
        [SerializeField] private int _iceDefense = 100;
        [SerializeField] private int _windDefense = 100;
        [SerializeField] private int _dragonDefense = 100;
        [SerializeField] private int _resistBonus;
        [SerializeField] private BurnStatusAsset _burnStatus;

        public int ValueBurnRatio => _valueBurnRatio;
        public int ResistBonus => _resistBonus;
        public BurnStatusAsset BurnStatus => _burnStatus;

        public int GetDefense(PachimonAttribute attribute)
        {
            return attribute switch
            {
                PachimonAttribute.Fire => _fireDefense,
                PachimonAttribute.Aqua => _aquaDefense,
                PachimonAttribute.Leaf => _leafDefense,
                PachimonAttribute.Electric => _electricDefense,
                PachimonAttribute.Poison => _poisonDefense,
                PachimonAttribute.Ice => _iceDefense,
                PachimonAttribute.Wind => _windDefense,
                PachimonAttribute.Dragon => _dragonDefense,
                _ => throw new System.ArgumentOutOfRangeException(
                    nameof(attribute), attribute, null),
            };
        }

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (EffectId != BattleFieldEffectId.FireBarrier)
            {
                errors?.Add("Fire Barrier Definition must use FireBarrier ID.");
            }
            if (_burnStatus == null)
            {
                errors?.Add("Fire Barrier Definition requires a Burn Status.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string displayName,
            string description,
            int valueBurnRatio,
            BurnStatusAsset burnStatus,
            Sprite icon = null)
        {
            ConfigureDefinitionForEditor(
                BattleFieldEffectId.FireBarrier,
                displayName,
                description,
                icon);
            _valueBurnRatio = valueBurnRatio;
            _fireDefense = 200;
            _aquaDefense = 0;
            _leafDefense = 100;
            _electricDefense = 100;
            _poisonDefense = 100;
            _iceDefense = 100;
            _windDefense = 100;
            _dragonDefense = 100;
            _resistBonus = 0;
            _burnStatus = burnStatus;
        }
#endif
    }
}
