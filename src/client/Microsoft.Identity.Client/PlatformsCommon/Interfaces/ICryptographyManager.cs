﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client.PlatformsCommon.Interfaces
{
    internal interface ICryptographyManager
    {
        string CreateBase64UrlEncodedSha256Hash(string input);
        string GenerateCodeVerifier();
        string CreateSha256Hash(string input);
        string CreateSha256HashHex(string input);
        byte[] CreateSha256HashBytes(string input);
        byte[] SignWithCertificate(string message, X509Certificate2 certificate, RSASignaturePadding signaturePadding);
    }
}
