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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace DesktopTestApp
{
    class PublicClientHandler
    {
        private readonly string _clientName = "DesktopTestApp";

        private readonly string _b2cAuthorityHost = "msidlabb2c.b2clogin.com";
        private readonly string _b2cTenantId = "msidlabb2c.onmicrosoft.com";
        private readonly string _b2CClientId = "e3b9ad76-9763-4827-b088-80c7a7888f79";

        private readonly string _customDomainAuthorityHost = "public.msidlabb2c.com";
        private readonly string _customDomainTenantId = "public.msidlabb2c.com";
        private readonly string _b2CCustomDomainClientId = "64a88201-6bbd-49f5-ab46-9153798493fd ";

        public bool UseB2CAuthorityHost { get; set; } = false;
        public bool UseB2CCustomDomain { get; set; } = false;

        public PublicClientHandler(string clientId, LogCallback logCallback)
        {
            ApplicationId = clientId;
            PublicClientApplication = PublicClientApplicationBuilder.Create(ApplicationId)
                .WithLogging(logCallback, LogLevel.Verbose, true)
#if ARIA_TELEMETRY_ENABLED
                .WithTelemetry((new Microsoft.Identity.Client.AriaTelemetryProvider.ServerTelemetryHandler()).OnEvents)
#endif
                .BuildConcrete();


            CreateOrUpdatePublicClientApp(InteractiveAuthority, ApplicationId);
        }

        public string ApplicationId { get; set; }
        public string InteractiveAuthority { get; set; }
        public string AuthorityOverride { get; set; }
        public string ExtraQueryParams { get; set; }
        public string LoginHint { get; set; }
        public IAccount CurrentUser { get; set; }
        public PublicClientApplication PublicClientApplication { get; set; }
        public string B2CPolicy { get; set; }

        public async Task<AuthenticationResult> AcquireTokenInteractiveAsync(
            IEnumerable<string> scopes,
            Prompt uiBehavior,
            string extraQueryParams)
        {
            CreateOrUpdatePublicClientApp(InteractiveAuthority, ApplicationId);

            AcquireTokenInteractiveParameterBuilder request;
            AuthenticationResult result;
            if (CurrentUser != null)
            {
                request = PublicClientApplication
                    .AcquireTokenInteractive(scopes, null)
                    .WithAccount(CurrentUser)
                    .WithPrompt(uiBehavior)
                    .WithExtraQueryParameters(extraQueryParams);

                if (UseB2CAuthorityHost || UseB2CCustomDomain)
                {
                    request.WithB2CPolicy(B2CPolicy);
                }
                result = await request
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }
            else
            {
                request = PublicClientApplication
                    .AcquireTokenInteractive(scopes, null)
                    .WithLoginHint(LoginHint)
                    .WithPrompt(uiBehavior)
                    .WithExtraQueryParameters(extraQueryParams);

                if (UseB2CAuthorityHost || UseB2CCustomDomain)
                {
                    request.WithB2CPolicy(B2CPolicy);
                }

                result = await request
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }
            CurrentUser = result.Account;

            return result;
        }

        public async Task<AuthenticationResult> AcquireTokenInteractiveWithAuthorityAsync(
            IEnumerable<string> scopes,
            Prompt uiBehavior,
            string extraQueryParams)
        {
            CreateOrUpdatePublicClientApp(InteractiveAuthority, ApplicationId);

            AuthenticationResult result;
            if (CurrentUser != null)
            {
                result = await PublicClientApplication
                    .AcquireTokenInteractive(scopes, null)
                    .WithAccount(CurrentUser)
                    .WithPrompt(uiBehavior)
                    .WithExtraQueryParameters(extraQueryParams)
                    .WithAuthority(AuthorityOverride)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }
            else
            {
                result = await PublicClientApplication
                    .AcquireTokenInteractive(scopes, null)
                    .WithLoginHint(LoginHint)
                    .WithPrompt(uiBehavior)
                    .WithExtraQueryParameters(extraQueryParams)
                    .WithAuthority(AuthorityOverride)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }

            CurrentUser = result.Account;
            return result;
        }

        public async Task<AuthenticationResult> AcquireTokenSilentAsync(IEnumerable<string> scopes, bool forceRefresh)
        {
            var builder = PublicClientApplication
                .AcquireTokenSilent(scopes, CurrentUser)
                .WithForceRefresh(forceRefresh);

            if (!string.IsNullOrWhiteSpace(AuthorityOverride))
            {
                builder = builder.WithAuthority(AuthorityOverride);
            }

            return await builder.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<AuthenticationResult> AcquireTokenInteractiveWithB2CAuthorityAsync(
          IEnumerable<string> scopes,
          Prompt uiBehavior,
          string extraQueryParams,
          string b2cAuthority)
        {
            CreateOrUpdatePublicClientApp(b2cAuthority, ApplicationId);
            AuthenticationResult result;
            result = await PublicClientApplication
                   .AcquireTokenInteractive(scopes, null)
                   .WithAccount(CurrentUser)
                   .WithPrompt(uiBehavior)
                   .WithExtraQueryParameters(extraQueryParams)
                   .WithB2CPolicy(B2CPolicy)
                   .ExecuteAsync(CancellationToken.None)
                   .ConfigureAwait(false);

            CurrentUser = result.Account;
            return result;
        }

        public void CreateOrUpdatePublicClientApp(string interactiveAuthority, string applicationId)
        {
            if (UseB2CAuthorityHost || UseB2CCustomDomain)
            {
                CreateB2CClientApp();
            }
            else
            {
                var builder = PublicClientApplicationBuilder
                .Create(ApplicationId)
                .WithClientName(_clientName);

                if (!string.IsNullOrWhiteSpace(interactiveAuthority))
                {
                    // Use the override authority provided
                    builder.WithAuthority(new Uri(interactiveAuthority), true);
                }
                PublicClientApplication = builder.BuildConcrete();
            }

            PublicClientApplication.UserTokenCache.SetBeforeAccess(TokenCacheHelper.BeforeAccessNotification);
            PublicClientApplication.UserTokenCache.SetAfterAccess(TokenCacheHelper.AfterAccessNotification);
        }

        private void CreateB2CClientApp()
        {
            var builder = PublicClientApplicationBuilder
                .Create(DetermineClientIdForB2C())
                .WithClientName(_clientName);
            if (UseB2CCustomDomain)
            {
                builder.WithB2CHost(_customDomainAuthorityHost, _customDomainTenantId);
            }
            else
            {
                builder.WithB2CHost(_b2cAuthorityHost, _b2cTenantId);
            }
            PublicClientApplication = builder.BuildConcrete();
        }

        private string DetermineClientIdForB2C()
        {
            if (UseB2CCustomDomain)
            {
                return _b2CCustomDomainClientId;
            }
            return _b2CClientId;
        }
    }
}
