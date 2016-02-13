//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Accounts;
using Android.App;
using Android.OS;
using Java.Util.Concurrent;
using Android.Content.PM;
using Java.Security;
using Java.IO;
using Android.Util;
using System.Collections.Generic;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class WebUI : IWebUI
    {

        private IAccountManagerFuture accountManagerFeature;

        private static SemaphoreSlim returnedUriReady;
        private static AuthorizationResult authorizationResult;
        private readonly PlatformParameters parameters;

        public WebUI(IPlatformParameters parameters)
        {
            this.parameters = parameters as PlatformParameters;
            if (this.parameters == null)
            {
                throw new ArgumentException("parameters should be of type PlatformParameters", "parameters");
            }
        }

        public async Task<AuthorizationResult> AcquireAuthorizationAsync(Uri authorizationUri, Uri redirectUri, IDictionary<string, string> additionalHeaders, CallState callState)
        {
            returnedUriReady = new SemaphoreSlim(0);

            try
            {
                var agentIntent = new Intent(this.parameters.CallerActivity, typeof(AuthenticationAgentActivity));
                agentIntent.PutExtra("Url", authorizationUri.AbsoluteUri);
                agentIntent.PutExtra("Callback", redirectUri.AbsoluteUri);
                this.parameters.CallerActivity.StartActivityForResult(agentIntent, 0);
            }
            catch (Exception ex)
            {
                throw new MsalException(MsalError.AuthenticationUiFailed, ex);
            }

            await returnedUriReady.WaitAsync().ConfigureAwait(false);
            return authorizationResult;
        }

        public static void SetAuthorizationResult(AuthorizationResult authorizationResultInput)
        {
            authorizationResult = authorizationResultInput;
            returnedUriReady.Release();
        }
    }
}
