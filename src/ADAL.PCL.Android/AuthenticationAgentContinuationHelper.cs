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

using Android.App;
using Android.Content;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    public static class AuthenticationAgentContinuationHelper
    {
        public static void SetAuthenticationAgentContinuationEventArgs(int requestCode, Result resultCode, Intent data)
        {
            AuthorizationResult authorizationResult;
            switch (resultCode)
            {
                case Result.Ok: authorizationResult= new AuthorizationResult(AuthorizationStatus.Success, data.GetStringExtra("ReturnedUrl")); break;
                case Result.Canceled: authorizationResult = new AuthorizationResult(AuthorizationStatus.UserCancel, null); break;
                default: authorizationResult = new AuthorizationResult(AuthorizationStatus.UnknownError, null); break;
            }

            WebUI.SetAuthorizationResult(authorizationResult);
        }
    }
}
