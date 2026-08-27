using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.UI
{
    internal static class SubStatIconProvider
    {
        private const string ResourceFolder = "UI/SubStatIcons/";
        private static readonly Dictionary<PachimonDisplayStat, Sprite> Cache = new();

        public static Sprite Get(PachimonDisplayStat stat)
        {
            if (Cache.TryGetValue(stat, out var sprite))
            {
                return sprite;
            }

            sprite = Resources.Load<Sprite>(ResourceFolder + GetResourceName(stat));
            if (sprite != null)
            {
                Cache[stat] = sprite;
            }
            return sprite;
        }

        private static string GetResourceName(PachimonDisplayStat stat)
        {
            return stat switch
            {
                PachimonDisplayStat.DamageBonus => "DamageBonus",
                PachimonDisplayStat.ResistBonus => "ResistBonus",
                PachimonDisplayStat.Speed => "Speed",
                PachimonDisplayStat.Haste => "Haste",
                PachimonDisplayStat.GenerationPower => "GenerationPower",
                PachimonDisplayStat.StatusMastery => "StatusMastery",
                PachimonDisplayStat.SustainPower => "SustainPower",
                PachimonDisplayStat.StatusResistance => "StatusResistance",
                _ => throw new System.ArgumentOutOfRangeException(
                    nameof(stat),
                    stat,
                    "Only SubStats have overlay icons."),
            };
        }
    }
}
