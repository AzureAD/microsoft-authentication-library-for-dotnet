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
    /// Parameters returned by the WWW-Authenticate header. This allows for dynamic
    /// scenarios such as claim challenge, CAE, CA auth context.
    /// See https://aka.ms/msal-net/wwwAuthenticate.
    /// </summary>
    public class WwwAuthenticateParameters
    {
        /// <summary>
        /// Resource for which to request scopes.
        /// This is the App ID URI of the API that returned the WWW-Authenticate header.
        /// </summary>
        [Obsolete("The client apps should know which App ID URI it requests scopes for.", true)]
        public string Resource { get; set; }

        /// <summary>
        /// Scopes to request.
        /// If it's not provided by the web API, it's computed from the Resource.
        /// </summary>
        [Obsolete("The client apps should know which scopes to request for.", true)]
        public IEnumerable<string> Scopes { get; set; }

        /// <summary>
        /// Authority from which to request an access token.
        /// </summary>
        public string Authority { get; set; }

        /// <summary>
        /// Claims demanded by the web API.
        /// </summary>
        public string Claims { get; set; }

        /// <summary>
        /// Error.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Return the <see cref="RawParameters"/> of key <paramref name="key"/>.
        /// </summary>
        /// <param name="key">Name of the raw parameter to retrieve.</param>
        /// <returns>The raw parameter if it exists,
        /// or throws a <see cref="System.Collections.Generic.KeyNotFoundException"/> otherwise.
        /// </returns>
        public string this[string key]
        {
            get
            {
                return RawParameters[key];
            }
        }

        /// <summary>
        /// Dictionary of raw parameters in the WWW-Authenticate header (extracted from the WWW-Authenticate header
        /// string value, without any processing). This allows support for APIs which are not mappable easily to the standard
        /// or framework specific (Microsoft.Identity.Model, Microsoft.Identity.Web).
        /// </summary>
        internal IDictionary<string, string> RawParameters { get; private set; }

        /// <summary>
        /// Gets Azure AD tenant ID.
        /// </summary>
        public string GetTenantId() => Instance.Authority
                                               .CreateAuthority(Authority, validateAuthority: true)
                                               .TenantId;

        /// <summary>
        /// Create WWW-Authenticate parameters from the HttpResponseHeaders.
        /// </summary>
        /// <param name="httpResponseHeaders">HttpResponseHeaders.</param>
        /// <param name="scheme">Authentication scheme. Default is "Bearer".</param>
        /// <returns>The parameters requested by the web API.</returns>
        /// <remarks>Currently it only supports the Bearer scheme</remarks>
        public static WwwAuthenticateParameters CreateFromResponseHeaders(
            HttpResponseHeaders httpResponseHeaders,
            string scheme = "Bearer")
        {
            if (httpResponseHeaders.WwwAuthenticate.Any())
            {
                // TODO: add POP support
                AuthenticationHeaderValue bearer = httpResponseHeaders.WwwAuthenticate.First(v => string.Equals(v.Scheme, scheme, StringComparison.OrdinalIgnoreCase));
                string wwwAuthenticateValue = bearer.Parameter;
                return CreateFromWwwAuthenticateHeaderValue(wwwAuthenticateValue);
            }

            return CreateWwwAuthenticateParameters(new Dictionary<string, string>());
        }

        /// <summary>
        /// Creates parameters from the WWW-Authenticate string.
        /// </summary>
        /// <param name="wwwAuthenticateValue">String contained in a WWW-Authenticate header.</param>
        /// <returns>The parameters requested by the web API.</returns>
        public static WwwAuthenticateParameters CreateFromWwwAuthenticateHeaderValue(string wwwAuthenticateValue)
        {
            if (string.IsNullOrWhiteSpace(wwwAuthenticateValue))
            {
                throw new ArgumentNullException(nameof(wwwAuthenticateValue));
            }

            IDictionary<string, string> parameters = CoreHelpers.SplitWithQuotes(wwwAuthenticateValue, ',')
                .Select(v => ExtractKeyValuePair(v.Trim()))
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

            return CreateWwwAuthenticateParameters(parameters);
        }

        /// <summary>
        /// Create the authenticate parameters by attempting to call the resource unauthenticated, and analyzing the response.
        /// </summary>
        /// <param name="resourceUri">URI of the resource.</param>
        /// <returns>WWW-Authenticate Parameters extracted from response to the unauthenticated call.</returns>
        public static Task<WwwAuthenticateParameters> CreateFromResourceResponseAsync(string resourceUri)
        {
            return CreateFromResourceResponseAsync(resourceUri, default);
        }

        /// <summary>
        /// Create the authenticate parameters by attempting to call the resource unauthenticated, and analyzing the response.
        /// </summary>
        /// <param name="resourceUri">URI of the resource.</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <returns>WWW-Authenticate Parameters extracted from response to the unauthenticated call.</returns>
        public static Task<WwwAuthenticateParameters> CreateFromResourceResponseAsync(string resourceUri, CancellationToken cancellationToken = default)
        {
            var httpClientFactory = PlatformProxyFactory.CreatePlatformProxy(null).CreateDefaultHttpClientFactory();
            var httpClient = httpClientFactory.GetHttpClient();
            return CreateFromResourceResponseAsync(httpClient, resourceUri, cancellationToken);
        }

        /// <summary>
        /// Create the authenticate parameters by attempting to call the resource unauthenticated, and analyzing the response.
        /// </summary>
        /// <param name="httpClient">Instance of <see cref="HttpClient"/> to make the request with.</param>
        /// <param name="resourceUri">URI of the resource.</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <returns>WWW-Authenticate Parameters extracted from response to the unauthenticated call.</returns>
        public static async Task<WwwAuthenticateParameters> CreateFromResourceResponseAsync(HttpClient httpClient, string resourceUri, CancellationToken cancellationToken = default)
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
            var wwwAuthParam = CreateFromResponseHeaders(httpResponseMessage.Headers);
            return wwwAuthParam;
        }

        /// <summary>
        /// Gets the claim challenge from HTTP header.
        /// Used, for example, for CA auth context.
        /// </summary>
        /// <param name="httpResponseHeaders">The HTTP response headers.</param>
        /// <param name="scheme">Authentication scheme. Default is Bearer.</param>
        /// <returns></returns>
        public static string GetClaimChallengeFromResponseHeaders(
            HttpResponseHeaders httpResponseHeaders,
            string scheme = "Bearer")
        {
            WwwAuthenticateParameters parameters = CreateFromResponseHeaders(
                httpResponseHeaders,
                scheme);

            // read the header and checks if it contains an error with insufficient_claims value.
            if (parameters.Claims != null &&
                string.Equals(parameters.Error, "insufficient_claims", StringComparison.OrdinalIgnoreCase))
            {
                return parameters.Claims;
            }

            return null;
        }

        internal static WwwAuthenticateParameters CreateWwwAuthenticateParameters(IDictionary<string, string> values)
        {
            WwwAuthenticateParameters wwwAuthenticateParameters = new WwwAuthenticateParameters
            {
                RawParameters = values
            };

            string value;
            if (values.TryGetValue("authorization_uri", out value))
            {
                wwwAuthenticateParameters.Authority = value.Replace("/oauth2/authorize", string.Empty);
            }

            if (string.IsNullOrEmpty(wwwAuthenticateParameters.Authority))
            {
                if (values.TryGetValue("authorization", out value))
                {
                    wwwAuthenticateParameters.Authority = value.Replace("/oauth2/authorize", string.Empty);
                }
            }

            if (string.IsNullOrEmpty(wwwAuthenticateParameters.Authority))
            {
                if (values.TryGetValue("authority", out value))
                {
                    wwwAuthenticateParameters.Authority = value.TrimEnd('/');
                }
            }

            if (values.TryGetValue("claims", out value))
            {
                wwwAuthenticateParameters.Claims = GetJsonFragment(value);
            }

            if (values.TryGetValue("error", out value))
            {
                wwwAuthenticateParameters.Error = value;
            }

            return wwwAuthenticateParameters;
        }

        /// <summary>
        /// Extracts a key value pair from an expression of the form a=b
        /// </summary>
        /// <param name="assignment">assignment</param>
        /// <returns>Key Value pair</returns>
        private static KeyValuePair<string, string> ExtractKeyValuePair(string assignment)
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

        /// <summary>
        /// Checks if input is a base-64 encoded string.
        /// If it is one, decodes it to get a json fragment.
        /// </summary>
        /// <param name="inputString">Input string</param>
        /// <returns>a json fragment (original input string or decoded from base64 encoded).</returns>
        private static string GetJsonFragment(string inputString)
        {
            if (string.IsNullOrEmpty(inputString) || inputString.Length % 4 != 0 || inputString.Any(c => char.IsWhiteSpace(c)))
            {
                return inputString;
            }

            try
            {
                var decodedBase64Bytes = Convert.FromBase64String(inputString);
                var decoded = Encoding.UTF8.GetString(decodedBase64Bytes);
                return decoded;
            }
            catch
            {
                return inputString;
            }
        }
    }
}
