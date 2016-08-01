//------------------------------------------------------------------------------
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

using System.Net.Http;

namespace Microsoft.Identity.Client.Internal.Http
{
    internal class HttpClientFactory
    {
        // as per guidelines HttpClient should be a singeton instance in an application.
        private static HttpClient _client;
        private static readonly object LockObj = new object();
        public static bool ReturnHttpClientForMocks { set; get; }

        public static HttpClient GetHttpClient()
        {
            // we return a new instanceof httpclient beacause there
            // is no way to provide new http request message handler
            // for each request made and it makes mocking of network calls 
            // impossible. So to circumvent, we simply return new instance for
            // for mocking purposes.
            if (ReturnHttpClientForMocks)
            {
                return new HttpClient(HttpMessageHandlerFactory.GetMessageHandler(true));
            }

            if (_client == null)
            {
                lock (LockObj)
                {
                    if (_client == null)
                    {
                        _client = new HttpClient(HttpMessageHandlerFactory.GetMessageHandler(false));
                    }
                }
            }

            return _client;
        }
    }
}