using System;
using System.Linq;
using Pachimon.Data;
using Pachimon.Skills;

namespace Pachimon.Run
{
    public sealed class RunPachimonPoolGenerator
    {
        public const int SpeciesCount = PachimonCatalog.RequiredSpeciesCount;
        public const int InstancesPerSpecies = 2;
        public const int PoolSize = (SpeciesCount - 1) * InstancesPerSpecies;
        private readonly PachimonCatalog _catalog;
        private readonly SkillCatalog _skillCatalog;
        private readonly PachimonStatsGenerator _statsGenerator;

        public RunPachimonPoolGenerator(
            PachimonCatalog catalog,
            SkillCatalog skillCatalog,
            PachimonStatGenerationSettings statSettings = null)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _skillCatalog = skillCatalog ?? throw new ArgumentNullException(nameof(skillCatalog));
            _statsGenerator = new PachimonStatsGenerator(statSettings);
        }

        public RunPachimonPool Generate(int runSeed)
        {
            var random = new Random(unchecked(runSeed * 397) ^ 0x50414348);
            var catalogErrors = _catalog.ValidateContent();
            if (catalogErrors.Count > 0)
            {
                throw new InvalidOperationException(
                    "PachimonCatalog is invalid:\n" + string.Join("\n", catalogErrors));
            }

            var skillCatalogErrors = _skillCatalog.ValidateContent();
            if (skillCatalogErrors.Count > 0)
            {
                throw new InvalidOperationException(
                    "SkillCatalog is invalid:\n" + string.Join("\n", skillCatalogErrors));
            }

            var species = _catalog.Species.OrderBy(definition => definition.SpeciesId).ToArray();
            var excludedSpeciesId = species[random.Next(0, species.Length)].SpeciesId;
            var pool = new RunPachimonPool
            {
                ExcludedSpeciesId = excludedSpeciesId,
            };

            foreach (var definition in species)
            {
                var fixedSkill = _skillCatalog.Get(definition.FixedSkillId);
                if (fixedSkill == null)
                {
                    throw new InvalidOperationException(
                        $"Pachimon species {definition.SpeciesId} references missing fixed Skill {definition.FixedSkillId}.");
                }

                if (!fixedSkill.IsMapAssignable)
                {
                    throw new InvalidOperationException(
                        $"Fixed Skill {definition.FixedSkillId} for Pachimon species {definition.SpeciesId} must be Map-assignable.");
                }

                var speciesId = definition.SpeciesId;
                if (speciesId == excludedSpeciesId)
                {
                    continue;
                }

                for (var copyIndex = 1; copyIndex <= InstancesPerSpecies; copyIndex++)
                {
                    pool.Add(new PachimonInstance(
                        $"pachimon_{speciesId:D3}_{copyIndex}",
                        speciesId,
                        definition.AllocationType,
                        definition.FixedSkillId,
                        definition.PassiveId,
                        _statsGenerator.Generate(random)));
                }
            }

            if (pool.Instances.Count != PoolSize)
            {
                throw new InvalidOperationException(
                    $"Expected {PoolSize} run Pachimon, but generated {pool.Instances.Count}.");
            }

            return pool;
        }
    }
}
