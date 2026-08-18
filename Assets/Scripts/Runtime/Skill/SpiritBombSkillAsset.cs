using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "SpiritBombSkill", menuName = "Pachimon/Skills/Machine/Spirit Bomb")]
    public sealed class SpiritBombSkillAsset : MachineExclusiveSkillAsset
    {
        [SerializeField, Range(0, 100)] private int _currentMnPercent = 20;
        [SerializeField, Min(0)] private int _damageMultiplier = 4;

        public int CurrentMnPercent => _currentMnPercent;
        public int DamageMultiplier => _damageMultiplier;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int id,
            int currentMnPercent,
            int damageMultiplier,
            string description)
        {
            ConfigureMachineForEditor(id, "元気玉", 300, 100, 1000, 0, description);
            _currentMnPercent = currentMnPercent;
            _damageMultiplier = damageMultiplier;
        }
#endif
    }
}
