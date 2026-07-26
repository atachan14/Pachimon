using System;

namespace Pachimon.Battle
{
    /// <summary>
    /// Runtime and platform independent random stream used for Battle decisions.
    /// </summary>
    internal sealed class StableBattleRandom
    {
        private uint _state;

        public StableBattleRandom(int seed, uint streamSalt)
        {
            _state = unchecked((uint)seed) ^ streamSalt ^ 0x9E3779B9u;
            if (_state == 0u) _state = 0xA341316Cu;
        }

        public int Next(int exclusiveMaximum)
        {
            if (exclusiveMaximum <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(exclusiveMaximum));
            }

            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;
            return (int)(_state % (uint)exclusiveMaximum);
        }
    }
}
