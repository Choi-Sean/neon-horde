using UnityEngine;

namespace NeonHorde
{
    public enum WeaponId
    {
        Bolt, Aura, Orbit, Whip, Seeker, Lob, Chain, Boomerang,
        // evolutions (M4)
        Railgun, Nova, Halo, Reaper, Swarm, Meteor, Tesla, Cyclone
    }

    public enum WeaponBehavior { Linear, Aura, Orbit, Arc, Homing, Lob, Chain, Boomerang }

    public enum AimMode { Nearest, MoveDir, Random }

    /// <summary>Per-level tunable block. Missing fields default to the previous level.</summary>
    public struct WeaponLevel
    {
        public float damage;
        public float cooldown;
        public int count;        // projectiles / nodes / arcs per activation
        public float area;       // radius (aura, lob AoE, arc range)
        public float speed;      // projectile speed
        public int pierce;
        public float duration;   // lifetime / orbit lifetime
    }

    public struct WeaponDef
    {
        public WeaponId id;
        public string name;
        public WeaponBehavior behavior;
        public AimMode aim;
        public Color color;
        public WeaponLevel[] levels;      // index 0 == level 1, up to 8
        public WeaponId evolvesInto;      // Bolt -> Railgun ... (M4)
        public PassiveId evolveRequires;  // paired passive at max
        public bool hasEvolution;

        public WeaponLevel At(int level)
        {
            int i = Mathf.Clamp(level - 1, 0, levels.Length - 1);
            return levels[i];
        }
        public int MaxLevel => levels.Length;
    }

    /// <summary>
    /// M2 hardcoded weapon data. Migrates to WeaponDefSO assets later; kept as code so
    /// balance iteration stays in one file.
    /// </summary>
    public static class WeaponCatalog
    {
        static WeaponDef[] _defs;

        public static WeaponDef Get(WeaponId id) => _defs[(int)id];
        public static int BaseWeaponCount => 8; // Bolt..Boomerang are player-selectable

        static float Lerp(float a, float b, int i, int n) => Mathf.Lerp(a, b, n <= 1 ? 0f : i / (float)(n - 1));

        static WeaponLevel[] Curve(int n, float dmgA, float dmgB, float cdA, float cdB,
                                   int cntA, int cntB, float areaA, float areaB,
                                   float spd, int pierce, float dur)
        {
            var arr = new WeaponLevel[n];
            for (int i = 0; i < n; i++)
                arr[i] = new WeaponLevel
                {
                    damage = Lerp(dmgA, dmgB, i, n),
                    cooldown = Lerp(cdA, cdB, i, n),
                    count = Mathf.RoundToInt(Lerp(cntA, cntB, i, n)),
                    area = Lerp(areaA, areaB, i, n),
                    speed = spd,
                    pierce = pierce,
                    duration = dur
                };
            return arr;
        }

        static WeaponCatalog() => Build();

        static void Build()
        {
            _defs = new WeaponDef[System.Enum.GetValues(typeof(WeaponId)).Length];

            Set(new WeaponDef
            {
                id = WeaponId.Bolt, name = "볼트", behavior = WeaponBehavior.Linear, aim = AimMode.Nearest,
                color = Palette.Projectile,
                levels = Curve(8, 6f, 22f, 0.55f, 0.34f, 1, 3, 0f, 0f, 14f, 0, 1.6f),
                hasEvolution = true, evolvesInto = WeaponId.Railgun, evolveRequires = PassiveId.Damage
            });

            Set(new WeaponDef
            {
                id = WeaponId.Aura, name = "오라", behavior = WeaponBehavior.Aura, aim = AimMode.Nearest,
                color = new Color(0.5f, 1.4f, 2.2f, 1f),
                levels = Curve(8, 5f, 20f, 0.25f, 0.25f, 1, 1, 1.6f, 3.4f, 0f, 0, 0.25f),
                hasEvolution = true, evolvesInto = WeaponId.Nova, evolveRequires = PassiveId.Area
            });

            Set(new WeaponDef
            {
                id = WeaponId.Orbit, name = "궤도구", behavior = WeaponBehavior.Orbit, aim = AimMode.Nearest,
                color = new Color(2.2f, 1.0f, 2.2f, 1f),
                levels = Curve(8, 8f, 26f, 3.2f, 2.2f, 2, 5, 1.6f, 2.2f, 220f, 999, 3.2f),
                hasEvolution = true, evolvesInto = WeaponId.Halo, evolveRequires = PassiveId.Duration
            });

            Set(new WeaponDef
            {
                id = WeaponId.Whip, name = "채찍", behavior = WeaponBehavior.Arc, aim = AimMode.MoveDir,
                color = new Color(2.4f, 0.7f, 0.7f, 1f),
                levels = Curve(8, 10f, 34f, 0.9f, 0.6f, 1, 2, 2.6f, 4.2f, 0f, 999, 0.15f),
                hasEvolution = true, evolvesInto = WeaponId.Reaper, evolveRequires = PassiveId.Might
            });

            Set(new WeaponDef
            {
                id = WeaponId.Seeker, name = "유도탄", behavior = WeaponBehavior.Homing, aim = AimMode.Random,
                color = new Color(2.4f, 1.8f, 0.4f, 1f),
                levels = Curve(8, 7f, 24f, 1.0f, 0.55f, 1, 4, 0f, 0f, 9f, 1, 3.0f),
                hasEvolution = true, evolvesInto = WeaponId.Swarm, evolveRequires = PassiveId.ProjectileCount
            });

            Set(new WeaponDef
            {
                id = WeaponId.Lob, name = "화염구", behavior = WeaponBehavior.Lob, aim = AimMode.Random,
                color = new Color(2.6f, 0.9f, 0.2f, 1f),
                levels = Curve(8, 14f, 44f, 1.4f, 0.9f, 1, 3, 1.6f, 2.6f, 7f, 0, 0.9f),
                hasEvolution = true, evolvesInto = WeaponId.Meteor, evolveRequires = PassiveId.Area
            });

            Set(new WeaponDef
            {
                id = WeaponId.Chain, name = "번개", behavior = WeaponBehavior.Chain, aim = AimMode.Nearest,
                color = new Color(1.4f, 1.8f, 2.6f, 1f),
                levels = Curve(8, 9f, 28f, 1.1f, 0.7f, 2, 5, 3.0f, 4.5f, 22f, 0, 0.4f),
                hasEvolution = true, evolvesInto = WeaponId.Tesla, evolveRequires = PassiveId.Cooldown
            });

            Set(new WeaponDef
            {
                id = WeaponId.Boomerang, name = "부메랑", behavior = WeaponBehavior.Boomerang, aim = AimMode.MoveDir,
                color = new Color(0.8f, 2.2f, 1.6f, 1f),
                levels = Curve(8, 11f, 30f, 1.2f, 0.8f, 1, 3, 0f, 0f, 12f, 999, 2.2f),
                hasEvolution = true, evolvesInto = WeaponId.Cyclone, evolveRequires = PassiveId.ProjectileSpeed
            });

            // Evolutions — same behavior, bigger numbers (M4 unlock).
            EvolveFrom(WeaponId.Bolt, WeaponId.Railgun, "레일건", 2.2f);
            EvolveFrom(WeaponId.Aura, WeaponId.Nova, "노바", 2.4f);
            EvolveFrom(WeaponId.Orbit, WeaponId.Halo, "헤일로", 2.2f);
            EvolveFrom(WeaponId.Whip, WeaponId.Reaper, "리퍼", 2.3f);
            EvolveFrom(WeaponId.Seeker, WeaponId.Swarm, "스웜", 2.0f);
            EvolveFrom(WeaponId.Lob, WeaponId.Meteor, "메테오", 2.5f);
            EvolveFrom(WeaponId.Chain, WeaponId.Tesla, "테슬라", 2.3f);
            EvolveFrom(WeaponId.Boomerang, WeaponId.Cyclone, "사이클론", 2.2f);
        }

        static void Set(WeaponDef d) => _defs[(int)d.id] = d;

        static void EvolveFrom(WeaponId baseId, WeaponId evoId, string name, float mult)
        {
            var b = _defs[(int)baseId];
            var lv = new WeaponLevel[b.levels.Length];
            for (int i = 0; i < lv.Length; i++)
            {
                lv[i] = b.levels[i];
                lv[i].damage *= mult;
                lv[i].cooldown *= 0.8f;
                lv[i].count = Mathf.Max(lv[i].count, lv[i].count + 1);
                lv[i].area *= 1.3f;
            }
            _defs[(int)evoId] = new WeaponDef
            {
                id = evoId, name = name, behavior = b.behavior, aim = b.aim, color = b.color * 1.15f,
                levels = lv, hasEvolution = false
            };
        }
    }
}
