using System.Collections.Generic;
using Pachimon.Battle;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "SexyPoseSkill", menuName = "Pachimon/Skills/Machine/Sexy Pose")]
    public sealed class SexyPoseSkillAsset : MachineExclusiveSkillAsset
    {
        [SerializeField, Min(1)] private int _charmStacks = 15;
        [SerializeField, Min(0)] private int _stunRatio = 100;
        [SerializeField] private CharmStatusAsset _charmStatus;
        [SerializeField] private StunStatusAsset _stunStatus;

        public int CharmStacks => _charmStacks;
        public int StunRatio => _stunRatio;
        public CharmStatusAsset CharmStatus => _charmStatus;
        public StunStatusAsset StunStatus => _stunStatus;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_charmStatus == null)
                errors?.Add($"Skill {SkillId}: Charm Status is required.");
            if (_stunStatus == null)
                errors?.Add($"Skill {SkillId}: Stun Status is required.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int id,
            int charmStacks,
            int stunRatio,
            CharmStatusAsset charmStatus,
            StunStatusAsset stunStatus,
            string description)
        {
            ConfigureMachineForEditor(id, "セクシーポーズ", 100, 100,
                200, 50, description);
            _charmStacks = charmStacks;
            _stunRatio = stunRatio;
            _charmStatus = charmStatus;
            _stunStatus = stunStatus;
        }
#endif
    }
}
