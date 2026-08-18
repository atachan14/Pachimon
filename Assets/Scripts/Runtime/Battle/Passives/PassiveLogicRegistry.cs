using System;
using System.Collections.Generic;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Passives;

namespace Pachimon.Battle
{
    public sealed class PassiveLogicRegistry
    {
        public const int FirstPlaceholderPassiveId = 1;
        public const int LastPlaceholderPassiveId = 151;

        private static readonly PachimonAttribute[] PlaceholderAttributes =
        {
            PachimonAttribute.Fire,
            PachimonAttribute.Aqua,
            PachimonAttribute.Leaf,
            PachimonAttribute.Electric,
            PachimonAttribute.Poison,
            PachimonAttribute.Ice,
            PachimonAttribute.Wind,
            PachimonAttribute.Dragon,
        };

        private readonly Dictionary<int, Func<BattleUnitState, IPassiveLogic>>
            _factoriesByPassiveId = new();

        public PassiveLogicRegistry(PassiveCatalog passiveCatalog = null)
        {
            for (var passiveId = FirstPlaceholderPassiveId;
                 passiveId <= LastPlaceholderPassiveId;
                 passiveId++)
            {
                var capturedId = passiveId;
                if (passiveCatalog?.Get(passiveId)
                    is StoredChargePassiveAsset storedCharge)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new StoredChargePassiveLogic(owner, storedCharge);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is StaticElectricityPassiveAsset staticElectricity)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new StaticElectricityPassiveLogic(
                            owner,
                            staticElectricity);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is ThunderManPassiveAsset thunderMan)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new ThunderManPassiveLogic(owner, thunderMan);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is ParalysisGenerationPassiveAsset paralysisGeneration)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new ParalysisGenerationPassiveLogic(
                            owner,
                            paralysisGeneration);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is FieldValueAmplificationPassiveAsset fieldAmplification)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new FieldValueAmplificationPassiveLogic(
                            owner,
                            fieldAmplification);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is ToxinGrowthPassiveAsset toxinGrowth)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new ToxinGrowthPassiveLogic(owner, toxinGrowth);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is PoisonKnightPassiveAsset poisonKnight)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new PoisonKnightPassiveLogic(owner, poisonKnight);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is PoisonMagicianPassiveAsset poisonMagician)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new PoisonMagicianPassiveLogic(owner, poisonMagician);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is LastTouchPassiveAsset lastTouch)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new LastTouchPassiveLogic(owner, lastTouch);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is FireGrowthOnDamagePassiveAsset fireGrowth)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new FireGrowthOnDamagePassiveLogic(owner, fireGrowth);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is DarkFlamePassiveAsset darkFlame)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new DarkFlamePassiveLogic(owner, darkFlame);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is FireArcherPassiveAsset fireArcher)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new FireArcherPassiveLogic(owner, fireArcher);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is BurnPursuitPassiveAsset burnPursuit)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new BurnPursuitPassiveLogic(owner, burnPursuit);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is SunnyManPassiveAsset sunnyMan)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new SunnyManPassiveLogic(owner, sunnyMan);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is RainManPassiveAsset rainMan)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new RainManPassiveLogic(owner, rainMan);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is LifeWaterPassiveAsset lifeWater)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new LifeWaterPassiveLogic(owner, lifeWater);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is WaterBlessingPassiveAsset waterBlessing)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new WaterBlessingPassiveLogic(owner, waterBlessing);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is WaterCuttingPassiveAsset waterCutting)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new WaterCuttingPassiveLogic(owner, waterCutting);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is WeaklingBullyPassiveAsset weaklingBully)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new WeaklingBullyPassiveLogic(owner, weaklingBully);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is BotanicalGardenPassiveAsset botanicalGarden)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new BotanicalGardenPassiveLogic(owner, botanicalGarden);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is BurningFlowerPassiveAsset burningFlower)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new BurningFlowerPassiveLogic(owner, burningFlower);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is HealthyPlantPassiveAsset healthyPlant)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new HealthyPlantPassiveLogic(owner, healthyPlant);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is EntanglingVinePassiveAsset entanglingVine)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new EntanglingVinePassiveLogic(owner, entanglingVine);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is WarmPlantPassiveAsset warmPlant)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new WarmPlantPassiveLogic(owner, warmPlant);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is SturdyPlantPassiveAsset sturdyPlant)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new SturdyPlantPassiveLogic(owner, sturdyPlant);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is PowderPlantPassiveAsset powderPlant)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new PowderPlantPassiveLogic(owner, powderPlant);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is RunningStartPassiveAsset runningStart)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new RunningStartPassiveLogic(owner, runningStart);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is WindBlessingPassiveAsset windBlessing)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new WindBlessingPassiveLogic(owner, windBlessing);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is WeatherChildPassiveAsset weatherChild)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new WeatherChildPassiveLogic(owner, weatherChild);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is WindRiderPassiveAsset windRider)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new WindRiderPassiveLogic(owner, windRider);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is WindMagicianPassiveAsset windMagician)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new WindMagicianPassiveLogic(owner, windMagician);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is TeamAttributeDamagePassiveAsset teamAttributeDamage)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new TeamAttributeDamagePassiveLogic(
                            owner,
                            teamAttributeDamage);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is ResistAdvantageDamagePassiveAsset resistAdvantage)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new ResistAdvantageDamagePassiveLogic(
                            owner,
                            resistAdvantage);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is IncomingAttributeDamagePassiveAsset incomingDamage)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new IncomingAttributeDamagePassiveLogic(
                            owner,
                            incomingDamage);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is TargetSlowDamagePassiveAsset targetSlowDamage)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new TargetSlowDamagePassiveLogic(
                            owner,
                            targetSlowDamage);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is TargetStatusDamagePassiveAsset targetStatusDamage)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new TargetStatusDamagePassiveLogic(
                            owner,
                            targetStatusDamage);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is FrozenGroundPassiveAsset frozenGround)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new FrozenGroundPassiveLogic(owner, frozenGround);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is IceGrowthOnDamagePassiveAsset iceGrowth)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new IceGrowthOnDamagePassiveLogic(owner, iceGrowth);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is IceWitchPassiveAsset iceWitch)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new IceWitchPassiveLogic(owner, iceWitch);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is IceArmorPassiveAsset iceArmor)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new IceArmorPassiveLogic(owner, iceArmor);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is ChillSpreadPassiveAsset chillSpread)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new ChillSpreadPassiveLogic(owner, chillSpread);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is ComboMasterPassiveAsset comboMaster)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new ComboMasterPassiveLogic(owner, comboMaster);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is DragonBoxerPassiveAsset dragonBoxer)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new DragonBoxerPassiveLogic(owner, dragonBoxer);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is SweetSciencePassiveAsset sweetScience)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new SweetSciencePassiveLogic(owner, sweetScience);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is DragonSkeletonPassiveAsset dragonSkeleton)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new DragonSkeletonPassiveLogic(owner, dragonSkeleton);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is DragonRagePassiveAsset dragonRage)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new DragonRagePassiveLogic(owner, dragonRage);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is ManyHitsPassiveAsset manyHits)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new ManyHitsPassiveLogic(owner, manyHits);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is OutgoingAttributeDamagePassiveAsset outgoingAttribute)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new OutgoingAttributeDamagePassiveLogic(
                            owner,
                            outgoingAttribute);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is DragonGuardPassiveAsset dragonGuard)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new DragonGuardPassiveLogic(owner, dragonGuard);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId) != null)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new StatOnlyPassiveLogic(owner);
                    continue;
                }

                var attribute = PlaceholderAttributes[
                    (passiveId - FirstPlaceholderPassiveId) % PlaceholderAttributes.Length];
                _factoriesByPassiveId[passiveId] = owner =>
                    new OutgoingAttributeDamagePassiveLogic(capturedId, owner, attribute);
            }
        }

        public static bool TryGetPlaceholderAttribute(
            int passiveId,
            PassiveCatalog passiveCatalog,
            out PachimonAttribute attribute)
        {
            if (passiveId < FirstPlaceholderPassiveId
                || passiveId > LastPlaceholderPassiveId
                || passiveCatalog?.Get(passiveId) != null)
            {
                attribute = default;
                return false;
            }

            attribute = PlaceholderAttributes[
                (passiveId - FirstPlaceholderPassiveId) % PlaceholderAttributes.Length];
            return true;
        }

        public IPassiveLogic Create(int passiveId, BattleUnitState owner)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (!_factoriesByPassiveId.TryGetValue(passiveId, out var factory))
            {
                throw new InvalidOperationException(
                    $"Passive {passiveId} has no registered Logic.");
            }

            return factory(owner);
        }

        public void RegisterOrReplace(
            int passiveId,
            Func<BattleUnitState, IPassiveLogic> factory)
        {
            if (passiveId <= 0) throw new ArgumentOutOfRangeException(nameof(passiveId));
            _factoriesByPassiveId[passiveId] = factory
                ?? throw new ArgumentNullException(nameof(factory));
        }

        private sealed class StatOnlyPassiveLogic : IPassiveLogic
        {
            public StatOnlyPassiveLogic(BattleUnitState owner)
            {
                Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            }

            public BattleUnitState Owner { get; }

            public void Handle(IBattleEvent battleEvent)
            {
            }
        }
    }
}
