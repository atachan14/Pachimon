namespace Pachimon.Trainer
{
    public sealed class TrainerProfile
    {
        public TrainerProfile(TrainerRole role, string styleId, string nameId)
        {
            Role = role;
            StyleId = styleId;
            NameId = nameId;
        }

        public TrainerRole Role { get; }
        public string StyleId { get; }
        public string NameId { get; }
    }
}
