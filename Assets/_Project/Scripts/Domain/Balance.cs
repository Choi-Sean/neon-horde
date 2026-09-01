namespace NeonHorde
{
    /// <summary>
    /// M0/M1 tuning constants. Migrates to a BalanceConfig ScriptableObject once the
    /// numbers start moving often (see docs/ARCHITECTURE.md section 8).
    /// </summary>
    public static class Balance
    {
        public const float FixedDt = 1f / 60f;
        public const float MaxFrameCatchUp = 0.25f;

        public const float PlayerMoveSpeed = 6f;
        public const int   PlayerMaxHp = 100;
        public const float PlayerPickupRadius = 1.5f;

        public const float CameraOrthoSize = 8f;
        public const float CameraFollowSmoothing = 8f;

        // M1 weapon (Bolt). Migrates to WeaponDefSO level tables in M2.
        public const float BoltBaseDamage = 6f;
        public const float BoltBaseCooldown = 0.55f;
        public const float BoltSpeed = 14f;
        public const float BoltRange = 9f;
        public const float BoltLifetime = 1.6f;
    }
}
