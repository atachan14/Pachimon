using Pachimon.Battle;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "FreezingSkill", menuName = "Pachimon/Skills/Machine/Freezing")]
    public sealed class FreezingSkillAsset : MachineExclusiveSkillAsset
    {
        [SerializeField, Range(0, 100)] private int _damagePercent = 50;
        [SerializeField] private FreezeStatusAsset _freezeStatus;
        public int DamagePercent => _damagePercent;
        public FreezeStatusAsset FreezeStatus => _freezeStatus;

#if UNITY_EDITOR
        public void ConfigureForEditor(int id, int startup, int recovery,
            int cooldown, int mana, int damagePercent,
            FreezeStatusAsset freezeStatus,
            string description)
        {
            ConfigureMachineForEditor(id, "氷結", startup, recovery,
                cooldown, mana, description, Data.AllocationType.Ice);
            _damagePercent = damagePercent;
            _freezeStatus = freezeStatus;
        }
#endif
    }
}
