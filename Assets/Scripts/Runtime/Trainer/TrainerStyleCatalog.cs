using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pachimon.Trainer
{
    [CreateAssetMenu(fileName = "TrainerStyleCatalog", menuName = "Pachimon/Trainer Style Catalog")]
    public sealed class TrainerStyleCatalog : ScriptableObject
    {
        [SerializeField] private Sprite _playerBattleGraphic;
        [SerializeField] private List<TrainerStyleDefinition> _styles = new();

        public Sprite PlayerBattleGraphic => _playerBattleGraphic;
        public IReadOnlyList<TrainerStyleDefinition> Styles => _styles;

        public TrainerStyleDefinition Get(string styleId)
        {
            return _styles.FirstOrDefault(style => style != null && style.StyleId == styleId);
        }

        public IReadOnlyList<TrainerStyleDefinition> GetCandidates(
            TrainerTheme theme,
            TrainerStyleCategory category)
        {
            return _styles
                .Where(style => style != null
                    && style.Theme == theme
                    && style.StyleCategory == category)
                .ToArray();
        }

        public IReadOnlyList<string> ValidateMinimumContent()
        {
            var errors = new List<string>();
            var validStyles = _styles.Where(style => style != null).ToArray();

            foreach (var duplicateId in validStyles
                         .Where(style => !string.IsNullOrWhiteSpace(style.StyleId))
                         .GroupBy(style => style.StyleId)
                         .Where(group => group.Count() > 1)
                         .Select(group => group.Key))
            {
                errors.Add($"Duplicate TrainerStyle ID: {duplicateId}");
            }

            if (validStyles.Length != _styles.Count
                || validStyles.Any(style => string.IsNullOrWhiteSpace(style.StyleId)))
            {
                errors.Add("TrainerStyle contains a null entry or empty ID.");
            }

            foreach (TrainerTheme theme in System.Enum.GetValues(typeof(TrainerTheme)))
            {
                foreach (TrainerGender gender in System.Enum.GetValues(typeof(TrainerGender)))
                {
                    if (!validStyles.Any(style => style.StyleCategory == TrainerStyleCategory.Normal
                        && style.Theme == theme
                        && style.Gender == gender))
                    {
                        errors.Add($"Normal TrainerStyle is missing for theme {theme} / {gender}.");
                    }
                }
            }

            foreach (var theme in TrainerThemeUtility.AttributeThemes)
            {
                var count = validStyles.Count(style => style.StyleCategory == TrainerStyleCategory.League
                    && style.Theme == theme);
                if (count != 4)
                {
                    errors.Add($"League theme {theme} requires 4 styles, but has {count}.");
                }
            }

            if (validStyles.Any(style => style.StyleCategory == TrainerStyleCategory.Normal
                && string.IsNullOrWhiteSpace(style.NormalTitle)))
            {
                errors.Add("Every Normal TrainerStyle requires a title.");
            }

            if (validStyles.Any(style => style.BattleGraphic == null))
            {
                errors.Add("Every TrainerStyle requires a BattleGraphic.");
            }

            return errors;
        }

#if UNITY_EDITOR
        public void SetStylesForEditor(IEnumerable<TrainerStyleDefinition> styles)
        {
            _styles = new List<TrainerStyleDefinition>(styles);
        }
#endif
    }
}
