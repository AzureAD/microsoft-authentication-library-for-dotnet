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
    /// See https://learn.microsoft.com/en-us/openspecs/office_protocols/ms-sipae/b3ac8451-ee93-43a8-a51a-baedfdd3bed5.
    /// </summary>
    public class AuthenticationInfoParameters
    {
        private const string AuthenticationInfoKey = "Authentication-Info";
        /// <summary>
        /// The next nonce to be used in the preceding authentication request.
        /// </summary>
        public string NextNonce { get; private set; }

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

                    if (paramValues.TryGetValue("nextnonce", out string value))
                    {
                        parameters.NextNonce = value;
                    }
                }
            }
            catch(Exception ex)
            {
                throw new MsalClientException(MsalError.UnableToParseAuthenticationHeader, MsalErrorMessage.UnableToParseAuthenticationHeader, ex);
            }

            return parameters;
        }
    }
}
