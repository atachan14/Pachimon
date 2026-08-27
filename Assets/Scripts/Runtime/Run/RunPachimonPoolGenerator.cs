using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Data;
using Pachimon.Skills;

namespace Pachimon.Run
{
    public sealed class RunPachimonPoolGenerator
    {
        public const int PoolSize = 300;
        public const int MaximumParticipatingSpecies = 150;
        public const int MinimumSpeciesPerAllocationType = 4;
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

            var enabledSpecies = _catalog.Species
                .Where(definition => definition != null && definition.IsRunEnabled)
                .OrderBy(definition => definition.SpeciesId)
                .ToArray();
            ValidateEnabledSpecies(enabledSpecies);

            var participatingSpecies = enabledSpecies.ToList();
            Shuffle(participatingSpecies, random);
            var excludedSpeciesId = 0;
            if (participatingSpecies.Count > MaximumParticipatingSpecies)
            {
                excludedSpeciesId = participatingSpecies[MaximumParticipatingSpecies].SpeciesId;
                participatingSpecies.RemoveRange(
                    MaximumParticipatingSpecies,
                    participatingSpecies.Count - MaximumParticipatingSpecies);
            }

            var baseInstanceCount = PoolSize / participatingSpecies.Count;
            var extraInstanceCount = PoolSize % participatingSpecies.Count;
            var pool = new RunPachimonPool
            {
                ExcludedSpeciesId = excludedSpeciesId,
            };

            for (var speciesIndex = 0;
                 speciesIndex < participatingSpecies.Count;
                 speciesIndex++)
            {
                var definition = participatingSpecies[speciesIndex];
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
                var instanceCount = baseInstanceCount
                    + (speciesIndex < extraInstanceCount ? 1 : 0);
                for (var copyIndex = 1; copyIndex <= instanceCount; copyIndex++)
                {
                    pool.Add(new PachimonInstance(
                        $"pachimon_{speciesId:D3}_{copyIndex:D2}",
                        speciesId,
                        definition.AllocationType,
                        definition.FixedSkillId,
                        definition.PassiveId,
                        _statsGenerator.Generate(random, definition),
                        PachimonSubStatBindings.CreateRandom(
                            random,
                            definition.InitialStats)));
                }
            }

            if (pool.Instances.Count != PoolSize)
            {
                throw new InvalidOperationException(
                    $"Expected {PoolSize} run Pachimon, but generated {pool.Instances.Count}.");
            }

            return pool;
        }

        private void ValidateEnabledSpecies(
            IReadOnlyCollection<PachimonSpeciesAsset> enabledSpecies)
        {
            if (enabledSpecies.Count == 0)
            {
                throw new InvalidOperationException(
                    "PachimonCatalog has no Run-enabled Species.");
            }

            if (enabledSpecies.Count > PachimonCatalog.RequiredSpeciesCount)
            {
                throw new InvalidOperationException(
                    $"PachimonCatalog has too many Run-enabled Species: {enabledSpecies.Count}.");
            }

            var errors = new List<string>();
            foreach (AllocationType type in Enum.GetValues(typeof(AllocationType)))
            {
                if (type == AllocationType.Unassigned)
                {
                    continue;
                }

                var count = enabledSpecies.Count(species => species.AllocationType == type);
                if (count < MinimumSpeciesPerAllocationType)
                {
                    errors.Add(
                        $"{type} requires at least {MinimumSpeciesPerAllocationType} "
                        + $"Run-enabled Species, but has {count}.");
                }
            }

            foreach (var definition in enabledSpecies)
            {
                var fixedSkill = _skillCatalog.Get(definition.FixedSkillId);
                if (fixedSkill == null)
                {
                    errors.Add(
                        $"Pachimon species {definition.SpeciesId} references missing "
                        + $"fixed Skill {definition.FixedSkillId}.");
                }
                else if (!fixedSkill.IsMapAssignable)
                {
                    errors.Add(
                        $"Fixed Skill {definition.FixedSkillId} for Pachimon species "
                        + $"{definition.SpeciesId} must be Map-assignable.");
                }
            }

            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    "Run-enabled Pachimon content is invalid:\n"
                    + string.Join("\n", errors));
            }
        }

        private static void Shuffle<T>(IList<T> values, Random random)
        {
            for (var index = values.Count - 1; index > 0; index--)
            {
                var swapIndex = random.Next(index + 1);
                (values[index], values[swapIndex]) = (values[swapIndex], values[index]);
            }
        }
    }
}
