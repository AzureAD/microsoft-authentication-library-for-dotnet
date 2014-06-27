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
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    static partial class OAuth2Request
    {
        public static void SendAuthorizeRequest(Authenticator authenticator, string resource, Uri redirectUri, string clientId, string userId, string extraQueryParameters, IWebUI webUI, CallState callState)//, string EventKey)
        {
            if (!string.IsNullOrWhiteSpace(redirectUri.Fragment))
            {
                throw new ArgumentException(ActiveDirectoryAuthenticationErrorMessage.RedirectUriContainsFragment, "redirectUri");
            }

            IDictionary<string, object> payload = new Dictionary<string, object>();
            payload["correlation_id"] = callState.CorrelationId.ToString();
            payload["redirect_uri"] = redirectUri.AbsoluteUri;
            payload["user_id"] = userId;
            payload["resource"] = resource;
            payload["client_id"] = clientId;

            Uri authorizationUri = CreateAuthorizationUri(authenticator, resource, redirectUri, clientId, userId, extraQueryParameters, callState);
            webUI.Authenticate(authorizationUri, redirectUri, callState, payload);
        }
    }
}