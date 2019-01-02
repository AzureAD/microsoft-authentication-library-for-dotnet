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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.CallConfig;
using Microsoft.Identity.Client.Config;

namespace DesktopTestApp
{
    internal class PublicClientHandler
    {
        private readonly string _component = "DesktopTestApp";

        public PublicClientHandler(string clientId)
        {
            ApplicationId = clientId;
            PublicClientApplication = PublicClientApplicationBuilder
                                      .Create(ApplicationId).WithUserTokenCache(TokenCacheHelper.GetUserCache())
                                      .WithAadAuthority(AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount, true, true)
                                      .WithComponent(_component).BuildConcrete();
        }

        public string ApplicationId { get; set; }
        public string InteractiveAuthority { get; set; }
        public string AuthorityOverride { get; set; }
        public string ExtraQueryParams { get; set; }
        public string LoginHint { get; set; }
        public IAccount CurrentUser { get; set; }
        public PublicClientApplication PublicClientApplication { get; set; }

        public async Task<AuthenticationResult> AcquireTokenInteractiveAsync(
            IEnumerable<string> scopes, 
            UIBehavior uiBehavior, 
            string extraQueryParams)
        {
            CreateOrUpdatePublicClientApp(InteractiveAuthority, ApplicationId);

            var builder = AcquireTokenInteractiveParameterBuilder.Create(scopes).WithUiBehavior(uiBehavior).WithExtraQueryParameters(extraQueryParams);
            builder = CurrentUser == null ? builder.WithLoginHint(LoginHint) : builder.WithAccount(CurrentUser);

            AuthenticationResult result = await PublicClientApplication
                           .AcquireTokenAsync(builder.Build(), CancellationToken.None).ConfigureAwait(false);

            CurrentUser = result.Account;
            return result;
        }

        public async Task<AuthenticationResult> AcquireTokenInteractiveWithAuthorityAsync(
            IEnumerable<string> scopes,
            UIBehavior uiBehavior,
            string extraQueryParams)
        {
            CreateOrUpdatePublicClientApp(InteractiveAuthority, ApplicationId);

            var builder = AcquireTokenInteractiveParameterBuilder
                          .Create(scopes).WithUiBehavior(uiBehavior).WithExtraQueryParameters(extraQueryParams)
                          .WithAuthorityOverride(AuthorityOverride);

            builder = CurrentUser == null ? builder.WithLoginHint(LoginHint) : builder.WithAccount(CurrentUser);

            AuthenticationResult result = await PublicClientApplication.AcquireTokenAsync(builder.Build(), CancellationToken.None).ConfigureAwait(false);

            CurrentUser = result.Account;
            return result;
        }

        public async Task<AuthenticationResult> AcquireTokenSilentAsync(IEnumerable<string> scopes, bool forceRefresh)
        {
            var builder = AcquireTokenSilentParameterBuilder
                          .Create(scopes, CurrentUser).WithAuthorityOverride(AuthorityOverride)
                          .WithForceRefresh(forceRefresh);

            return await PublicClientApplication.AcquireTokenSilentAsync(builder.Build(), CancellationToken.None)
                                                .ConfigureAwait(false);
        }

        public void CreateOrUpdatePublicClientApp(string interactiveAuthority, string applicationId)
        {
            if (string.IsNullOrWhiteSpace(interactiveAuthority))
            {
                // Use default authority
                PublicClientApplication = PublicClientApplicationBuilder
                                          .Create(ApplicationId).WithUserTokenCache(TokenCacheHelper.GetUserCache())
                                          .WithComponent(_component).BuildConcrete();
            }
            else
            {
                // Use the override authority provided
                PublicClientApplication = PublicClientApplicationBuilder
                                          .Create(ApplicationId).WithUserTokenCache(TokenCacheHelper.GetUserCache())
                                          .WithComponent(_component).WithAuthority(interactiveAuthority, true, true).BuildConcrete();
            }
        }
    }
}
