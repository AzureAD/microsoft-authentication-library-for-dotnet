// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.AuthScheme.SSHCertificates;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;

namespace Microsoft.Identity.Client.SSHCertificates
{
    /// <summary>
    /// Extensions that add support for SSH certificates
    /// </summary>
    public static class SSHExtensionsPublic
    {
        /// <summary>
        /// Instructs AAD to return an SSH certificate instead of a Bearer token. The SSH certificate 
        /// (not the same as public / private key pair used by SSH), can be used to securely deploy 
        /// a public SSH key to a machine. See https://aka.ms/msal-net-ssh for details.
        /// </summary>
        /// <param name="builder">Interactive authentication builder</param>
        /// <param name="publicKeyJwk">The public SSH key in JWK format (https://tools.ietf.org/html/rfc7517). 
        /// Currently only RSA is supported, and the JWK should contain only the RSA modulus and exponent</param>
        /// <param name="keyId">A key identifier, it can be in any format. Used to distinguish between 
        /// different keys when fetching an SSH certificate from the token cache.</param>
        public static AcquireTokenInteractiveParameterBuilder WithSSHCertificateAuthenticationScheme(
            this AcquireTokenInteractiveParameterBuilder builder,
            string publicKeyJwk,
            string keyId)
        {
            builder.CommonParameters.AuthenticationScheme = new SSHCertAuthenticationScheme(keyId, publicKeyJwk);
            return builder;
        }
    }
}
