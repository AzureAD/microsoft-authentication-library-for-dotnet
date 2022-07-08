// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Shared;

namespace Microsoft.Identity.Client.Platforms.netcore
{
    internal class NetCoreDeviceAuthManager : DeviceAuthManager
    {
        protected override DeviceAuthJWTResponse GetDeviceAuthJwtResponse(string submitUrl, string nonce, X509Certificate2 certificate)
        {
            return new DeviceAuthJWTResponse(submitUrl, nonce, Convert.ToBase64String(certificate.GetRawCertData()));
        }

        protected override byte[] SignWithCertificate(DeviceAuthJWTResponse responseJwt, X509Certificate2 certificate)
        {
            return new CommonCryptographyManager().SignWithCertificate(responseJwt.GetResponseToSign(), certificate);
        }
    }
}
