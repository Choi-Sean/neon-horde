using System;

namespace NeonHorde
{
    /// <summary>
    /// Central persistent-profile controller: currencies, character unlock/select,
    /// permanent (gold) upgrades, and the meta multipliers applied to every run.
    /// Registered in ServiceLocator by GameBootstrap.
    /// </summary>
    public sealed class MetaController
    {
        public MetaState State { get; }
        readonly ISaveService _save;

        public event Action Changed;

        public MetaController(MetaState state, ISaveService save)
        {
            State = state;
            _save = save;
        }

        public void Save()
        {
            _save?.Save(State);
            Changed?.Invoke();
        }

        // ---- currency ----

        public long Gold => State.gold;
        public int Cores => State.cores;

        public void AddGold(long amt) { State.gold += amt; Save(); }
        public void AddCores(int amt) { State.cores += amt; Save(); }

        public bool SpendGold(long amt)
        {
            if (amt <= 0) return true;
            if (State.gold < amt) return false;
            State.gold -= amt;
            Save();
            return true;
        }

        public bool SpendCores(int amt)
        {
            if (amt <= 0) return true;
            if (State.cores < amt) return false;
            State.cores -= amt;
            Save();
            return true;
        }

        // ---- characters ----

        public bool IsCharacterUnlocked(CharacterId id)
        {
            var d = CharacterCatalog.Get(id);
            return d.ownedByDefault || State.unlockedCharacters.Contains(id.ToString());
        }

        public void GrantCharacter(CharacterId id)
        {
            if (!State.unlockedCharacters.Contains(id.ToString()))
                State.unlockedCharacters.Add(id.ToString());
            Save();
        }

        /// <summary>Unlock via cores. Returns false if unaffordable / already owned.</summary>
        public bool UnlockCharacterWithCores(CharacterId id)
        {
            if (IsCharacterUnlocked(id)) return false;
            var d = CharacterCatalog.Get(id);
            if (!SpendCores(d.coreCost)) return false;
            GrantCharacter(id);
            return true;
        }

        public CharacterId SelectedCharacter
        {
            get
            {
                if (Enum.TryParse(State.selectedCharacter, out CharacterId id) && IsCharacterUnlocked(id))
                    return id;
                return CharacterId.Pulse;
            }
        }

        public void SelectCharacter(CharacterId id)
        {
            if (!IsCharacterUnlocked(id)) return;
            State.selectedCharacter = id.ToString();
            Save();
        }

        // ---- permanent upgrades ----

        public int PowerLevel(PowerId id)
        {
            foreach (var e in State.powerLevels)
                if (e.id == id.ToString()) return e.level;
            return 0;
        }

        void SetPowerLevel(PowerId id, int level)
        {
            foreach (var e in State.powerLevels)
                if (e.id == id.ToString()) { e.level = level; return; }
            State.powerLevels.Add(new MetaState.PowerEntry { id = id.ToString(), level = level });
        }

        public long PowerCost(PowerId id) => PowerCatalog.Get(id).CostForLevel(PowerLevel(id));

        public bool BuyPower(PowerId id)
        {
            int lvl = PowerLevel(id);
            var d = PowerCatalog.Get(id);
            if (lvl >= d.maxLevel) return false;
            if (!SpendGold(d.CostForLevel(lvl))) return false;
            SetPowerLevel(id, lvl + 1);
            Save();
            return true;
        }

        // ---- run mods ----

        public MetaMods BuildMods()
        {
            var m = MetaMods.Identity;
            m.maxHp = 1f + PowerLevel(PowerId.StartHp) * PowerCatalog.Get(PowerId.StartHp).perLevel;
            m.damage = 1f + PowerLevel(PowerId.Damage) * PowerCatalog.Get(PowerId.Damage).perLevel;
            m.moveSpeed = 1f + PowerLevel(PowerId.MoveSpeed) * PowerCatalog.Get(PowerId.MoveSpeed).perLevel;
            m.magnet = 1f + PowerLevel(PowerId.Magnet) * PowerCatalog.Get(PowerId.Magnet).perLevel;
            m.gold = 1f + PowerLevel(PowerId.GoldGain) * PowerCatalog.Get(PowerId.GoldGain).perLevel;
            m.xp = 1f + PowerLevel(PowerId.XpGain) * PowerCatalog.Get(PowerId.XpGain).perLevel;
            m.armor = PowerLevel(PowerId.Armor);
            return m;
        }

        public int FreeRevives => PowerLevel(PowerId.Revive);
    }
}
