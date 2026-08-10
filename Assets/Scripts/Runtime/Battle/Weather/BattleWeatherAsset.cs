using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    public enum BattleWeatherId
    {
        Temperature = 1,
        Rain = 2,
        Thunder = 3,
        Wind = 4,
    }

    public abstract class BattleWeatherAsset : ScriptableObject
    {
        [SerializeField] private BattleWeatherId _weatherId;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private Sprite _icon;

        public BattleWeatherId WeatherId => _weatherId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;

        public virtual void CollectValidationErrors(ICollection<string> errors)
        {
            if (errors == null) throw new ArgumentNullException(nameof(errors));
            if (string.IsNullOrWhiteSpace(_displayName))
            {
                errors.Add($"Weather {_weatherId}: display name is missing.");
            }
        }

#if UNITY_EDITOR
        protected void ConfigureDefinitionForEditor(
            BattleWeatherId weatherId,
            string displayName,
            string description,
            Sprite icon = null)
        {
            _weatherId = weatherId;
            _displayName = displayName;
            _description = description;
            _icon = icon;
        }
#endif
    }
}
