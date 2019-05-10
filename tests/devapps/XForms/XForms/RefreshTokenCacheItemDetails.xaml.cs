// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace XForms
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class RefreshTokenCacheItemDetails : ContentPage
    {
        internal RefreshTokenCacheItemDetails(MsalRefreshTokenCacheItem msalRefreshTokenCacheItem)
        {
            InitializeComponent();

            environmentLabel.Text = msalRefreshTokenCacheItem.Environment;
            clientIdLabel.Text = msalRefreshTokenCacheItem.ClientId;

            credentialTypeLabel.Text = msalRefreshTokenCacheItem.CredentialType;
            userIdentifierLabel.Text = msalRefreshTokenCacheItem.HomeAccountId;
            rawClientInfoLabel.Text = msalRefreshTokenCacheItem.RawClientInfo;

            clientInfoUniqueIdentifierLabel.Text = msalRefreshTokenCacheItem.ClientInfo.UniqueObjectIdentifier;
            clientInfoUniqueTenantIdentifierLabel.Text = msalRefreshTokenCacheItem.ClientInfo.UniqueTenantIdentifier;

            secretLabel.Text = StringShortenerConverter.GetShortStr(msalRefreshTokenCacheItem.Secret, 100);
        }
    }
}
