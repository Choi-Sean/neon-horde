using System;
using System.Collections.Generic;

namespace NeonHorde
{
    public enum IapKind { Character, CoreBundle, RemoveAds, StarterPack }

    public struct IapProduct
    {
        public string id;
        public IapKind kind;
        public string title;
        public string priceText;      // display only; store is source of truth
        public CharacterId character;
        public int cores;
        public long gold;
    }

    public static class IapCatalog
    {
        public static readonly IapProduct[] All =
        {
            new IapProduct { id = "char.volt",   kind = IapKind.Character, title = "VOLT 해금",   priceText = "₩3,300", character = CharacterId.Volt },
            new IapProduct { id = "char.aegis",  kind = IapKind.Character, title = "AEGIS 해금",  priceText = "₩4,400", character = CharacterId.Aegis },
            new IapProduct { id = "char.halo",   kind = IapKind.Character, title = "HALO 해금",   priceText = "₩5,500", character = CharacterId.Halo },
            new IapProduct { id = "cores.small", kind = IapKind.CoreBundle, title = "코어 60",    priceText = "₩3,300", cores = 60 },
            new IapProduct { id = "cores.medium",kind = IapKind.CoreBundle, title = "코어 160",   priceText = "₩7,700", cores = 160 },
            new IapProduct { id = "cores.large", kind = IapKind.CoreBundle, title = "코어 360",   priceText = "₩16,000", cores = 360 },
            new IapProduct { id = "remove_ads",  kind = IapKind.RemoveAds,  title = "광고 제거",   priceText = "₩4,400" },
            new IapProduct { id = "starter_pack",kind = IapKind.StarterPack,title = "스타터 팩",   priceText = "₩3,300", cores = 80, gold = 500 },
        };

        public static bool TryGet(string id, out IapProduct product)
        {
            foreach (var p in All) if (p.id == id) { product = p; return true; }
            product = default;
            return false;
        }
    }

    public struct PurchaseResult { public bool success; public string productId; public string error; }

    public interface IIapService
    {
        IReadOnlyList<IapProduct> Products { get; }
        void Purchase(string productId, Action<PurchaseResult> onDone);
        void Restore(Action<bool> onDone);
    }

    /// <summary>
    /// Editor / pre-integration stub — "buys" instantly and applies the grant.
    /// Replace with Unity IAP (com.unity.purchasing) wired to App Store / Play Console
    /// products — see docs/EXTERNAL_SETUP.md.
    /// </summary>
    public sealed class StubIapService : IIapService
    {
        public IReadOnlyList<IapProduct> Products => IapCatalog.All;

        public void Purchase(string productId, Action<PurchaseResult> onDone)
        {
            bool ok = IapCatalog.TryGet(productId, out _);
            UnityEngine.Debug.Log($"[IAP:STUB] purchase {productId} -> {(ok ? "success" : "unknown product")}");
            ServiceLocator.TryGet<IAnalyticsService>(out var a);
            a?.Log("iap_purchase", ("product", productId), ("result", ok ? "success_stub" : "unknown"));
            onDone?.Invoke(new PurchaseResult { success = ok, productId = productId, error = ok ? null : "unknown product" });
        }

        public void Restore(Action<bool> onDone) => onDone?.Invoke(true);
    }

    /// <summary>Applies a completed purchase to the profile.</summary>
    public static class IapFulfillment
    {
        public static void Grant(string productId, MetaController meta)
        {
            if (!IapCatalog.TryGet(productId, out var p) || meta == null) return;
            if (!meta.State.ownedProducts.Contains(productId)) meta.State.ownedProducts.Add(productId);

            switch (p.kind)
            {
                case IapKind.Character: meta.GrantCharacter(p.character); break;
                case IapKind.CoreBundle: meta.AddCores(p.cores); break;
                case IapKind.RemoveAds: meta.State.adsRemoved = true; meta.Save(); break;
                case IapKind.StarterPack:
                    meta.AddCores(p.cores);
                    meta.AddGold(p.gold);
                    meta.State.adsRemoved = true;
                    meta.Save();
                    break;
            }
        }
    }
}
