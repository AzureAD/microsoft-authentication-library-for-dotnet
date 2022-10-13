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
    /// 
    /// </summary>
    public class AuthenticationInfoParameters
    {
        private const string _authenticationInfoKey = "Authentication-Info";
        /// <summary>
        /// 
        /// </summary>
        public string NextNonce { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="respnseHeaders"></param>
        /// <returns></returns>
        public static AuthenticationInfoParameters CreateFromHeaders(HttpResponseHeaders respnseHeaders)
        {
            AuthenticationInfoParameters parameters = new AuthenticationInfoParameters();

            if (respnseHeaders.Contains(_authenticationInfoKey))
            {
                var authInfoValue = respnseHeaders.Where(header => header.Key == _authenticationInfoKey).Single().Value.FirstOrDefault();

                var AuthValuesSplit = authInfoValue.Split(new char[] { ' ' }, 2);

                var paramValues = CoreHelpers.SplitWithQuotes(AuthValuesSplit[1], ',')
                        .Select(v => AuthenticationHeaderParser.ExtractKeyValuePair(v.Trim()))
                        .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

                if (paramValues.TryGetValue("nextnonce", out string value))
                {
                    parameters.NextNonce = value;
                }

            }

            return parameters;
        }
    }
}
