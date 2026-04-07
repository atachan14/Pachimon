using Pachimon.Map;

namespace Pachimon.Run
{
    public sealed class RunContext
    {
        public RunContext(RunState runState, RunMap runMap, MapRunController mapRunController)
        {
            RunState = runState;
            RunMap = runMap;
            MapRunController = mapRunController;
        }

        public RunState RunState { get; }

        public RunMap RunMap { get; }

        public MapRunController MapRunController { get; }
    }
}
