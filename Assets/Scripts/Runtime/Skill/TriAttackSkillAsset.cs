using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "TriAttackSkill", menuName = "Pachimon/Skills/Machine/Tri Attack")]
    public sealed class TriAttackSkillAsset : MachineExclusiveSkillAsset
    {
        [SerializeField, Min(0)] private int _baseDamage = 100;
        [SerializeField, HideInInspector] private int _attributeRatio = 100;

        public int BaseDamage => _baseDamage;
        public int AttributeRatio => AttributeDamageRules.ScalingRatio;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int id,
            int startup,
            int recovery,
            int cooldown,
            int mana,
            int baseDamage,
            int attributeRatio,
            string description)
        {
            ConfigureMachineForEditor(id, "トライアタック", startup, recovery,
                cooldown, mana, description);
            _baseDamage = baseDamage;
            _attributeRatio = attributeRatio;
        }
#endif
    }
}
