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
using System.Threading.Tasks;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Helpers;
using Microsoft.Identity.Core.Http;
using Microsoft.Identity.Core.OAuth2;
using Microsoft.Identity.Core.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Common.Mocks
{
    internal class MockWebUI : IWebUI
    {
        public MockWebUI()
        {
            AddStateInAuthorizationResult = true;
        }

        public Exception ExceptionToThrow { get; set; }

        public AuthorizationResult MockResult { get; set; }

        public IDictionary<string, string> QueryParamsToValidate { get; set; }

        public bool AddStateInAuthorizationResult { get; set; }

        public RequestContext RequestContext { get; set; }

        public string Environment { get; set; }

        public async Task<AuthorizationResult> AcquireAuthorizationAsync(Uri authorizationUri, Uri redirectUri, RequestContext requestContext)
        {
            if (ExceptionToThrow != null)
            {
                throw ExceptionToThrow;
            }

            if (Environment != null)
            {
                Assert.AreEqual(Environment, authorizationUri.Host);
            }

            IDictionary<string, string> inputQp = CoreHelpers.ParseKeyValueList(authorizationUri.Query.Substring(1), '&', true, null);
            Assert.IsNotNull(inputQp[OAuth2Parameter.State]);
            if (AddStateInAuthorizationResult)
            {
                MockResult.State = inputQp[OAuth2Parameter.State];
            }

            //match QP passed in for validation. 
            if (QueryParamsToValidate != null)
            {
                Assert.IsNotNull(authorizationUri.Query);
                foreach (var key in QueryParamsToValidate.Keys)
                {
                    Assert.IsTrue(inputQp.ContainsKey(key));
                    Assert.AreEqual(QueryParamsToValidate[key], inputQp[key]);
                }
            }

            return await Task.Factory.StartNew(() => MockResult).ConfigureAwait(false);
        }

        public void ValidateRedirectUri(Uri redirectUri)
        {
            RedirectUriHelper.Validate(redirectUri);
        }
    }
}
