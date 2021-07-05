// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

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
        /// </summary>
        public string Resource { get; set; }

        /// <summary>
        /// Scopes to request.
        /// </summary>
        public string[] Scopes
        {
            get
            {
                if (scopes != null)
                {
                    return scopes;
                }
                else if (!string.IsNullOrEmpty(Resource))
                {
                    return new[] { $"{Resource}/.default" };
                }
                else
                {
                    return null;
                }
            }
            set
            {
                scopes = value;
            }
        }

        private string[] scopes;

        /// <summary>
        /// Authority from which to request an access token.
        /// </summary>
        public string Authority { get; set; }

        /// <summary>
        /// Requested claims.
        /// </summary>
        public string Claims { get; set; }

        /// <summary>
        /// Error.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Dictionary of raw parameters in the WWW-Authenticate header (extracted from the WWW-Authenticate header
        /// string value, without any processing. This allows support for APIs which are not mappable easily to the standard
        /// or framework specific (SAL, Microsoft.Identity.Web).
        /// </summary>
        public IDictionary<string, string> RawParameters { get; private set; }

        /// <summary>
        /// Create WWW-Authenticate parameters from the HttpResponseHeaders.
        /// </summary>
        /// <param name="httpResponseHeaders">HttpResponseHeaders.</param>
        /// <param name="scheme">Authentication scheme. Default is "Bearer".</param>
        /// <returns></returns>
        public static WwwAuthenticateParameters ExtractWwwAuthenticateParametersFromHeaders(
            HttpResponseHeaders httpResponseHeaders,
            string scheme = "Bearer")
        {
            if (httpResponseHeaders.WwwAuthenticate.Any())
            {
                // TODO: could it be Pop too?
                AuthenticationHeaderValue bearer = httpResponseHeaders.WwwAuthenticate.First(v => string.Equals(v.Scheme, scheme, StringComparison.OrdinalIgnoreCase));
                string wwwAuthenticateValue = bearer.Parameter;
                return ExtractParametersFromWwwAuthenticateHeaderValue(wwwAuthenticateValue);
            }

            return CreateWwwAuthenticateParameters(new Dictionary<string, string>());
        }

        /// <summary>
        /// Extract parameters from the WWW-Authenticate string.
        /// </summary>
        /// <param name="wwwAuthenticateValue">String contained in a WWW-Authenticate header.</param>
        /// <returns>Dictionary of parameters (name/value).</returns>
        public static WwwAuthenticateParameters ExtractParametersFromWwwAuthenticateHeaderValue(string wwwAuthenticateValue)
        {
            if (string.IsNullOrWhiteSpace(wwwAuthenticateValue))
            {
                throw new ArgumentException($"'{nameof(wwwAuthenticateValue)}' cannot be null or whitespace.", nameof(wwwAuthenticateValue));
            }

            IDictionary<string, string> parameters = SplitWithQuotes(wwwAuthenticateValue, ',')
                .Select(v => ExtractKeyValuePair(v.Trim()))
                .ToDictionary(pair => pair.Key, pair => pair.Value);

            return CreateWwwAuthenticateParameters(parameters);
        }

        /// <summary>
        /// Create the authenticate parameters by attempting to call the resource unauthenticated, and analysis the response
        /// </summary>
        /// <param name="resourceUri">Uri of the resouce</param>
        /// <returns>WwwAuthenticateParameters extracted from response to the un-authenticated call.</returns>
        public static async Task<WwwAuthenticateParameters> CreateFromResourceResponseAsync(string resourceUri)
        {
            // call this endpoint and see what the header says and return that
            HttpClient httpClient = new HttpClient();
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, resourceUri);
            HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequestMessage).ConfigureAwait(false);
            var wwwAuthParam = ExtractWwwAuthenticateParametersFromHeaders(httpResponseMessage.Headers);
            return wwwAuthParam;
        }

        /// <summary>
        /// Extracts the claim challenge from HTTP header.
        /// </summary>
        /// <param name="httpResponseHeaders">The HTTP response headers.</param>
        /// <param name="scheme">Authentication scheme. Default is Bearer.</param>
        /// <returns></returns>
        public static string ExtractClaimChallengeFromHttpHeader(
            HttpResponseHeaders httpResponseHeaders,
            string scheme = "Bearer")
        {
            WwwAuthenticateParameters parameters = ExtractWwwAuthenticateParametersFromHeaders(
                httpResponseHeaders,
                scheme);

            try
            {
                // read the header and checks if it conatins error with insufficient_claims value.
                if (null != parameters.Error && "insufficient_claims" == parameters.Error)
                {
                    if (null != parameters.Claims)
                    {
                        return parameters.Claims;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return null;
        }

        internal static WwwAuthenticateParameters CreateWwwAuthenticateParameters(IDictionary<string, string> values)
        {
            WwwAuthenticateParameters wwwAuthenticateParameters = new WwwAuthenticateParameters();
            wwwAuthenticateParameters.RawParameters = values;
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

            if (values.TryGetValue("resource_id", out value))
            {
                wwwAuthenticateParameters.Resource = value;
            }

            if (string.IsNullOrEmpty(wwwAuthenticateParameters.Resource))
            {
                if (values.TryGetValue("resource", out value))
                {
                    wwwAuthenticateParameters.Resource = value;
                }
            }

            if (string.IsNullOrEmpty(wwwAuthenticateParameters.Resource))
            {
                if (values.TryGetValue("client_id", out value))
                {
                    wwwAuthenticateParameters.Resource = value;
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

        internal static List<string> SplitWithQuotes(string input, char delimiter)
        {
            List<string> items = new List<string>();

            if (string.IsNullOrWhiteSpace(input))
            {
                return items;
            }

            int startIndex = 0;
            bool insideString = false;
            string item;
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == delimiter && !insideString)
                {
                    item = input.Substring(startIndex, i - startIndex);
                    if (!string.IsNullOrWhiteSpace(item.Trim()))
                    {
                        items.Add(item);
                    }

                    startIndex = i + 1;
                }
                else if (input[i] == '"')
                {
                    insideString = !insideString;
                }
            }

            item = input.Substring(startIndex);
            if (!string.IsNullOrWhiteSpace(item.Trim()))
            {
                items.Add(item);
            }

            return items;
        }

        /// <summary>
        /// Extracts a key value pair from an expression of the form a=b
        /// </summary>
        /// <param name="assignment">assignment</param>
        /// <returns>Key Value pair</returns>
        private static KeyValuePair<string, string> ExtractKeyValuePair(string assignment)
        {
            string[] segments = SplitWithQuotes(assignment, '=')
                .Select(s => s.Trim().Trim('"'))
                .ToArray();
            if (segments.Length != 2)
            {
                throw new ArgumentException(nameof(assignment), $"{assignment} isn't of the form a=b");
            }

            return new KeyValuePair<string, string>(segments[0].ToLowerInvariant(), segments[1]);
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
            catch (Exception)
            {
                return inputString;
            }
        }
    }
}
