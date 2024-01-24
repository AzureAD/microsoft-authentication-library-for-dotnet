// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Parameters returned by the WWW-Authenticate header. This allows for dynamic
    /// scenarios such as Claims challenge, Continuous Access Evaluation (CAE), and Conditional Access (CA).
    /// See https://aka.ms/msal-net/wwwAuthenticate.
    /// </summary>
    public class WwwAuthenticateParameters
    {
        private static readonly ISet<string> s_knownAuthenticationSchemes = new HashSet<string>(
                                                                              new[] {
                                                                                       Constants.BearerAuthHeaderPrefix,
                                                                                       Constants.PoPAuthHeaderPrefix
                                                                              }, 
                                                                              StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Resource for which to request scopes.
        /// This is the App ID URI of the API that returned the WWW-Authenticate header.
        /// </summary>
        /// <remarks>
        /// Clients that perform resource validation (e.g. by comparing the host part of the resource against a list of known good hosts), 
        /// can still use the indexer to retrieve the raw value of the resource / scope.
        /// 
        /// If a resource is used, add "/.default" to it to transform it into a scope, e.g. "https://graph.microsoft.com/.default" is the OAuth2 scope for "https://graph.microsoft.com" resource.
        /// MSAL only works with scopes.
        /// </remarks>
        [Obsolete("The client apps should know which App ID URI it requests scopes for.", true)]
        public string Resource { get; set; }

        /// <summary>
        /// Scopes to request.
        /// If it's not provided by the web API, it's computed from the Resource.
        /// </summary>
        /// <remarks>
        /// Clients that perform resource validation (e.g. by comparing the host part of the resource against a list of known good hosts), 
        /// can still use the indexer to retrieve the raw value of the resource / scope. 
        /// 
        /// If a resource is used, add "/.default" to it to transform it into a scope, e.g. "https://graph.microsoft.com/.default" is the OAuth2 scope for "https://graph.microsoft.com" resource.
        /// MSAL only works with scopes.
        /// </remarks>
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
        /// AuthScheme.
        /// See the <see href="https://developer.mozilla.org/docs/Web/HTTP/Headers/WWW-Authenticate#syntax">documentation on WWW-Authenticate</see> for more details
        /// </summary>
        public string AuthenticationScheme { get; private set; }

        /// <summary>
        /// The nonce acquired from the WWW-Authenticate header.
        /// </summary>
        public string Nonce { get; private set; }

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
                                               .CreateAuthority(Authority, validateAuthority: true)?
                                               .TenantId;
        #region Obsolete Api
        /// <summary>
        /// Create the authenticate parameters by attempting to call the resource unauthenticated, and analyzing the response.
        /// </summary>
        /// <param name="resourceUri">URI of the resource.</param>
        /// <returns>WWW-Authenticate Parameters extracted from response to the unauthenticated call.</returns>
        [System.ComponentModel.EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This api is now obsolete and has been replaced with CreateFromAuthenticationResponseAsync(...)")]
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
        [System.ComponentModel.EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This api is now obsolete and has been replaced with CreateFromAuthenticationResponseAsync(...)")]
        public static Task<WwwAuthenticateParameters> CreateFromResourceResponseAsync(string resourceUri, CancellationToken cancellationToken = default)
        {
            return CreateFromResourceResponseAsync(AuthenticationHeaderParser.GetHttpClient(), resourceUri, cancellationToken);
        }

        /// <summary>
        /// Create the authenticate parameters by attempting to call the resource unauthenticated, and analyzing the response.
        /// </summary>
        /// <param name="httpClient">Instance of <see cref="HttpClient"/> to make the request with.</param>
        /// <param name="resourceUri">URI of the resource.</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <returns>WWW-Authenticate Parameters extracted from response to the unauthenticated call.</returns>
        [System.ComponentModel.EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This api is now obsolete and has been replaced with replaced with CreateFromAuthenticationResponseAsync(HttpResponseHeaders, string)")]
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
        /// Create WWW-Authenticate parameters from the HttpResponseHeaders.
        /// </summary>
        /// <param name="httpResponseHeaders">HttpResponseHeaders.</param>
        /// <param name="scheme">Authentication scheme. Default is "Bearer".</param>
        /// <returns>The parameters requested by the web API.</returns>
        /// <remarks>Currently it only supports the Bearer scheme</remarks>
        [System.ComponentModel.EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This api is now obsolete and has been replaced with CreateFromAuthenticationHeaders(...)")]
        public static WwwAuthenticateParameters CreateFromResponseHeaders(
            HttpResponseHeaders httpResponseHeaders,
            string scheme = "Bearer")
        {
            if (httpResponseHeaders.WwwAuthenticate.Any())
            {
                AuthenticationHeaderValue headerValue = httpResponseHeaders.WwwAuthenticate.FirstOrDefault(v => string.Equals(v.Scheme, scheme, StringComparison.OrdinalIgnoreCase));

                if (headerValue != null)
                {
                    string wwwAuthenticateValue = headerValue.Parameter;
                    var parameters = CreateFromWwwAuthenticateHeaderValue(wwwAuthenticateValue);
                    parameters.AuthenticationScheme = scheme;
                    return parameters;
                }
            }

            return CreateWwwAuthenticateParameters(new Dictionary<string, string>(), string.Empty);
        }

        /// <summary>
        /// Creates parameters from the WWW-Authenticate string.
        /// </summary>
        /// <param name="wwwAuthenticateValue">String contained in a WWW-Authenticate header.</param>
        /// <returns>The parameters requested by the web API.</returns>
        [System.ComponentModel.EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This api is now obsolete and should not be used.")]
        public static WwwAuthenticateParameters CreateFromWwwAuthenticateHeaderValue(string wwwAuthenticateValue)
        {
            return CreateFromWwwAuthenticationHeaderValue(wwwAuthenticateValue, string.Empty);
        }
        #endregion Obsolete Api

        #region Single Scheme Api
        /// <summary>
        /// Create the authenticate parameters by attempting to call the resource unauthenticated, and analyzing the response.
        /// </summary>
        /// <param name="resourceUri">URI of the resource.</param>
        /// <param name="scheme">Authentication scheme.</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <returns>WWW-Authenticate Parameters extracted from response to the unauthenticated call.</returns>
        public static Task<WwwAuthenticateParameters> CreateFromAuthenticationResponseAsync(string resourceUri, string scheme, CancellationToken cancellationToken = default)
        {
            return CreateFromAuthenticationResponseAsync(resourceUri, scheme, AuthenticationHeaderParser.GetHttpClient(), cancellationToken);
        }

        /// <summary>
        /// Create the authenticate parameters by attempting to call the resource unauthenticated, and analyzing the response.
        /// </summary>
        /// <param name="httpClient">Instance of <see cref="HttpClient"/> to make the request with.</param>
        /// <param name="resourceUri">URI of the resource.</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <param name="scheme">Authentication scheme.</param>
        /// <returns>WWW-Authenticate Parameters extracted from response to the unauthenticated call.</returns>
        public static async Task<WwwAuthenticateParameters> CreateFromAuthenticationResponseAsync(string resourceUri, string scheme, HttpClient httpClient, CancellationToken cancellationToken = default)
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
            var wwwAuthParams = CreateFromAuthenticationHeaders(httpResponseMessage.Headers, scheme);
            return wwwAuthParams;
        }

        /// <summary>
        /// Create WWW-Authenticate parameters from the HttpResponseHeaders.
        /// </summary>
        /// <param name="httpResponseHeaders">HttpResponseHeaders.</param>
        /// <param name="scheme">Authentication scheme.</param>
        /// <returns>The parameters requested by the web API.</returns>
        public static WwwAuthenticateParameters CreateFromAuthenticationHeaders(
            HttpResponseHeaders httpResponseHeaders,
            string scheme)
        {
            AuthenticationHeaderValue header = httpResponseHeaders.WwwAuthenticate.FirstOrDefault(v => string.Equals(v.Scheme, scheme, StringComparison.OrdinalIgnoreCase));

            if (header != null)
            {
                string wwwAuthenticateValue = header.Parameter;
                WwwAuthenticateParameters parameters;
                try
                {
                    parameters = CreateFromWwwAuthenticationHeaderValue(wwwAuthenticateValue, scheme);

                    return parameters;
                }
                catch(Exception ex)
                {
                    if (ex is MsalException)
                    {
                        throw;
                    }

                    throw new MsalClientException(MsalError.UnableToParseAuthenticationHeader, MsalErrorMessage.UnableToParseAuthenticationHeader + $"Response Headers: {httpResponseHeaders} See inner exception for details.", ex);
                }
            }

            return CreateWwwAuthenticateParameters(new Dictionary<string, string>(), string.Empty);
        }
        #endregion Single Scheme Api

        #region Multi Scheme Api
        /// <summary>
        /// Create the authenticate parameters by attempting to call the resource unauthenticated, and analyzing the response.
        /// </summary>
        /// <param name="resourceUri">URI of the resource.</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <returns>WWW-Authenticate Parameters extracted from response to the unauthenticated call.</returns>
        public static Task<IReadOnlyList<WwwAuthenticateParameters>> CreateFromAuthenticationResponseAsync(string resourceUri, CancellationToken cancellationToken = default)
        {
            return CreateFromAuthenticationResponseAsync(resourceUri, AuthenticationHeaderParser.GetHttpClient(), cancellationToken);
        }

        /// <summary>
        /// Create the authenticate parameters by attempting to call the resource unauthenticated, and analyzing the response.
        /// </summary>
        /// <param name="httpClient">Instance of <see cref="HttpClient"/> to make the request with.</param>
        /// <param name="resourceUri">URI of the resource.</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <returns>WWW-Authenticate Parameters extracted from response to the unauthenticated call.</returns>
        public static async Task<IReadOnlyList<WwwAuthenticateParameters>> CreateFromAuthenticationResponseAsync(string resourceUri, HttpClient httpClient, CancellationToken cancellationToken = default)
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
            var wwwAuthParams = CreateFromAuthenticationHeaders(httpResponseMessage.Headers);
            return wwwAuthParams;
        }

        /// <summary>
        /// Create WWW-Authenticate parameters from the HttpResponseHeaders for each auth scheme.
        /// </summary>
        /// <param name="httpResponseHeaders">HttpResponseHeaders.</param>
        /// <returns>The parameters requested by the web API.</returns>
        /// <remarks>Currently it only supports the Bearer scheme</remarks>
        public static IReadOnlyList<WwwAuthenticateParameters> CreateFromAuthenticationHeaders(
            HttpResponseHeaders httpResponseHeaders)
        {
            List<WwwAuthenticateParameters> parameterList = new List<WwwAuthenticateParameters>();

            foreach (AuthenticationHeaderValue wwwAuthenticateHeaderValue in httpResponseHeaders.WwwAuthenticate)
            {
                try
                {
                    var parameters = CreateFromWwwAuthenticationHeaderValue(wwwAuthenticateHeaderValue.Parameter, wwwAuthenticateHeaderValue.Scheme);
                    parameterList.Add(parameters);
                }
                catch (Exception ex) when (ex is not MsalException)
                {
                    throw new MsalClientException(MsalError.UnableToParseAuthenticationHeader, MsalErrorMessage.UnableToParseAuthenticationHeader + " See inner exception for details.", ex);
                }
            }

            return parameterList;
        }
        #endregion Multi Scheme Api

        /// <summary>
        /// Gets the claim challenge from HTTP header.
        /// Used, for example, for Conditional Access (CA).
        /// </summary>
        /// <param name="httpResponseHeaders">The HTTP response headers.</param>
        /// <param name="scheme">Authentication scheme. Default is Bearer.</param>
        /// <returns>The claims challenge</returns>
        public static string GetClaimChallengeFromResponseHeaders(
            HttpResponseHeaders httpResponseHeaders,
            string scheme = Constants.BearerAuthHeaderPrefix)
        {
            WwwAuthenticateParameters parameters = CreateFromAuthenticationHeaders(
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

        /// <summary>
        /// Creates parameters from the WWW-Authenticate string.
        /// </summary>
        /// <param name="wwwAuthenticateValue">String contained in a WWW-Authenticate header.</param>
        /// <param name="scheme">Auth scheme of the result.</param>
        /// <returns>The parameters requested by the web API.</returns>
        private static WwwAuthenticateParameters CreateFromWwwAuthenticationHeaderValue(string wwwAuthenticateValue, string scheme)
        {
            IDictionary<string, string> parameters = null;

            if (!string.IsNullOrWhiteSpace(wwwAuthenticateValue))
            {
                var parametersList = GetParsedAuthValueElements(wwwAuthenticateValue);

                parameters = parametersList.Select(v => AuthenticationHeaderParser.CreateKeyValuePair(v.Trim(), scheme))
                                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
            }

            return CreateWwwAuthenticateParameters(parameters, scheme);
        }

        private static string[] GetParsedAuthValueElements(string wwwAuthenticateValue)
        {
            char[] charsToTrim = { ',', ' ' };
            List<string> authValuesSplit = CoreHelpers.SplitWithQuotes(wwwAuthenticateValue, ' ');

            //Ensure that known headers are not being parsed.
            if (s_knownAuthenticationSchemes.Contains(authValuesSplit[0]))
            {
                authValuesSplit = authValuesSplit.Skip(1).ToList();
            }

            return authValuesSplit.Select(authValue => authValue.TrimEnd(charsToTrim)).ToArray();
        }

        internal static WwwAuthenticateParameters CreateWwwAuthenticateParameters(IDictionary<string, string> values, string scheme)
        {
            WwwAuthenticateParameters wwwAuthenticateParameters = new WwwAuthenticateParameters();

            wwwAuthenticateParameters.AuthenticationScheme = scheme;

            if (values == null)
            {
                wwwAuthenticateParameters.RawParameters = new Dictionary<string, string>();
                return wwwAuthenticateParameters;
            }

            wwwAuthenticateParameters.RawParameters = values;

            if (values.TryGetValue("authorization_uri", out string value))
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

            if (values.TryGetValue("nonce", out value))
            {
                wwwAuthenticateParameters.Nonce = value;
            }

            return wwwAuthenticateParameters;
        }

        /// <summary>
        /// Checks if input is a base-64 encoded string.
        /// If it is one, decodes it to get a JSON fragment.
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
