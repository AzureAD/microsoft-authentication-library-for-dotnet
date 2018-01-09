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

using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Http
{
    internal static class HttpMessageHandlerFactory
    {
        internal static HttpMessageHandler GetMessageHandler(bool useDefaultCredentials)
        {
            if (UseMocks)
            {
                if (MockHandlerQueue.Count > 0)
                {
                    return MockHandlerQueue.Dequeue();
                }

                throw new ArgumentException("No mocks available to consume");
            }

            return new HttpClientHandler { UseDefaultCredentials = useDefaultCredentials, Proxy = WebProxyProvider.DefaultWebProxy};
        }

        private static readonly Queue<HttpMessageHandler> MockHandlerQueue = new Queue<HttpMessageHandler>();

        public static void AddMockHandler(HttpMessageHandler mockHandler)
        {
            MockHandlerQueue.Enqueue(mockHandler);
        }

        public static void InitializeMockProvider()
        {
            UseMocks = true;
            MockHandlerQueue.Clear();
        }

        public static int MockHandlersCount()
        {
            return MockHandlerQueue.Count;
        }

        public static bool UseMocks
        {
            get; set;
        }
    }
}