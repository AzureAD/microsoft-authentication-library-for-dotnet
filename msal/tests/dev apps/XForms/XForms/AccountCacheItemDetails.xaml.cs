using Microsoft.Identity.Core.Cache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace XForms
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
            clientInfoUniqueIdentifierLabel.Text = msalAccountCacheItem.ClientInfo.UniqueIdentifier;
            clientInfoUniqueTenantIdentifierLabel.Text = msalAccountCacheItem.ClientInfo.UniqueTenantIdentifier;
        }
	}
}