// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Internal.ClientCredential;
using Microsoft.Identity.Client.Kerberos;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.UI;
using Microsoft.IdentityModel.Abstractions;

namespace Microsoft.Identity.Client
{
    internal sealed class ApplicationConfiguration : IAppConfig
    {
        public ApplicationConfiguration(MsalClientType applicationType)
        {
            switch (applicationType)
            {
                case MsalClientType.ConfidentialClient:
                    IsConfidentialClient = true;
                    break;

                case MsalClientType.ManagedIdentityClient:
                    IsManagedIdentity = true;
                    break;
            }
        }

        public const string DefaultClientName = "UnknownClient";
        public const string DefaultClientVersion = "0.0.0.0";

        // For telemetry, the ClientName of the application.
        private string _clientName = DefaultClientName;
        public string ClientName
        {
            get => _clientName;
            internal set { _clientName = string.IsNullOrWhiteSpace(value) ? DefaultClientName : value; }
        }

        // For telemetry, the ClientVersion of the application.
        private string _clientVersion = DefaultClientVersion;
        public string ClientVersion
        {
            get => _clientVersion;
            internal set { _clientVersion = string.IsNullOrWhiteSpace(value) ? DefaultClientVersion : value; }
        }

        public ITelemetryClient[] TelemetryClients { get; internal set; } = Array.Empty<ITelemetryClient>();

        public Func<object> ParentActivityOrWindowFunc { get; internal set; }

        public bool UseCorporateNetwork { get; internal set; }
        public string IosKeychainSecurityGroup { get; internal set; }

        public bool IsBrokerEnabled { get; internal set; }

        // Legacy options for UWP. .NET broker options are in BrokerOptions
        public WindowsBrokerOptions UwpBrokerOptions { get; set; }

        public BrokerOptions BrokerOptions { get; set; }

        public Func<CoreUIParent, ApplicationConfiguration, ILoggerAdapter, IBroker> BrokerCreatorFunc { get; set; }
        public Func<IWebUIFactory> WebUiFactoryCreator { get; set; }

        /// <summary>
        /// Service principal name for Kerberos Service Ticket.
        /// </summary>
        public string KerberosServicePrincipalName { get; set; } = string.Empty;

        /// <summary>
        /// Kerberos Service Ticket container to be used.
        /// </summary>
        public KerberosTicketContainer TicketContainer { get; set; } = KerberosTicketContainer.IdToken;

        [Obsolete("Telemetry is sent automatically by MSAL.NET. See https://aka.ms/msal-net-telemetry.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ITelemetryConfig TelemetryConfig { get; internal set; }

        public IHttpManager HttpManager { get; internal set; }

        public IPlatformProxy PlatformProxy { get; internal set; }

        public CacheOptions AccessorOptions { get; set; }

        public Authority Authority { get; internal set; }
        public string ClientId { get; internal set; }
        public string RedirectUri { get; internal set; }
        public bool EnablePiiLogging { get; internal set; }
        public LogLevel LogLevel { get; internal set; } = LogLevel.Info;
        public bool IsDefaultPlatformLoggingEnabled { get; internal set; }
        public IMsalHttpClientFactory HttpClientFactory { get; internal set; }
        public bool IsExtendedTokenLifetimeEnabled { get; set; }
        public LogCallback LoggingCallback { get; internal set; }
        public IIdentityLogger IdentityLogger { get; internal set; }
        public string Component { get; internal set; }
        public IDictionary<string, string> ExtraQueryParameters { get; internal set; } = new Dictionary<string, string>();
        public bool UseRecommendedDefaultRedirectUri { get; internal set; }
        public bool ExperimentalFeaturesEnabled { get; set; } = false;
        public IEnumerable<string> ClientCapabilities { get; set; }
        public bool SendX5C { get; internal set; } = false;
        public bool LegacyCacheCompatibilityEnabled { get; internal set; } = true;
        public bool CacheSynchronizationEnabled { get; internal set; } = true;
        public bool MultiCloudSupportEnabled { get; set; } = false;
        public bool RetryOnServerErrors { get; set; } = true;

        public Func<AppTokenProviderParameters, Task<AppTokenProviderResult>> AppTokenProvider;

        #region ManagedIdentity
        public ManagedIdentityId ManagedIdentityId { get; internal set; }
        public bool IsManagedIdentity { get; }

        public CryptoKeyType ManagedIdentityCredentialKeyType { get; internal set; }

        public X509Certificate2 ManagedIdentityClientCertificate { get; internal set; }

        #endregion

        #region ClientCredentials

        public IClientCredential ClientCredential { get; internal set; }

        /// <summary>
        /// This is here just to support the public IAppConfig. Should not be used internally, instead use the <see cref="ClientCredential" /> abstraction.
        /// </summary>
        public string ClientSecret
        {
            get
            {
                if (ClientCredential is SecretStringClientCredential secretCred)
                {
                    return secretCred.Secret;
                }

                return null;
            }
        }

        /// <summary>
        /// This is here just to support the public IAppConfig. Should not be used internally, instead use the <see cref="ClientCredential" /> abstraction.
        /// </summary>
        public X509Certificate2 ClientCredentialCertificate
        {
            get
            {
                if (ClientCredential is CertificateAndClaimsClientCredential cred)
                {
                    return cred.Certificate;
                }

                return null;
            }
        }
        #endregion

        #region Region
        public string AzureRegion { get; set; }
        #endregion

        #region Authority
        // These are all used to create the Authority when the app is built.

        public string TenantId { get; internal set; }

        public InstanceDiscoveryResponse CustomInstanceDiscoveryMetadata { get; set; }
        public Uri CustomInstanceDiscoveryMetadataUri { get; set; }

        /// <summary>
        /// Should _not_ go in the interface, only for builder usage while determining authorities with ApplicationOptions
        /// </summary>
        public AadAuthorityAudience AadAuthorityAudience { get; set; }

        /// <summary>
        /// Should _not_ go in the interface, only for builder usage while determining authorities with ApplicationOptions
        /// </summary>
        public AzureCloudInstance AzureCloudInstance { get; set; }

        /// <summary>
        /// Should _not_ go in the interface, only for builder usage while determining authorities with ApplicationOptions
        /// </summary>
        public string Instance { get; set; }

        /// <summary>
        /// Should _not_ go in the interface, only for builder usage while determining authorities with ApplicationOptions
        /// </summary>
        public bool ValidateAuthority { get; set; }

        #endregion

        #region Test Hooks
        public ILegacyCachePersistence UserTokenLegacyCachePersistenceForTest { get; set; }

        public ITokenCacheInternal UserTokenCacheInternalForTest { get; set; }
        public ITokenCacheInternal AppTokenCacheInternalForTest { get; set; }

        public IDeviceAuthManager DeviceAuthManagerForTest { get; set; }
        public IKeyMaterialManager KeyMaterialManagerForTest { get; set; }
        public bool IsConfidentialClient { get; }
        public bool IsInstanceDiscoveryEnabled { get; internal set; } = true;
#endregion

    }
}
