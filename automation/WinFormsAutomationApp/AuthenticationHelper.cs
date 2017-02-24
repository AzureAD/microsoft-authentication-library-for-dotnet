using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace WinFormsAutomationApp
{
    internal static class AuthenticationHelper
    {
        public static async Task<string> AcquireToken(Dictionary<string, string> input)
        {
            AuthenticationContext ctx = new AuthenticationContext(input["authority"]);
            string output = string.Empty;
            try
            {
                AuthenticationResult result =
                    await
                        ctx.AcquireTokenAsync(input["resource"], input["client_id"], new Uri(input["redirect_uri"]),
                            GetPlatformParametersInstance(input["prompt_behavior"])).ConfigureAwait(false);
                output = result.ToJson();
            }
            catch (Exception exc)
            {
                output = exc.Message;
            }

            return output;
        }

        public static async Task<string> AcquireTokenSilent(Dictionary<string, string> input)
        {
            AuthenticationContext ctx = new AuthenticationContext(input["authority"]);
            string output = string.Empty;
            try
            {
                AuthenticationResult result = await ctx.AcquireTokenSilentAsync(input["resource"], input["client_id"]).ConfigureAwait(false);
                output = result.ToJson();
            }
            catch (Exception exc)
            {
                output = exc.Message;
            }

            return output;
        }

        public static async Task<string> ExpireAccessToken(Dictionary<string, string> input)
        {
            Task<string> myTask = Task<string>.Factory.StartNew(() =>
            {
                List<KeyValuePair<TokenCacheKey, AuthenticationResultEx>> CacheItems = QueryCache(input["authority"],
                    input["client_id"], input["unique_id"], input["displayable_id"]);

                foreach (KeyValuePair<TokenCacheKey, AuthenticationResultEx> item in CacheItems)
                {
                    // if resource was passed to cache lookup, then only expire token for that resource.
                    // otherwise expire all matching access tokens.
                    if (input["resource"] == null || item.Key.ResourceEquals(input["resource"]))
                    {
                        item.Value.Result.ExpiresOn = DateTime.UtcNow;
                        TokenCache.DefaultShared.tokenCacheDictionary[item.Key] = item.Value;
                    }
                }
                Dictionary<string, string> output = new Dictionary<string, string>();
                //Send back error if userId or displayableId is not sent back to the user
                output.Add("expired_access_token_count", CacheItems.Count.ToString());
                return output.ToJson();
            });

            return await myTask.ConfigureAwait(false);
        }

        public static async Task<string> InvalidateRefreshToken(Dictionary<string, string> input)
        {
            Task<string> myTask = Task<string>.Factory.StartNew(() =>
            {
                List<KeyValuePair<TokenCacheKey, AuthenticationResultEx>> CacheItems = QueryCache(input["authority"],
                    input["client_id"], input["unique_id"], input["displayable_id"]);

                foreach (KeyValuePair<TokenCacheKey, AuthenticationResultEx> item in CacheItems)
                {
                    item.Value.RefreshToken = "bad_refresh_token";
                    TokenCache.DefaultShared.tokenCacheDictionary[item.Key] = item.Value;
                }
                Dictionary<string, string> output = new Dictionary<string, string>();
                //Send back error if userId or displayableId is not sent back to the user
                output.Add("invalidated_refresh_token_count", CacheItems.Count.ToString());
                return output.ToJson();
            });

            return await myTask.ConfigureAwait(false);
        }

        private static List<KeyValuePair<TokenCacheKey, AuthenticationResultEx>> QueryCache(string authority,
            string clientId, string uniqueId, string displayableId)
        {
            return TokenCache.DefaultShared.tokenCacheDictionary.Where(
                p =>
                    (string.IsNullOrWhiteSpace(authority) || p.Key.Authority == authority)
                    && (string.IsNullOrWhiteSpace(clientId) || p.Key.ClientIdEquals(clientId))
                    && (string.IsNullOrWhiteSpace(uniqueId) || p.Key.UniqueId == uniqueId)
                    && (string.IsNullOrWhiteSpace(displayableId) || p.Key.DisplayableIdEquals(displayableId))).ToList();
        }

        public static async Task<string> ReadCache(Dictionary<string, string> input)
        {
            Task<string> myTask = Task<string>.Factory.StartNew(() =>
            {
                int count = TokenCache.DefaultShared.Count;
                Dictionary<string, string> output = new Dictionary<string, string>();
                output.Add("item_count", count.ToString());
                var list = TokenCache.DefaultShared.ReadItems().ToJson();
                output.Add("AccessToken", list);
                return output.ToJson();
            });

            return await myTask.ConfigureAwait(false);
        }

        public static async Task<string> ClearCache(Dictionary<string, string> input)
        {
            Task<string> myTask = Task<string>.Factory.StartNew(() =>
            {
                int count = TokenCache.DefaultShared.Count;
                Dictionary<string, string> output = new Dictionary<string, string>();
                output.Add("item_count", count.ToString());
                TokenCache.DefaultShared.Clear();
                output.Add("cache_clear_status", "Cleared the entire cache");
                return output.ToJson();
            });

            return await myTask.ConfigureAwait(false);
        }


        public static async Task<string> AcquireTokenUsingDeviceProfile(Dictionary<string, string> input)
        {
            Task<string> myTask = Task<string>.Factory.StartNew(() =>
            {
                int count = TokenCache.DefaultShared.Count;
                Dictionary<string, string> output = new Dictionary<string, string>();
                output.Add("item_count", count.ToString());
                var list = TokenCache.DefaultShared.ReadItems().ToJson();
                output.Add("AccessToken", list);
                return output.ToJson();
            });

            return await myTask.ConfigureAwait(false);
        }

        public static string ToJson(this object obj)
        {
            using (MemoryStream mstream = new MemoryStream())
            {
                DataContractJsonSerializer serializer =
                    new DataContractJsonSerializer(obj.GetType());
                serializer.WriteObject(mstream, obj);
                mstream.Position = 0;

                using (StreamReader reader = new StreamReader(mstream))
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
            Dictionary<string, string> jsonDictitionary = new Dictionary<string, string>();
            jsonDictitionary.Add("AccessTokenType", "access_token_type");
            jsonDictitionary.Add("AccessToken", "access_token");
            jsonDictitionary.Add("ExpiresOn", "expires_on");
            jsonDictitionary.Add("ExtendedExpiresOn", "extended_expires_on");
            jsonDictitionary.Add("ExtendedLifeTimeToken", "extended_lifetime_token");
            jsonDictitionary.Add("IdToken", "id_token");
            jsonDictitionary.Add("TenantId", "tenant_id");
            jsonDictitionary.Add("UserInfo", "user_info");
            jsonDictitionary.Add("DisplayableId", "displayable_id");
            jsonDictitionary.Add("FamilyName", "family_name");
            jsonDictitionary.Add("GivenName", "given_name");
            jsonDictitionary.Add("IdentityProvider", "identity_provider");
            jsonDictitionary.Add("PasswordChangeUrl", "password_change_url");
            jsonDictitionary.Add("PasswordExpiresOn", "password_expires_on");
            jsonDictitionary.Add("UniqueId", "unique_id");

            foreach (KeyValuePair<string, string> entry in jsonDictitionary)
            {
                if (result.Contains(entry.Key))
                {
                    result = result.Replace(entry.Key, entry.Value);
                }
            }

            return result;
        }

        private static IPlatformParameters GetPlatformParametersInstance(string promptBehaviorString)
        {
            IPlatformParameters platformParameters = null;
            PromptBehavior pb = PromptBehavior.Auto;
            if (!string.IsNullOrEmpty(promptBehaviorString))
            {
                pb = (PromptBehavior) Enum.Parse(typeof (PromptBehavior), promptBehaviorString, true);
            }

#if __ANDROID__
        platformParameters = new PlatformParameters(this);
#else
#if __IOS__
        platformParameters = new PlatformParameters(this);
#else
#if (WINDOWS_UWP || WINDOWS_APP)
            platformParameters = new PlatformParameters(PromptBehavior.Always, false);
#else
            //desktop
            platformParameters = new PlatformParameters(pb, null);
#endif
#endif
#endif
            return platformParameters;
        }
    }
}
