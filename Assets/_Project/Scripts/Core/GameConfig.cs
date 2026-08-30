namespace NeonHorde
{
    /// <summary>Runtime quality knobs derived from the player's settings.</summary>
    public static class GameConfig
    {
        public static int MaxEnemiesOnScreen = 4000;
        public static bool DamageNumbers = true;
        public static bool ScreenShake = true;

        public static void ApplyFromSettings(int quality)
        {
            bool low = quality == 0;
            MaxEnemiesOnScreen = low ? 900 : 4000;
            DamageNumbers = !low;
            ScreenShake = true;
        }
    }
}
