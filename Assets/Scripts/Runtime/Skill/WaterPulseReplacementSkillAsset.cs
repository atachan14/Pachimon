using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "WaterPulseReplacementSkill", menuName = "Pachimon/Skills/Water Pulse")]
    public sealed class WaterPulseReplacementSkillAsset : SkillAsset
    {
        [SerializeField, Range(1, 100)] private int _maxMnCostPercent = 4;
        [SerializeField, Min(0)] private int _damagePerMana = 3;
        [SerializeField, Min(0)] private int _aquaDamageRatio = 100;

        public int MaxMnCostPercent => _maxMnCostPercent;
        public int DamagePerMana => _damagePerMana;
        public int AquaDamageRatio => _aquaDamageRatio;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int skillId,
            int startup,
            int recovery,
            int cooldown,
            int maxMnCostPercent,
            int damagePerMana,
            int aquaDamageRatio,
            string description)
        {
            base.ConfigureForEditor(skillId, "水の波動", AllocationType.Aqua,
                true, recovery, cooldown, description, 0, startup);
            _maxMnCostPercent = maxMnCostPercent;
            _damagePerMana = damagePerMana;
            _aquaDamageRatio = aquaDamageRatio;
        }
#endif
    }
}
