using System.Collections.Generic;

namespace Pachimon.Run
{
    public sealed class RunPachimonPool
    {
        private readonly Dictionary<string, PachimonInstance> _instancesById = new();

        public List<PachimonInstance> Instances { get; } = new();

        public int ExcludedSpeciesId { get; internal set; }

        public void Add(PachimonInstance instance)
        {
            Instances.Add(instance);
            _instancesById.Add(instance.InstanceId, instance);
        }

        public PachimonInstance Get(string instanceId)
        {
            return instanceId != null && _instancesById.TryGetValue(instanceId, out var instance)
                ? instance
                : null;
        }
    }
}
