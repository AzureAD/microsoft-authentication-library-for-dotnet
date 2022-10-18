// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.PlatformsCommon.Factories;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Parsed authentication headers to retreve header values from HttpResponseHeaders.
    /// </summary>
    public class AuthenticationHeaderParser
    {
        /// <summary>
        /// Parameters returned by the WWW-Authenticate header. This allows for dynamic
        /// scenarios such as claim challenge, CAE, CA auth context.
        /// See https://aka.ms/msal-net/wwwAuthenticate.
        /// </summary>
        public IReadOnlyList<WwwAuthenticateParameters> WwwAuthenticateParameters { get; private set; }

        /// <summary>
        /// Parameters returned by the Authentication-Info header. 
        /// This allows for authentication scenarios such as Proof-Of-Posession.
        /// </summary>
        public AuthenticationInfoParameters AuthenticationInfoParameters { get; private set; }

        /// <summary>
        /// Nonce parsed from HttpResponseHeaders
        /// </summary>
        public string Nonce { get; private set; }

        /// <summary>
        /// Creates the authenticate parameters by attempting to call the resource unauthenticated, and analyzing the response.
        /// </summary>
        /// <param name="resourceUri">URI of the resource.</param>
        /// <returns></returns>
        public static async Task<AuthenticationHeaderParser> ParseAuthenticationHeadersAsync(string resourceUri)
        {
            return await ParseAuthenticationHeadersAsync(resourceUri, default).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates the authenticate parameters by attempting to call the resource unauthenticated, and analyzing the response.
        /// </summary>
        /// <param name="resourceUri">URI of the resource.</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <returns></returns>
        public static async Task<AuthenticationHeaderParser> ParseAuthenticationHeadersAsync(string resourceUri, CancellationToken cancellationToken)
        {
            return await ParseAuthenticationHeadersAsync(resourceUri, GetHttpClient(), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates the authenticate parameters by attempting to call the resource unauthenticated, and analyzing the response.
        /// </summary>
        /// <param name="resourceUri">URI of the resource.</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <param name="httpClient">Instance of <see cref="HttpClient"/> to make the request with.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static async Task<AuthenticationHeaderParser> ParseAuthenticationHeadersAsync(string resourceUri, HttpClient httpClient, CancellationToken cancellationToken)
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
        /// Creates a parsed set of parameters from the provided HttpResponseHeaders.
        /// </summary>
        /// <param name="httpResponseHeaders"></param>
        /// <remarks>For known values, such as the nonce used for Proof-of-Possession, the parser will first check for it in the WWW-Authenticate headers
        /// If it cannot find it, it will then check the Authentication-Info parameters for the value.</remarks>
        /// <returns></returns>
        public static AuthenticationHeaderParser ParseAuthenticationHeaders(HttpResponseHeaders httpResponseHeaders)
        {
            AuthenticationHeaderParser authenticationHeaderParser = new AuthenticationHeaderParser();
            AuthenticationInfoParameters authenticationInfoParameters = new AuthenticationInfoParameters();
            string serverNonce = null;

            //Check for WWW-AuthenticateHeaders
            if (httpResponseHeaders.WwwAuthenticate.Count != 0)
            {
                var WwwParameters = Client.WwwAuthenticateParameters.CreateFromAuthenticationHeaders(httpResponseHeaders);

                if (WwwParameters.Any(parameter => parameter.AuthScheme == "PoP"))
                {
                    serverNonce = WwwParameters.Where(parameter => parameter.AuthScheme == "PoP").Single().ServerNonce;
                }

                authenticationHeaderParser.WwwAuthenticateParameters = WwwParameters;
            }
            else
            {
                authenticationHeaderParser.WwwAuthenticateParameters = new List<WwwAuthenticateParameters>();

                //If no WWW-AuthenticateHeaders exist, attempt to parse AuthenticationInfo headers instead
                authenticationInfoParameters = AuthenticationInfoParameters.CreateFromHeaders(httpResponseHeaders);
                authenticationHeaderParser.AuthenticationInfoParameters = authenticationInfoParameters;
            }

            //If server nonce is not acquired from WWW-Authenticate headers, use next nonce from Authentication-Info parameters.
            authenticationHeaderParser.Nonce = serverNonce ?? authenticationInfoParameters.NextNonce;

            return authenticationHeaderParser;
        }

        /// <summary>
        /// Created an HttpClient
        /// </summary>
        internal static HttpClient GetHttpClient()
        {
            var httpClientFactory = PlatformProxyFactory.CreatePlatformProxy(null).CreateDefaultHttpClientFactory();
            return httpClientFactory.GetHttpClient();
        }

        /// <summary>
        /// Extracts a key value pair from an expression of the form a=b
        /// </summary>
        /// <param name="assignment">assignment</param>
        /// <returns>Key Value pair</returns>
        internal static KeyValuePair<string, string> ExtractKeyValuePair(string assignment)
        {
            string[] segments = CoreHelpers.SplitWithQuotes(assignment, '=')
                .Select(s => s.Trim().Trim('"'))
                .ToArray();

            if (segments.Length != 2)
            {
                throw new ArgumentException(nameof(assignment), $"{assignment} isn't of the form a=b");
            }

            return new KeyValuePair<string, string>(segments[0], segments[1]);
        }
    }

}
