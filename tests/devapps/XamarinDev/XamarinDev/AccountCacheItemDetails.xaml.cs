// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Cache.Items;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace XamarinDev
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class AccountCacheItemDetails : ContentPage
	{
		internal AccountCacheItemDetails (MsalAccountCacheItem msalAccountCacheItem)
		{
			InitializeComponent ();

            authorityTypeLabel.Text = msalAccountCacheItem.AuthorityType;
            environmentLabel.Text = msalAccountCacheItem.Environment;
            userIdentifierLabel.Text = msalAccountCacheItem.HomeAccountId;
            preferredUsernameLabel.Text = msalAccountCacheItem.PreferredUsername;

            localAccountIdLabel.Text = msalAccountCacheItem.LocalAccountId;

            rawClientInfoLabel.Text = msalAccountCacheItem.RawClientInfo;
            clientInfoUniqueIdentifierLabel.Text = msalAccountCacheItem.ClientInfo.UniqueObjectIdentifier;
            clientInfoUniqueTenantIdentifierLabel.Text = msalAccountCacheItem.ClientInfo.UniqueTenantIdentifier;
        }
	}
}
