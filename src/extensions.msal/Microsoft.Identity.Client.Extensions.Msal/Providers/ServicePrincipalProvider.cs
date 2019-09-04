// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.Identity.Client.Extensions.Msal.Providers
{
    /// <inheritdoc />
    /// <summary>
    ///     ServicePrincipalProbe looks to the application setting and environment variables to build a ICredentialProvider.
    /// </summary>
    public class ServicePrincipalTokenProvider : ITokenProvider
    {
        private readonly IServicePrincipalConfiguration _config;
        private readonly ILogger _logger;

        /// <summary>
        /// Create a new instance of a ServicePrincipalProbe
        /// </summary>
        /// <param name="config">optional configuration; if not specified the default configuration will use environment variables</param>
        /// <param name="logger">optional TraceSource for detailed logging information</param>
        public ServicePrincipalTokenProvider(IConfiguration config = null, ILogger logger = null)
        {
            _logger = logger;
            config = config ?? new ConfigurationBuilder().AddEnvironmentVariables().Build();
            _config = new DefaultServicePrincipalConfiguration(config);
        }

        // Async method lacks 'await' operators and will run synchronously
        /// <inheritdoc />
        public Task<bool> IsAvailableAsync(CancellationToken cancel = default)
        {
            Log(Microsoft.Extensions.Logging.LogLevel.Information, "checking if provider is available");
            var available = IsClientSecret() || IsClientCertificate();
            Log(Microsoft.Extensions.Logging.LogLevel.Information, $"provider available: {available}");
            return Task.FromResult(available);
        }


        /// <inheritdoc />
        public async Task<IToken> GetTokenAsync(IEnumerable<string> scopes, CancellationToken cancel = default)
        {
            var provider = await CreateProviderAsync().ConfigureAwait(false);
            Log(Microsoft.Extensions.Logging.LogLevel.Information, "fetching token");
            return await provider.GetTokenAsync(scopes, cancel).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IToken> GetTokenWithResourceUriAsync(string resourceUri, CancellationToken cancel = default)
        {
            var provider = await CreateProviderAsync().ConfigureAwait(false);
            Log(Microsoft.Extensions.Logging.LogLevel.Information, "fetching token");
            var scopes = new List<string>{resourceUri + "/.default"};
            return await provider.GetTokenAsync(scopes, cancel).ConfigureAwait(false);
        }

        private async Task<InternalServicePrincipalTokenProvider> CreateProviderAsync()
        {
            var available = await IsAvailableAsync().ConfigureAwait(false);
            if (!available)
            {
                Log(Microsoft.Extensions.Logging.LogLevel.Information, "provider is not available");
                throw new InvalidOperationException("The required environment variables are not available.");
            }

            var authorityWithTenant = AadAuthority.CreateFromAadCanonicalAuthorityTemplate(_config.Authority, _config.TenantId);
            if (!IsClientCertificate())
            {
                Log(Microsoft.Extensions.Logging.LogLevel.Information, "provider is configured to use client certificates");
                return new InternalServicePrincipalTokenProvider(authorityWithTenant, _config.TenantId, _config.ClientId, _config.ClientSecret);
            }

            X509Certificate2 cert;
            if (!string.IsNullOrWhiteSpace(_config.CertificateBase64))
            {
                Log(Microsoft.Extensions.Logging.LogLevel.Information, $"decoding certificate from base64 directly from environment variable {Constants.AzureCertificateEnvName}");
                // If the certificate is provided as base64 encoded string in env, decode and hydrate a x509 cert
                var decoded = Convert.FromBase64String(_config.CertificateBase64);
                cert = new X509Certificate2(decoded);
            }
            else
            {
                Log(Microsoft.Extensions.Logging.LogLevel.Information, $"using certificate store with name {StoreNameWithDefault} and location {StoreLocationFromEnv}");
                // Try to use the certificate store
                var store = new X509Store(StoreNameWithDefault, StoreLocationFromEnv);
                store.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection certs;
                if (!string.IsNullOrEmpty(_config.CertificateSubjectDistinguishedName))
                {
                    Log(Microsoft.Extensions.Logging.LogLevel.Information, $"finding certificates in store by distinguished name {_config.CertificateSubjectDistinguishedName}");
                    certs = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName,
                        _config.CertificateSubjectDistinguishedName, true);
                }
                else
                {
                    Log(Microsoft.Extensions.Logging.LogLevel.Information, $"finding certificates in store by thumbprint {_config.CertificateThumbprint}");
                    certs = store.Certificates.Find(X509FindType.FindByThumbprint, _config.CertificateThumbprint, true);
                }


                if (certs.Count < 1)
                {
                    var msg = string.IsNullOrEmpty(_config.CertificateSubjectDistinguishedName)
                        ? $"Unable to find certificate with thumbprint '{_config.CertificateThumbprint}' in certificate store named '{StoreNameWithDefault}' and store location {StoreLocationFromEnv}"
                        : $"Unable to find certificate with distinguished name '{_config.CertificateSubjectDistinguishedName}' in certificate store named '{StoreNameWithDefault}' and store location {StoreLocationFromEnv}";

                    throw new InvalidOperationException(msg);
                }

                cert = certs[0];
            }

            return new InternalServicePrincipalTokenProvider(authorityWithTenant, _config.TenantId, _config.ClientId, cert);
        }

        private StoreLocation StoreLocationFromEnv
        {
            get
            {
                var loc = _config.CertificateStoreLocation;
                if (!string.IsNullOrWhiteSpace(loc) && Enum.TryParse(loc, true, out StoreLocation sLocation))
                {
                    return sLocation;
                }

                return StoreLocation.CurrentUser;
            }
        }

        private string StoreNameWithDefault
        {
            get
            {
                var name = _config.CertificateStoreName;
                return string.IsNullOrWhiteSpace(name) ? "My" : name;
            }
        }

        internal bool IsClientSecret()
        {
            Log(Microsoft.Extensions.Logging.LogLevel.Information, $"checking if {Constants.AzureTenantIdEnvName}, {Constants.AzureClientIdEnvName} and {Constants.AzureClientSecretEnvName} are set");
            var vars = new List<string>
            {
                _config.TenantId,
                _config.ClientId,
                _config.ClientSecret
            };
            var isClientSecret = vars.All(item => !string.IsNullOrWhiteSpace(item));
            Log(Microsoft.Extensions.Logging.LogLevel.Information, $"set: {isClientSecret}");
            return isClientSecret;
        }

        internal bool IsClientCertificate()
        {
            Log(Microsoft.Extensions.Logging.LogLevel.Information, $"checking if {Constants.AzureTenantIdEnvName}, {Constants.AzureClientIdEnvName} and ({Constants.AzureCertificateEnvName} or {Constants.AzureCertificateThumbprintEnvName} or {Constants.AzureCertificateSubjectDistinguishedNameEnvName}) are set");
            var tenantAndClient = new List<string>
            {
                _config.TenantId,
                _config.ClientId
            };
            if (tenantAndClient.All(item => !string.IsNullOrWhiteSpace(item)))
            {
                return !string.IsNullOrWhiteSpace(_config.CertificateBase64) ||
                       !string.IsNullOrWhiteSpace(_config.CertificateThumbprint) ||
                       !string.IsNullOrWhiteSpace(_config.CertificateSubjectDistinguishedName);
            }

            return false;
        }

        private void Log(Microsoft.Extensions.Logging.LogLevel level, string message, [CallerMemberName] string memberName = default)
        {
            _logger?.Log(level, $"{nameof(ServicePrincipalTokenProvider)}.{memberName} :: {message}");
        }
    }
}
