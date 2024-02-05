// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.PlatformsCommon.Shared
{
    internal class DeviceAuthHelper
    {
        public static IDictionary<string, string> ParseChallengeData(HttpResponseHeaders responseHeaders)
        {
            IDictionary<string, string> data = new Dictionary<string, string>();
            string wwwAuthenticate = responseHeaders.GetValues(PKeyAuthConstants.WwwAuthenticateHeader).SingleOrDefault();
            wwwAuthenticate = wwwAuthenticate?.Substring(PKeyAuthConstants.PKeyAuthName.Length + 1);
            if (string.IsNullOrEmpty(wwwAuthenticate))
            {
                return data;
            }

            var headerPairs = CoreHelpers.SplitWithQuotes(wwwAuthenticate, ',');
            foreach (string pair in headerPairs)
            {
                var keyValue = CoreHelpers.SplitWithQuotes(pair, '=');
                if (keyValue.Count == 2)
                {
                    data.Add(keyValue[0].Trim(), keyValue[1].Trim().Replace("\"", ""));
                }
            }

            return data;
        }

        public static bool IsDeviceAuthChallenge(HttpResponseHeaders responseHeaders)
        {
            //For PKeyAuth, challenge headers returned from the STS will be case sensitive so a case sensitive check is used to determine
            //if the response is a PKeyAuth challenge.
            return responseHeaders != null
                   && responseHeaders.Contains(PKeyAuthConstants.WwwAuthenticateHeader)
                   && responseHeaders.GetValues(PKeyAuthConstants.WwwAuthenticateHeader).First()
                       .StartsWith(PKeyAuthConstants.PKeyAuthName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Constructs a bypass response to the PKeyAuth challenge on platforms where the challenge cannot be completed.
        /// </summary>
        public static string GetBypassChallengeResponse(HttpResponseHeaders responseHeaders)
        {
            var challengeData = ParseChallengeData(responseHeaders);
            return string.Format(CultureInfo.InvariantCulture,
                                   PKeyAuthConstants.PKeyAuthBypassReponseFormat,
                                   challengeData[PKeyAuthConstants.ChallengeResponseContext],
                                   challengeData[PKeyAuthConstants.ChallengeResponseVersion]);
        }

        /// <summary>
        /// Constructs a bypass response to the PKeyAuth challenge on platforms where the challenge cannot be completed.
        /// </summary>
        public static string GetBypassChallengeResponse(Dictionary<string, string> response)
        {
            return string.Format(CultureInfo.InvariantCulture,
                                   PKeyAuthConstants.PKeyAuthBypassReponseFormat,
                                   response[PKeyAuthConstants.ChallengeResponseContext],
                                   response[PKeyAuthConstants.ChallengeResponseVersion]);
        }

        //PKeyAuth can only be performed on operating systems with a major OS version of 6.
        //This corresponds to windows 7, 8, 8.1 and their server equivalents.       
        public static bool CanOSPerformPKeyAuth()
        {
#if NET_CORE || NET6_WIN || NETFRAMEWORK || NETSTANDARD
            try
            {
                // No support on Mac or Linux
                if (!DesktopOsHelper.IsWindows())
                    return false;

                return !DesktopOsHelper.IsWin10OrServerEquivalent();
            }
            catch (DllNotFoundException)
            {
                // When running mobile UI tests, NETSTANDARD flag is set (instead of the mobile flags), which results in above method throwing the exception.
                // Catching the exception and returning false, in this case.
                return false;
            }
#else
            return false;
#endif
        }
    }
}
