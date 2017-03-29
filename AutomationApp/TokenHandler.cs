using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

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

        public async Task<IAuthenticationResult> AcquireToken(Dictionary<string, string> input)
        {
            EnsurePublicClientApplication(input);

            string[] scope = { "mail.read" };

            IAuthenticationResult result =
                await
                    _publicClientApplication.AcquireTokenAsync(scope)
                        .ConfigureAwait(false);
            CurrentUser = result.User;
            return result;
        }

        public async Task<IAuthenticationResult> AcquireTokenSilent(Dictionary<string, string> input)
        {
            EnsurePublicClientApplication(input);

            string[] scope = { "mail.read" };

            IAuthenticationResult result = await
                _publicClientApplication.AcquireTokenSilentAsync(scope, CurrentUser)
                .ConfigureAwait(false);

            return result;
        }

        public void ExpireAccessToken(Dictionary<string, string> input)
        {
            EnsurePublicClientApplication(input);

            _publicClientApplication.Remove(CurrentUser);
        }
    }
}
