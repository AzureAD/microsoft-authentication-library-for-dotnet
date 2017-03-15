using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Json;
using System.Web.Script.Serialization;
using Microsoft.Identity.Client;


namespace AutomationApp
{
    internal static class AuthenticationHelper
    {

        public static async Task<string> AcquireToken(Dictionary<string, string> input)
        {
            PublicClientApplication ctx = new PublicClientApplication(input["client_id"], input["authority"]);
            string output = string.Empty;
            string [] scope = new string[] {"mail.read"};
            
            try
            {
                AuthenticationResult result =
                    await
                        ctx.AcquireTokenAsync(scope)
                            .ConfigureAwait(false);
                output = result.ToJson();
            }
            catch (Exception exc)
            {
                output = exc.Message;
            }

            return output;
        }

        public static string ToJson(this object obj)
        {
            using (MemoryStream msStream = new MemoryStream())
            {
                DataContractJsonSerializer serializer =
                    new DataContractJsonSerializer(obj.GetType());
                serializer.WriteObject(msStream, obj);
                msStream.Position = 0;

                using (StreamReader reader = new StreamReader(msStream))
                {
                    return JsonOutputFormat(reader.ReadToEnd());
                }
            }
        }

        public static Dictionary<string, string> CreateDictionaryFromJson(string json)
        {
            var jss = new JavaScriptSerializer();
            return jss.Deserialize<Dictionary<string, string>>(json);
        }

        private static string JsonOutputFormat(string result)
        {
            Dictionary<string, string> jsonDictionary = new Dictionary<string, string>();
            jsonDictionary.Add("AccessTokenType", "access_token_type");
            jsonDictionary.Add("AccessToken", "access_token");
            jsonDictionary.Add("ExpiresOn", "expires_on");
            jsonDictionary.Add("ExtendedExpiresOn", "extended_expires_on");
            jsonDictionary.Add("ExtendedLifeTimeToken", "extended_lifetime_token");
            jsonDictionary.Add("IdToken", "id_token");
            jsonDictionary.Add("TenantId", "tenant_id");
            jsonDictionary.Add("UserInfo", "user_info");
            jsonDictionary.Add("DisplayableId", "displayable_id");
            jsonDictionary.Add("FamilyName", "family_name");
            jsonDictionary.Add("GivenName", "given_name");
            jsonDictionary.Add("IdentityProvider", "identity_provider");
            jsonDictionary.Add("PasswordChangeUrl", "password_change_url");
            jsonDictionary.Add("PasswordExpiresOn", "password_expires_on");
            jsonDictionary.Add("UniqueId", "unique_id");

            foreach (KeyValuePair<string, string> entry in jsonDictionary)
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
