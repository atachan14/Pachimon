using Pachimon.Battle;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "WindGodSkill", menuName = "Pachimon/Skills/Machine/Wind God")]
    public sealed class WindGodSkillAsset : MachineExclusiveSkillAsset
    {
        [SerializeField, Min(0)] private int _baseDamage = 500;
        [SerializeField, Min(0)] private int _windRatio = 100;
        [SerializeField, Min(1)] private int _durationTicks = 300;
        [SerializeField] private WindGodStatusAsset _status;

        public int BaseDamage => _baseDamage;
        public int WindRatio => _windRatio;
        public int DurationTicks => _durationTicks;
        public WindGodStatusAsset Status => _status;

#if UNITY_EDITOR
        public void ConfigureForEditor(int id, int startup, int recovery,
            int cooldown, int mana, int baseDamage, int windRatio,
            int durationTicks, WindGodStatusAsset status, string description)
        {
            ConfigureMachineForEditor(id, "風神", startup, recovery,
                cooldown, mana, description, Data.AllocationType.Wind);
            _baseDamage = baseDamage;
            _windRatio = windRatio;
            _durationTicks = durationTicks;
            _status = status;
        }
#endif
    }
}
