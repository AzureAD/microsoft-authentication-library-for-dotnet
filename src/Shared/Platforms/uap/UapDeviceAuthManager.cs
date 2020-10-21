// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#if WINDOWS_APP
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.Utils;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Certificates;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;

namespace Microsoft.Identity.Client.Platforms.uap
{
    internal class UapDeviceAuthManager : IDeviceAuthManager
    {
        public bool CanHandleDeviceAuthChallenge
        {
            get { return true; }
        }

        public bool TryCreateDeviceAuthChallengeResponseAsync(HttpResponseHeaders headers, Uri endpointUri, out string responseHeader)
        {
            responseHeader = string.Empty;
            Certificate certificate = null;
            string authHeaderTemplate = "PKeyAuth {0}, Context=\"{1}\", Version=\"{2}\"";

            if (!DeviceAuthHelper.IsDeviceAuthChallenge(headers))
            {
                return false;
            }
            if (!DeviceAuthHelper.CanOSPerformPKeyAuth())
            {
                responseHeader = DeviceAuthHelper.GetBypassChallengeResponse(headers);
                return false;
            }

            IDictionary<string, string> challengeData = DeviceAuthHelper.ParseChallengeData(headers);

            if (!challengeData.ContainsKey("SubmitUrl"))
            {
                challengeData["SubmitUrl"] = endpointUri.AbsoluteUri;
            }

            try
            {
                certificate = Task.FromResult(FindCertificateAsync(challengeData)).Result.Result;
            }
            catch (MsalException ex)
            {
                if (ex.ErrorCode == MsalError.DeviceCertificateNotFound)
                {
                    responseHeader = DeviceAuthHelper.GetBypassChallengeResponse(headers);
                    return true;
                }
            }

            DeviceAuthJWTResponse responseJWT = new DeviceAuthJWTResponse(challengeData["SubmitUrl"],
                challengeData["nonce"], Convert.ToBase64String(certificate.GetCertificateBlob().ToArray()));
            IBuffer input = CryptographicBuffer.ConvertStringToBinary(responseJWT.GetResponseToSign(),
                BinaryStringEncoding.Utf8);
            CryptographicKey keyPair =
                Task.FromResult(PersistedKeyProvider.OpenKeyPairFromCertificateAsync(certificate, HashAlgorithmNames.Sha256, CryptographicPadding.RsaPkcs1V15)).Result.GetResults();

            IBuffer signed = Task.FromResult(CryptographicEngine.SignAsync(keyPair, input)).Result.GetResults();

            string signedJwt = string.Format(CultureInfo.InvariantCulture, "{0}.{1}", responseJWT.GetResponseToSign(),
                Base64UrlHelpers.Encode(signed.ToArray()));
            string authToken = string.Format(CultureInfo.InvariantCulture, " AuthToken=\"{0}\"", signedJwt);
            responseHeader = string.Format(CultureInfo.InvariantCulture, authHeaderTemplate, authToken, challengeData["Context"], challengeData["Version"]);
            return true;
        }

        private async Task<Certificate> FindCertificateAsync(IDictionary<string, string> challengeData)
        {
            CertificateQuery query = new CertificateQuery();
            IReadOnlyList<Certificate> certificates = null;
            string errMessage = null;

            if (challengeData.ContainsKey("CertAuthorities"))
            {
                errMessage = "Cert Authorities:" + challengeData["CertAuthorities"];
                string[] certAuthorities = challengeData["CertAuthorities"].Split(';');
                foreach (var certAuthority in certAuthorities)
                {
                    //reverse the tokenized string and replace "," with " + "
                    string[] dNames = certAuthority.Split(new[] { "," }, StringSplitOptions.None);
                    string distinguishedIssuerName = dNames[dNames.Length - 1];
                    for (int i = dNames.Length - 2; i >= 0; i--)
                    {
                        distinguishedIssuerName += " + " + dNames[i].Trim();
                    }

                    query.IssuerName = distinguishedIssuerName;
                    certificates = await CertificateStores.FindAllAsync(query).AsTask().ConfigureAwait(false);
                    if (certificates.Count > 0)
                    {
                        break;
                    }
                }
            }
            else
            {
                errMessage = "Cert Thumbprint:" + challengeData["CertThumbprint"];
                query.Thumbprint = HexStringToByteArray(challengeData["CertThumbprint"]);
                certificates = await CertificateStores.FindAllAsync(query).AsTask().ConfigureAwait(false);
            }

            if (certificates == null || certificates.Count == 0)
            {
                throw new MsalException(MsalError.DeviceCertificateNotFound,
                    string.Format(CultureInfo.InvariantCulture, MsalErrorMessage.DeviceCertificateNotFoundTemplate, errMessage));
            }

            return certificates[0];
        }

        private static byte[] HexStringToByteArray(string hex)
        {
            if (hex.Length % 2 == 1)
            {
                throw new MsalException("The binary key cannot have an odd number of digits");
            }

            byte[] arr = new byte[hex.Length >> 1];

            for (int i = 0; i < (hex.Length >> 1); ++i)
            {
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
            }

            return arr;
        }

        private static int GetHexVal(char hex)
        {
            int val = (int)hex;
            return val - (val < 58 ? 48 : 55);
        }
    }
}
#endif
