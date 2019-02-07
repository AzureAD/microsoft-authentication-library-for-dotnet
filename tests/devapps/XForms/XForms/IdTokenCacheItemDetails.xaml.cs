using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Utils;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace XForms
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class IdTokenCacheItemDetails : ContentPage
    {
        internal IdTokenCacheItemDetails(MsalIdTokenCacheItem msalIdTokenCacheItem)
        {
            InitializeComponent();

            credentialTypeLabel.Text = msalIdTokenCacheItem.CredentialType;

            clientIdLabel.Text = msalIdTokenCacheItem.ClientId;

            authorityLabel.Text = msalIdTokenCacheItem.Authority;
            environmentLabel.Text = msalIdTokenCacheItem.Environment;
            tenantIdLabel.Text = msalIdTokenCacheItem.TenantId;

            userIdentifierLabel.Text = msalIdTokenCacheItem.HomeAccountId;

            secretLabel.Text = StringShortenerConverter.GetShortStr(msalIdTokenCacheItem.Secret, 100);

            idTokenLabel.Text = JsonHelper.SerializeToJson(msalIdTokenCacheItem.IdToken);

            rawClientInfoLabel.Text = msalIdTokenCacheItem.RawClientInfo;
            clientInfoUniqueIdentifierLabel.Text = msalIdTokenCacheItem.ClientInfo.UniqueObjectIdentifier;
            clientInfoUniqueTenantIdentifierLabel.Text = msalIdTokenCacheItem.ClientInfo.UniqueTenantIdentifier;


            //userDisplayableIdLabel.Text = MsalAccountCacheItem.PreferredUsername;
            //userNameLabel.Text = MsalAccountCacheItem.Name;
            //userIdentityProviderLabel.Text = MsalAccountCacheItem.IdentityProvider;
        }
    }
}