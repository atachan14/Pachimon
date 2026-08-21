using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "LeafSlicerSkill", menuName = "Pachimon/Skills/Leaf Slicer")]
    public sealed class LeafSlicerSkillAsset : InitialAttributeDamageSkillAsset
    {
#if UNITY_EDITOR
        public void ConfigureForEditor(int id, string name, int recovery,
            int cooldown, int mana, string description, int damage, int ratio)
        {
            ConfigureInitialSkillForEditor(id, name, AllocationType.Leaf,
                recovery, cooldown, mana, description, damage, ratio);
        }
#endif
    }
}
