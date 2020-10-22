// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Identity.Client.AuthScheme.PoP;

namespace Microsoft.Identity.Client.AppConfig
{
#if DESKTOP || NET_CORE
    /// <summary>
    /// Configuration properties used to construct a proof of possesion request.
    /// </summary>
    public class PopAuthenticationConfiguration
    {
        /// <summary>
        /// An HTTP uri to the protected resource which requires a PoP token. The PoP token will be cryptographically bound to the request.
        /// </summary>
        public Uri RequestUri { get; }

        /// <summary>
        /// The HTTP method ("GET", "POST" etc.) method that will be bound to the token. Leave null and the POP token will not be bound to the method.
        /// </summary>
        public HttpMethod HttpMethod { get; set; }

        /// <summary>
        ///An extensibility point that allows developers to define their own key management. 
        ///Leave null and MSAL will use a default implementation, which generates an RSA key pair in memory and refreshes it every 8 hours.
        ///Important note: developers wanting to change the key (e.g. rotate the key) should create a new instance of this object,
        ///as MSAL will keep a thumbprint of keys in memory
        /// </summary>
        public IPoPCryptoProvider PopCryptoProvider { get; set; }

        /// <summary>
        /// This will be populated with the POP authentication header.
        /// </summary>
        public AuthenticationHeaderValue PopAuthenticationRequestHeader {get; private set;}

        /// <summary>
        /// Constructs the configuration properties used to construct a proof of possesion request
        /// </summary>
        /// <param name="requestUri"></param>
        public PopAuthenticationConfiguration(Uri requestUri)
        {
            RequestUri = requestUri ?? throw new ArgumentNullException(nameof(requestUri));
        }

        internal void SetPopHttpRequestHeaders(AuthenticationHeaderValue header)
        {
            PopAuthenticationRequestHeader = header;
        }
    }
#endif
}
