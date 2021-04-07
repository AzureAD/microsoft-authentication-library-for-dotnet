// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Platforms.net45
{
    internal class NetDesktopDeviceAuthManager : DeviceAuthManager
    {
        protected override DeviceAuthJWTResponse GetDeviceAuthJwtResponse(string submitUrl, string nonce, X509Certificate2 certificate)
        {
            return new DeviceAuthJWTResponse(submitUrl, nonce, Convert.ToBase64String(certificate.GetRawCertData()));
        }

        protected override byte[] SignWithCertificate(DeviceAuthJWTResponse responseJwt, X509Certificate2 certificate)
        {
            CngKey key = NetDesktopCryptographyManager.GetCngPrivateKey(certificate);
            byte[] signedData = null;
            using (Native.RSACng rsa = new Native.RSACng(key))
            {
                rsa.SignatureHashAlgorithm = CngAlgorithm.Sha256;
                signedData = rsa.SignData(responseJwt.GetResponseToSign().ToByteArray());
            }

            return signedData;
        }
    }
}
