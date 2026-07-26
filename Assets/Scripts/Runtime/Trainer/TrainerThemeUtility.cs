using System.Collections.Generic;

namespace Pachimon.Trainer
{
    public static class TrainerThemeUtility
    {
        public static IReadOnlyList<TrainerTheme> AttributeThemes { get; } = new[]
        {
            TrainerTheme.Fire, TrainerTheme.Aqua, TrainerTheme.Leaf,
            TrainerTheme.Electric, TrainerTheme.Poison, TrainerTheme.Ice,
            TrainerTheme.Wind, TrainerTheme.Dragon,
        };
    }
}
