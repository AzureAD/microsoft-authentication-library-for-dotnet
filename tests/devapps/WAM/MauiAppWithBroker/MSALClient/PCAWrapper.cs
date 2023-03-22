// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Abstractions;

namespace MauiAppWithBroker.MSALClient
{
    /// <summary>
    /// This is a wrapper for PublicClientApplication. It is singleton.
    /// </summary>
    public class PCAWrapper
    {

        /// <summary>
        /// This is the singleton used by ux. Since PCAWrapper constructor does not have perf or memory issue, it is instantiated directly.
        /// </summary>
        public static PCAWrapper Instance { get; } = new PCAWrapper();

        /// <summary>
        /// Instance of PublicClientApplication. It is provided, if App wants more customization.
        /// </summary>
        internal IPublicClientApplication PCA { get; }

        // private constructor for singleton
        private PCAWrapper()
        {
            // Create PublicClientApplication once. Make sure that all the config parameters below are passed
            PCA = PublicClientApplicationBuilder
                                        .Create(AppConstants.ClientId)
                                        .WithTenantId(AppConstants.TenantId)
                                        .WithExperimentalFeatures() // this is for upcoming logger
                                        .WithLogging(_logger)
                                        .WithBroker()
                                        .WithRedirectUri(PlatformConfig.Instance.RedirectUri)
                                        .WithIosKeychainSecurityGroup("com.microsoft.adalcache")
                                        .Build();
        }

        /// <summary>
        /// Acquire the token silently
        /// </summary>
        /// <param name="scopes">desired scopes</param>
        /// <returns>Authentication result</returns>
        internal async Task<AuthenticationResult> AcquireTokenSilentAsync(string[] scopes)
        {
            var accts = await PCA.GetAccountsAsync().ConfigureAwait(false);
            var acct = accts.FirstOrDefault();

            var silentParamBuilder = PCA.AcquireTokenSilent(scopes, acct);
            var authResult = await silentParamBuilder
                                        .ExecuteAsync().ConfigureAwait(false);
            return authResult;

        }

        /// <summary>
        /// Perform the interactive acquisition of the token for the given scope
        /// </summary>
        /// <param name="scopes">desired scopes</param>
        /// <returns></returns>
        internal async Task<AuthenticationResult> AcquireTokenInteractiveAsync(string[] scopes)
        {
            return await PCA.AcquireTokenInteractive(scopes)
                                    .WithParentActivityOrWindow(PlatformConfig.Instance.ParentWindow)
                                    .ExecuteAsync()
                                    .ConfigureAwait(false);
        }

        /// <summary>
        /// Signout may not perform the complete signout as company portal may hold
        /// the token.
        /// </summary>
        /// <returns></returns>
        internal async Task SignOutAsync()
        {
            var accounts = await PCA.GetAccountsAsync().ConfigureAwait(false);
            foreach (var acct in accounts)
            {
                await PCA.RemoveAsync(acct).ConfigureAwait(false);
            }
        }

        // Custom logger for sample
        private MyLogger _logger = new MyLogger();

        // Custom logger class
        private class MyLogger : IIdentityLogger
        {
            /// <summary>
            /// Checks if log is enabled or not based on the Entry level
            /// </summary>
            /// <param name="eventLogLevel"></param>
            /// <returns></returns>
            public bool IsEnabled(EventLogLevel eventLogLevel)
            {
                //Try to pull the log level from an environment variable
                var msalEnvLogLevel = Environment.GetEnvironmentVariable("MSAL_LOG_LEVEL");

                EventLogLevel envLogLevel = EventLogLevel.Informational;
                Enum.TryParse<EventLogLevel>(msalEnvLogLevel, out envLogLevel);

                return envLogLevel <= eventLogLevel;
            }

            /// <summary>
            /// Log to console for demo purpose
            /// </summary>
            /// <param name="entry">Log Entry values</param>
            public void Log(LogEntry entry)
            {
                Console.WriteLine(entry.Message);
            }
        }

    }
}
