namespace NeonHorde
{
    /// <summary>
    /// Cross-scene launch parameters for the next run. Set by the menu / character
    /// select / daily challenge, consumed by RunManager on load.
    /// </summary>
    public static class RunConfig
    {
        public static bool HasOverride;
        public static int Seed;
        public static CharacterId Character = CharacterId.Pulse;
        public static bool IsDaily;

        public static void SetRun(CharacterId character, int? seed = null, bool daily = false)
        {
            HasOverride = true;
            Character = character;
            IsDaily = daily;
            Seed = seed ?? unchecked((int)System.DateTime.UtcNow.Ticks);
        }

        public static void Clear() => HasOverride = false;
    }
}
