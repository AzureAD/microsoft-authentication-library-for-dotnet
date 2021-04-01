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
            wwwAuthenticate = wwwAuthenticate.Substring(PKeyAuthConstants.PKeyAuthName.Length + 1);
            if (string.IsNullOrEmpty(wwwAuthenticate))
            {
                return data;
            }

            List<string> headerPairs = CoreHelpers.SplitWithQuotes(wwwAuthenticate, ',');
            foreach (string pair in headerPairs)
            {
                List<string> keyValue = CoreHelpers.SplitWithQuotes(pair, '=');
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
                   && responseHeaders != null
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
#if NET_CORE || NET5_WIN || DESKTOP || NETSTANDARD
            return !Platforms.Features.DesktopOs.DesktopOsHelper.IsWin10OrServerEquivalent();
#else
            return false;
#endif
        }

        /// <summary>
        /// Formats PKeyAuth header
        /// </summary>
        public static void FormatResponseHeader(
            DeviceAuthJWTResponse responseJWT,
            byte[] signedResponse,
            IDictionary<string, string> challengeData,
            out string responseHeader)
        {
            string authHeaderTemplate = "PKeyAuth {0}, Context=\"{1}\", Version=\"{2}\"";

            string signedJwt = string.Format(CultureInfo.InvariantCulture, "{0}.{1}", responseJWT.GetResponseToSign(), Base64UrlHelpers.Encode(signedResponse));
            string authToken = string.Format(CultureInfo.InvariantCulture, " AuthToken=\"{0}\"", signedJwt);

            responseHeader = string.Format(CultureInfo.InvariantCulture, authHeaderTemplate, authToken,
                challengeData["Context"],
                challengeData["Version"]);
        }

        public static X509Certificate2 FindCertificate(IDictionary<string, string> challengeData)
        {
            var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            try
            {
                store.Open(OpenFlags.ReadOnly);
                var certCollection = store.Certificates;
                if (challengeData.ContainsKey("CertAuthorities"))
                {
                    return FindCertificateByCertAuthorities(challengeData, certCollection);
                }

                X509Certificate2Collection signingCert = null;
                signingCert = certCollection.Find(X509FindType.FindByThumbprint, challengeData["CertThumbprint"],
                    false);
                if (signingCert.Count == 0)
                {
                    throw new MsalException(MsalError.DeviceCertificateNotFound,
                        string.Format(CultureInfo.CurrentCulture, MsalErrorMessage.DeviceCertificateNotFoundTemplate,
                            "Cert thumbprint:" + challengeData["CertThumbprint"]));
                }

                return signingCert[0];
            }
            finally
            {

#if NETSTANDARD || WINDOWS_APP
                store.Dispose();
#else
                store.Close();
#endif
            }
        }

        private static X509Certificate2 FindCertificateByCertAuthorities(IDictionary<string, string> challengeData, X509Certificate2Collection certCollection)
        {
            X509Certificate2Collection signingCert = null;
            string[] certAuthorities = challengeData["CertAuthorities"].Split(new[] { ";" },
                StringSplitOptions.None);
            foreach (var certAuthority in certAuthorities)
            {
                //reverse the tokenized string and replace "," with " + "
                string[] dNames = certAuthority.Split(new[] { "," }, StringSplitOptions.None);
                string distinguishedIssuerName = dNames[dNames.Length - 1];
                for (int i = dNames.Length - 2; i >= 0; i--)
                {
                    distinguishedIssuerName += " + " + dNames[i].Trim();
                }

                signingCert = certCollection.Find(X509FindType.FindByIssuerDistinguishedName,
                    distinguishedIssuerName, false);
                if (signingCert.Count > 0)
                {
                    break;
                }
            }

            if (signingCert == null || signingCert.Count == 0)
            {
                throw new MsalException(MsalError.DeviceCertificateNotFound,
                    string.Format(CultureInfo.CurrentCulture,
                        MsalErrorMessage.DeviceCertificateNotFoundTemplate,
                        "Cert Authorities:" + challengeData["CertAuthorities"]));
            }

            return signingCert[0];
        }
    }
}
