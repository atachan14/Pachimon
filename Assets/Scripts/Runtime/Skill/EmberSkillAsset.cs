using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "EmberSkill", menuName = "Pachimon/Skills/Ember")]
    public sealed class EmberSkillAsset : InitialAttributeDamageSkillAsset
    {
#if UNITY_EDITOR
        public void ConfigureForEditor(int id, string name, int recovery,
            int cooldown, int mana, string description, int damage, int ratio)
        {
            ConfigureInitialSkillForEditor(id, name, AllocationType.Fire,
                recovery, cooldown, mana, description, damage, ratio);
        }
#endif
    }
}
