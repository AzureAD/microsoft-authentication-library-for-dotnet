// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace XamarinDev
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

            if (msalRefreshTokenCacheItem.RawClientInfo != null)
            {
                var clientInfo = ClientInfo.CreateFromJson(msalRefreshTokenCacheItem.RawClientInfo);

                clientInfoUniqueIdentifierLabel.Text = clientInfo.UniqueObjectIdentifier;
                clientInfoUniqueTenantIdentifierLabel.Text = clientInfo.UniqueTenantIdentifier;
            }

            secretLabel.Text = StringShortenerConverter.GetShortStr(msalRefreshTokenCacheItem.Secret, 100);
        }
    }
}
