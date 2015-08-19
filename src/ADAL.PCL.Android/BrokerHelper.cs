using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    class BrokerHelper : IBrokerHelper
    {
        public bool CanUseBroker { get; }
        public Task<AuthenticationResultEx> AcquireTokenUsingBroker(IDictionary<string, string> brokerPayload)
        {

            this.VerifyManifestPermissions();

            AccountManager accountManager = AccountManager.Get(Application.Context);

            this.VerifyBrokerApp(accountManager);
            this.AuthenticateViaBroker(accountManager, requestParameters, this.VerifyAccount(accountManager));
        }
    }
}