using Pachimon.Reward;

namespace Pachimon.Trainer
{
    public sealed class TrainerProfile
    {
        public TrainerProfile(
            TrainerRole role,
            string styleId,
            string nameId,
            PachimonAttribute? favoredAttribute = null,
            PachimonAttribute? weakAttribute = null)
        {
            Role = role;
            StyleId = styleId;
            NameId = nameId;
            FavoredAttribute = favoredAttribute;
            WeakAttribute = weakAttribute;
        }

        public TrainerRole Role { get; }
        public string StyleId { get; }
        public string NameId { get; }
        public PachimonAttribute? FavoredAttribute { get; }
        public PachimonAttribute? WeakAttribute { get; }
    }
}
