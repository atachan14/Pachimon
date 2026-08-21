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
                else if (skill is EmberSkillAsset ember)
                {
                    _logicBySkillId[skill.SkillId] =
                        new BasicAttributeDamageSkillLogic(
                            ember,
                            PachimonAttribute.Fire);
                }
                else if (skill is WaterGunSkillAsset waterGun)
                {
                    _logicBySkillId[skill.SkillId] =
                        new BasicAttributeDamageSkillLogic(
                            waterGun,
                            PachimonAttribute.Aqua);
                }
                else if (skill is LeafSlicerSkillAsset leafSlicer)
                {
                    _logicBySkillId[skill.SkillId] =
                        new BasicAttributeDamageSkillLogic(
                            leafSlicer,
                            PachimonAttribute.Leaf);
                }
                else if (skill is ElectricShockSkillAsset electricShock)
                {
                    _logicBySkillId[skill.SkillId] =
                        new ElectricShockSkillLogic(electricShock);
                }
                else if (skill is PoisonNeedleSkillAsset poisonNeedle)
                {
                    _logicBySkillId[skill.SkillId] =
                        new PoisonNeedleSkillLogic(poisonNeedle);
                }
                else if (skill is ColdHandSkillAsset coldHand)
                {
                    _logicBySkillId[skill.SkillId] =
                        new ColdHandSkillLogic(coldHand);
                }
                else if (skill is WindGunSkillAsset windGun)
                {
                    _logicBySkillId[skill.SkillId] =
                        new BasicAttributeDamageSkillLogic(
                            windGun,
                            PachimonAttribute.Wind);
                }
                else if (skill is DragonStraightSkillAsset dragonStraight)
                {
                    _logicBySkillId[skill.SkillId] =
                        new BasicAttributeDamageSkillLogic(
                            dragonStraight,
                            PachimonAttribute.Dragon);
                }
                else if (skill is TriAttackSkillAsset triAttack)
                {
                    _logicBySkillId[skill.SkillId] = new TriAttackSkillLogic(triAttack);
                }
                else if (skill is BodySlamSkillAsset bodySlam)
                {
                    _logicBySkillId[skill.SkillId] = new BodySlamSkillLogic(bodySlam);
                }
                else if (skill is FakeOutSkillAsset fakeOut)
                {
                    _logicBySkillId[skill.SkillId] = new FakeOutSkillLogic(fakeOut);
                }
                else if (skill is DestructionBeamSkillAsset destructionBeam)
                {
                    _logicBySkillId[skill.SkillId] =
                        new DestructionBeamSkillLogic(destructionBeam);
                }
                else if (skill is SexyPoseSkillAsset sexyPose)
                {
                    _logicBySkillId[skill.SkillId] = new SexyPoseSkillLogic(sexyPose);
                }
                else if (skill is IntangibilitySkillAsset intangibility)
                {
                    _logicBySkillId[skill.SkillId] =
                        new IntangibilitySkillLogic(intangibility);
                }
                else if (skill is SpiritBombSkillAsset spiritBomb)
                {
                    _logicBySkillId[skill.SkillId] =
                        new SpiritBombSkillLogic(spiritBomb);
                }
                else if (skill is CloneTechniqueSkillAsset cloneTechnique)
                {
                    _logicBySkillId[skill.SkillId] =
                        new CloneTechniqueSkillLogic(cloneTechnique);
                }
                else if (skill is BurningStrikeSkillAsset burningStrike)
                {
                    _logicBySkillId[skill.SkillId] =
                        new BurningStrikeSkillLogic(burningStrike);
                }
                else if (skill is WaterPulseReplacementSkillAsset regularWaterPulse)
                {
                    _logicBySkillId[skill.SkillId] =
                        new WaterPulseReplacementSkillLogic(regularWaterPulse);
                }
                else if (skill is PlantRageSkillAsset plantRage)
                {
                    _logicBySkillId[skill.SkillId] =
                        new PlantRageSkillLogic(plantRage);
                }
                else if (skill is ChainThunderSkillAsset chainThunder)
                {
                    _logicBySkillId[skill.SkillId] =
                        new ChainThunderSkillLogic(chainThunder);
                }
                else if (skill is DeathmatchSkillAsset deathmatch)
                {
                    _logicBySkillId[skill.SkillId] =
                        new DeathmatchSkillLogic(deathmatch);
                }
                else if (skill is FreezingSkillAsset freezing)
                {
                    _logicBySkillId[skill.SkillId] =
                        new FreezingSkillLogic(freezing);
                }
                else if (skill is WindGodSkillAsset windGod)
                {
                    _logicBySkillId[skill.SkillId] =
                        new WindGodSkillLogic(windGod);
                }
                else if (skill is DragonInstallSkillAsset dragonInstall)
                {
                    _logicBySkillId[skill.SkillId] =
                        new DragonInstallSkillLogic(dragonInstall);
                }
                else if (skill is AquaShockSkillAsset aquaShock)
                {
                    _logicBySkillId[skill.SkillId] =
                        new AquaShockSkillLogic(aquaShock);
                }
                else if (skill is LightningCloudSkillAsset lightningCloud)
                {
                    _logicBySkillId[skill.SkillId] =
                        new LightningCloudSkillLogic(lightningCloud);
                }
                else if (skill is ElectricShieldSkillAsset electricShield)
                {
                    _logicBySkillId[skill.SkillId] =
                        new ElectricShieldSkillLogic(electricShield);
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
                else if (skill is WaterCutterSkillAsset waterCutter)
                {
                    _logicBySkillId[skill.SkillId] =
                        new WaterCutterSkillLogic(waterCutter);
                }
                else if (skill is MuddyWaterSkillAsset muddyWater)
                {
                    _logicBySkillId[skill.SkillId] =
                        new MuddyWaterSkillLogic(muddyWater);
                }
                else if (skill is WaterSpoutSkillAsset waterSpout)
                {
                    _logicBySkillId[skill.SkillId] =
                        new WaterSpoutSkillLogic(waterSpout);
                }
                else if (skill is EvaporationSkillAsset evaporation)
                {
                    _logicBySkillId[skill.SkillId] =
                        new EvaporationSkillLogic(evaporation);
                }
                else if (skill is BeatVineSkillAsset beatVine)
                {
                    _logicBySkillId[skill.SkillId] =
                        new BeatVineSkillLogic(beatVine);
                }
                else if (skill is FireVineSkillAsset fireVine)
                {
                    _logicBySkillId[skill.SkillId] =
                        new FireVineSkillLogic(fireVine);
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
                else if (skill is IcePebbleSkillAsset icePebble)
                {
                    _logicBySkillId[skill.SkillId] =
                        new IcePebbleSkillLogic(icePebble);
                }
                else if (skill is FrostArrowSkillAsset frostArrow)
                {
                    _logicBySkillId[skill.SkillId] =
                        new FrostArrowSkillLogic(frostArrow);
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
                else if (skill is CuttingDanceSkillAsset cuttingDance)
                {
                    _logicBySkillId[skill.SkillId] =
                        new CuttingDanceSkillLogic(cuttingDance);
                }
                else if (skill is KachofugetsuSkillAsset kachofugetsu)
                {
                    _logicBySkillId[skill.SkillId] =
                        new KachofugetsuSkillLogic(kachofugetsu);
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
                else if (skill is PoisonMistSkillAsset poisonMist)
                {
                    _logicBySkillId[skill.SkillId] =
                        new PoisonMistSkillLogic(poisonMist);
                }
                else if (skill is FirstTouchSkillAsset firstTouch)
                {
                    _logicBySkillId[skill.SkillId] =
                        new FirstTouchSkillLogic(firstTouch);
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
