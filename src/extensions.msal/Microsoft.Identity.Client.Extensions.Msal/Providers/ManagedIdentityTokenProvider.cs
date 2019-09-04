// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client.Extensions.Msal.Providers.Exceptions;

namespace Microsoft.Identity.Client.Extensions.Msal.Providers
{
    /// <summary>
    ///     ManagedIdentityTokenProvider will look in environment variable to determine if the managed identity provider
    ///     is available. If the managed identity provider is available, the provider will provide AAD tokens using the
    ///     IMDS endpoint.
    /// </summary>
    public class ManagedIdentityTokenProvider : ITokenProvider
    {
        private readonly HttpClient _httpClient;
        private readonly IManagedIdentityConfiguration _config;
        private readonly string _overrideClientId;
        private readonly ILogger _logger;

        internal ManagedIdentityTokenProvider(HttpClient httpClient, IConfiguration config = null,
            string overrideClientId = null, ILogger logger = null)
        {
            _httpClient = httpClient;
            config = config ?? new ConfigurationBuilder().AddEnvironmentVariables().Build();
            _config = new DefaultManagedIdentityConfiguration(config);
            _overrideClientId = overrideClientId;
            _logger = logger;
        }

        /// <summary>
        ///     Create a Managed Identity probe with a specified client identity
        /// </summary>
        /// <param name="config">option configuration structure -- if not supplied, a default environmental configuration is used.</param>
        /// <param name="overrideClientId">override the client identity found in the config for use when querying the Azure IMDS endpoint</param>
        /// <param name="logger">TraceSource logger</param>
        public ManagedIdentityTokenProvider(IConfiguration config = null, string overrideClientId = null, ILogger logger = null)
            : this(null, config, overrideClientId, logger: logger) { }

        /// <inheritdoc />
        public async Task<bool> IsAvailableAsync(CancellationToken cancel = default)
        {
            // check App Service MSI
            if (IsAppService())
            {
                Log(Microsoft.Extensions.Logging.LogLevel.Information, "AppService Managed Identity is available");
                return true;
            }
            Log(Microsoft.Extensions.Logging.LogLevel.Information, "AppService Managed Identity is not available");

            try
            {
                Log(Microsoft.Extensions.Logging.LogLevel.Information, "Attempting to fetch test token with Virtual Machine Managed Identity");
                // if service is listening on VM IP check if a token can be acquired
                var provider = BuildInternalProvider(maxRetries: 2, httpClient: _httpClient);
                var token = await provider.GetTokenAsync(Constants.AzureResourceManagerResourceUri, cancel)
                    .ConfigureAwait(false);
                Log(Microsoft.Extensions.Logging.LogLevel.Information, $"provider available: {token != null}");
                return token != null;
            }
            catch (Exception ex) when (ex is TooManyRetryAttemptsException || ex is BadRequestManagedIdentityException)
            {
                Log(Microsoft.Extensions.Logging.LogLevel.Information, "Exceeded retry limit for Virtual Machine Managed Identity request");
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<IToken> GetTokenAsync(IEnumerable<string> scopes, CancellationToken cancel = default)
        {
            const string resourceDefaultSuffix = @".default";
            var internalProvider = BuildInternalProvider(httpClient: _httpClient);

            var resourceUriInScopes = scopes?.FirstOrDefault(i => i.EndsWith(resourceDefaultSuffix, StringComparison.OrdinalIgnoreCase));
            if (resourceUriInScopes == null)
            {
                throw new NoResourceUriInScopesException();
            }

            var resourceUri = resourceUriInScopes.Substring(0, resourceUriInScopes.Length - resourceDefaultSuffix.Length);
            if (resourceUri.EndsWith("//"))
            {
                resourceUri = resourceUri.Substring(0, resourceUri.Length - 1);
            }

            Log(Microsoft.Extensions.Logging.LogLevel.Information, $"Attempting to fetch token with resourceUri: {resourceUri} from scope: {resourceUriInScopes}");
            return await internalProvider.GetTokenAsync(resourceUri, cancel).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IToken> GetTokenWithResourceUriAsync(string resourceUri, CancellationToken cancel = default)
        {
            var internalProvider = BuildInternalProvider(httpClient: _httpClient);
            Log(Microsoft.Extensions.Logging.LogLevel.Information, $"Attempting to fetch token with resourceUri: {resourceUri}");
            return await internalProvider.GetTokenAsync(resourceUri, cancel).ConfigureAwait(false);
        }


        /// <summary>
        /// IsAppService tells us if we are executing within AppService with Managed Identities enabled
        /// </summary>
        /// <returns></returns>
        private bool IsAppService()
        {
            var vars = new List<string>
            {
                _config.ManagedIdentitySecret,
                _config.ManagedIdentityEndpoint
            };
            return vars.All(item => !string.IsNullOrWhiteSpace(item));
        }

        private InternalManagedIdentityCredentialProvider BuildInternalProvider(int maxRetries = 5, HttpClient httpClient = null)
        {
            var endpoint = IsAppService() ? _config.ManagedIdentityEndpoint : Constants.ManagedIdentityTokenEndpoint;
            Log(Microsoft.Extensions.Logging.LogLevel.Information, "building " + (IsAppService() ? "an AppService " : "a VM") + " Managed Identity provider");
            return new InternalManagedIdentityCredentialProvider(endpoint,
                httpClient: httpClient,
                secret: _config.ManagedIdentitySecret,
                clientId: ClientId,
                maxRetries: maxRetries,
                logger: _logger);
        }

        private string ClientId => string.IsNullOrWhiteSpace(_overrideClientId) ? _config.ClientId : _overrideClientId;

        private void Log(Microsoft.Extensions.Logging.LogLevel level, string message, [CallerMemberName] string memberName = default)
        {
            _logger?.Log(level, $"{nameof(ManagedIdentityTokenProvider)}.{memberName} :: {message}");
        }
    }
}
