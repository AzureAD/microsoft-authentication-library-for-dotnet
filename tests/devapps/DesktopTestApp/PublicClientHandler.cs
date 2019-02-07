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
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;

namespace DesktopTestApp
{
    class PublicClientHandler
    {
        private readonly string _component = "DesktopTestApp";

        public PublicClientHandler(string clientId, LogCallback logCallback)
        {
            ApplicationId = clientId;
            PublicClientApplication = PublicClientApplicationBuilder.Create(ApplicationId)
                .WithComponent(_component)
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

        public async Task<AuthenticationResult> AcquireTokenInteractiveAsync(
            IEnumerable<string> scopes,
            Prompt uiBehavior,
            string extraQueryParams,
            UIParent uiParent)
        {
            CreateOrUpdatePublicClientApp(InteractiveAuthority, ApplicationId);

            AuthenticationResult result;
            if (CurrentUser != null)
            {
                result = await PublicClientApplication.AcquireTokenAsync(
                    scopes,
                    CurrentUser,
                    uiBehavior,
                    extraQueryParams,
                    uiParent).ConfigureAwait(false);
            }
            else
            {
                result = await PublicClientApplication.AcquireTokenAsync(
                    scopes,
                    LoginHint,
                    uiBehavior,
                    extraQueryParams,
                    uiParent).ConfigureAwait(false);
            }
            CurrentUser = result.Account;

            return result;
        }

        public async Task<AuthenticationResult> AcquireTokenInteractiveWithAuthorityAsync(
            IEnumerable<string> scopes,
            Prompt uiBehavior,
            string extraQueryParams,
            UIParent uiParent)
        {
            CreateOrUpdatePublicClientApp(InteractiveAuthority, ApplicationId);

            AuthenticationResult result;
            if (CurrentUser != null)
            {
                result = await PublicClientApplication.AcquireTokenAsync(
                    scopes,
                    CurrentUser,
                    uiBehavior,
                    extraQueryParams,
                    null,
                    AuthorityOverride,
                    uiParent).ConfigureAwait(false);
            }
            else
            {
                result = await PublicClientApplication.AcquireTokenAsync(
                    scopes,
                    LoginHint,
                    uiBehavior,
                    extraQueryParams,
                    null,
                    AuthorityOverride,
                    uiParent).ConfigureAwait(false);
            }

            CurrentUser = result.Account;
            return result;
        }

        public async Task<AuthenticationResult> AcquireTokenSilentAsync(IEnumerable<string> scopes, bool forceRefresh)
        {
            return await PublicClientApplication.AcquireTokenSilentAsync(
                scopes,
                CurrentUser,
                AuthorityOverride,
                forceRefresh).ConfigureAwait(false);
        }

        public void CreateOrUpdatePublicClientApp(string interactiveAuthority, string applicationId)
        {
            var builder = PublicClientApplicationBuilder.Create(ApplicationId)
                .WithComponent(_component);

            if (!string.IsNullOrWhiteSpace(interactiveAuthority))
            {
                // Use the override authority provided
                builder = builder.WithAuthority(new Uri(interactiveAuthority), true);

            }

            PublicClientApplication = builder.BuildConcrete();

            PublicClientApplication.UserTokenCache.SetBeforeAccess(TokenCacheHelper.BeforeAccessNotification);
            PublicClientApplication.UserTokenCache.SetAfterAccess(TokenCacheHelper.AfterAccessNotification);
        }
    }
}
