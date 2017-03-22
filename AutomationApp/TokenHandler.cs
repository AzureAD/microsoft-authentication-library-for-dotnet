using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Runtime.Serialization.Json;
using System.Windows.Forms;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal.Cache;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Instance;

namespace AutomationApp
{
    public class TokenHandler
    {
        #region Properties
        private static readonly Dictionary<string, string> JsonLabelReplacements = new Dictionary<string, string>();
        public User CurrentUser { get; set; }
        
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
            string result = String.Empty;
            using (MemoryStream msStream = new MemoryStream())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(obj.GetType());
                serializer.WriteObject(msStream, obj);
                msStream.Position = 0;

                using (StreamReader reader = new StreamReader(msStream))
                {
                    result = reader.ReadToEnd();
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

        /*private List<KeyValuePair<TokenCacheKey, AuthenticationResult>> QueryCache(string authority, string clientId,
            string uniqueId, string displayableId)
        {
           return CurrentTokenCache.
        }*/

        public async Task<string> AcquireToken(Dictionary<string, string> input)
        {
            PublicClientApplication client = new PublicClientApplication(input["client_id"], input["authority"]);
            string[] scope = new string[] { "mail.read" };

            AuthenticationResult result =
                await
                    client.AcquireTokenAsync(scope)
                        .ConfigureAwait(false);
            CurrentUser = result.User;
            return ToJson(result);
        }

        public async Task<string> AcquireTokenSilent(Dictionary<string, string> input)
        {
            PublicClientApplication client = new PublicClientApplication(input["client_id"]);
            string[] scope = new string[] { "mail.read" };

            AuthenticationResult result = await 
                //client.AcquireTokenSilentCommonAsync(Authority.CreateAuthority(input["authority"], true), scope, CurrentUser, false)
                client.AcquireTokenSilentAsync(scope, CurrentUser)
                .ConfigureAwait(false);

            return ToJson(result);
        }

        public async Task<string> ExpireAccessToken(Dictionary<string, string> input)
        {
            PublicClientApplication client = new PublicClientApplication(input["client_id"]);
            string[] scope = new string[] { "mail.read" };

            AuthenticationResult result = await client
                .AcquireTokenAsync(scope)
                .ConfigureAwait(false);

            string expireResult = result.ExpiresOn.AddSeconds(5).ToString();

            return ToJson(expireResult);
        }
        
       /* public async Task<string> ReadCache(Dictionary<string, string> input)
        {
            
        }

        /*
        public async Task<string> ClearCache(Dictionary<string, string> input)
        {

        }*/
    }
}
