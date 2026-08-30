using System;

namespace NeonHorde
{
    public enum AccountState { Guest, Linked }

    /// <summary>
    /// Player identity + cloud-save link. The guest stub keeps everything local; the
    /// real implementation is Unity Authentication (anonymous sign-in, then link
    /// email / Google / Apple) + Cloud Save. See docs/EXTERNAL_SETUP.md.
    /// </summary>
    public interface IAccountService
    {
        AccountState State { get; }
        bool IsLinked { get; }
        string DisplayName { get; }

        void LinkWithEmail(string email, Action<bool> onDone);
        void LinkWithProvider(string provider, Action<bool> onDone);   // "google" | "apple"
        void Unlink();
    }

    public sealed class GuestAccountService : IAccountService
    {
        readonly MetaController _meta;
        public GuestAccountService(MetaController meta) => _meta = meta;

        public AccountState State => _meta.State.accountLinked ? AccountState.Linked : AccountState.Guest;
        public bool IsLinked => _meta.State.accountLinked;
        public string DisplayName => IsLinked ? _meta.State.accountName : "게스트";

        public void LinkWithEmail(string email, Action<bool> onDone)
        {
            // STUB: a real backend verifies + merges cloud save here.
            var s = _meta.State;
            s.accountLinked = true;
            s.accountId = "stub-email:" + email;
            s.accountName = email;
            _meta.Save();
            ServiceLocator.TryGet<IAnalyticsService>(out var a);
            a?.Log("account_linked", ("method", "email_stub"));
            onDone?.Invoke(true);
        }

        public void LinkWithProvider(string provider, Action<bool> onDone)
        {
            var s = _meta.State;
            s.accountLinked = true;
            s.accountId = $"stub-{provider}:{Guid.NewGuid():N}";
            s.accountName = provider == "apple" ? "Apple 계정" : "Google 계정";
            _meta.Save();
            ServiceLocator.TryGet<IAnalyticsService>(out var a);
            a?.Log("account_linked", ("method", provider + "_stub"));
            onDone?.Invoke(true);
        }

        public void Unlink()
        {
            var s = _meta.State;
            s.accountLinked = false;
            s.accountId = "";
            s.accountName = "";
            _meta.Save();
        }
    }
}
