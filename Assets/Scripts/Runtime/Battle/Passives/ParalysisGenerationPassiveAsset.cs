using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(fileName = "ParalysisGenerationPassive", menuName = "Pachimon/Passives/Paralysis Generation")]
    public sealed class ParalysisGenerationPassiveAsset : PassiveAsset
    {
        [SerializeField, Min(0)] private int _electricFromParalysisRatio = 50;
        public int ElectricFromParalysisRatio => _electricFromParalysisRatio;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int passiveId, string displayName, string description,
            int electricFromParalysisRatio)
        {
            ConfigureBaseForEditor(passiveId, displayName, description);
            _electricFromParalysisRatio = electricFromParalysisRatio;
        }
#endif
    }
}
