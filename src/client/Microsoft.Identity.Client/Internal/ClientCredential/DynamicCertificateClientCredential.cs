// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Client credential that resolves certificates dynamically at runtime via a provider delegate.
    /// Used when certificates need to be rotated or selected based on runtime conditions.
    /// </summary>
    internal class DynamicCertificateClientCredential : CertificateAndClaimsClientCredential
    {
        public DynamicCertificateClientCredential(
            Func<AssertionRequestOptions, Task<X509Certificate2>> certificateProvider) 
            : base(
                certificateProvider: certificateProvider, 
                claimsToSign: null, 
                appendDefaultClaims: true) 
        { 
        }
    }
}
