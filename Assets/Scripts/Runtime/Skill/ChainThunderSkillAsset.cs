using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "ChainThunderSkill", menuName = "Pachimon/Skills/Machine/Chain Thunder")]
    public sealed class ChainThunderSkillAsset : MachineExclusiveSkillAsset
    {
        [SerializeField, Min(0)] private int _baseDamage = 80;
        [SerializeField, Min(0)] private int _electricRatio = 100;

        public int BaseDamage => _baseDamage;
        public int ElectricRatio => _electricRatio;

#if UNITY_EDITOR
        public void ConfigureForEditor(int id, int startup, int recovery,
            int cooldown, int mana, int baseDamage, int electricRatio,
            string description)
        {
            ConfigureMachineForEditor(id, "チェインサンダー", startup,
                recovery, cooldown, mana, description,
                Data.AllocationType.Electric);
            _baseDamage = baseDamage;
            _electricRatio = electricRatio;
        }
#endif
    }
}
