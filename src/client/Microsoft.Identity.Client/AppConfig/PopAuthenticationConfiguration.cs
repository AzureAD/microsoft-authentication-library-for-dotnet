// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Identity.Client.AuthScheme.PoP;

namespace Microsoft.Identity.Client.AppConfig
{

    /// <summary>
    /// Configuration properties used to construct a proof of possession request.
    /// </summary>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
    public class PoPAuthenticationConfiguration
    {
        /// <summary>
        /// Constructs the configuration properties used to construct a proof of possession request
        /// </summary>
        /// <param name="requestUri">An Uri to the protected resource which requires a PoP token. The PoP token will be cryptographically bound to the request uri</param>
        public PoPAuthenticationConfiguration(Uri requestUri)
        {
            ConfidentialClientApplication.GuardMobileFrameworks();
            RequestUri = requestUri ?? throw new ArgumentNullException(nameof(requestUri));
        }

        /// <summary>
        /// An Uri to the protected resource which requires a PoP token. The PoP token will be cryptographically bound to the request uri (to the host and query parts of it)
        /// </summary>
        public Uri RequestUri { get; }

        /// <summary>
        /// The HTTP method ("GET", "POST" etc.) method that will be bound to the token. Leave null and the POP token will not be bound to the method.
        /// </summary>
        public HttpMethod HttpMethod { get; set; }

        /// <summary>
        /// An extensibility point that allows developers to define their own key management. 
        /// Leave <c>null</c> and MSAL will use a default implementation, which generates an RSA key pair in memory and refreshes it every 8 hours.
        /// Important note: if you want to change the key (e.g. rotate the key), you should create a new instance of this object,
        /// as MSAL.NET will keep a thumbprint of keys in memory.
        /// </summary>
        public IPoPCryptoProvider PopCryptoProvider { get; set; }
    }
}
