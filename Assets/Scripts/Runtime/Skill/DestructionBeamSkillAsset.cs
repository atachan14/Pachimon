using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "DestructionBeamSkill", menuName = "Pachimon/Skills/Machine/Destruction Beam")]
    public sealed class DestructionBeamSkillAsset : MachineExclusiveSkillAsset
    {
        [SerializeField, Range(0, 100)] private int _maxHpPercent = 50;

        public int MaxHpPercent => _maxHpPercent;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int id,
            int startup,
            int recovery,
            int cooldown,
            int mana,
            int maxHpPercent,
            string description)
        {
            ConfigureMachineForEditor(id, "はかいこうせん", startup, recovery,
                cooldown, mana, description);
            _maxHpPercent = maxHpPercent;
        }
#endif
    }
}
