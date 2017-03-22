using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.Serialization.Json;
using Microsoft.Identity.Client;

namespace AutomationApp
{
    public class TokenHandler
    {
        #region Properties
        private static readonly Dictionary<string, string> JsonLabelReplacements = new Dictionary<string, string>();
        public User CurrentUser { get; set; }

        private PublicClientApplication _publicClientApplication;
       #endregion

        #region Constructor
        static TokenHandler()
        {
            JsonLabelReplacements.Add("AccessTokenType", "access_token_type");
            JsonLabelReplacements.Add("AccessToken", "access_token");
            JsonLabelReplacements.Add("ExpiresOn", "expires_on");
            JsonLabelReplacements.Add("ExtendedExpiresOn", "extended_expires_on");
            JsonLabelReplacements.Add("ExtendedLifeTimeToken", "extended_lifetime_token");
            JsonLabelReplacements.Add("IdToken", "id_token");
            JsonLabelReplacements.Add("TenantId", "tenant_id");
            JsonLabelReplacements.Add("UserInfo", "user_info");
            JsonLabelReplacements.Add("DisplayableId", "displayable_id");
            JsonLabelReplacements.Add("FamilyName", "family_name");
            JsonLabelReplacements.Add("GivenName", "given_name");
            JsonLabelReplacements.Add("IdentityProvider", "identity_provider");
            JsonLabelReplacements.Add("PasswordChangeUrl", "password_change_url");
            JsonLabelReplacements.Add("PasswordExpiresOn", "password_expires_on");
            JsonLabelReplacements.Add("UniqueId", "unique_id");
        }
        #endregion

        private static string ToJson(object obj)
        {
            using (MemoryStream msStream = new MemoryStream())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(obj.GetType());
                serializer.WriteObject(msStream, obj);
                msStream.Position = 0;

                using (StreamReader reader = new StreamReader(msStream))
                {
                    var result = reader.ReadToEnd();
                    foreach (KeyValuePair<string, string> entry in JsonLabelReplacements)
                    {
                        if (result.Contains(entry.Key))
                        {
                            result = result.Replace(entry.Key, entry.Value);
                        }
                    }
                    return result;
                }
            }
        }

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
            return ToJson(result);
        }

        public async Task<string> AcquireTokenSilent(Dictionary<string, string> input)
        {
            EnsurePublicClientApplication(input);

            string[] scope = { "mail.read" };

            AuthenticationResult result = await
                _publicClientApplication.AcquireTokenSilentAsync(scope, CurrentUser)
                .ConfigureAwait(false);

            return ToJson(result);
        }

        public async Task<string> ExpireAccessToken(Dictionary<string, string> input)
        {
            EnsurePublicClientApplication(input);

            string[] scope = { "mail.read" };

            AuthenticationResult result = await
                _publicClientApplication.AcquireTokenSilentAsync(scope, CurrentUser)
                .ConfigureAwait(false);

            string expireResult = result.ExpiresOn.AddSeconds(5).ToString();

            return ToJson(expireResult);
        }
    }
}
