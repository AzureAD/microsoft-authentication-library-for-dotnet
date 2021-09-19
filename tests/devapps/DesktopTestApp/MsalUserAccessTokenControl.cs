// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;

namespace DesktopTestApp
{
    public partial class MsalUserAccessTokenControl : UserControl
    {
        private readonly ITokenCacheInternal _cache;
        private readonly MsalAccessTokenCacheItem _item;
        public delegate Task RefreshViewAsync();

        public RefreshViewAsync RefreshViewAsyncDelegate { get; set; }

        // todo add id token
        internal MsalUserAccessTokenControl(ITokenCacheInternal cache, MsalAccessTokenCacheItem item) : this()
        {
            _cache = cache;
            _item = item;
            accessTokenAuthorityLabel.Text = _item.Authority;
            accessTokenScopesLabel.Text = string.Join(" ", _item.ScopeSet.ToArray());
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
            _cache.Accessor.DeleteAccessToken(_item);
            RefreshViewAsyncDelegate?.Invoke();
        }
    }
}
