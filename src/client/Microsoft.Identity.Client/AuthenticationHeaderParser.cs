// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.PlatformsCommon.Factories;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Parsed authentication headers to retrieve header values from <see cref="HttpResponseHeaders"/>.
    /// </summary>
    public class AuthenticationHeaderParser
    {
        private static readonly Lazy<IMsalHttpClientFactory> _httpClientFactory = new(
                                                                          () => PlatformProxyFactory.CreatePlatformProxy(null).CreateDefaultHttpClientFactory());

        /// <summary>
        /// Parameters returned by the <c>WWW-Authenticate</c> header. This allows for dynamic
        /// scenarios such as Claims challenge, Continuous Access Evaluation (CAE), and Conditional Access auth context.
        /// See <see href="https://aka.ms/msal-net/wwwAuthenticate">our documentation</see> for additional details.
        /// </summary>
        public IReadOnlyList<WwwAuthenticateParameters> WwwAuthenticateParameters { get; private set; }

        /// <summary>
        /// Parameters returned by the <c>Authentication-Info</c> header. 
        /// This allows for more complex authentication scenarios, such as Proof-Of-Possession.
        /// </summary>
        public AuthenticationInfoParameters AuthenticationInfoParameters { get; private set; }

        /// <summary>
        /// Nonce parsed from <see cref="HttpResponseHeaders"/>. This is acquired from the Proof-of-Possession <c>WWW-Authenticate</c> header or the <c>Authentication-Info</c> header
        /// </summary>
        public string PopNonce { get; private set; }

        /// <summary>
        /// Creates the authentication parameters by attempting to call the resource unauthenticated and analyzing the response.
        /// </summary>
        /// <param name="resourceUri">Resource URI.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>An instance of <see cref="AuthenticationHeaderParser"/> containing authentication header data.</returns>
        /// <overloads></overloads>
        public static Task<AuthenticationHeaderParser> ParseAuthenticationHeadersAsync(string resourceUri, CancellationToken cancellationToken = default)
        {
            return ParseAuthenticationHeadersAsync(resourceUri, GetHttpClient(), cancellationToken);
        }

        /// <inheritdoc cref="ParseAuthenticationHeadersAsync(string, CancellationToken)"/>
        /// <param name="resourceUri">Resource URI.</param>
        /// <param name="httpClient">Instance of <see cref="HttpClient"/> to make the request with.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <exception cref="ArgumentNullException">Thrown if the <c>resourceUri</c> or <c>httpClient</c> is null.</exception>
        public static async Task<AuthenticationHeaderParser> ParseAuthenticationHeadersAsync(string resourceUri, HttpClient httpClient, CancellationToken cancellationToken = default)
        {
            if (httpClient is null)
            {
                throw new ArgumentNullException(nameof(httpClient));
            }
            if (string.IsNullOrWhiteSpace(resourceUri))
            {
                throw new ArgumentNullException(nameof(resourceUri));
            }

            // call this endpoint and see what the header says and return that
            HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(resourceUri, cancellationToken).ConfigureAwait(false);

            return ParseAuthenticationHeaders(httpResponseMessage.Headers);
        }

        /// <summary>
        /// Creates a parsed set of parameters from the provided <see cref="HttpResponseHeaders"/>.
        /// </summary>
        /// <param name="httpResponseHeaders">HTTP response headers.</param>
        /// <remarks>For known values, such as the nonce used for Proof-of-Possession, the parser will first check for it in the <c>WWW-Authenticate</c> headers
        /// If it cannot find it, it will then check the <c>Authentication-Info</c> parameters for the value.</remarks>
        /// <returns>An instance of <see cref="AuthenticationHeaderParser"/>.</returns>
        public static AuthenticationHeaderParser ParseAuthenticationHeaders(HttpResponseHeaders httpResponseHeaders)
        {
            AuthenticationHeaderParser authenticationHeaderParser = new();
            AuthenticationInfoParameters authenticationInfoParameters = new();
            string serverNonce = null;

            //Check for WWW-AuthenticateHeaders
            if (httpResponseHeaders.WwwAuthenticate.Count != 0)
            {
                IReadOnlyList<WwwAuthenticateParameters> wwwParameters = Client.WwwAuthenticateParameters.CreateFromAuthenticationHeaders(httpResponseHeaders);

                serverNonce = wwwParameters.SingleOrDefault(parameter => string.Equals(parameter.AuthenticationScheme, Constants.PoPAuthHeaderPrefix, StringComparison.Ordinal))?.Nonce;

                authenticationHeaderParser.WwwAuthenticateParameters = wwwParameters;
            }
            else
            {
                authenticationHeaderParser.WwwAuthenticateParameters = new List<WwwAuthenticateParameters>();

                //If no WWW-AuthenticateHeaders exist, attempt to parse AuthenticationInfo headers instead
                authenticationInfoParameters = AuthenticationInfoParameters.CreateFromResponseHeaders(httpResponseHeaders);
                authenticationHeaderParser.AuthenticationInfoParameters = authenticationInfoParameters;
            }

            //If server nonce is not acquired from WWW-Authenticate headers, use next nonce from Authentication-Info parameters.
            authenticationHeaderParser.PopNonce = serverNonce ?? authenticationInfoParameters.NextNonce;

            return authenticationHeaderParser;
        }

        /// <summary>
        /// Creates a new <see cref="HttpClient"/> instance.
        /// </summary>
        internal static HttpClient GetHttpClient()
        {
            return _httpClientFactory.Value.GetHttpClient();
        }

        /// <summary>
        /// Creates a key-value pair from an expression of the form a=b if possible.
        /// Otherwise, the key value pair will be returned as (key:<paramref name="authScheme"/>, value:<paramref name="paramValue"/>).
        /// </summary>
        /// <param name="paramValue">assignment</param>
        /// <param name="authScheme">authScheme</param>
        /// <returns>An instance of <see cref="KeyValuePair{TKey, TValue}"/>.</returns>
        internal static KeyValuePair<string, string> CreateKeyValuePair(string paramValue, string authScheme)
        {
            string[] segments = CoreHelpers.SplitWithQuotes(paramValue, '=')
                .Select(s => s.Trim().Trim('"'))
                .ToArray();

            if (segments.Length < 2)
            {
                // Extracted assignment isn't of the form a=b. To enable this value to be more easily discoverable, it is set with the auth scheme as the key."
                return new KeyValuePair<string, string>(authScheme, paramValue);
            }

            return new KeyValuePair<string, string>(segments[0], segments[1]);
        }
    }

}
