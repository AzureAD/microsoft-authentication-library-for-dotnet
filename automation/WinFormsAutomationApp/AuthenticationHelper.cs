using Microsoft.IdentityModel.Clients.ActiveDirectory;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace WinFormsAutomationApp
{
    internal static class AuthenticationHelper
    {
        #region Public Methods      
        public static async Task<string> AcquireToken(Dictionary<string, string> input)
        {
            Dictionary<string, object> res = new Dictionary<string, object>();
            AuthenticationContext ctx = new AuthenticationContext(input["authority"]);
            try
            {
                AuthenticationResult result = null;
                 if (input.ContainsKey("user_identifier") && input.ContainsKey("password"))
                {
                    UserPasswordCredential user = new UserPasswordCredential(input["user_identifier"], input["password"]);
                    result =
                        await
                            ctx.AcquireTokenAsync(input["resource"], input["client_id"],user).ConfigureAwait(false);
                }
                else
                {
                    string prompt = input.ContainsKey("prompt_behavior") ? input["prompt_behavior"] : null;
                    result =
                        await
                            ctx.AcquireTokenAsync(input["resource"], input["client_id"], new Uri(input["redirect_uri"]),
                                GetPlatformParametersInstance(prompt)).ConfigureAwait(false);
                }
                res = ProcessResult(result, input);
            }
            catch (Exception exc)
            {
                res.Add("error", exc.Message);
            }
            return FromDictionaryToJson(res);
        }       

        public static async Task<string> AcquireTokenSilent(Dictionary<string, string> input)
        {
            AuthenticationContext ctx = new AuthenticationContext(input["authority"]);
            Dictionary<string, object> res = new Dictionary<string, object>();
            try
            {
                AuthenticationResult result = await ctx.AcquireTokenSilentAsync(input["resource"], input["client_id"]).ConfigureAwait(false);
                res = ProcessResult(result, input);
            }
            catch (Exception exc)
            {
                res.Add("error", exc.Message);
            }
            return FromDictionaryToJson(res);
        }

        public static async Task<string> ExpireAccessToken(Dictionary<string, string> input)
        {
           
            Task<string> myTask = Task<string>.Factory.StartNew(() =>
            {
                TokenCache.DefaultShared.ReadItems();
                List<KeyValuePair<TokenCacheKey, AuthenticationResultEx>> CacheItems = QueryCache(input["authority"],
                    input["client_id"], input["user_identifier"]);

                foreach (KeyValuePair<TokenCacheKey, AuthenticationResultEx> item in CacheItems)
                {
                    // if resource was passed to cache lookup, then only expire token for that resource.
                    // otherwise expire all matching access tokens.
                    if (input["resource"] == null || item.Key.ResourceEquals(input["resource"]))
                    {
                        var updated = item;
                        updated.Value.Result.ExpiresOn = DateTime.UtcNow;
                        UpdateCache(item, updated);
                    }
                }
                Dictionary<string, object> output = new Dictionary<string, object>();
                //Send back error if userId or displayableId is not sent back to the user
                output.Add("expired_access_token_count", CacheItems.Count.ToString());
                return output.FromDictionaryToJson();
                             
            });

            return await myTask.ConfigureAwait(false);
        }

        public static async Task<string> InvalidateRefreshToken(Dictionary<string, string> input)
        {
            Dictionary<string, object> output = new Dictionary<string, object>();
            Task<string> myTask = Task<string>.Factory.StartNew(() =>
            {
                try
                {
                    TokenCache.DefaultShared.ReadItems();
                    List<KeyValuePair<TokenCacheKey, AuthenticationResultEx>> CacheItems = QueryCache(input["authority"],
                    input["client_id"], input["user_identifier"]);

                    foreach (KeyValuePair<TokenCacheKey, AuthenticationResultEx> item in CacheItems)
                    {
                        var updated = item;
                        updated.Value.RefreshToken = "bad_refresh_token";
                        updated.Value.Result.ExpiresOn = DateTime.UtcNow;
                        UpdateCache(item, updated);
                    }
                    //Send back error if userId or displayableId is not sent back to the user
                    output.Add("invalidated_refresh_token_count", CacheItems.Count.ToString());                   
                }
                catch (Exception exc)
                {
                    output.Add("error", exc.Message); 
                }
                return output.FromDictionaryToJson();
            });

            return await myTask.ConfigureAwait(false);
        }

        public static async Task<string> ReadCache()
        {
            Task<string> myTask = Task<string>.Factory.StartNew(() =>
            {

                Dictionary<string, object> output = new Dictionary<string, object>();
                TokenCache.DefaultShared.ReadItems();
                var list = TokenCache.DefaultShared.tokenCacheDictionary;
                
                if (list.Any())
                {
                    output.Add("Count", list.Count());
                    var item = list.FirstOrDefault();
                    output.Add("AccessToken", item.Value.Result.AccessToken);
                    output.Add("expires_on", item.Value.Result.ExpiresOn);
                    output.Add("refresh_token", item.Value.RefreshToken);
                }
              
                return FromDictionaryToJson(output);
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
            Dictionary<string, object> res = new Dictionary<string, object>();
            IWebDriver driver = null;
            AuthenticationContext ctx = new AuthenticationContext(input["authority"]);

            try
            {
                //Fetch values from input and create DeviceCodeResult object
                DeviceCodeResult deviceCodeResult = new DeviceCodeResult();
                deviceCodeResult.VerificationUrl = input["verification_url"];
                deviceCodeResult.UserCode = input["user_code"];
                deviceCodeResult.DeviceCode = input["device_code"];
                deviceCodeResult.ClientId = input["client_id"];
                deviceCodeResult.Resource = input["resource"];
                deviceCodeResult.ExpiresOn = Convert.ToDateTime(input["expires_on"]);

                //Try to get access token form given device code.
                AuthenticationResult result = await ctx.AcquireTokenByDeviceCodeAsync(deviceCodeResult);
                res.Add("unique_id", result.UserInfo.UniqueId);
                res.Add("access_token", result.AccessToken);
                res.Add("tenant_id", result.TenantId);               
            }
            catch (Exception exc)
            {
                res.Add("error", exc.Message);
            }
            finally
            {
               if(driver != null) driver.Quit();
            }
            return FromDictionaryToJson(res);
        }

        public static async Task<string> AcquireDeviceCode(Dictionary<string, string> input)
        {
            Dictionary<string, object> res = new Dictionary<string, object>();
            AuthenticationContext ctx = new AuthenticationContext(input["authority"]);
            DeviceCodeResult result;
            try
            {
                result = await ctx.AcquireDeviceCodeAsync(input["resource"], input["client_id"]);
                res.Add("device_code", result.DeviceCode);
                res.Add("verification_url", result.VerificationUrl);
                res.Add("user_code", result.UserCode);
                res.Add("client_id", result.ClientId);
                res.Add("resource", result.Resource);
                res.Add("expires_on", result.ExpiresOn.DateTime);
            }
            catch (Exception exc)
            {
                res.Add("error", exc.Message);
            }
            return FromDictionaryToJson(res);
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

        #endregion

        #region Private Methods
        // This function clears cookies from the browser control used by ADAL.
        public static void ClearCookies()
        {
            const int INTERNET_OPTION_END_BROWSER_SESSION = 42;
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_END_BROWSER_SESSION, IntPtr.Zero, 0);
        }

        [DllImport("wininet.dll", SetLastError = true)]
        private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int lpdwBufferLength);

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
                pb = (PromptBehavior)Enum.Parse(typeof(PromptBehavior), promptBehaviorString, true);
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

        private static string FromDictionaryToJson(this Dictionary<string, object> dictionary)
        {
            var jss = new JavaScriptSerializer();
            return jss.Serialize(dictionary);
        }

        private static Dictionary<string, object> ProcessResult(AuthenticationResult result, Dictionary<string, string> input)
        {
            Dictionary<string, object> res = new Dictionary<string, object>();
            res.Add("unique_id", result.UserInfo.UniqueId);
            res.Add("access_token", result.AccessToken);
            res.Add("tenant_id", result.TenantId);
            res.Add("refresh_token", TokenCache.DefaultShared.tokenCacheDictionary.Where(x => x.Key.UniqueId == result.UserInfo.UniqueId).FirstOrDefault().Value.RefreshToken);
            return res;
        }

        private static void NotifyBeforeAccessCache(string resource, string clientid, string uniqueid, string displayableid)
        {
            TokenCache.DefaultShared.OnBeforeAccess(new TokenCacheNotificationArgs
            {
                TokenCache = TokenCache.DefaultShared,
                Resource = resource,
                ClientId = clientid,
                UniqueId = uniqueid,
                DisplayableId = displayableid
            });
        }

        private static void NotifyAfterAccessCache(string resource, string clientid, string uniqueid, string displayableid)
        {
            TokenCache.DefaultShared.OnAfterAccess(new TokenCacheNotificationArgs
            {
                TokenCache = TokenCache.DefaultShared,
                Resource = resource,
                ClientId = clientid,
                UniqueId = uniqueid,
                DisplayableId = displayableid
            });
        }

        private static void UpdateCache(KeyValuePair<TokenCacheKey, AuthenticationResultEx> item, KeyValuePair<TokenCacheKey, AuthenticationResultEx> updated)
        {
            NotifyBeforeAccessCache(item.Key.Resource, item.Key.ClientId, item.Value.Result.UserInfo.UniqueId, item.Value.Result.UserInfo.DisplayableId);
            TokenCache.DefaultShared.tokenCacheDictionary[updated.Key] = updated.Value;
            TokenCache.DefaultShared.StoreToCache(updated.Value, updated.Key.Authority, updated.Key.Resource, updated.Key.ClientId, updated.Key.TokenSubjectType, new CallState(new Guid()));
            NotifyAfterAccessCache(updated.Key.Resource, updated.Key.ClientId, updated.Value.Result.UserInfo.UniqueId, updated.Value.Result.UserInfo.DisplayableId);
        }

        private static List<KeyValuePair<TokenCacheKey, AuthenticationResultEx>> QueryCache(string authority,
            string clientId, string displayableId)
        {
            return TokenCache.DefaultShared.tokenCacheDictionary.Where(
                p =>
                    (string.IsNullOrWhiteSpace(authority) || p.Key.Authority == authority)
                    && (string.IsNullOrWhiteSpace(clientId) || p.Key.ClientIdEquals(clientId))
                    && (string.IsNullOrWhiteSpace(displayableId) || p.Key.DisplayableIdEquals(displayableId))).ToList();
        }
        #endregion

    }    
}
