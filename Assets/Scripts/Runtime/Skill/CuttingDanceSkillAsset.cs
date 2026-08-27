using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "CuttingDanceSkill", menuName = "Pachimon/Skills/Cutting Dance")]
    public sealed class CuttingDanceSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _baseWindDamage = 100;
        [SerializeField, HideInInspector] private int _windDamageRatio = 100;
        [SerializeField, Min(0)] private int _baseErosion = 20;
        [SerializeField, HideInInspector] private int _erosionWindRatio = 100;
        [SerializeField, Min(0)] private int _baseChainCount = 2;
        [SerializeField, Min(1)] private int _addChainGainUnits = 100;
        [SerializeField] private WindErosionStatusAsset _erosionStatus;

        public int BaseWindDamage => _baseWindDamage;
        public int WindDamageRatio => AttributeDamageRules.ScalingRatio;
        public int BaseErosion => _baseErosion;
        public int ErosionWindRatio => AttributeDamageRules.ScalingRatio;
        public int BaseChainCount => _baseChainCount;
        public int AddChainGainUnits => _addChainGainUnits;
        public WindErosionStatusAsset ErosionStatus => _erosionStatus;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_erosionStatus == null)
                errors?.Add($"Skill {SkillId}: Wind Erosion is required.");
        }
#if UNITY_EDITOR
        public void ConfigureForEditor(int id, string name, int recovery,
            int cooldown, int mana, string description, int baseWindDamage,
            int windDamageRatio, int baseErosion, int erosionWindRatio,
            int baseChainCount, int addChainGainUnits,
            WindErosionStatusAsset erosionStatus)
        {
            base.ConfigureForEditor(id, name, AllocationType.Wind, true,
                recovery, cooldown, description, mana);
            _baseWindDamage = baseWindDamage;
            _windDamageRatio = windDamageRatio;
            _baseErosion = baseErosion;
            _erosionWindRatio = erosionWindRatio;
            _baseChainCount = baseChainCount;
            _addChainGainUnits = addChainGainUnits;
            _erosionStatus = erosionStatus;
        }
#endif
    }
}
