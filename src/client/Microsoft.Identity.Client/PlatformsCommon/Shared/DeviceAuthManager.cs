// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.PlatformsCommon.Shared
{
    internal class DeviceAuthManager : IDeviceAuthManager
    {
        private readonly ICryptographyManager _cryptographyManager;

        public DeviceAuthManager(ICryptographyManager cryptographyManager)
        {
            _cryptographyManager = cryptographyManager;
        }

        public bool TryCreateDeviceAuthChallengeResponse(HttpResponseHeaders responseHeaders, Uri endpointUri, out string responseHeader)
        {
            responseHeader = string.Empty;
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

            if (!challengeData.TryGetValue("SubmitUrl", out string submitUrl))
            {
                submitUrl = endpointUri.AbsoluteUri;
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

            DeviceAuthJWTResponse responseJWT = GetDeviceAuthJwtResponse(submitUrl, challengeData["nonce"], certificate);

            byte[] signedResponse = SignWithCertificate(responseJWT, certificate);

            FormatResponseHeader(responseJWT, signedResponse, challengeData, out responseHeader);

            return true;
        }

        private DeviceAuthJWTResponse GetDeviceAuthJwtResponse(string submitUrl, string nonce, X509Certificate2 certificate)
        {
            return new DeviceAuthJWTResponse(submitUrl, nonce, Convert.ToBase64String(certificate.GetRawCertData()));
        }

        private byte[] SignWithCertificate(DeviceAuthJWTResponse responseJwt, X509Certificate2 certificate)
        {
            return _cryptographyManager.SignWithCertificate(responseJwt.GetResponseToSign(), certificate);
        }

        private void FormatResponseHeader(
            DeviceAuthJWTResponse responseJWT,
            byte[] signedResponse,
            IDictionary<string, string> challengeData,
            out string responseHeader)
        {
            string signedJwt = $"{responseJWT.GetResponseToSign()}.{Base64UrlHelpers.Encode(signedResponse)}";
            string authToken = $"AuthToken=\"{signedJwt}\"";

            responseHeader = $"PKeyAuth {authToken}, Context=\"{challengeData["Context"]}\", Version=\"{challengeData["Version"]}\"";
        }

        private X509Certificate2 FindCertificate(IDictionary<string, string> challengeData)
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

        private X509Certificate2 FindCertificateByCertAuthorities(IDictionary<string, string> challengeData, X509Certificate2Collection certCollection)
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
