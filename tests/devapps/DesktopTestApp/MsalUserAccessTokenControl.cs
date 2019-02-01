
using System;
using System.Globalization;
using System.Windows.Forms;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;

namespace DesktopTestApp
{
    public partial class MsalUserAccessTokenControl : UserControl
    {
        private readonly ITokenCacheInternal _cache;
        private readonly MsalAccessTokenCacheItem _item;
        public delegate void RefreshView();

        public RefreshView RefreshViewDelegate { get; set; }

        // todo add id token
        internal MsalUserAccessTokenControl(ITokenCacheInternal cache, MsalAccessTokenCacheItem item) : this()
        {
            _cache = cache;
            _item = item;
            accessTokenAuthorityLabel.Text = _item.Authority;
            accessTokenScopesLabel.Text = _item.NormalizedScopes;
            expiresOnLabel.Text = _item.ExpiresOn.ToString(CultureInfo.CurrentCulture);
        }
        
        public MsalUserAccessTokenControl()
        {
            InitializeComponent();
        }

        private void expireAccessTokenButton_Click(object sender, System.EventArgs e)
        {
            expiresOnLabel.Text = DateTimeOffset.UtcNow.ToString(CultureInfo.CurrentCulture);
            _item.ExpiresOnUnixTimestamp = CoreHelpers.DateTimeToUnixTimestamp(DateTimeOffset.UtcNow);
            _cache.AddAccessTokenCacheItem(_item);
        }

        private void deleteAccessTokenButton_Click(object sender, EventArgs e)
        {
            var requestContext = RequestContext.CreateForTest();

            _cache.DeleteAccessToken(_item, null, requestContext);
            RefreshViewDelegate?.Invoke();
        }
    }
}
