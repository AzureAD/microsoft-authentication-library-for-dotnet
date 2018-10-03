//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Identity.Core.Http
{
    /// <summary>
    /// Sends an HTTP request. Uses a simple retry mechanism - if the request
    /// timed out or returned error code 500-600, then retry once. 
    /// After retry, throw a "service not available" service exception (i.e. only for 500-600 errors).
    /// Does not retry / throw in case of other errors (e.g. 429), just returns the http response.
    /// </summary>
    internal class HttpRequest
    {
        private HttpRequest()
        {
        }

        public static async Task<HttpResponse> SendPostAsync(
            Uri endpoint,
            IDictionary<string, string> headers,
            IDictionary<string, string> bodyParameters,
            RequestContext requestContext)
        {
            IHttpManager mgr = new HttpManager();
            return await mgr.SendPostAsync(endpoint, headers, bodyParameters, requestContext).ConfigureAwait(false);
        }

        public static async Task<HttpResponse> SendPostAsync(
            Uri endpoint,
            IDictionary<string, string> headers,
            HttpContent body,
            RequestContext requestContext)
        {
            IHttpManager mgr = new HttpManager();
            return await mgr.SendPostAsync(endpoint, headers, body, requestContext).ConfigureAwait(false);
        }

        public static async Task<HttpResponse> SendGetAsync(
            Uri endpoint,
            Dictionary<string, string> headers,
            RequestContext requestContext)
        {
            IHttpManager mgr = new HttpManager();
            return await mgr.SendGetAsync(endpoint, headers, requestContext).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs the POST request just like <see cref="SendPostAsync(Uri, IDictionary{string, string}, HttpContent, RequestContext)"/>
        /// but does not throw a ServiceUnavailable service exception. Instead, it returns the <see cref="IHttpWebResponse"/> associated
        /// with the request.
        /// </summary>
        public static async Task<IHttpWebResponse> SendPostForceResponseAsync(
            Uri uri,
            Dictionary<string, string> headers,
            StringContent body,
            RequestContext requestContext)
        {
            IHttpManager mgr = new HttpManager();
            return await mgr.SendPostForceResponseAsync(uri, headers, body, requestContext).ConfigureAwait(false);
        }
    }
}