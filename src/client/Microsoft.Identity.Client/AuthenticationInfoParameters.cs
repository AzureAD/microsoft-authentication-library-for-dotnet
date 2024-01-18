// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using Microsoft.Identity.Client.Utils;

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
        public static AuthenticationInfoParameters CreateFromResponseHeaders(HttpResponseHeaders httpResponseHeaders)
        {
            AuthenticationInfoParameters parameters = new AuthenticationInfoParameters();

            try
            {
                var authInfoValueList = httpResponseHeaders.SingleOrDefault(header => header.Key == AuthenticationInfoKey).Value;

                if (authInfoValueList != null)
                {
                    var authInfoValue = authInfoValueList.FirstOrDefault();
                    var AuthValuesSplit = authInfoValue.Split(new char[] { ' ' }, 2);
                    IDictionary<string, string> paramValues;

                    if (AuthValuesSplit.Count() != 2)
                    {
                        //Header is not in the form of a=b.
                        paramValues = new Dictionary<string, string>();
                        paramValues.Add(new KeyValuePair<string, string>(AuthenticationInfoKey, authInfoValue));
                    }
                    else
                    {
                        paramValues = CoreHelpers.SplitWithQuotes(AuthValuesSplit[1], ',')
                                .Select(v => AuthenticationHeaderParser.CreateKeyValuePair(v.Trim(), AuthenticationInfoKey))
                                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

                        if (paramValues.TryGetValue("nextnonce", out string value))
                        {
                            parameters.NextNonce = value;
                        }
                    }

                    parameters.RawParameters = paramValues;
                }

                return parameters;
            }
            catch (Exception ex) when (ex is not MsalClientException)
            {
                throw new MsalClientException(MsalError.UnableToParseAuthenticationHeader, $"{MsalErrorMessage.UnableToParseAuthenticationHeader}Response Headers: {httpResponseHeaders} See inner exception for details.", ex);
            }
        }
    }
}
