using System.Collections.Generic;
using System.Linq;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "SkillCatalog", menuName = "Pachimon/Skills/Skill Catalog")]
    public sealed class SkillCatalog : ScriptableObject
    {
        [SerializeField] private List<SkillAsset> _skills = new();

        public IReadOnlyList<SkillAsset> Skills => _skills;

        public SkillAsset Get(int skillId)
        {
            return _skills.FirstOrDefault(skill => skill != null && skill.SkillId == skillId);
        }

        public IReadOnlyList<SkillAsset> GetMapAssignableSkills()
        {
            return _skills.Where(skill => skill != null && skill.IsMapAssignable).ToArray();
        }

        public IReadOnlyList<SkillAsset> GetMapAssignableSkills(AllocationType allocationType)
        {
            return _skills.Where(skill => skill != null
                    && skill.IsMapAssignable
                    && skill.AllocationType == allocationType)
                .ToArray();
        }

        public IReadOnlyList<string> ValidateContent()
        {
            var errors = new List<string>();
            var validSkills = _skills.Where(skill => skill != null).ToArray();

            if (validSkills.Length != _skills.Count)
            {
                errors.Add("SkillCatalog contains a null entry.");
            }

            foreach (var duplicateId in validSkills
                         .GroupBy(skill => skill.SkillId)
                         .Where(group => group.Count() > 1)
                         .Select(group => group.Key))
            {
                errors.Add($"Duplicate Skill ID: {duplicateId}");
            }

            foreach (var skill in validSkills)
            {
                skill.CollectValidationErrors(errors);
            }

            for (var skillId = SkillIdRanges.FirstMapAssignableId;
                 skillId <= SkillIdRanges.LastMapAssignableId;
                 skillId++)
            {
                var skill = validSkills.FirstOrDefault(item => item.SkillId == skillId);
                if (skill == null)
                {
                    errors.Add($"Map-assignable Skill ID {skillId} is missing.");
                }
                else if (!skill.IsMapAssignable)
                {
                    errors.Add($"Skill {skillId} must be Map-assignable.");
                }
            }

            var struggle = validSkills.FirstOrDefault(skill => skill.SkillId == SkillIdRanges.StruggleId);
            if (struggle == null)
            {
                errors.Add($"System Skill {SkillIdRanges.StruggleId} (Struggle) is missing.");
            }
            else if (struggle.IsMapAssignable)
            {
                errors.Add("Struggle cannot be Map-assignable.");
            }

            return errors;
        }

#if UNITY_EDITOR
        public void SetSkillsForEditor(IEnumerable<SkillAsset> skills)
        {
            _skills = new List<SkillAsset>(skills);
        }
#endif
    }
}
