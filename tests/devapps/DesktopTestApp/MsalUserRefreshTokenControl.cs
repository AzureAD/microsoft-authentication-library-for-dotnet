
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;

namespace DesktopTestApp
{
    public partial class MsalUserRefreshTokenControl : UserControl
    {
        private readonly ITokenCacheInternal _cache;
        private readonly PublicClientApplication _publicClient;
        private readonly MsalRefreshTokenCacheItem _rtItem;
        private readonly MsalAccountCacheItem accountItem;
        public delegate void RefreshView();

        private const string GarbageRtValue = "garbage-refresh-token";

        public RefreshView RefreshViewDelegate { get; set; }

        internal MsalUserRefreshTokenControl(PublicClientApplication publicClient, MsalRefreshTokenCacheItem rtItem) : this()
        {
            _publicClient = publicClient;
            _cache = publicClient.UserTokenCacheInternal;
            _rtItem = rtItem;

            accountItem = _cache.GetAccount(_rtItem, RequestContext.CreateForTest());
            upnLabel.Text = accountItem.PreferredUsername;

            invalidateRefreshTokenBtn.Enabled = !_rtItem.Secret.Equals(GarbageRtValue, StringComparison.OrdinalIgnoreCase);
        }

        public MsalUserRefreshTokenControl()
        {
            InitializeComponent();
        }

        private void InvalidateRefreshTokenBtn_Click(object sender, System.EventArgs e)
        {
            _rtItem.Secret = GarbageRtValue;
            _cache.SaveRefreshTokenCacheItem(_rtItem, null);
            invalidateRefreshTokenBtn.Enabled = false;
        }

        private async void signOutUserOneBtn_Click(object sender, System.EventArgs e)
        {
            IEnumerable<IAccount> accounts = await _publicClient.GetAccountsAsync().ConfigureAwait(false);

            while (accounts.Any())
            {
                await _publicClient.RemoveAsync(accounts.FirstOrDefault()).ConfigureAwait(false);
                accounts = await _publicClient.GetAccountsAsync().ConfigureAwait(false);
            }

            RefreshViewDelegate?.Invoke();
        }
    }
}
