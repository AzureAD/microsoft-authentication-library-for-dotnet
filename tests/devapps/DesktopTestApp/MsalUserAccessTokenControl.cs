// ------------------------------------------------------------------------------
// 
// Copyright (c) Microsoft Corporation.
// All rights reserved.
// 
// This code is licensed under the MIT License.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// ------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Windows.Forms;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
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
