using System;
using System.Collections.Generic;
using Pachimon.Data;
using Pachimon.Reward;
using Pachimon.Skills;
using Pachimon.Passives;

namespace Pachimon.Battle
{
    public sealed class SkillLogicRegistry
    {
        private readonly Dictionary<int, ISkillLogic> _logicBySkillId = new();

        public SkillLogicRegistry(
            SkillCatalog skillCatalog,
            PassiveCatalog passiveCatalog)
        {
            if (skillCatalog == null) throw new ArgumentNullException(nameof(skillCatalog));

            var basicLogicByType = new Dictionary<AllocationType, ISkillLogic>
            {
                [AllocationType.Fire] = new BasicAttributeDamageSkillLogic(PachimonAttribute.Fire),
                [AllocationType.Aqua] = new BasicAttributeDamageSkillLogic(PachimonAttribute.Aqua),
                [AllocationType.Leaf] = new BasicAttributeDamageSkillLogic(PachimonAttribute.Leaf),
                [AllocationType.Electric] = new ElectricShockSkillLogic(),
                [AllocationType.Poison] = new BasicAttributeDamageSkillLogic(PachimonAttribute.Poison),
                [AllocationType.Ice] = new ColdHandSkillLogic(),
                [AllocationType.Wind] = new BasicAttributeDamageSkillLogic(PachimonAttribute.Wind),
                [AllocationType.Dragon] = new BasicAttributeDamageSkillLogic(PachimonAttribute.Dragon),
            };

            foreach (var skill in skillCatalog.Skills)
            {
                if (skill == null) continue;
                if (skill.SkillId == SkillIdRanges.StruggleId)
                {
                    _logicBySkillId[skill.SkillId] = new StruggleSkillLogic();
                }
                else if (skill is AquaShockSkillAsset aquaShock)
                {
                    _logicBySkillId[skill.SkillId] =
                        new AquaShockSkillLogic(aquaShock);
                }
                else if (skill is BackfireSkillAsset backfire)
                {
                    _logicBySkillId[skill.SkillId] =
                        new BackfireSkillLogic(backfire);
                }
                else if (skill is FireArrowSkillAsset fireArrow)
                {
                    _logicBySkillId[skill.SkillId] =
                        new FireArrowSkillLogic(fireArrow);
                }
                else if (skill is CombustionSkillAsset combustion)
                {
                    _logicBySkillId[skill.SkillId] =
                        new CombustionSkillLogic(combustion);
                }
                else if (skill is ElectricExplosionSkillAsset electricExplosion)
                {
                    _logicBySkillId[skill.SkillId] =
                        new ElectricExplosionSkillLogic(
                            electricExplosion);
                }
                else if (skill is ElectricQuickAttackSkillAsset quickAttack)
                {
                    _logicBySkillId[skill.SkillId] =
                        new ElectricQuickAttackSkillLogic(
                            quickAttack);
                }
                else if (skill is ElectromagneticCannonSkillAsset cannon)
                {
                    _logicBySkillId[skill.SkillId] =
                        new ElectromagneticCannonSkillLogic(
                            cannon);
                }
                else if (skill is ChargeSkillAsset charge)
                {
                    _logicBySkillId[skill.SkillId] =
                        new ChargeSkillLogic(charge);
                }
                else if (skill.IsMapAssignable
                    && basicLogicByType.TryGetValue(skill.AllocationType, out var logic))
                {
                    _logicBySkillId[skill.SkillId] = logic;
                }
            }
        }

        public bool TryGet(int skillId, out ISkillLogic logic)
        {
            return _logicBySkillId.TryGetValue(skillId, out logic);
        }

        public void RegisterOrReplace(int skillId, ISkillLogic logic)
        {
            if (skillId <= 0) throw new ArgumentOutOfRangeException(nameof(skillId));
            _logicBySkillId[skillId] = logic ?? throw new ArgumentNullException(nameof(logic));
        }
    }
}
