// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Utils;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace XForms
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AccessTokenCacheItemDetails : ContentPage
    {
        internal AccessTokenCacheItemDetails(MsalAccessTokenCacheItem msalAccessTokenCacheItem,
            MsalIdTokenCacheItem msalIdTokenCacheItem)
        {
            InitializeComponent();

            clientIdLabel.Text = msalAccessTokenCacheItem.ClientId;

            credentialTypeLabel.Text = msalAccessTokenCacheItem.CredentialType;
            environmentLabel.Text = msalAccessTokenCacheItem.Environment;
            tenantIdLabel.Text = msalAccessTokenCacheItem.TenantId;

            userIdentifierLabel.Text = msalAccessTokenCacheItem.HomeAccountId;
            userAssertionHashLabel.Text = msalAccessTokenCacheItem.UserAssertionHash;

            expiresOnLabel.Text = msalAccessTokenCacheItem.ExpiresOn.ToString(CultureInfo.InvariantCulture);
            scopesLabel.Text = msalAccessTokenCacheItem.NormalizedScopes;

            cachedAtLabel.Text = CoreHelpers
                .UnixTimestampStringToDateTime(msalAccessTokenCacheItem.CachedAt)
                .ToString(CultureInfo.InvariantCulture);

            rawClientInfoLabel.Text = msalAccessTokenCacheItem.RawClientInfo;
            clientInfoUniqueIdentifierLabel.Text = msalAccessTokenCacheItem.ClientInfo.UniqueObjectIdentifier;
            clientInfoUniqueTenantIdentifierLabel.Text = msalAccessTokenCacheItem.ClientInfo.UniqueTenantIdentifier;

            secretLabel.Text = StringShortenerConverter.GetShortStr(msalAccessTokenCacheItem.Secret, 100);
        }
    }
}
