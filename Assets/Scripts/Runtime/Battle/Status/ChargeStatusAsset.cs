using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(
        fileName = "ChargeStatus",
        menuName = "Pachimon/Battle/Status/Charge")]
    public sealed class ChargeStatusAsset : BattleStatusAsset
    {
        [SerializeField] private string _chargingDisplayName = "充電中";
        [SerializeField, TextArea] private string _chargingDescription = string.Empty;
        [SerializeField] private string _chargedDisplayName = "充電完了";
        [SerializeField, TextArea] private string _chargedDescription = string.Empty;
        [SerializeField, Min(0)] private int _chargingResistBonusRatio = 40;
        [SerializeField, Min(0)] private int _chargingElectricRatio = 50;
        [SerializeField, Min(1)] private int _chargedDurationRatio = 200;
        [SerializeField, Min(0)] private int _chargedElectricRatio = 150;
        [SerializeField, Min(0)] private int _chargedSpeedRatio = 100;

        public int ChargingResistBonusRatio => _chargingResistBonusRatio;
        public int ChargingElectricRatio => _chargingElectricRatio;
        public int ChargedDurationRatio => _chargedDurationRatio;
        public int ChargedElectricRatio => _chargedElectricRatio;
        public int ChargedSpeedRatio => _chargedSpeedRatio;
        public string ChargingDescription => _chargingDescription;
        public string ChargedDescription => _chargedDescription;

        public override string GetDisplayName(BattleStatusInstance instance)
        {
            var state = GetState(instance);
            var phaseName = state.Phase == ChargePhase.Charging
                ? _chargingDisplayName
                : _chargedDisplayName;
            var text = $"{phaseName} {instance.Value}";
            return instance.RemainingTicks.HasValue
                ? $"{text} [{instance.RemainingTicks.Value}]"
                : text;
        }

        public override string GetDescription(BattleStatusInstance instance)
        {
            return GetState(instance).Phase == ChargePhase.Charging
                ? _chargingDescription
                : _chargedDescription;
        }

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (StatusId != BattleStatusId.Charge)
            {
                errors?.Add("Charge Definition must use Charge ID.");
            }
            if (string.IsNullOrWhiteSpace(_chargingDisplayName)
                || string.IsNullOrWhiteSpace(_chargedDisplayName))
            {
                errors?.Add("Charge Definition requires both Phase names.");
            }
        }

        private static ChargeStatusRuntimeState GetState(
            BattleStatusInstance instance)
        {
            if (instance == null)
            {
                throw new System.ArgumentNullException(nameof(instance));
            }
            return instance.RuntimeData as ChargeStatusRuntimeState
                ?? throw new System.InvalidOperationException(
                    "Charge Status requires Charge runtime data.");
        }

#if UNITY_EDITOR
        public void SetPhaseDescriptionTemplatesForEditor(
            string chargingDescription,
            string chargedDescription)
        {
            _chargingDescription = chargingDescription ?? string.Empty;
            _chargedDescription = chargedDescription ?? string.Empty;
        }

        public void ConfigureForEditor(
            string displayName,
            string description,
            string chargingDisplayName,
            string chargingDescription,
            string chargedDisplayName,
            string chargedDescription,
            int chargingResistBonusRatio,
            int chargingElectricRatio,
            int chargedDurationRatio,
            int chargedElectricRatio,
            int chargedSpeedRatio,
            Sprite icon = null)
        {
            ConfigureDefinitionForEditor(
                BattleStatusId.Charge,
                displayName,
                description,
                icon);
            _chargingDisplayName = chargingDisplayName ?? string.Empty;
            _chargingDescription = chargingDescription ?? string.Empty;
            _chargedDisplayName = chargedDisplayName ?? string.Empty;
            _chargedDescription = chargedDescription ?? string.Empty;
            _chargingResistBonusRatio = chargingResistBonusRatio;
            _chargingElectricRatio = chargingElectricRatio;
            _chargedDurationRatio = chargedDurationRatio;
            _chargedElectricRatio = chargedElectricRatio;
            _chargedSpeedRatio = chargedSpeedRatio;
        }
#endif
    }
}
