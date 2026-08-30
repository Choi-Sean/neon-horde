using UnityEngine;

namespace NeonHorde
{
    public enum CharacterId { Pulse = 0, Volt = 1, Aegis = 2, Halo = 3 }

    public enum CharacterAbility { Shockwave, KillChain, LowHpInvuln, LevelUpBlast }

    public struct CharacterDef
    {
        public CharacterId id;
        public string name;
        public string abilityText;
        public WeaponId startWeapon;
        public CharacterAbility ability;
        public CharacterMods mods;
        public bool ownedByDefault;
        public int coreCost;
        public string iapProductId;
        public Color color;
    }

    public static class CharacterCatalog
    {
        public static readonly CharacterDef[] All =
        {
            new CharacterDef
            {
                id = CharacterId.Pulse, name = "PULSE", abilityText = "5초마다 충격파(넉백)",
                startWeapon = WeaponId.Bolt, ability = CharacterAbility.Shockwave,
                mods = new CharacterMods { moveSpeedMul = 1f, damageMul = 1f, maxHpMul = 1f, xpMul = 1f },
                ownedByDefault = true, coreCost = 0, iapProductId = null,
                color = new Color(0.2f, 1.6f, 2.2f, 1f)
            },
            new CharacterDef
            {
                id = CharacterId.Volt, name = "VOLT", abilityText = "처치 시 10% 연쇄 폭발",
                startWeapon = WeaponId.Chain, ability = CharacterAbility.KillChain,
                mods = new CharacterMods { moveSpeedMul = 1f, damageMul = 1.15f, maxHpMul = 0.85f, xpMul = 1f },
                ownedByDefault = false, coreCost = 60, iapProductId = "char.volt",
                color = new Color(1.6f, 1.8f, 2.6f, 1f)
            },
            new CharacterDef
            {
                id = CharacterId.Aegis, name = "AEGIS", abilityText = "HP 30% 이하 시 3초 무적 (쿨 20초)",
                startWeapon = WeaponId.Aura, ability = CharacterAbility.LowHpInvuln,
                mods = new CharacterMods { moveSpeedMul = 0.9f, damageMul = 1f, maxHpMul = 1.3f, xpMul = 1f },
                ownedByDefault = false, coreCost = 90, iapProductId = "char.aegis",
                color = new Color(0.5f, 1.4f, 2.2f, 1f)
            },
            new CharacterDef
            {
                id = CharacterId.Halo, name = "HALO", abilityText = "경험치 +25%, 레벨업 시 주변 폭발",
                startWeapon = WeaponId.Orbit, ability = CharacterAbility.LevelUpBlast,
                mods = new CharacterMods { moveSpeedMul = 1f, damageMul = 1f, maxHpMul = 0.75f, xpMul = 1.25f },
                ownedByDefault = false, coreCost = 120, iapProductId = "char.halo",
                color = new Color(2.2f, 1.0f, 2.2f, 1f)
            },
        };

        public static CharacterDef Get(CharacterId id) => All[(int)id];
    }
}
