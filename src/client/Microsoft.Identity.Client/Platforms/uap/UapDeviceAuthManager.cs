// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.OAuth2;
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

        public async Task<string> CreateDeviceAuthChallengeResponseAsync(IDictionary<string, string> challengeData)
        {
            string authHeaderTemplate = "PKeyAuth {0}, Context=\"{1}\", Version=\"{2}\"";

            Certificate certificate = null;
            try
            {
                certificate = await FindCertificateAsync(challengeData).ConfigureAwait(false);
            }
            catch (MsalException ex)
            {
                if (ex.ErrorCode == MsalError.DeviceCertificateNotFound)
                {
                    return await Task.FromResult(string.Format(CultureInfo.InvariantCulture, @"PKeyAuth Context=""{0}"",Version=""{1}""", challengeData["Context"], challengeData["Version"])).ConfigureAwait(false);
                }
            }

            DeviceAuthJWTResponse response = new DeviceAuthJWTResponse(challengeData["SubmitUrl"],
                challengeData["nonce"], Convert.ToBase64String(certificate.GetCertificateBlob().ToArray()));
            IBuffer input = CryptographicBuffer.ConvertStringToBinary(response.GetResponseToSign(),
                BinaryStringEncoding.Utf8);
            CryptographicKey keyPair = await
                PersistedKeyProvider.OpenKeyPairFromCertificateAsync(certificate, HashAlgorithmNames.Sha256,
                    CryptographicPadding.RsaPkcs1V15).AsTask().ConfigureAwait(false);

            IBuffer signed = await CryptographicEngine.SignAsync(keyPair, input).AsTask().ConfigureAwait(false);

            string signedJwt = string.Format(CultureInfo.InvariantCulture, "{0}.{1}", response.GetResponseToSign(),
                Base64UrlHelpers.Encode(signed.ToArray()));
            string authToken = string.Format(CultureInfo.InvariantCulture, " AuthToken=\"{0}\"", signedJwt);
            return string.Format(CultureInfo.InvariantCulture, authHeaderTemplate, authToken, challengeData["Context"], challengeData["Version"]);
        }

        private static async Task<Certificate> FindCertificateAsync(IDictionary<string, string> challengeData)
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
