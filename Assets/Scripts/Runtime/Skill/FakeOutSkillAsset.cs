using System.Collections.Generic;
using Pachimon.Battle;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "FakeOutSkill", menuName = "Pachimon/Skills/Machine/Fake Out")]
    public sealed class FakeOutSkillAsset : MachineExclusiveSkillAsset
    {
        [SerializeField, Min(0)] private int _trueDamage = 50;
        [SerializeField, Min(1)] private int _stunTicks = 100;
        [SerializeField] private StunStatusAsset _stunStatus;

        public int TrueDamage => _trueDamage;
        public int StunTicks => _stunTicks;
        public StunStatusAsset StunStatus => _stunStatus;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_stunStatus == null)
                errors?.Add($"Skill {SkillId}: Stun Status is required.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int id,
            int trueDamage,
            int stunTicks,
            StunStatusAsset stunStatus,
            string description)
        {
            ConfigureMachineForEditor(id, "ねこだまし", 0, 0, 0, 0, description);
            _trueDamage = trueDamage;
            _stunTicks = stunTicks;
            _stunStatus = stunStatus;
        }
#endif
    }
}
