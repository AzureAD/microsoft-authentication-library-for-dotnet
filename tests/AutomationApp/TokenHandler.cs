// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace AutomationApp
{
    public class TokenHandler
    {
        #region Properties
        public IAccount CurrentUser { get; set; }
        private IPublicClientApplication _publicClientApplication;
        private readonly AppLogger _appLogger;
        #endregion

        internal TokenHandler(AppLogger appLogger)
        {
            _appLogger = appLogger;
        }

        private void EnsurePublicClientApplication(Dictionary<string, string> input)
        {
            if (_publicClientApplication != null)
            {
                return;
            }

            if (!input.ContainsKey("client_id"))
            {
                return;
            }

            var builder = PublicClientApplicationBuilder
                .Create(input["client_id"])
                .WithLogging(_appLogger.Log);

#if ARIA_TELEMETRY_ENABLED
            builder.WithTelemetry(new Microsoft.Identity.Client.AriaTelemetryProvider.ServerTelemetryHandler()).OnEvents);
#endif

            if (input.ContainsKey("authority"))
            {
                builder.WithAuthority(new Uri(input["authority"]));
            }

            _publicClientApplication = builder.Build();
        }

        public async Task<AuthenticationResult> AcquireTokenAsync(Dictionary<string, string> input)
        {
            EnsurePublicClientApplication(input);

            string[] scope = { "mail.read" };

            AuthenticationResult result = await _publicClientApplication
                .AcquireTokenInteractive(scope)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            CurrentUser = result.Account;
            return result;
        }

        public async Task<AuthenticationResult> AcquireTokenSilentAsync(Dictionary<string, string> input)
        {
            EnsurePublicClientApplication(input);

            string[] scope = { "mail.read" };

            AuthenticationResult result = await _publicClientApplication
                .AcquireTokenSilentWithAccount(scope, CurrentUser)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            return result;
        }

        public void ExpireAccessToken(Dictionary<string, string> input)
        {
            EnsurePublicClientApplication(input);

            _publicClientApplication.RemoveAsync(CurrentUser);
        }
    }
}
