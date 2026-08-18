using Pachimon.Battle;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "DragonInstallSkill", menuName = "Pachimon/Skills/Machine/Dragon Install")]
    public sealed class DragonInstallSkillAsset : MachineExclusiveSkillAsset
    {
        [SerializeField, Min(1)] private int _durationTicks = 400;
        [SerializeField, Min(0)] private int _multiplierPercent = 200;
        [SerializeField] private DragonInstallStatusAsset _status;

        public int DurationTicks => _durationTicks;
        public int MultiplierPercent => _multiplierPercent;
        public DragonInstallStatusAsset Status => _status;

#if UNITY_EDITOR
        public void ConfigureForEditor(int id, int startup, int recovery,
            int cooldown, int mana, int durationTicks,
            int multiplierPercent, DragonInstallStatusAsset status,
            string description)
        {
            ConfigureMachineForEditor(id, "ドラゴンインストール", startup,
                recovery, cooldown, mana, description,
                Data.AllocationType.Dragon);
            _durationTicks = durationTicks;
            _multiplierPercent = multiplierPercent;
            _status = status;
        }
#endif
    }
}
