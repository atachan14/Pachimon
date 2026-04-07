using System.Collections.Generic;

namespace Pachimon.Run
{
    public sealed class RunState
    {
        public RunState(int runSeed)
        {
            RunSeed = runSeed;
        }

        public int RunSeed { get; }

        public int Gold { get; set; }

        public int BadgeCount { get; set; }

        public string CurrentNodeId { get; set; }

        public bool IsRunFinished { get; set; }

        public List<string> PlayerPachimonIds { get; } = new();

        public HashSet<string> ResolvedNodeIds { get; } = new();
    }
}
