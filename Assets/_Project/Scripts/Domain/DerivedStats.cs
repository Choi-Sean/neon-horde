using System.Collections.Generic;
using UnityEngine;

namespace NeonHorde
{
    /// <summary>Flat multiplier bundle a character contributes. Zero == "use 1".</summary>
    public struct CharacterMods
    {
        public float moveSpeedMul;
        public float damageMul;
        public float maxHpMul;
        public float xpMul;
    }

    /// <summary>Permanent (gold-bought) meta upgrades folded into every run.</summary>
    public struct MetaMods
    {
        public float damage;
        public float moveSpeed;
        public float maxHp;
        public float magnet;
        public float gold;
        public float xp;
        public int armor;

        public static MetaMods Identity => new MetaMods
        {
            damage = 1f, moveSpeed = 1f, maxHp = 1f, magnet = 1f, gold = 1f, xp = 1f, armor = 0
        };
    }

    /// <summary>Combat stats derived from passives + character mods + meta upgrades.</summary>
    public struct DerivedStats
    {
        public float moveSpeedMul;
        public float damageMul;
        public float cooldownMul;
        public float areaMul;
        public float projectileSpeedMul;
        public float durationMul;
        public int projectileBonus;
        public float pickupRadiusMul;
        public float xpMul;
        public float critChance;
        public float critMult;
        public int armor;
        public float regenPerSec;
        public float maxHpMul;
        public float goldMul;
        public float enemyRateMul;

        public static DerivedStats Identity => new DerivedStats
        {
            moveSpeedMul = 1f, damageMul = 1f, cooldownMul = 1f, areaMul = 1f,
            projectileSpeedMul = 1f, durationMul = 1f, projectileBonus = 0,
            pickupRadiusMul = 1f, xpMul = 1f, critChance = 0.05f, critMult = 2f,
            armor = 0, regenPerSec = 0f, maxHpMul = 1f, goldMul = 1f, enemyRateMul = 1f
        };

        public static DerivedStats Compute(IReadOnlyDictionary<PassiveId, int> passives,
                                           CharacterMods character = default,
                                           MetaMods meta = default)
        {
            if (meta.damage <= 0f) meta = MetaMods.Identity;

            var s = Identity;
            int L(PassiveId id) => passives != null && passives.TryGetValue(id, out var v) ? v : 0;
            float PL(PassiveId id) => PassiveCatalog.Get(id).perLevel * L(id);

            s.damageMul = (1f + PL(PassiveId.Might) + PL(PassiveId.Damage)) * meta.damage;
            s.cooldownMul = Mathf.Clamp(1f - PL(PassiveId.Cooldown), 0.35f, 1f);
            s.areaMul = 1f + PL(PassiveId.Area);
            s.projectileBonus = L(PassiveId.ProjectileCount);
            s.durationMul = 1f + PL(PassiveId.Duration);
            s.projectileSpeedMul = 1f + PL(PassiveId.ProjectileSpeed);
            s.moveSpeedMul = (1f + PL(PassiveId.MoveSpeed)) * meta.moveSpeed;
            s.maxHpMul = (1f + PL(PassiveId.MaxHp)) * meta.maxHp;
            s.regenPerSec = PL(PassiveId.Regen);
            s.armor = L(PassiveId.Armor) + meta.armor;
            s.pickupRadiusMul = (1f + PL(PassiveId.Magnet)) * meta.magnet;
            s.xpMul = (1f + PL(PassiveId.Xp)) * meta.xp;
            s.critChance = 0.05f + PL(PassiveId.Luck);
            s.goldMul = (1f + PL(PassiveId.Greed)) * meta.gold;
            s.enemyRateMul = 1f + 0.5f * L(PassiveId.Greed);

            s.moveSpeedMul *= character.moveSpeedMul <= 0f ? 1f : character.moveSpeedMul;
            s.damageMul *= character.damageMul <= 0f ? 1f : character.damageMul;
            s.maxHpMul *= character.maxHpMul <= 0f ? 1f : character.maxHpMul;
            s.xpMul *= character.xpMul <= 0f ? 1f : character.xpMul;
            return s;
        }
    }
}
