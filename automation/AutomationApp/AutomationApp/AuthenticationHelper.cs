using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace AutomationApp
{
    internal static class AuthenticationHelper
    {
        public static async Task<string> AcquireToken(Dictionary<string, string> input)
        {
            return null;
        }

        public static async Task<string> AcquireTokenSilent(Dictionary<string, string> input)
        {
            AuthenticationContext ctx = new AuthenticationContext("");
            string output = string.Empty;
            try
            {
                AuthenticationResult result = await ctx.AcquireTokenSilentAsync("", "");
                output = result.ToJson();
            }
            catch (Exception exc)
            {
                output = exc.InnerException.Message;
            }

            return output;
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
                    return reader.ReadToEnd();
                }
            }
        }

        public static Dictionary<string, string> CreateDictionaryFromJson(string json)
        {
            DataContractJsonSerializer dcjs = new DataContractJsonSerializer(typeof (Dictionary<string, string>));
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                return (Dictionary<string, string>) dcjs.ReadObject(ms);
            }
        }

        private static IPlatformParameters GetPlatformParametersInstance()
        {
            IPlatformParameters platformParameters = null;

#if __ANDROID__
        platformParameters = new PlatformParameters(this);
#else
#if __IOS__
        platformParameters = new PlatformParameters(this);
#else
#if (WINDOWS_UWP || WINDOWS_APP)
            platformParameters = new PlatformParameters(PromptBehavior.Always, false);
#endif
#endif
#endif
            return platformParameters;
        }
    }
}
