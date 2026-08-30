namespace NeonHorde
{
    /// <summary>
    /// Deterministic PRNG (xoshiro128**). Single source of gameplay randomness per run
    /// so daily seeds / replays / leaderboard validation stay reproducible.
    /// </summary>
    public sealed class SeededRng
    {
        uint _s0, _s1, _s2, _s3;

        public SeededRng(ulong seed) => SetSeed(seed);

        public void SetSeed(ulong seed)
        {
            // splitmix64 to spread the seed across the 128-bit state
            ulong z = seed == 0 ? 0x9E3779B97F4A7C15UL : seed;
            _s0 = (uint)SplitMix(ref z);
            _s1 = (uint)(SplitMix(ref z) >> 16);
            _s2 = (uint)SplitMix(ref z);
            _s3 = (uint)(SplitMix(ref z) >> 16);
            if ((_s0 | _s1 | _s2 | _s3) == 0) _s0 = 0x1234567u;
        }

        static ulong SplitMix(ref ulong z)
        {
            z += 0x9E3779B97F4A7C15UL;
            ulong r = z;
            r = (r ^ (r >> 30)) * 0xBF58476D1CE4E5B9UL;
            r = (r ^ (r >> 27)) * 0x94D049BB133111EBUL;
            return r ^ (r >> 31);
        }

        static uint Rotl(uint x, int k) => (x << k) | (x >> (32 - k));

        public uint NextUInt()
        {
            uint result = Rotl(_s1 * 5u, 7) * 9u;
            uint t = _s1 << 9;
            _s2 ^= _s0;
            _s3 ^= _s1;
            _s1 ^= _s2;
            _s0 ^= _s3;
            _s2 ^= t;
            _s3 = Rotl(_s3, 11);
            return result;
        }

        /// <summary>[minInclusive, maxExclusive)</summary>
        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            uint range = (uint)(maxExclusive - minInclusive);
            return minInclusive + (int)(NextUInt() % range);
        }

        /// <summary>[0, 1)</summary>
        public float NextFloat() => (NextUInt() >> 8) * (1f / 16777216f);

        public float Range(float min, float max) => min + (max - min) * NextFloat();

        public bool Chance(float probability) => NextFloat() < probability;

        public (uint, uint, uint, uint) GetState() => (_s0, _s1, _s2, _s3);

        public void SetState((uint s0, uint s1, uint s2, uint s3) st)
            => (_s0, _s1, _s2, _s3) = st;
    }
}
