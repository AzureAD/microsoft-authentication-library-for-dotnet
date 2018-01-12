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
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.Microsoft.Identity.Unit.Mocks
{
    internal class MockHttpMessageHandler : HttpMessageHandler
    {
        public HttpResponseMessage ResponseMessage { get; set; }

        public string Url { get; set; }

        public IDictionary<string, string> QueryParams { get; set; }

        public IDictionary<string, string> PostData { get; set; }

        public IDictionary<string, string> Headers { get; set; }

        public HttpMethod Method { get; set; }

        public Exception ExceptionToThrow { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {

            if (ExceptionToThrow != null)
            {
                throw ExceptionToThrow;
            }

            Assert.AreEqual(Method, request.Method);

            Uri uri = request.RequestUri;
            if (!string.IsNullOrEmpty(Url))
            {
                Assert.AreEqual(Url, uri.AbsoluteUri.Split(new[] { '?' })[0]);
            }

            //match QP passed in for validation. 
            if (QueryParams != null)
            {
                Assert.IsFalse(string.IsNullOrEmpty(uri.Query),
                    string.Format(CultureInfo.InvariantCulture,
                        "provided url ({0}) does not contain query parameters, as expected", uri.AbsolutePath));
                IDictionary<string, string> inputQp = CoreHelpers.ParseKeyValueList(uri.Query.Substring(1), '&', false, null);
                foreach (var key in QueryParams.Keys)
                {
                    Assert.IsTrue(inputQp.ContainsKey(key),
                    string.Format(CultureInfo.InvariantCulture,
                        "expected QP ({0}) not found in the url ({1})", key, uri.AbsolutePath));
                    Assert.AreEqual(QueryParams[key], inputQp[key]);
                }
            }

            //match QP passed in for validation. 
            if (QueryParams != null)
            {
                Assert.IsFalse(string.IsNullOrEmpty(uri.Query));
                IDictionary<string, string> inputQp = CoreHelpers.ParseKeyValueList(uri.Query.Substring(1), '&', false, null);
                foreach (var key in QueryParams.Keys)
                {
                    Assert.IsTrue(inputQp.ContainsKey(key));
                    Assert.AreEqual(QueryParams[key], inputQp[key]);
                }
            }

            if (PostData != null)
            {
                string postData = request.Content.ReadAsStringAsync().Result;
                Dictionary<string, string> requestPostDataPairs = CoreHelpers.ParseKeyValueList(postData, '&', true, null);

                foreach (var key in PostData.Keys)
                {
                    Assert.IsTrue(requestPostDataPairs.ContainsKey(key));
                    Assert.AreEqual(PostData[key], requestPostDataPairs[key]);
                }
            }

            return new TaskFactory().StartNew(() => ResponseMessage, cancellationToken);
        }
    }
}
