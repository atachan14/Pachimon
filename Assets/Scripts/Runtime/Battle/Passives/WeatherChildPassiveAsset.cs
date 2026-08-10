using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(
        fileName = "WeatherChildPassive",
        menuName = "Pachimon/Passives/Weather Child Passive")]
    public sealed class WeatherChildPassiveAsset : PassiveAsset
    {
        [SerializeField, Min(0)] private int _damageBonusPerWeather = 20;

        public int DamageBonusPerWeather => _damageBonusPerWeather;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_damageBonusPerWeather < 0)
            {
                errors.Add(
                    $"Passive {PassiveId}: Damage Bonus cannot be negative.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int passiveId,
            string displayName,
            string description,
            int damageBonusPerWeather)
        {
            ConfigureBaseForEditor(passiveId, displayName, description);
            _damageBonusPerWeather = damageBonusPerWeather;
        }
#endif
    }
}
