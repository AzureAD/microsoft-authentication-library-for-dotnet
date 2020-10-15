// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using Microsoft.Identity.Client.AuthScheme.PoP;

namespace Microsoft.Identity.Client.AppConfig
{
    /// <summary>
    /// Configuration properties used to construct a Proof of possesion request
    /// </summary>
    public class PopAuthenticationConfiguration
    {
        /// <summary>
        /// An HTTP uri to the protected resource which requires a PoP token. The PoP token will be cryptographically bound to the request.
        /// </summary>
        public Uri RequestUri { get; set; }

        /// <summary>
        /// The HttP method to be bound to the request
        /// </summary>
        public HttpMethod PopHttpMethod { get; set; }

        /// <summary>
        /// A provider that can handle the asymmetric key operations needed by POP, that encapsulates a pair of public and private keys
        /// and some typical crypto operations
        /// </summary>
        public IPoPCryptoProvider PopCryptoProvider { get; set; }

        /// <summary>
        /// Constructs the configuration properties used to construct a proof of possesion request
        /// </summary>
        /// <param name="requestUri"></param>
        public PopAuthenticationConfiguration(Uri requestUri)
        {
            if (requestUri == null || string.IsNullOrEmpty(requestUri.AbsoluteUri))
            {
                throw new ArgumentNullException(nameof(requestUri));
            }

            RequestUri = requestUri;
        }
    }
}
