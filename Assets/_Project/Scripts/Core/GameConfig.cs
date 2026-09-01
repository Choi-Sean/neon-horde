namespace NeonHorde
{
    /// <summary>Runtime quality knobs derived from the player's settings.</summary>
    public static class GameConfig
    {
        // Real-art enemies are big sprites — a couple hundred already fills the screen.
        public static int MaxEnemiesOnScreen = 200;
        public static bool DamageNumbers = true;
        public static bool ScreenShake = true;

        public static void ApplyFromSettings(int quality)
        {
            bool low = quality == 0;
            MaxEnemiesOnScreen = low ? 120 : 200;
            DamageNumbers = !low;
            ScreenShake = true;
        }
    }
}
