using Pachimon.Battle;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "FreezingSkill", menuName = "Pachimon/Skills/Machine/Freezing")]
    public sealed class FreezingSkillAsset : MachineExclusiveSkillAsset
    {
        [SerializeField] private FreezeStatusAsset _freezeStatus;
        public FreezeStatusAsset FreezeStatus => _freezeStatus;

#if UNITY_EDITOR
        public void ConfigureForEditor(int id, int startup, int recovery,
            int cooldown, int mana, FreezeStatusAsset freezeStatus,
            string description)
        {
            ConfigureMachineForEditor(id, "氷結", startup, recovery,
                cooldown, mana, description, Data.AllocationType.Ice);
            _freezeStatus = freezeStatus;
        }
#endif
    }
}
