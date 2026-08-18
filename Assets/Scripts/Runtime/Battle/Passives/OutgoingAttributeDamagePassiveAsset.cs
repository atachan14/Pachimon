using Pachimon.Passives;
using Pachimon.Reward;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(
        fileName = "OutgoingAttributeDamagePassive",
        menuName = "Pachimon/Passives/Outgoing Attribute Damage")]
    public sealed class OutgoingAttributeDamagePassiveAsset : PassiveAsset
    {
        [SerializeField] private PachimonAttribute _attribute;
        [SerializeField, Min(0)] private int _damagePercent = 130;

        public PachimonAttribute Attribute => _attribute;
        public int DamagePercent => _damagePercent;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int id,
            string displayName,
            string descriptionTemplate,
            PachimonAttribute attribute,
            int damagePercent)
        {
            ConfigureBaseForEditor(id, displayName, descriptionTemplate);
            _attribute = attribute;
            _damagePercent = damagePercent;
        }
#endif
    }
}
