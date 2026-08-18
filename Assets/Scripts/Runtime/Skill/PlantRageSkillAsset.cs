using Pachimon.Battle;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "PlantRageSkill", menuName = "Pachimon/Skills/Machine/Plant Rage")]
    public sealed class PlantRageSkillAsset : MachineExclusiveSkillAsset
    {
        [SerializeField] private ResponsivePlantFieldEffectAsset _responsivePlant;

        public ResponsivePlantFieldEffectAsset ResponsivePlant => _responsivePlant;

#if UNITY_EDITOR
        public void ConfigureForEditor(int id, int startup, int recovery,
            int cooldown, int mana, ResponsivePlantFieldEffectAsset plant,
            string description)
        {
            ConfigureMachineForEditor(id, "植物の怒り", startup, recovery,
                cooldown, mana, description, Data.AllocationType.Leaf);
            _responsivePlant = plant;
        }
#endif
    }
}
