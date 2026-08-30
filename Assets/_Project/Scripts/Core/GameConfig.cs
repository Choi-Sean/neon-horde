namespace NeonHorde
{
    /// <summary>Runtime quality knobs derived from the player's settings.</summary>
    public static class GameConfig
    {
        // Enemies render as pooled SpriteRenderers now (see SpritePool). A few hundred
        // on screen already reads as a "horde"; keep the cap sane for mobile draw cost.
        public static int MaxEnemiesOnScreen = 600;
        public static bool DamageNumbers = true;
        public static bool ScreenShake = true;

        public static void ApplyFromSettings(int quality)
        {
            bool low = quality == 0;
            MaxEnemiesOnScreen = low ? 350 : 600;
            DamageNumbers = !low;
            ScreenShake = true;
        }
    }
}
