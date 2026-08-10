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
                [AllocationType.Poison] = new PoisonNeedleSkillLogic(),
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
                else if (skill is WaterPulseSkillAsset waterPulse)
                {
                    _logicBySkillId[skill.SkillId] =
                        new WaterPulseSkillLogic(waterPulse);
                }
                else if (skill is LaunchCeremonySkillAsset launchCeremony)
                {
                    _logicBySkillId[skill.SkillId] =
                        new LaunchCeremonySkillLogic(launchCeremony);
                }
                else if (skill is WaterVeilSkillAsset waterVeil)
                {
                    _logicBySkillId[skill.SkillId] =
                        new WaterVeilSkillLogic(waterVeil);
                }
                else if (skill is SunbathSkillAsset sunbath)
                {
                    _logicBySkillId[skill.SkillId] = new SunbathSkillLogic(sunbath);
                }
                else if (skill is ChainVinesSkillAsset chainVines)
                {
                    _logicBySkillId[skill.SkillId] = new ChainVinesSkillLogic(chainVines);
                }
                else if (skill is SolarBeamSkillAsset solarBeam)
                {
                    _logicBySkillId[skill.SkillId] = new SolarBeamSkillLogic(solarBeam);
                }
                else if (skill is EntanglingVinesSkillAsset entanglingVines)
                {
                    _logicBySkillId[skill.SkillId] = new EntanglingVinesSkillLogic(entanglingVines);
                }
                else if (skill is ParalysisPowderSkillAsset paralysisPowder)
                {
                    _logicBySkillId[skill.SkillId] = new ParalysisPowderSkillLogic(paralysisPowder);
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
                else if (skill is ChainBurnSkillAsset chainBurn)
                {
                    _logicBySkillId[skill.SkillId] =
                        new ChainBurnSkillLogic(chainBurn);
                }
                else if (skill is FireBarrierSkillAsset fireBarrier)
                {
                    _logicBySkillId[skill.SkillId] =
                        new FireBarrierSkillLogic(fireBarrier);
                }
                else if (skill is SunnyDaySkillAsset sunnyDay)
                {
                    _logicBySkillId[skill.SkillId] =
                        new SunnyDaySkillLogic(sunnyDay);
                }
                else if (skill is RainDanceSkillAsset rainDance)
                {
                    _logicBySkillId[skill.SkillId] =
                        new RainDanceSkillLogic(rainDance);
                }
                else if (skill is HeavySnowSkillAsset heavySnow)
                {
                    _logicBySkillId[skill.SkillId] =
                        new HeavySnowSkillLogic(heavySnow);
                }
                else if (skill is IceShieldSkillAsset iceShield)
                {
                    _logicBySkillId[skill.SkillId] =
                        new IceShieldSkillLogic(iceShield);
                }
                else if (skill is IceShardSkillAsset iceShard)
                {
                    _logicBySkillId[skill.SkillId] =
                        new IceShardSkillLogic(iceShard);
                }
                else if (skill is IceBladeSkillAsset iceBlade)
                {
                    _logicBySkillId[skill.SkillId] =
                        new IceBladeSkillLogic(iceBlade);
                }
                else if (skill is FrozenBreakSkillAsset frozenBreak)
                {
                    _logicBySkillId[skill.SkillId] =
                        new FrozenBreakSkillLogic(frozenBreak);
                }
                else if (skill is WindStormSkillAsset windStorm)
                {
                    _logicBySkillId[skill.SkillId] =
                        new WindStormSkillLogic(windStorm);
                }
                else if (skill is FlyingAttackSkillAsset flyingAttack)
                {
                    _logicBySkillId[skill.SkillId] =
                        new FlyingAttackSkillLogic(flyingAttack);
                }
                else if (skill is WindErosionSkillAsset windErosion)
                {
                    _logicBySkillId[skill.SkillId] =
                        new WindErosionSkillLogic(windErosion);
                }
                else if (skill is HealingWindSkillAsset healingWind)
                {
                    _logicBySkillId[skill.SkillId] =
                        new HealingWindSkillLogic(healingWind);
                }
                else if (skill is SecondWindSkillAsset secondWind)
                {
                    _logicBySkillId[skill.SkillId] =
                        new SecondWindSkillLogic(secondWind);
                }
                else if (skill is DragonJabSkillAsset dragonJab)
                {
                    _logicBySkillId[skill.SkillId] =
                        new DragonJabSkillLogic(dragonJab);
                }
                else if (skill is DragonFootworkSkillAsset dragonFootwork)
                {
                    _logicBySkillId[skill.SkillId] =
                        new DragonFootworkSkillLogic(dragonFootwork);
                }
                else if (skill is DragonDanceSkillAsset dragonDance)
                {
                    _logicBySkillId[skill.SkillId] =
                        new DragonDanceSkillLogic(dragonDance);
                }
                else if (skill is DragonBreakSkillAsset dragonBreak)
                {
                    _logicBySkillId[skill.SkillId] =
                        new DragonBreakSkillLogic(dragonBreak);
                }
                else if (skill is DragonHookSkillAsset dragonHook)
                {
                    _logicBySkillId[skill.SkillId] =
                        new DragonHookSkillLogic(dragonHook);
                }
                else if (skill is DragonUpperSkillAsset dragonUpper)
                {
                    _logicBySkillId[skill.SkillId] =
                        new DragonUpperSkillLogic(dragonUpper);
                }
                else if (skill is DragonDefenseSkillAsset dragonDefense)
                {
                    _logicBySkillId[skill.SkillId] =
                        new DragonDefenseSkillLogic(dragonDefense);
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
                else if (skill is SmogSkillAsset smog)
                {
                    _logicBySkillId[skill.SkillId] =
                        new SmogSkillLogic(smog);
                }
                else if (skill is NeurotoxinSkillAsset neurotoxin)
                {
                    _logicBySkillId[skill.SkillId] =
                        new NeurotoxinSkillLogic(neurotoxin);
                }
                else if (skill is ToxinTransferSkillAsset toxinTransfer)
                {
                    _logicBySkillId[skill.SkillId] =
                        new ToxinTransferSkillLogic(toxinTransfer);
                }
                else if (skill is ToxinExplosionSkillAsset toxinExplosion)
                {
                    _logicBySkillId[skill.SkillId] =
                        new ToxinExplosionSkillLogic(toxinExplosion);
                }
                else if (skill is PoisonShieldSkillAsset poisonShield)
                {
                    _logicBySkillId[skill.SkillId] =
                        new PoisonShieldSkillLogic(poisonShield);
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
