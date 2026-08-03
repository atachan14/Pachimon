using Pachimon.Data;
using Pachimon.Map;
using Pachimon.Trainer;
using Pachimon.Skills;
using Pachimon.Items;
using Pachimon.Passives;

namespace Pachimon.Run
{
    public sealed class RunContext
    {
        public RunContext(
            RunPachimonPool pachimonPool,
            RunState runState,
            RunMap runMap,
            PachimonCatalog pachimonCatalog,
            SkillCatalog skillCatalog,
            PassiveCatalog passiveCatalog,
            PassiveStatModifierRegistry passiveStatModifierRegistry,
            ItemCatalog itemCatalog,
            TrainerStyleCatalog trainerStyleCatalog,
            TrainerNameCatalog trainerNameCatalog,
            MapRunController mapRunController)
        {
            PachimonPool = pachimonPool;
            RunState = runState;
            RunMap = runMap;
            PachimonCatalog = pachimonCatalog;
            SkillCatalog = skillCatalog;
            PassiveCatalog = passiveCatalog;
            PassiveStatModifierRegistry = passiveStatModifierRegistry;
            ItemCatalog = itemCatalog;
            TrainerStyleCatalog = trainerStyleCatalog;
            TrainerNameCatalog = trainerNameCatalog;
            MapRunController = mapRunController;
        }

        public RunPachimonPool PachimonPool { get; }

        public RunState RunState { get; }

        public RunMap RunMap { get; }

        public PachimonCatalog PachimonCatalog { get; }

        public SkillCatalog SkillCatalog { get; }

        public PassiveCatalog PassiveCatalog { get; }

        public PassiveStatModifierRegistry PassiveStatModifierRegistry { get; }

        public ItemCatalog ItemCatalog { get; }

        public TrainerStyleCatalog TrainerStyleCatalog { get; }

        public TrainerNameCatalog TrainerNameCatalog { get; }

        public MapRunController MapRunController { get; }
    }
}
