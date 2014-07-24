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
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal partial class AcquireTokenInteractiveHandler
    {
        private readonly AuthenticationContextDelegate authenticationContextDelegate;

        public AcquireTokenInteractiveHandler(Authenticator authenticator, TokenCache tokenCache, AuthenticationContextDelegate authenticationContextDelegate, IWebAuthenticationBrokerContinuationEventArgs args)
            : this(
                authenticator, 
                tokenCache, 
                (string)args.ContinuationData[WabArgName.Resource], 
                (string)args.ContinuationData[WabArgName.ClientId],
                new Uri((string)args.ContinuationData[WabArgName.RedirectUri]), 
                PromptBehavior.RefreshSession,
                new UserIdentifier((string)args.ContinuationData[WabArgName.UserId],
                    (UserIdentifierType)((int)args.ContinuationData[WabArgName.UserIdType])),
                null, 
                NetworkPlugin.WebUIFactory.Create(), 
                false)
        {
            CallState callState = new CallState(new Guid((string)args.ContinuationData[WabArgName.CorrelationId]), false);
            this.authorizationResult = this.webUi.ProcessAuthorizationResult(args, callState);
            this.authenticationContextDelegate = authenticationContextDelegate;
        }

        protected override Task PreTokenRequest()
        {
            base.PreTokenRequest();
            this.VerifyAuthorizationResult();

            return CompletedTask;
        }

        internal void AcquireAuthorization()
        {
            Uri authorizationUri = this.CreateAuthorizationUri(false);

            IDictionary<string, object> payload = new Dictionary<string, object>();
            payload[WabArgName.CorrelationId] = this.CallState.CorrelationId.ToString();
            payload[WabArgName.RedirectUri] = redirectUri.AbsoluteUri;
            payload[WabArgName.UserId] = userId.Id;
            payload[WabArgName.UserIdType] = (int)userId.Type;
            payload[WabArgName.Resource] = this.Resource;
            payload[WabArgName.ClientId] = this.ClientKey.ClientId;

            webUi.Authenticate(authorizationUri, this.redirectUri, payload, this.CallState);
        }

        protected override async Task PostRunAsync(AuthenticationResult result)
        {
            await base.PostRunAsync(result);
            // Execute callback 
            if (this.authenticationContextDelegate != null)
            {
                var dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
                await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => this.authenticationContextDelegate(result));
            }
        }

        private static class WabArgName
        {
            public const string Resource = "resource";
            public const string ClientId = "client_id";
            public const string RedirectUri = "redirect_uri";
            public const string UserId = "user_id";
            public const string UserIdType = "user_id_type";
            public const string CorrelationId = "correlation_id";
        }
    }
}
