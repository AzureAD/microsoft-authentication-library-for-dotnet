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

using System;
using System.Threading;
using System.Threading.Tasks;
using SafariServices;
using Foundation;

namespace Microsoft.Identity.Core.UI
{
    internal abstract class WebviewBase : NSObject, IWebUI, ISFSafariViewControllerDelegate
    {
        protected static SemaphoreSlim returnedUriReady;
        protected static AuthorizationResult authorizationResult;
        protected SFSafariViewController safariViewController;

        public abstract Task<AuthorizationResult> AcquireAuthorizationAsync(Uri authorizationUri, Uri redirectUri,
            RequestContext requestContext);

        public static bool ContinueAuthentication(string url)
        {
            if (returnedUriReady == null)
            {
                return false;
            }

            authorizationResult = new AuthorizationResult(AuthorizationStatus.Success, url);
            returnedUriReady.Release();
            return true;
        }
    }
}
