using System;

namespace NeonHorde
{
    public sealed class UpgradeOption
    {
        public string key;    // "w:Bolt" / "nw:Aura" / "p:Might" / "np:Luck" / "evo:Railgun"
        public string title;
        public string desc;
        public Action apply;

        public UpgradeOption(string key, string title, string desc, Action apply)
        {
            this.key = key;
            this.title = title;
            this.desc = desc;
            this.apply = apply;
        }
    }
}
