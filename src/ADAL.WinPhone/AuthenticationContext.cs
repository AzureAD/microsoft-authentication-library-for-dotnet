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
using System.Threading.Tasks;

using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Security.Authentication.Web;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// Optional delegate that can be defined by the developer to process the result of acquire token requests.
    /// </summary>
    /// <param name="result">Authentication result instance received from acquire token call</param>
    public delegate void AuthenticationContextDelegate(AuthenticationResult result);

    /// <summary>
    /// The main class representing the authority issuing tokens for resources.
    /// </summary>
    public sealed partial class AuthenticationContext
    {
        private AuthenticationContextDelegate authenticationContextDelegate;

        private AuthenticationContext()
        {
        }

        /// <summary>
        /// Factory method to create the context with the address of the authority.
        /// Using this constructor will turn ON validation of the authority URL by default if validation is supported for the authority address.
        /// </summary>
        /// <param name="authority">Address of the authority to issue token.</param>
        /// <returns> IAsyncOperation representing the instance of the class.</returns>
        public static IAsyncOperation<AuthenticationContext> CreateAsync(string authority)
        {
            return RunTaskAsAsyncOperation(UpdateAuthenticatorFromTemplateAsync(new AuthenticationContext(authority)));
        }

        /// <summary>
        /// Factory method to create the context with the address of the authority and flag to turn address validation off.
        /// Using this constructor, address validation can be turned off. Make sure you are aware of the security implication of not validating the address.
        /// </summary>
        /// <param name="authority">Address of the authority to issue token.</param>
        /// <param name="validateAuthority">Flag to turn address validation ON or OFF.</param>
        /// <returns> IAsyncOperation representing the instance of the class.</returns>
        [DefaultOverload]
        public static IAsyncOperation<AuthenticationContext> CreateAsync(string authority, bool validateAuthority)
        {
            return RunTaskAsAsyncOperation(UpdateAuthenticatorFromTemplateAsync(new AuthenticationContext(authority, validateAuthority)));
        }

        /// <summary>
        /// Factory method to create the context with the address of the authority and flag to turn address validation off.
        /// Using this constructor, address validation can be turned off. Make sure you are aware of the security implication of not validating the address.
        /// </summary>
        /// <param name="authority">Address of the authority to issue token.</param>
        /// <param name="validateAuthority">Flag to turn address validation ON or OFF.</param>
        /// <param name="tokenCache">Token cache used to lookup cached tokens on calls to AcquireToken</param>
        /// <returns> IAsyncOperation representing the instance of the class.</returns>
        public static IAsyncOperation<AuthenticationContext> CreateAsync(string authority, bool validateAuthority, TokenCache tokenCache)
        {
            return RunTaskAsAsyncOperation(UpdateAuthenticatorFromTemplateAsync(new AuthenticationContext(authority, validateAuthority, tokenCache)));
        }

        /// <summary>
        /// Factory method to create the context with the address of the authority and flag to turn address validation off.
        /// Using this constructor, address validation can be turned off. Make sure you are aware of the security implication of not validating the address.
        /// </summary>
        /// <param name="authority">Address of the authority to issue token.</param>
        /// <param name="validateAuthority">Flag to turn address validation ON or OFF.</param>
        /// <param name="tokenCache">Token cache used to lookup cached tokens on calls to AcquireToken</param>
        /// <param name="correlationId">Correlation Id which would be sent to the service with the next request</param>
        /// <returns> IAsyncOperation representing the instance of the class.</returns>
        public static IAsyncOperation<AuthenticationContext> CreateAsync(string authority, bool validateAuthority, TokenCache tokenCache, Guid correlationId)
        {
            return RunTaskAsAsyncOperation(UpdateAuthenticatorFromTemplateAsync(new AuthenticationContext(authority, validateAuthority, tokenCache) { CorrelationId = correlationId }));
        }

        /// <summary>
        /// Starts security token acquisition from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority. Pass null or application's callback URI for SSO mode.</param>
        /// <param name="authDelegate">Optional delegate that can be passed by the developer to process authentication result.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        [DefaultOverload]
        public void AcquireTokenAndContinue(string resource, string clientId, Uri redirectUri, AuthenticationContextDelegate authDelegate)
        {
            this.AcquireTokenAndContinueCommon(resource, clientId, redirectUri ?? Constant.SsoPlaceHolderUri, UserIdentifier.AnyUser, null, authDelegate);
        }

        /// <summary>
        /// Starts security token acquisition from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority. Pass null or application's callback URI for SSO mode.</param>
        /// <param name="userId">Identifier of the user token is requested for. If created from DisplayableId, this parameter will be used to pre-populate the username field in the authentication form. Please note that the end user can still edit the username field and authenticate as a different user. 
        /// If you want to be notified of such change with an exception, create UserIdentifier with type RequiredDisplayableId. This parameter can be <see cref="UserIdentifier"/>.Any.</param>
        /// <param name="authDelegate">Optional delegate that can be passed by the developer to process authentication result.</param>        
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        [DefaultOverload]
        public void AcquireTokenAndContinue(string resource, string clientId, Uri redirectUri, UserIdentifier userId, AuthenticationContextDelegate authDelegate)
        {
            this.AcquireTokenAndContinueCommon(resource, clientId, redirectUri ?? Constant.SsoPlaceHolderUri, userId, null, authDelegate);
        }

        /// <summary>
        /// Acquires security token from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority. Pass null or application's callback URI for SSO mode.</param>
        /// <param name="userId">Identifier of the user token is requested for. If created from DisplayableId, this parameter will be used to pre-populate the username field in the authentication form. Please note that the end user can still edit the username field and authenticate as a different user. 
        /// If you want to be notified of such change with an exception, create UserIdentifier with type RequiredDisplayableId. This parameter can be <see cref="UserIdentifier"/>.Any.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <param name="authDelegate">Optional delegate that can be passed by the developer to process authentication result.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public void AcquireTokenAndContinue(string resource, string clientId, Uri redirectUri, UserIdentifier userId, string extraQueryParameters, AuthenticationContextDelegate authDelegate)
        {
            this.AcquireTokenAndContinueCommon(resource, clientId, redirectUri ?? Constant.SsoPlaceHolderUri, userId, extraQueryParameters, authDelegate);
        }

        /// <summary>
        /// Continues security token acquisition from the authority.
        /// </summary>
        /// <param name="args">Information to an app that was launched after being suspended for a web authentication broker operation.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public IAsyncOperation<AuthenticationResult> ContinueAcquireTokenAsync(IWebAuthenticationBrokerContinuationEventArgs args)
        {
            var handler = new AcquireTokenInteractiveHandler(this.Authenticator, this.TokenCache, args);
            return this.RunInteractiveHandlerAsync(handler).AsAsyncOperation();
        }

        private async Task<AuthenticationResult> RunInteractiveHandlerAsync(AcquireTokenInteractiveHandler handler)
        {
            AuthenticationResult result = await RunTask(handler.RunAsync());
            
            // Execute callback 
            if (this.authenticationContextDelegate != null)
            {
                var dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
                await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => this.authenticationContextDelegate(result));
                this.authenticationContextDelegate = null;
            }

            return result;
        }

        private void AcquireTokenAndContinueCommon(string resource, string clientId, Uri redirectUri, UserIdentifier userId, string extraQueryParameters, AuthenticationContextDelegate authDelegate)
        {
            IWebUI webUi = this.CreateWebAuthenticationDialog();
            var handler = new AcquireTokenInteractiveHandler(this.Authenticator, this.TokenCache, resource, clientId, redirectUri, webUi.PromptBehavior, userId, extraQueryParameters, webUi, false);
            handler.AcquireAuthorization();
            this.authenticationContextDelegate = authDelegate;
        }

        private static IAsyncOperation<AuthenticationContext> RunTaskAsAsyncOperation(Task<AuthenticationContext> task)
        {
            return task.AsAsyncOperation();
        }

        private async static Task<AuthenticationContext> UpdateAuthenticatorFromTemplateAsync(AuthenticationContext context)
        {
            await context.Authenticator.UpdateFromTemplateAsync(AcquireTokenHandlerBase.CreateCallState(context.CorrelationId, false));
            return context;
        }

        private IWebUI CreateWebAuthenticationDialog()
        {
            return NetworkPlugin.WebUIFactory.Create();
        }
    }
}