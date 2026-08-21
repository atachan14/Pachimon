using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(
        fileName = "EvaporationSkill",
        menuName = "Pachimon/Skills/Evaporation Skill")]
    public sealed class EvaporationSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _baseFireDamage = 70;
        [SerializeField, HideInInspector] private int _fireDamageRatio = 100;
        [SerializeField, Min(0)] private int _baseAquaDamage = 70;
        [SerializeField, HideInInspector] private int _aquaDamageRatio = 100;
        [SerializeField, Min(0)] private int _baseFirePenetration = 20;
        [SerializeField, Min(0)] private int _firePenetrationRatio = 100;
        [SerializeField, Min(0)] private int _baseAquaPenetration = 20;
        [SerializeField, Min(0)] private int _aquaPenetrationRatio = 100;
        [SerializeField, Min(0)] private int _baseFireWeakness = 10;
        [SerializeField, Min(0)] private int _fireWeaknessRatio = 100;
        [SerializeField, Min(0)] private int _baseAquaWeakness = 10;
        [SerializeField, Min(0)] private int _aquaWeaknessRatio = 100;
        [SerializeField] private WeaknessStatusAsset _weaknessStatus;

        public int BaseFireDamage => _baseFireDamage;
        public int FireDamageRatio => AttributeDamageRules.ScalingRatio;
        public int BaseAquaDamage => _baseAquaDamage;
        public int AquaDamageRatio => AttributeDamageRules.ScalingRatio;
        public int BaseFirePenetration => _baseFirePenetration;
        public int FirePenetrationRatio => _firePenetrationRatio;
        public int BaseAquaPenetration => _baseAquaPenetration;
        public int AquaPenetrationRatio => _aquaPenetrationRatio;
        public int BaseFireWeakness => _baseFireWeakness;
        public int FireWeaknessRatio => _fireWeaknessRatio;
        public int BaseAquaWeakness => _baseAquaWeakness;
        public int AquaWeaknessRatio => _aquaWeaknessRatio;
        public WeaknessStatusAsset WeaknessStatus => _weaknessStatus;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Fire)
                errors.Add($"Skill {SkillId}: Evaporation must be Fire.");
            if (_weaknessStatus == null)
                errors.Add($"Skill {SkillId}: Weakness Status is required.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int skillId,
            string displayName,
            int baseRecoveryTicks,
            int baseCooldownTicks,
            int baseManaCost,
            string description,
            int baseFireDamage,
            int fireDamageRatio,
            int baseAquaDamage,
            int aquaDamageRatio,
            int baseFirePenetration,
            int firePenetrationRatio,
            int baseAquaPenetration,
            int aquaPenetrationRatio,
            int baseFireWeakness,
            int fireWeaknessRatio,
            int baseAquaWeakness,
            int aquaWeaknessRatio,
            WeaknessStatusAsset weaknessStatus)
        {
            base.ConfigureForEditor(
                skillId,
                displayName,
                AllocationType.Fire,
                isMapAssignable: true,
                baseRecoveryTicks,
                baseCooldownTicks,
                description,
                baseManaCost);
            _baseFireDamage = baseFireDamage;
            _fireDamageRatio = fireDamageRatio;
            _baseAquaDamage = baseAquaDamage;
            _aquaDamageRatio = aquaDamageRatio;
            _baseFirePenetration = baseFirePenetration;
            _firePenetrationRatio = firePenetrationRatio;
            _baseAquaPenetration = baseAquaPenetration;
            _aquaPenetrationRatio = aquaPenetrationRatio;
            _baseFireWeakness = baseFireWeakness;
            _fireWeaknessRatio = fireWeaknessRatio;
            _baseAquaWeakness = baseAquaWeakness;
            _aquaWeaknessRatio = aquaWeaknessRatio;
            _weaknessStatus = weaknessStatus;
        }
#endif
    }
}
