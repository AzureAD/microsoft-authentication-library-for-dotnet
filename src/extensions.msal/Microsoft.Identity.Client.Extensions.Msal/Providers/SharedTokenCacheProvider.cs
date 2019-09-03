// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.Identity.Client.Extensions.Msal.Providers
{
    /// <summary>
    /// SharedTokenCacheProbe provides shared access to tokens from the Microsoft family of products.
    /// This probe will provided access to tokens from accounts that have been authenticated in other Microsoft products to provide a single sign-on experience.
    /// </summary>
    public class SharedTokenCacheProvider : ITokenProvider
    {
        private static readonly string s_cacheFilePath =
            Path.Combine(SharedUtilities.GetUserRootDirectory(), "msal.cache");
        private const string ServiceName = "Microsoft.Developer.IdentityService";
        private const string AzureCliClientId = "04b07795-8ddb-461a-bbee-02f9e1bf7b46";
        private const string MsalClientVersion = "1.0.0.0";
        private const string MsalAccountName = "MSALCache";
        private readonly IPublicClientApplication _app;
        private readonly MsalCacheHelper _cacheHelper;
        private readonly IConfiguration _config;
        private readonly ILogger _logger;

        internal SharedTokenCacheProvider(StorageCreationPropertiesBuilder builder, IConfiguration config = null, ILogger logger = null)
        {
            _logger = logger;
            _config = config ?? new ConfigurationBuilder().AddEnvironmentVariables().Build();

            var authority = _config.GetValue<string>(Constants.AadAuthorityEnvName) ??
                string.Format(CultureInfo.InvariantCulture,
                    AadAuthority.AadCanonicalAuthorityTemplate,
                    AadAuthority.DefaultTrustedHost,
                    "common");

            _app = PublicClientApplicationBuilder
                .Create(AzureCliClientId)
                .WithAuthority(new Uri(authority))
                .Build();

            var cacheStore = new MsalCacheStorage(builder.Build());
            _cacheHelper = new MsalCacheHelper(_app.UserTokenCache, cacheStore);
            _cacheHelper.RegisterCache(_app.UserTokenCache);
        }

        /// <inheritdoc />
        public SharedTokenCacheProvider(IConfiguration config = null, ILogger logger = null) :
            this(new StorageCreationPropertiesBuilder(
                Path.GetFileName(s_cacheFilePath),
                Path.GetDirectoryName(s_cacheFilePath),
                AzureCliClientId)
            .WithMacKeyChain(serviceName: ServiceName, accountName: MsalAccountName)
            .WithLinuxKeyring(
                schemaName: "msal.cache",
                collection: "default",
                secretLabel: "MSALCache",
                attribute1: new KeyValuePair<string, string>("MsalClientID", ServiceName),
                attribute2: new KeyValuePair<string, string>("MsalClientVersion", MsalClientVersion)),
                config,
                logger)
        {
        }

        /// <inheritdoc />
        public async Task<bool> IsAvailableAsync(CancellationToken cancel = default)
        {
            Log(Microsoft.Extensions.Logging.LogLevel.Information, "checking for accounts in shared developer tool cache");
            var accounts = await GetAccountsAsync().ConfigureAwait(false);
            var available = accounts.Any();
            Log(Microsoft.Extensions.Logging.LogLevel.Information, $"provider available: {available}");
            return available;
        }

        /// <inheritdoc />
        public async Task<IToken> GetTokenAsync(IEnumerable<string> scopes, CancellationToken cancel = default)
        {
            Log(Microsoft.Extensions.Logging.LogLevel.Information, "checking for accounts in shared developer tool cache");
            var accounts = (await GetAccountsAsync().ConfigureAwait(false)).ToList();
            if(!accounts.Any())
            {
                throw new InvalidOperationException("there are no accounts available to acquire a token");
            }
            var res = await _app.AcquireTokenSilent(scopes, accounts.First())
                .ExecuteAsync(cancel)
                .ConfigureAwait(false);
            return new AccessTokenWithExpiration { ExpiresOn = res.ExpiresOn, AccessToken = res.AccessToken };
        }

        /// <inheritdoc />
        public async Task<IToken> GetTokenWithResourceUriAsync(string resourceUri, CancellationToken cancel = default)
        {
            Log(Microsoft.Extensions.Logging.LogLevel.Information, "checking for accounts in shared developer tool cache");
            var accounts = (await GetAccountsAsync().ConfigureAwait(false)).ToList();
            if(!accounts.Any())
            {
                throw new InvalidOperationException("there are no accounts available to acquire a token");
            }

            var scopes = new List<string>{resourceUri + "/.default"};
            var res = await _app.AcquireTokenSilent(scopes, accounts.First())
                .ExecuteAsync(cancel)
                .ConfigureAwait(false);
            return new AccessTokenWithExpiration { ExpiresOn = res.ExpiresOn, AccessToken = res.AccessToken };
        }

        private async Task<IEnumerable<IAccount>> GetAccountsAsync()
        {
            var accounts = (await _app.GetAccountsAsync().ConfigureAwait(false)).ToList();
            if (accounts.Any())
            {
                Log(Microsoft.Extensions.Logging.LogLevel.Information,
                    $"found the following account usernames: {string.Join(", ", accounts.Select(i => i.Username))}");
            }
            else
            {
                const string msg = "no accounts found in the shared cache -- perhaps, log into Visual Studio, Azure CLI, Azure PowerShell, etc";
                Log(Microsoft.Extensions.Logging.LogLevel.Information, msg);
            }
            var username = _config.GetValue<string>(Constants.AzurePreferredAccountUsernameEnvName);

            if (string.IsNullOrWhiteSpace(username))
            {
                return accounts;
            }

            Log(Microsoft.Extensions.Logging.LogLevel.Information,
                $"since {Constants.AzurePreferredAccountUsernameEnvName} is set accounts will be filtered by username: {username}");
            return accounts.Where(i => i.Username == username);
        }

        private void Log(Microsoft.Extensions.Logging.LogLevel level, string message, [CallerMemberName] string memberName = default)
        {
            _logger?.Log(level, $"{nameof(SharedTokenCacheProvider)}.{memberName} :: {message}");
        }
    }
}
