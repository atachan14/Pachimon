using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "ElectricShieldSkill", menuName = "Pachimon/Skills/Electric Shield")]
    public sealed class ElectricShieldSkillAsset : SkillAsset
    {
        [SerializeField, Min(1)] private int _durationTicks = 100;
        [SerializeField, Min(0)] private int _baseShieldValue = 150;
        [SerializeField, Min(0)] private int _shieldElectricRatio = 100;
        [SerializeField, Min(0)] private int _baseSelfParalysis = 50;
        [SerializeField, Min(0)] private int _selfParalysisElectricRatio = 100;
        [SerializeField, Min(0)] private int _baseCounterParalysis = 25;
        [SerializeField, Min(0)] private int _counterParalysisElectricRatio = 100;
        [SerializeField] private SlowStatusAsset _paralysisStatus;
        [SerializeField] private ElectricShieldStatusAsset _shieldStatus;

        public int DurationTicks => _durationTicks;
        public int BaseShieldValue => _baseShieldValue;
        public int ShieldElectricRatio => _shieldElectricRatio;
        public int BaseSelfParalysis => _baseSelfParalysis;
        public int SelfParalysisElectricRatio => _selfParalysisElectricRatio;
        public int BaseCounterParalysis => _baseCounterParalysis;
        public int CounterParalysisElectricRatio => _counterParalysisElectricRatio;
        public SlowStatusAsset ParalysisStatus => _paralysisStatus;
        public ElectricShieldStatusAsset ShieldStatus => _shieldStatus;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Electric)
                errors?.Add($"Skill {SkillId}: Electric Shield must be Electric.");
            if (_paralysisStatus == null || _shieldStatus == null)
                errors?.Add($"Skill {SkillId}: Status definitions are required.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int skillId, string displayName,
            int recovery, int cooldown, int mana, string description,
            int durationTicks,
            int baseShieldValue, int shieldElectricRatio,
            int baseSelfParalysis, int selfParalysisElectricRatio,
            int baseCounterParalysis, int counterParalysisElectricRatio,
            SlowStatusAsset paralysisStatus,
            ElectricShieldStatusAsset shieldStatus)
        {
            base.ConfigureForEditor(skillId, displayName, AllocationType.Electric,
                true, recovery, cooldown, description, mana);
            _durationTicks = durationTicks;
            _baseShieldValue = baseShieldValue;
            _shieldElectricRatio = shieldElectricRatio;
            _baseSelfParalysis = baseSelfParalysis;
            _selfParalysisElectricRatio = selfParalysisElectricRatio;
            _baseCounterParalysis = baseCounterParalysis;
            _counterParalysisElectricRatio = counterParalysisElectricRatio;
            _paralysisStatus = paralysisStatus;
            _shieldStatus = shieldStatus;
        }
#endif
    }
}
