// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using System.Globalization;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Platforms.netstandard13
{
    class NetStandardDeviceAuthManager : IDeviceAuthManager
    {
        public bool TryCreateDeviceAuthChallengeResponseAsync(HttpResponseHeaders responseHeaders, Uri endpointUri, out string responseHeader)
        {
            responseHeader = string.Empty;
            string authHeaderTemplate = "PKeyAuth {0}, Context=\"{1}\", Version=\"{2}\"";
            X509Certificate2 certificate = null;

            if (!DeviceAuthHelper.IsDeviceAuthChallenge(responseHeaders))
            {
                return false;
            }
            if (!DeviceAuthHelper.CanOSPerformPKeyAuth())
            {
                responseHeader = DeviceAuthHelper.GetBypassChallengeResponse(responseHeaders);
                return true;
            }

            IDictionary<string, string> challengeData = DeviceAuthHelper.ParseChallengeData(responseHeaders);

            if (!challengeData.ContainsKey("SubmitUrl"))
            {
                challengeData["SubmitUrl"] = endpointUri.AbsoluteUri;
            }

            try
            {
                certificate = FindCertificate(challengeData);
            }
            catch (MsalException ex)
            {
                if (ex.ErrorCode == MsalError.DeviceCertificateNotFound)
                {
                    responseHeader = DeviceAuthHelper.GetBypassChallengeResponse(responseHeaders);
                    return true;
                }
            }

            DeviceAuthJWTResponse responseJWT = new DeviceAuthJWTResponse(challengeData["SubmitUrl"],
                challengeData["nonce"], Convert.ToBase64String(certificate.RawData));

            byte[] signedResponse = new NetStandard13CryptographyManager().SignWithCertificate(responseJWT.GetResponseToSign(), certificate);

            string signedJwt = string.Format(CultureInfo.InvariantCulture, "{0}.{1}", responseJWT.GetResponseToSign(),
                Base64UrlHelpers.Encode(signedResponse));
            string authToken = string.Format(CultureInfo.InvariantCulture, " AuthToken=\"{0}\"", signedJwt);

            responseHeader = string.Format(CultureInfo.InvariantCulture, authHeaderTemplate, authToken,
                challengeData["Context"],
                challengeData["Version"]);

            return true;
        }

        private static X509Certificate2 FindCertificate(IDictionary<string, string> challengeData)
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
                store.Dispose();
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
