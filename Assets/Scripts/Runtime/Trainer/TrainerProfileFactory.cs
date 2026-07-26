using System;
using System.Collections.Generic;
using System.Linq;

namespace Pachimon.Trainer
{
    public sealed class TrainerProfileFactory
    {
        private readonly TrainerStyleCatalog _styleCatalog;
        private readonly TrainerNameCatalog _nameCatalog;
        private readonly Random _random;
        private readonly HashSet<string> _usedLeagueStyleIds = new();
        private readonly Dictionary<TrainerGender, Queue<TrainerNameDefinition>> _nameDecks = new();

        public TrainerProfileFactory(
            TrainerStyleCatalog styleCatalog,
            TrainerNameCatalog nameCatalog,
            Random random)
        {
            _styleCatalog = styleCatalog ?? throw new ArgumentNullException(nameof(styleCatalog));
            _nameCatalog = nameCatalog ?? throw new ArgumentNullException(nameof(nameCatalog));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public TrainerProfile Create(TrainerRole role, TrainerTheme theme)
        {
            var category = role == TrainerRole.Normal
                ? TrainerStyleCategory.Normal
                : TrainerStyleCategory.League;
            var candidates = _styleCatalog.GetCandidates(theme, category)
                .Where(style => category == TrainerStyleCategory.Normal
                    || !_usedLeagueStyleIds.Contains(style.StyleId))
                .ToArray();

            if (candidates.Length == 0)
            {
                throw new InvalidOperationException(
                    $"No unused {category} TrainerStyle is available for theme {theme}.");
            }

            var style = candidates[_random.Next(candidates.Length)];
            if (category == TrainerStyleCategory.League)
            {
                _usedLeagueStyleIds.Add(style.StyleId);
            }

            var name = TakeName(style.Gender);
            return new TrainerProfile(role, style.StyleId, name.NameId);
        }

        private TrainerNameDefinition TakeName(TrainerGender gender)
        {
            if (!_nameDecks.TryGetValue(gender, out var deck) || deck.Count == 0)
            {
                deck = CreateNameDeck(gender);
                _nameDecks[gender] = deck;
            }

            return deck.Dequeue();
        }

        private Queue<TrainerNameDefinition> CreateNameDeck(TrainerGender gender)
        {
            var names = _nameCatalog.GetByGender(gender).ToList();
            if (names.Count == 0)
            {
                throw new InvalidOperationException($"No Trainer names exist for gender {gender}.");
            }

            for (var index = names.Count - 1; index > 0; index--)
            {
                var swapIndex = _random.Next(index + 1);
                (names[index], names[swapIndex]) = (names[swapIndex], names[index]);
            }

            return new Queue<TrainerNameDefinition>(names);
        }
    }
}
