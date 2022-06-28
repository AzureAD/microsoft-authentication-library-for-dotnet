// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.ApiConfig.DownStreamRestApi
{
    internal class DownstreamRestApiHelper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="authResult"></param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> TryGetResponseFromResourceAsync(string endpoint, AuthenticationResult authResult)
        {
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue(authResult.TokenType, authResult.AccessToken);
            return await client.SendAsync(request).ConfigureAwait(false);
        }
    }
}
