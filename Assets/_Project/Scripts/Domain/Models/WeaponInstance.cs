namespace NeonHorde
{
    /// <summary>A weapon the player currently owns, with its level and firing timers.</summary>
    public sealed class WeaponInstance
    {
        public WeaponId id;
        public int level = 1;
        public float cooldownTimer;
        public float subTimer;      // burst spacing within one activation
        public int burstLeft;       // projectiles left to fire this activation
        public float orbitPhase;    // orbit weapons

        public WeaponInstance(WeaponId id) => this.id = id;

        public WeaponDef Def => WeaponCatalog.Get(id);
        public WeaponLevel Level => Def.At(level);
        public bool AtMax => level >= Def.MaxLevel;
    }
}
