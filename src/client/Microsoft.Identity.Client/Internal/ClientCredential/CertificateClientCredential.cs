// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    internal class CertificateClientCredential : CertificateAndClaimsClientCredential
    {
        public static int MinKeySizeInBits { get; } = 2048;

        public CertificateClientCredential(X509Certificate2 certificate) : base(certificate, null, true) 
        { 

        }
    }
}
