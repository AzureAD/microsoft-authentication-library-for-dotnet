// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    internal class CertificateClientCredential : CertificateAndClaimsClientCredential
    {
        public CertificateClientCredential(X509Certificate2 certificate) : base(certificate, null, true) 
        { 

        }
    }
}
