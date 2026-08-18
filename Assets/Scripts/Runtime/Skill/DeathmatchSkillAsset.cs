using Pachimon.Battle;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "DeathmatchSkill", menuName = "Pachimon/Skills/Machine/Deathmatch")]
    public sealed class DeathmatchSkillAsset : MachineExclusiveSkillAsset
    {
        [SerializeField] private ToxinStatusAsset _toxinStatus;
        public ToxinStatusAsset ToxinStatus => _toxinStatus;

#if UNITY_EDITOR
        public void ConfigureForEditor(int id, int startup, int recovery,
            int cooldown, int mana, ToxinStatusAsset toxinStatus,
            string description)
        {
            ConfigureMachineForEditor(id, "デスマッチ", startup, recovery,
                cooldown, mana, description, Data.AllocationType.Poison);
            _toxinStatus = toxinStatus;
        }
#endif
    }
}
