using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.PlatformsCommon
{
    internal class DeviceAuthHelper
    {
        public static IDictionary<string, string> ParseChallengeData(HttpResponse response)
        {
            IDictionary<string, string> data = new Dictionary<string, string>();
            string wwwAuthenticate = response.Headers.GetValues(PKeyAuthConstants.WwwAuthenticateHeader).Single();
            wwwAuthenticate = wwwAuthenticate.Substring(PKeyAuthConstants.PKeyAuthName.Length + 1);
            List<string> headerPairs = CoreHelpers.SplitWithQuotes(wwwAuthenticate, ',');
            foreach (string pair in headerPairs)
            {
                List<string> keyValue = CoreHelpers.SplitWithQuotes(pair, '=');
                data.Add(keyValue[0].Trim(), keyValue[1].Trim().Replace("\"", ""));
            }

            return data;
        }

        public static bool IsDeviceAuthChallenge(HttpResponse response)
        {
            return response != null
                   && response.Headers != null
                   && response.StatusCode == HttpStatusCode.Unauthorized
                   && response.Headers.Contains(PKeyAuthConstants.WwwAuthenticateHeader)
                   && response.Headers.GetValues(PKeyAuthConstants.WwwAuthenticateHeader).First()
                       .StartsWith(PKeyAuthConstants.PKeyAuthName, StringComparison.OrdinalIgnoreCase);
        }

        public static string GetBypassChallengeResponse(HttpResponse response)
        {
            var challengeData = DeviceAuthHelper.ParseChallengeData(response);
            return string.Format(CultureInfo.InvariantCulture,
                                   @"PKeyAuth Context=""{0}"",Version=""{1}""",
                                   challengeData[PKeyAuthConstants.ChallengeResponseContext],
                                   challengeData[PKeyAuthConstants.ChallengeResponseVersion]);
        }
    }
}
