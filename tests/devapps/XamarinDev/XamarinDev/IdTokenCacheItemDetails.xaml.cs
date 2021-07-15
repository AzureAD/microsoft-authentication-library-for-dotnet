using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Utils;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace XamarinDev
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class IdTokenCacheItemDetails : ContentPage
    {
        internal IdTokenCacheItemDetails(MsalIdTokenCacheItem msalIdTokenCacheItem)
        {
            InitializeComponent();

            credentialTypeLabel.Text = msalIdTokenCacheItem.CredentialType;

            clientIdLabel.Text = msalIdTokenCacheItem.ClientId;

            authorityLabel.Text = msalIdTokenCacheItem.Environment + "/" + msalIdTokenCacheItem.TenantId ?? "common";
            environmentLabel.Text = msalIdTokenCacheItem.Environment;
            tenantIdLabel.Text = msalIdTokenCacheItem.TenantId;

            userIdentifierLabel.Text = msalIdTokenCacheItem.HomeAccountId;

            secretLabel.Text = StringShortenerConverter.GetShortStr(msalIdTokenCacheItem.Secret, 100);            

            rawClientInfoLabel.Text = msalIdTokenCacheItem.RawClientInfo;

            if (msalIdTokenCacheItem.RawClientInfo != null)
            {
                var clientInfo = ClientInfo.CreateFromJson(msalIdTokenCacheItem.RawClientInfo);

                clientInfoUniqueIdentifierLabel.Text = clientInfo.UniqueObjectIdentifier;
                clientInfoUniqueTenantIdentifierLabel.Text = clientInfo.UniqueTenantIdentifier;
            }
        }
    }
}
