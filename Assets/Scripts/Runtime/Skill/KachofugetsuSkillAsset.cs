using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "KachofugetsuSkill", menuName = "Pachimon/Skills/Kachofugetsu")]
    public sealed class KachofugetsuSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _baseFireDamage = 50;
        [SerializeField, HideInInspector] private int _fireDamageRatio = 100;
        [SerializeField, Min(0)] private int _baseAquaDamage = 50;
        [SerializeField, HideInInspector] private int _aquaDamageRatio = 100;
        [SerializeField, Min(0)] private int _baseWindDamage = 50;
        [SerializeField, HideInInspector] private int _windDamageRatio = 100;
        public int BaseFireDamage => _baseFireDamage;
        public int FireDamageRatio => AttributeDamageRules.ScalingRatio;
        public int BaseAquaDamage => _baseAquaDamage;
        public int AquaDamageRatio => AttributeDamageRules.ScalingRatio;
        public int BaseWindDamage => _baseWindDamage;
        public int WindDamageRatio => AttributeDamageRules.ScalingRatio;
#if UNITY_EDITOR
        public void ConfigureForEditor(int id, string name, int recovery,
            int cooldown, int mana, string description, int baseFireDamage,
            int fireDamageRatio, int baseAquaDamage, int aquaDamageRatio,
            int baseWindDamage, int windDamageRatio)
        {
            base.ConfigureForEditor(id, name, AllocationType.Wind, true,
                recovery, cooldown, description, mana);
            _baseFireDamage = baseFireDamage;
            _fireDamageRatio = fireDamageRatio;
            _baseAquaDamage = baseAquaDamage;
            _aquaDamageRatio = aquaDamageRatio;
            _baseWindDamage = baseWindDamage;
            _windDamageRatio = windDamageRatio;
        }
#endif
    }
}
