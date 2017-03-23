using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.Serialization.Json;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;

namespace AutomationApp
{
    public class TokenHandler
    {
        #region Properties
        public User CurrentUser { get; set; }

        private PublicClientApplication _publicClientApplication;
        #endregion

        private void EnsurePublicClientApplication(Dictionary<string, string> input)
        {
            if (_publicClientApplication != null) return;
            if (!input.ContainsKey("client_id")) return;
            _publicClientApplication = input.ContainsKey("authority")
                ? new PublicClientApplication(input["client_id"], input["authority"])
                : new PublicClientApplication(input["client_id"]);
        }

        public async Task<string> AcquireToken(Dictionary<string, string> input)
        {
            EnsurePublicClientApplication(input);

            string[] scope = { "mail.read" };

            AuthenticationResult result =
                await
                    _publicClientApplication.AcquireTokenAsync(scope)
                        .ConfigureAwait(false);
            CurrentUser = result.User;
            return JsonHelper.SerializeToJson(result);
        }

        public async Task<string> AcquireTokenSilent(Dictionary<string, string> input)
        {
            EnsurePublicClientApplication(input);

            string[] scope = { "mail.read" };

            AuthenticationResult result = await
                _publicClientApplication.AcquireTokenSilentAsync(scope, CurrentUser)
                .ConfigureAwait(false);

            return JsonHelper.SerializeToJson(result);
        }

        public async Task<string> ExpireAccessToken(Dictionary<string, string> input)
        {
            EnsurePublicClientApplication(input);

            string[] scope = { "mail.read" };

            AuthenticationResult result = await
                _publicClientApplication.AcquireTokenSilentAsync(scope, CurrentUser)
                .ConfigureAwait(false);

            string expireResult = result.ExpiresOn.AddSeconds(5).ToString();

            return JsonHelper.SerializeToJson(result);
        }
    }
}
