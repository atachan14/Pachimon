using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "BodySlamSkill", menuName = "Pachimon/Skills/Machine/Body Slam")]
    public sealed class BodySlamSkillAsset : MachineExclusiveSkillAsset
    {
        [SerializeField, Min(0)] private int _currentHpPercent = 10;

        public int CurrentHpPercent => _currentHpPercent;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int id,
            int startup,
            int recovery,
            int cooldown,
            int mana,
            int currentHpPercent,
            string description)
        {
            ConfigureMachineForEditor(id, "のしかかり", startup, recovery,
                cooldown, mana, description);
            _currentHpPercent = currentHpPercent;
        }
#endif
    }
}
