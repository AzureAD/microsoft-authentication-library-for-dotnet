// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using Microsoft.Identity.Client.AuthScheme.PoP;

namespace Microsoft.Identity.Client.AppConfig
{

    /// <summary>
    /// Details about the HTTP request and configuration properties used to construct a proof of possession request.
    /// </summary>
    /// <remarks> 
    /// POP tokens are signed by the process making the request. By default, MSAL will generate a key in memory.
    /// To use a hardware key or an external key, implement <see cref="PopCryptoProvider"/>.
    /// </remarks>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
    public class PoPAuthenticationConfiguration
    {
        /// <summary>
        /// Creates a configuration using the default key management - an RSA key will be created in memory and rotated every 8h.
        /// Uses <see cref="HttpMethod"/>, <see cref="HttpHost"/> etc. to control which elements of the request should be included in the POP token.
        /// </summary>
        /// <remarks>
        /// See https://datatracker.ietf.org/doc/html/draft-ietf-oauth-signed-http-request-03#page-3 for details about signed HTTP requests.
        /// </remarks>
        public PoPAuthenticationConfiguration()
        {
            ClientApplicationBase.GuardMobileFrameworks();
        }

        /// <summary>
        /// Creates a configuration using the default key management, and which binds all the details of the HttpRequestMessage.
        /// </summary>
        /// <remarks>
        /// Currently only the HttpMethod (m), UrlHost (u) and UrlPath (p) are used to create the signed HTTP request - see https://datatracker.ietf.org/doc/html/draft-ietf-oauth-signed-http-request-03#page-3
        /// </remarks>
        public PoPAuthenticationConfiguration(HttpRequestMessage httpRequestMessage)
        {
            if (httpRequestMessage == null)
                throw new ArgumentNullException(nameof(httpRequestMessage));

            HttpMethod = httpRequestMessage.Method;
            HttpHost = httpRequestMessage.RequestUri.Authority; // this includes the optional port
            HttpPath = httpRequestMessage.RequestUri.AbsolutePath;
        }

        /// <summary>
        /// Creates a configuration using the default key management, and which binds only the Uri part of the HTTP request.
        /// </summary>
        /// <remarks>
        /// The UrlHost (u) and UrlPath (p) are used to create the signed HTTP request - see https://datatracker.ietf.org/doc/html/draft-ietf-oauth-signed-http-request-03#page-3
        /// </remarks>
        public PoPAuthenticationConfiguration(Uri requestUri)
        {
            if (requestUri == null)
                throw new ArgumentNullException(nameof(requestUri));

            HttpHost = requestUri.Authority;
            HttpPath = requestUri.AbsolutePath;
        }

        /// <summary>
        /// The HTTP method ("GET", "POST" etc.) method that will be bound to the token. Leave null and the POP token will not be bound to the method.
        /// Corresponds to the "m" part of the a signed HTTP request. Optional.
        /// </summary>
        /// <remarks>
        /// See https://datatracker.ietf.org/doc/html/draft-ietf-oauth-signed-http-request-03#section-3
        /// </remarks>
        public HttpMethod HttpMethod { get; set; }

        /// <summary>
        /// The URL host of the protected API. The "u" part of a signed HTTP request.  This MAY include the port separated from the host by a colon in host:port format. Optional.
        /// </summary>
        public string HttpHost { get; set; }

        /// <summary>
        /// The "p" part of the signed HTTP request. 
        /// </summary>
        public string HttpPath { get; set; }

        /// <summary>
        /// An extensibility point that allows developers to define their own key management. 
        /// Leave <c>null</c> and MSAL will use a default implementation, which generates an RSA key pair in memory and refreshes it every 8 hours.
        /// Important note: if you want to change the key (e.g. rotate the key), you should create a new instance of this object,
        /// as MSAL.NET will keep a thumbprint of keys in memory.
        /// </summary>
        public IPoPCryptoProvider PopCryptoProvider { get; set; }

        /// <summary>
        /// If the protected resource (RP) requires use of a special nonce, they will publish it as part of the WWWAuthenticate header associated with a 401 HTTP response
        /// or as part of the AuthorityInfo header associated with 200 response. Set it here to make it part of the Signed HTTP Request part of the POP token.
        /// </summary>
        public string Nonce { get; set; }

        /// <summary>
        /// Allows app developers to bypass the creation of the SignedHttpRequest envelope by setting this property to false.
        /// App developers can use a package like Microsoft.IdentityModel.Protocols.SignedHttpRequest to later create and sign the envelope. 
        /// </summary>
        /// <remarks>
        /// If set to false, you do not need to implement the <see cref="IPoPCryptoProvider.Sign(byte[])"/> method when using custom keys. 
        /// </remarks>
        public bool SignHttpRequest { get; set; } = true;
    }
}
