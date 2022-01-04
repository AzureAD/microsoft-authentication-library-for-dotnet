// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.ApiConfig
{
    /// <summary>
    /// Allows the application to create their own client assertion parameters instead of relying on MSAL to create one from the 
    /// certificate specified via WithCertificate.
    /// 
    /// This is an advanced API. See https://docs.microsoft.com/en-gb/azure/active-directory/develop/msal-net-client-assertions for 
    /// the common use cases such as using a certificate.
    /// </summary>
    public interface IClientAssertionProvider
    {
        /// <summary>
        /// Returns a list of parameters that form the assertion.
        /// </summary>
        /// <param name="clientId">The client ID associated with the request.</param>
        /// <param name="tokenEndpoint">The token endpoint that will issue the token.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>a list of key value pairs that will be added to the token HTTP request.</returns>
        /// <remarks>
        /// For the JWT-Bearer assertion format see https://docs.microsoft.com/en-gb/azure/active-directory/develop/active-directory-certificate-credentials#assertion-format
        /// </remarks>
       Task<IReadOnlyList<KeyValuePair<string, string>>> GetClientAssertionParametersAsync(
            string clientId,
            string tokenEndpoint,
            CancellationToken cancellationToken);
    }
}
