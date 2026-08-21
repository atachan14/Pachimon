using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "WindGunSkill", menuName = "Pachimon/Skills/Wind Gun")]
    public sealed class WindGunSkillAsset : InitialAttributeDamageSkillAsset
    {
#if UNITY_EDITOR
        public void ConfigureForEditor(int id, string name, int recovery,
            int cooldown, int mana, string description, int damage, int ratio)
        {
            ConfigureInitialSkillForEditor(id, name, AllocationType.Wind,
                recovery, cooldown, mana, description, damage, ratio);
        }
#endif
    }
}
