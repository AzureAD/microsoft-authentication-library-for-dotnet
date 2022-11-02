// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Parameters returned by the Authentication-Info header. This allows for
    /// scenarios such as proof-of-possession, etc.
    /// See https://www.rfc-editor.org/rfc/rfc7615
    /// </summary>
    public class AuthenticationInfoParameters
    {
        private const string AuthenticationInfoKey = "Authentication-Info";
        /// <summary>
        /// The next nonce to be used in the preceding authentication request.
        /// </summary>
        public string NextNonce { get; private set; }

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
        /// Dictionary of raw parameters in the Authentication-Info header (extracted from the Authentication-Info header
        /// string value, without any processing). This allows support for APIs which are not mappable easily to the standard
        /// or framework specific (Microsoft.Identity.Model, Microsoft.Identity.Web).
        /// </summary>
        internal IDictionary<string, string> RawParameters { get; private set; }

        /// <summary>
        /// Create Authentication-Info parameters from the HttpResponseHeaders for each auth scheme.
        /// </summary>
        /// <param name="httpResponseHeaders">HttpResponseHeaders.</param>
        /// <returns>Authentication-Info provided by the endpoint</returns>
        public static AuthenticationInfoParameters CreateFromHeaders(HttpResponseHeaders httpResponseHeaders)
        {
            AuthenticationInfoParameters parameters = new AuthenticationInfoParameters();

            try
            {
                if (httpResponseHeaders.Contains(AuthenticationInfoKey))
                {
                    var authInfoValue = httpResponseHeaders.Where(header => header.Key == AuthenticationInfoKey).Single().Value.FirstOrDefault();


                    var AuthValuesSplit = authInfoValue.Split(new char[] { ' ' }, 2);

                    var paramValues = CoreHelpers.SplitWithQuotes(AuthValuesSplit[1], ',')
                            .Select(v => AuthenticationHeaderParser.ExtractKeyValuePair(v.Trim()))
                            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

                    parameters.RawParameters = paramValues;

                    if (paramValues.TryGetValue("nextnonce", out string value))
                    {
                        parameters.NextNonce = value;
                    }

                    return parameters;

                    //Could not get Auth info parameters
                    throw new MsalClientException(MsalError.UnableToParseAuthenticationHeader, MsalErrorMessage.UnableToParseAuthenticationHeader);
                }

                return null;
            }
            catch (Exception ex) when (ex is not MsalClientException)
            {
                throw new MsalClientException(MsalError.UnableToParseAuthenticationHeader, MsalErrorMessage.UnableToParseAuthenticationHeader, ex);
            }
        }
    }
}
