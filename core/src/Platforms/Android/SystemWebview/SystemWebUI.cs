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
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Microsoft.Identity.Client;
using Microsoft.Identity.Core;
using Uri = System.Uri;

namespace Microsoft.Identity.Core.UI.SystemWebview
{
    [Android.Runtime.Preserve(AllMembers = true)]
    internal class SystemWebUI : WebviewBase
    {
        private readonly CoreUIParent _parent;

        public SystemWebUI(CoreUIParent parent)
        {
            _parent = parent;
        }

        public RequestContext RequestContext { get; set; }

        public async override Task<AuthorizationResult> AcquireAuthorizationAsync(Uri authorizationUri, Uri redirectUri, RequestContext requestContext)
        {
            returnedUriReady = new SemaphoreSlim(0);

            try
            {
                var agentIntent = new Intent(_parent.Activity, typeof(AuthenticationActivity));
                agentIntent.PutExtra(AndroidConstants.RequestUrlKey, authorizationUri.AbsoluteUri);
                agentIntent.PutExtra(AndroidConstants.CustomTabRedirect, redirectUri.OriginalString);
                AuthenticationActivity.RequestContext = RequestContext;
                _parent.Activity.RunOnUiThread(()=> _parent.Activity.StartActivityForResult(agentIntent, 0));
            }
            catch (Exception ex)
            {
                requestContext.Logger.Error(ex);
                requestContext.Logger.ErrorPii(ex);
                throw CoreExceptionService.Instance.GetClientException(
                    CoreErrorCodes.AuthenticationUiFailedError, 
                    "AuthenticationActivity failed to start", 
                    ex);
            }

            await returnedUriReady.WaitAsync().ConfigureAwait(false);
            return authorizationResult;
        }
    }
}