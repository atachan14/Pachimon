using UnityEngine;

namespace Pachimon.Trainer
{
    [CreateAssetMenu(
        fileName = "TrainerMapIconCatalog",
        menuName = "Pachimon/Trainer Map Icon Catalog")]
    public sealed class TrainerMapIconCatalog : ScriptableObject
    {
        [SerializeField] private TrainerMapIconSet _normal;
        [SerializeField] private TrainerMapIconSet _gymLeader;
        [SerializeField] private TrainerMapIconSet _elite;

        public TrainerMapIconSet Normal => _normal;
        public TrainerMapIconSet GymLeader => _gymLeader;
        public TrainerMapIconSet Elite => _elite;

        public TrainerMapIconSet Get(TrainerRole role)
        {
            return role switch
            {
                TrainerRole.GymLeader => _gymLeader != null ? _gymLeader : _normal,
                TrainerRole.Elite => _elite != null ? _elite : _normal,
                _ => _normal,
            };
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            TrainerMapIconSet normal,
            TrainerMapIconSet gymLeader,
            TrainerMapIconSet elite)
        {
            _normal = normal;
            _gymLeader = gymLeader;
            _elite = elite;
        }
#endif
    }
}
