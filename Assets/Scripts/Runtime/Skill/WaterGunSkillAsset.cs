using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "WaterGunSkill", menuName = "Pachimon/Skills/Water Gun")]
    public sealed class WaterGunSkillAsset : InitialAttributeDamageSkillAsset
    {
#if UNITY_EDITOR
        public void ConfigureForEditor(int id, string name, int recovery,
            int cooldown, int mana, string description, int damage, int ratio)
        {
            ConfigureInitialSkillForEditor(id, name, AllocationType.Aqua,
                recovery, cooldown, mana, description, damage, ratio);
        }
#endif
    }
}
