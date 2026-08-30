namespace NeonHorde
{
    /// <summary>
    /// Derived per-run player stats. In M0 only movement/HP are used; weapon, area,
    /// cooldown, luck etc. get folded in during M1-M2.
    /// </summary>
    public struct PlayerStats
    {
        public float MoveSpeed;
        public int MaxHp;
        public float PickupRadius;

        public static PlayerStats Default => new()
        {
            MoveSpeed = Balance.PlayerMoveSpeed,
            MaxHp = Balance.PlayerMaxHp,
            PickupRadius = Balance.PlayerPickupRadius
        };
    }
}
