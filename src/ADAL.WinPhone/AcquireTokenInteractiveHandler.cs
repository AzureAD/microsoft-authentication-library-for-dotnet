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
using Windows.Security.Authentication.Web;
using Windows.Storage;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal partial class AcquireTokenInteractiveHandler
    {
        // This constructor is called by ContinueAcquireTokenAsync after WAB call has returned.
        public AcquireTokenInteractiveHandler(Authenticator authenticator, TokenCache tokenCache, IWebAuthenticationBrokerContinuationEventArgs args)
            : this(
                authenticator, 
                tokenCache, 
                (string)args.ContinuationData[WabArgName.Resource], 
                (string)args.ContinuationData[WabArgName.ClientId],
                GetRedirectUri((string)args.ContinuationData[WabArgName.RedirectUri]),	// Issue #129 - Windows Phone cannot handle ms-app URI's so use the placeholder URI for SSO
                PromptBehavior.Always,  // This is simply to disable cache lookup. In fact, there is no authorize call at this point and promptBehavior is not applicable.
                new UserIdentifier((string)args.ContinuationData[WabArgName.UserId],
                    (UserIdentifierType)((int)args.ContinuationData[WabArgName.UserIdType])),
                null, 
                NetworkPlugin.WebUIFactory.Create(), 
                false)
        {
            CallState callState = new CallState(new Guid((string)args.ContinuationData[WabArgName.CorrelationId]), false);
            this.authorizationResult = this.webUi.ProcessAuthorizationResult(args, callState);
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
            payload[WabArgName.RedirectUri] = this.redirectUriRequestParameter;
            payload[WabArgName.UserId] = userId.Id;
            payload[WabArgName.UserIdType] = (int)userId.Type;
            payload[WabArgName.Resource] = this.Resource;
            payload[WabArgName.ClientId] = this.ClientKey.ClientId;

            webUi.Authenticate(authorizationUri, this.redirectUri, payload, this.CallState);
        }

        private static Uri GetRedirectUri(string url)
        {
            if (url.StartsWith(Constant.MsAppScheme, StringComparison.OrdinalIgnoreCase))
            {
                return Constant.SsoPlaceHolderUri;
            }

            return new Uri(url);
        }

        private void SetRedirectUriRequestParameter()
        {
            if (ReferenceEquals(this.redirectUri, Constant.SsoPlaceHolderUri))
            {
                try
                {
                    this.redirectUriRequestParameter = WebAuthenticationBroker.GetCurrentApplicationCallbackUri().AbsoluteUri;
                }
                catch (FormatException ex)
                {
                    // This is the workaround for a bug in managed Uri class of WinPhone SDK which makes it throw UriFormatException when it gets called from unmanaged code. 
                    const string CurrentApplicationCallbackUriSetting = "CurrentApplicationCallbackUri";
                    if (ApplicationData.Current.LocalSettings.Values.ContainsKey(CurrentApplicationCallbackUriSetting))
                    {
                        this.redirectUriRequestParameter = (string)ApplicationData.Current.LocalSettings.Values[CurrentApplicationCallbackUriSetting];
                    }
                    else
                    {
                        throw new AdalException(AdalError.NeedToSetCallbackUriAsLocalSetting, AdalErrorMessage.NeedToSetCallbackUriAsLocalSetting, ex);
                    }
                }
            }
            else
            {
                this.redirectUriRequestParameter = redirectUri.AbsoluteUri;                
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
