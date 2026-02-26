// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal;
using Microsoft.IdentityModel.Abstractions;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Builder for creating <see cref="IAgenticApplication"/> instances.
    /// </summary>
    /// <remarks>
    /// Usage example:
    /// <code>
    /// IAgenticApplication agentApp = AgenticApplicationBuilder
    ///     .Create("agent-identity-id")
    ///     .WithAuthority("https://login.microsoftonline.com/", tenantId)
    ///     .WithPlatformCredential(platformClientId, certificate)
    ///     .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
    ///     .Build();
    /// </code>
    /// </remarks>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide on mobile
#endif
    public class AgenticApplicationBuilder
    {
        private readonly string _agentIdentity;
        private string _tenantId;
        private string _authorityUri = "https://login.microsoftonline.com/";
        private string _platformClientId;
        private X509Certificate2 _certificate;
        private bool _sendX5C = true;
        private string _tokenExchangeUrl = "api://AzureADTokenExchange/.default";
        private CacheOptions _cacheOptions;
        private IMsalHttpClientFactory _httpClientFactory;
        private IIdentityLogger _logger;
        private bool _enablePiiLogging;
        private IHttpManager _httpManager;
        private bool _enableInstanceDiscovery = true;

        private AgenticApplicationBuilder(string agentIdentity)
        {
            _agentIdentity = agentIdentity ?? throw new ArgumentNullException(nameof(agentIdentity));
        }

        /// <summary>
        /// Creates a new <see cref="AgenticApplicationBuilder"/> for the specified agent identity.
        /// </summary>
        /// <param name="agentIdentity">The FMI path or client ID of the agent identity.
        /// This is the identity that the agent will use to authenticate and acquire tokens.</param>
        /// <returns>A new builder instance.</returns>
        public static AgenticApplicationBuilder Create(string agentIdentity)
        {
            return new AgenticApplicationBuilder(agentIdentity);
        }

        /// <summary>
        /// Sets the authority and tenant for authentication.
        /// </summary>
        /// <param name="authorityUri">The authority URI, e.g., <c>https://login.microsoftonline.com/</c>.</param>
        /// <param name="tenantId">The Azure AD tenant ID.</param>
        /// <returns>The builder, for fluent chaining.</returns>
        public AgenticApplicationBuilder WithAuthority(string authorityUri, string tenantId)
        {
            _authorityUri = authorityUri ?? throw new ArgumentNullException(nameof(authorityUri));
            _tenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
            return this;
        }

        /// <summary>
        /// Configures the platform (host) credential used to obtain FMI tokens for the agent.
        /// This is the certificate-based credential of the platform application that hosts the agent.
        /// SN+I (Subject Name + Issuer) authentication is used by default, which is required for FMI flows.
        /// </summary>
        /// <param name="platformClientId">The client ID of the platform application that hosts the agent.</param>
        /// <param name="certificate">The X.509 certificate for the platform application.</param>
        /// <param name="sendX5C">Whether to send the X5C certificate chain. Defaults to <c>true</c> (required for FMI flows).</param>
        /// <returns>The builder, for fluent chaining.</returns>
        public AgenticApplicationBuilder WithPlatformCredential(
            string platformClientId, X509Certificate2 certificate, bool sendX5C = true)
        {
            _platformClientId = platformClientId ?? throw new ArgumentNullException(nameof(platformClientId));
            _certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));
            _sendX5C = sendX5C;
            return this;
        }

        /// <summary>
        /// Sets the token exchange URL used for FMI credential acquisition.
        /// Defaults to <c>api://AzureADTokenExchange/.default</c>.
        /// </summary>
        /// <param name="tokenExchangeUrl">The token exchange URL.</param>
        /// <returns>The builder, for fluent chaining.</returns>
        public AgenticApplicationBuilder WithTokenExchangeUrl(string tokenExchangeUrl)
        {
            _tokenExchangeUrl = tokenExchangeUrl ?? throw new ArgumentNullException(nameof(tokenExchangeUrl));
            return this;
        }

        /// <summary>
        /// Sets the cache options for token caching behavior.
        /// <see cref="CacheOptions.EnableSharedCacheOptions"/> is recommended for agentic scenarios.
        /// </summary>
        /// <param name="cacheOptions">The cache options.</param>
        /// <returns>The builder, for fluent chaining.</returns>
        public AgenticApplicationBuilder WithCacheOptions(CacheOptions cacheOptions)
        {
            _cacheOptions = cacheOptions;
            return this;
        }

        /// <summary>
        /// Sets a custom HTTP client factory for all HTTP communication.
        /// </summary>
        /// <param name="httpClientFactory">The HTTP client factory.</param>
        /// <returns>The builder, for fluent chaining.</returns>
        public AgenticApplicationBuilder WithHttpClientFactory(IMsalHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            return this;
        }

        /// <summary>
        /// Configures logging for the agentic application.
        /// </summary>
        /// <param name="logger">The identity logger implementation.</param>
        /// <param name="enablePiiLogging">Whether to enable Personally Identifiable Information (PII) logging.
        /// Default is <c>false</c>. Enable only for debugging.</param>
        /// <returns>The builder, for fluent chaining.</returns>
        public AgenticApplicationBuilder WithLogging(IIdentityLogger logger, bool enablePiiLogging = false)
        {
            _logger = logger;
            _enablePiiLogging = enablePiiLogging;
            return this;
        }

        /// <summary>
        /// Sets the HTTP manager for testing purposes. Internal use only.
        /// </summary>
        internal AgenticApplicationBuilder WithHttpManager(IHttpManager httpManager)
        {
            _httpManager = httpManager;
            return this;
        }

        /// <summary>
        /// Enables or disables instance discovery. For testing only.
        /// </summary>
        internal AgenticApplicationBuilder WithInstanceDiscovery(bool enable)
        {
            _enableInstanceDiscovery = enable;
            return this;
        }

        /// <summary>
        /// Builds the <see cref="IAgenticApplication"/> instance.
        /// </summary>
        /// <returns>A configured <see cref="IAgenticApplication"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when required configuration is missing.</exception>
        public IAgenticApplication Build()
        {
            Validate();

            return new AgenticApplication(
                _agentIdentity,
                _tenantId,
                _authorityUri,
                _platformClientId,
                _certificate,
                _sendX5C,
                _tokenExchangeUrl,
                _cacheOptions,
                _httpClientFactory,
                _logger,
                _enablePiiLogging,
                _httpManager,
                _enableInstanceDiscovery);
        }

        private void Validate()
        {
            if (string.IsNullOrEmpty(_agentIdentity))
            {
                throw new InvalidOperationException(
                    "Agent identity must be specified. Use AgenticApplicationBuilder.Create(agentIdentity).");
            }

            if (string.IsNullOrEmpty(_tenantId))
            {
                throw new InvalidOperationException(
                    "Authority and tenant ID must be specified. Call WithAuthority(authorityUri, tenantId).");
            }

            if (string.IsNullOrEmpty(_platformClientId))
            {
                throw new InvalidOperationException(
                    "Platform credential must be specified. Call WithPlatformCredential(platformClientId, certificate).");
            }

            if (_certificate == null)
            {
                throw new InvalidOperationException(
                    "Platform certificate must be specified. Call WithPlatformCredential(platformClientId, certificate).");
            }
        }
    }
}
