
using System;
using System.Windows.Forms;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Cache;

namespace DesktopTestApp
{
    public partial class MsalUserAccessTokenControl : UserControl
    {
        private readonly TokenCache _cache;
        private readonly AccessTokenCacheItem _item;
        public delegate void RefreshView();

        public RefreshView RefreshViewDelegate { get; set; }

        internal MsalUserAccessTokenControl(TokenCache cache, AccessTokenCacheItem item) : this()
        {
            _cache = cache;
            _item = item;
            accessTokenAuthorityLabel.Text = _item.Authority;
            accessTokenScopesLabel.Text = _item.Scope;
            expiresOnLabel.Text = _item.ExpiresOn.ToString();
        }
        
        public MsalUserAccessTokenControl()
        {
            InitializeComponent();
        }

        private void expireAccessTokenButton_Click(object sender, System.EventArgs e)
        {
            expiresOnLabel.Text = DateTimeOffset.UtcNow.ToString();
            _item.ExpiresOnUnixTimestamp = MsalHelpers.DateTimeToUnixTimestamp(DateTimeOffset.UtcNow);
            _cache.SaveAccesTokenCacheItem(_item);
        }

        private void deleteAccessTokenButton_Click(object sender, EventArgs e)
        {
            _cache.DeleteAccessToken(_item);
            RefreshViewDelegate?.Invoke();
        }
    }
}
