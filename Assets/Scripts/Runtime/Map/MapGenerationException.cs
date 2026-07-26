using System;

namespace Pachimon.Map
{
    public sealed class MapGenerationException : Exception
    {
        public MapGenerationException(string message)
            : base(message)
        {
        }
    }
}
