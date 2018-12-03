
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Cache;

namespace DesktopTestApp
{
    public partial class MsalUserRefreshTokenControl : UserControl
    {
        private TokenCache cache;
        private PublicClientApplication publicClient;
        private MsalRefreshTokenCacheItem rtItem;
        private MsalAccountCacheItem accountItem;
        public delegate void RefreshView();

        private const string GarbageRtValue = "garbage-refresh-token";

        public RefreshView RefreshViewDelegate { get; set; }

        internal MsalUserRefreshTokenControl(PublicClientApplication publicClient, MsalRefreshTokenCacheItem rtIitem) : this()
        {
            this.publicClient = publicClient;
            cache = publicClient.UserTokenCache;
            rtItem = rtIitem;

            accountItem = cache.GetAccount(rtIitem, new RequestContext(null, new MsalLogger(Guid.NewGuid(), null)));
            upnLabel.Text = accountItem.PreferredUsername;

            invalidateRefreshTokenBtn.Enabled = !rtItem.Secret.Equals(GarbageRtValue, StringComparison.OrdinalIgnoreCase);
        }

        public MsalUserRefreshTokenControl()
        {
            InitializeComponent();
        }

        private void InvalidateRefreshTokenBtn_Click(object sender, System.EventArgs e)
        {
            rtItem.Secret = GarbageRtValue;
            cache.SaveRefreshTokenCacheItem(rtItem, null);
            invalidateRefreshTokenBtn.Enabled = false;
        }

        private async void signOutUserOneBtn_Click(object sender, System.EventArgs e)
        {
            IEnumerable<IAccount> accounts = await publicClient.GetAccountsAsync().ConfigureAwait(false);

            while (accounts.Any())
            {
                await publicClient.RemoveAsync(accounts.FirstOrDefault()).ConfigureAwait(false);
                accounts = await publicClient.GetAccountsAsync().ConfigureAwait(false);
            }

            RefreshViewDelegate?.Invoke();
        }
    }
}
