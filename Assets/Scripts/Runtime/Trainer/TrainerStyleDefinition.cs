using System;
using UnityEngine;

namespace Pachimon.Trainer
{
    [Serializable]
    public sealed class TrainerStyleDefinition
    {
        [SerializeField] private string _styleId;
        [SerializeField] private TrainerTheme _theme;
        [SerializeField] private TrainerGender _gender;
        [SerializeField] private TrainerStyleCategory _styleCategory;
        [SerializeField] private string _normalTitle;
        [SerializeField] private Sprite _battleGraphic;

        public TrainerStyleDefinition(
            string styleId,
            TrainerTheme theme,
            TrainerGender gender,
            TrainerStyleCategory styleCategory,
            string normalTitle,
            Sprite battleGraphic)
        {
            _styleId = styleId;
            _theme = theme;
            _gender = gender;
            _styleCategory = styleCategory;
            _normalTitle = normalTitle;
            _battleGraphic = battleGraphic;
        }

        public string StyleId => _styleId;
        public TrainerTheme Theme => _theme;
        public TrainerGender Gender => _gender;
        public TrainerStyleCategory StyleCategory => _styleCategory;
        public string NormalTitle => _normalTitle;
        public Sprite BattleGraphic => _battleGraphic;
    }
}
