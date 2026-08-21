using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "DragonStraightSkill", menuName = "Pachimon/Skills/Dragon Straight")]
    public sealed class DragonStraightSkillAsset : InitialAttributeDamageSkillAsset
    {
#if UNITY_EDITOR
        public void ConfigureForEditor(int id, string name, int recovery,
            int cooldown, int mana, string description, int damage, int ratio)
        {
            ConfigureInitialSkillForEditor(id, name, AllocationType.Dragon,
                recovery, cooldown, mana, description, damage, ratio);
        }
#endif
    }
}
