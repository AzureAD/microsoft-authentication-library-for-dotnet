
using System;
using System.Windows.Forms;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Cache;

namespace DesktopTestApp
{
    public partial class MsalUserRefreshTokenControl : UserControl
    {
        private TokenCache _cache;
        private MsalRefreshTokenCacheItem _rtItem;
        private MsalAccountCacheItem _accountItem;
        public delegate void RefreshView();

        private const string GarbageRtValue = "garbage-refresh-token";

        public RefreshView RefreshViewDelegate { get; set; }

        internal MsalUserRefreshTokenControl(TokenCache cache, MsalRefreshTokenCacheItem rtIitem) : this()
        {
            _cache = cache;
            _rtItem = rtIitem;

            _accountItem = cache.GetAccount(rtIitem, new RequestContext(new MsalLogger(Guid.NewGuid(), null)));
            upnLabel.Text = _accountItem.PreferredUsername;

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

        private void signOutUserOneBtn_Click(object sender, System.EventArgs e)
        {
            _cache.Remove(
                new User(_rtItem.HomeAccountId, _accountItem.PreferredUsername, _accountItem.Environment), 
                    new RequestContext(new MsalLogger(Guid.NewGuid(), null)));
            RefreshViewDelegate?.Invoke();
        }
    }
}
