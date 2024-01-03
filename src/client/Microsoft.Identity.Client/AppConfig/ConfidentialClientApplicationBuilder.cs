// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.ClientCredential;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.TelemetryCore.TelemetryClient;
using Microsoft.IdentityModel.Abstractions;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// </summary>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide confidential client on mobile
#endif
    public class ConfidentialClientApplicationBuilder : AbstractApplicationBuilder<ConfidentialClientApplicationBuilder>
    {
        /// <inheritdoc/>
        internal ConfidentialClientApplicationBuilder(ApplicationConfiguration configuration)
            : base(configuration)
        {
            ApplicationBase.GuardMobileFrameworks();
        }

        /// <summary>
        /// Constructor of a ConfidentialClientApplicationBuilder from application configuration options.
        /// See https://aka.ms/msal-net-application-configuration
        /// </summary>
        /// <param name="options">Confidential client applications configuration options</param>
        /// <returns>A <see cref="ConfidentialClientApplicationBuilder"/> from which to set more
        /// parameters, and to create a confidential client application instance</returns>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide confidential client on mobile
#endif
        public static ConfidentialClientApplicationBuilder CreateWithApplicationOptions(
            ConfidentialClientApplicationOptions options)
        {
            ApplicationBase.GuardMobileFrameworks();

            var config = new ApplicationConfiguration(MsalClientType.ConfidentialClient);
            var builder = new ConfidentialClientApplicationBuilder(config).WithOptions(options);

            if (!string.IsNullOrWhiteSpace(options.ClientSecret))
            {
                builder = builder.WithClientSecret(options.ClientSecret);
            }

            if (!string.IsNullOrWhiteSpace(options.AzureRegion))
            {
                builder = builder.WithAzureRegion(options.AzureRegion);
            }

            builder = builder.WithCacheSynchronization(options.EnableCacheSynchronization);

            return builder;
        }

        /// <summary>
        /// Creates a ConfidentialClientApplicationBuilder from a clientID.
        /// See https://aka.ms/msal-net-application-configuration
        /// </summary>
        /// <param name="clientId">Client ID (also known as App ID) of the application as registered in the
        /// application registration portal (https://aka.ms/msal-net-register-app)/.</param>
        /// <returns>A <see cref="ConfidentialClientApplicationBuilder"/> from which to set more
        /// parameters, and to create a confidential client application instance</returns>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide confidential client on mobile
#endif
        public static ConfidentialClientApplicationBuilder Create(string clientId)
        {
            ApplicationBase.GuardMobileFrameworks();

            var config = new ApplicationConfiguration(MsalClientType.ConfidentialClient);
            return new ConfidentialClientApplicationBuilder(config)
                .WithClientId(clientId);
        }

        /// <summary>
        /// Sets the certificate associated with the application.
        /// </summary>
        /// <param name="certificate">The X509 certificate used as credentials to prove the identity of the application to Azure AD.</param>
        /// <remarks>
        /// You should use certificates with a private key size of at least 2048 bytes. Future versions of this library might reject certificates with smaller keys.
        /// Does not send the certificate (as x5c parameter) with the request by default.
        /// </remarks>
        public ConfidentialClientApplicationBuilder WithCertificate(X509Certificate2 certificate)
        {
            return WithCertificate(certificate, false);
        }

        /// <summary>
        /// Sets the certificate associated with the application.
        /// Applicable to first-party applications only, this method also allows to specify 
        /// if the <see href="https://datatracker.ietf.org/doc/html/rfc7517#section-4.7">x5c claim</see> should be sent to Azure AD.
        /// Sending the x5c enables application developers to achieve easy certificate roll-over in Azure AD:
        /// this method will send the certificate chain to Azure AD along with the token request,
        /// so that Azure AD can use it to validate the subject name based on a trusted issuer policy.
        /// This saves the application admin from the need to explicitly manage the certificate rollover
        /// (either via portal or PowerShell/CLI operation). For details see https://aka.ms/msal-net-sni
        /// </summary>
        /// <param name="certificate">The X509 certificate used as credentials to prove the identity of the application to Azure AD.</param>
        /// <param name="sendX5C">To send X5C with every request or not. The default is <c>false</c></param>
        /// <remarks>You should use certificates with a private key size of at least 2048 bytes. Future versions of this library might reject certificates with smaller keys. </remarks>
        public ConfidentialClientApplicationBuilder WithCertificate(X509Certificate2 certificate, bool sendX5C)
        {
            if (certificate == null)
            {
                throw new ArgumentNullException(nameof(certificate));
            }

            if (!certificate.HasPrivateKey)
            {
                throw new MsalClientException(MsalError.CertWithoutPrivateKey, MsalErrorMessage.CertMustHavePrivateKey(nameof(certificate)));
            }

            Config.ClientCredential = new CertificateClientCredential(certificate);
            Config.SendX5C = sendX5C;
            return this;
        }

        /// <summary>
        /// Sets the certificate associated with the application along with the specific claims to sign.
        /// By default, this will merge the <paramref name="claimsToSign"/> with the default required set of claims needed for authentication.
        /// If <paramref name="mergeWithDefaultClaims"/> is set to false, you will need to provide the required default claims. See https://aka.ms/msal-net-client-assertion
        /// </summary>
        /// <param name="certificate">The X509 certificate used as credentials to prove the identity of the application to Azure AD.</param>
        /// <param name="claimsToSign">The claims to be signed by the provided certificate.</param>
        /// <param name="mergeWithDefaultClaims">Determines whether or not to merge <paramref name="claimsToSign"/> with the default claims required for authentication.</param>
        /// <remarks>
        /// You should use certificates with a private key size of at least 2048 bytes. Future versions of this library might reject certificates with smaller keys.
        /// Does not send the certificate (as x5c parameter) with the request by default.
        /// </remarks>
        public ConfidentialClientApplicationBuilder WithClientClaims(X509Certificate2 certificate, IDictionary<string, string> claimsToSign, bool mergeWithDefaultClaims)
        {
            return WithClientClaims(certificate, claimsToSign, mergeWithDefaultClaims, false);
        }

        /// <summary>
        /// Sets the certificate associated with the application along with the specific claims to sign.
        /// By default, this will merge the <paramref name="claimsToSign"/> with the default required set of claims needed for authentication.
        /// If <paramref name="mergeWithDefaultClaims"/> is set to false, you will need to provide the required default claims. See https://aka.ms/msal-net-client-assertion
        /// </summary>
        /// <param name="certificate">The X509 certificate used as credentials to prove the identity of the application to Azure AD.</param>
        /// <param name="claimsToSign">The claims to be signed by the provided certificate.</param>
        /// <param name="mergeWithDefaultClaims">Determines whether or not to merge <paramref name="claimsToSign"/> with the default claims required for authentication.</param>
        /// <param name="sendX5C">To send X5C with every request or not.</param>
        /// <remarks>You should use certificates with a private key size of at least 2048 bytes. Future versions of this library might reject certificates with smaller keys.</remarks>
        public ConfidentialClientApplicationBuilder WithClientClaims(X509Certificate2 certificate, IDictionary<string, string> claimsToSign, bool mergeWithDefaultClaims = true, bool sendX5C = false)
        {
            if (certificate == null)
            {
                throw new ArgumentNullException(nameof(certificate));
            }

            if (claimsToSign == null || !claimsToSign.Any())
            {
                throw new ArgumentNullException(nameof(claimsToSign));
            }

            Config.ClientCredential = new CertificateAndClaimsClientCredential(certificate, claimsToSign, mergeWithDefaultClaims);
            Config.SendX5C = sendX5C;
            return this;
        }

        /// <summary>
        /// Sets the application secret
        /// </summary>
        /// <param name="clientSecret">Secret string previously shared with AAD at application registration to prove the identity
        /// of the application (the client) requesting the tokens</param>
        /// <returns></returns>
        public ConfidentialClientApplicationBuilder WithClientSecret(string clientSecret)
        {            
            if (string.IsNullOrWhiteSpace(clientSecret))
            {
                throw new ArgumentNullException(nameof(clientSecret));
            }

            Config.ClientCredential = new SecretStringClientCredential(clientSecret);
            return this;
        }

        /// <summary>
        /// Sets the application client assertion. See https://aka.ms/msal-net-client-assertion.
        /// This will create an assertion that will be held within the client application's memory for the duration of the client.
        /// You can use <see cref="WithClientAssertion(Func{string})"/> to set a delegate that will be executed for each authentication request. 
        /// This will allow you to update the client assertion used by the client application once the assertion expires.
        /// </summary>
        /// <param name="signedClientAssertion">The client assertion used to prove the identity of the application to Azure AD. This is a Base-64 encoded JWT.</param>
        /// <returns></returns>
        public ConfidentialClientApplicationBuilder WithClientAssertion(string signedClientAssertion)
        {
            if (string.IsNullOrWhiteSpace(signedClientAssertion))
            {
                throw new ArgumentNullException(nameof(signedClientAssertion));
            }

            Config.ClientCredential = new SignedAssertionClientCredential(signedClientAssertion);            
            return this;
        }

        /// <summary>
        /// Configures a delegate that creates a client assertion. See https://aka.ms/msal-net-client-assertion
        /// </summary>
        /// <param name="clientAssertionDelegate">delegate computing the client assertion used to prove the identity of the application to Azure AD.
        /// This is a delegate that computes a Base-64 encoded JWT for each authentication call.</param>
        /// <returns>The ConfidentialClientApplicationBuilder to chain more .With methods</returns>
        /// <remarks> Callers can use this mechanism to cache their assertions </remarks>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public ConfidentialClientApplicationBuilder WithClientAssertion(Func<string> clientAssertionDelegate)
        {
            if (clientAssertionDelegate == null)
            {
                throw new ArgumentNullException(nameof(clientAssertionDelegate));
            }

            Func<CancellationToken, Task<string>> clientAssertionAsyncDelegate = (_) =>
            {
                return Task.FromResult(clientAssertionDelegate());
            };

            Config.ClientCredential = new SignedAssertionDelegateClientCredential(clientAssertionAsyncDelegate);
            return this;
        }

        /// <summary>
        /// Configures an async delegate that creates a client assertion. See https://aka.ms/msal-net-client-assertion
        /// </summary>
        /// <param name="clientAssertionAsyncDelegate">An async delegate computing the client assertion used to prove the identity of the application to Azure AD.
        /// This is a delegate that computes a Base-64 encoded JWT for each authentication call.</param>
        /// <returns>The ConfidentialClientApplicationBuilder to chain more .With methods</returns>
        /// <remarks> Callers can use this mechanism to cache their assertions </remarks>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public ConfidentialClientApplicationBuilder WithClientAssertion(Func<CancellationToken, Task<string>> clientAssertionAsyncDelegate)
        {
            if (clientAssertionAsyncDelegate == null)
            {
                throw new ArgumentNullException(nameof(clientAssertionAsyncDelegate));
            }

            Config.ClientCredential = new SignedAssertionDelegateClientCredential(clientAssertionAsyncDelegate);
            return this;
        }

        /// <summary>
        /// Configures an async delegate that creates a client assertion. See https://aka.ms/msal-net-client-assertion
        /// </summary>
        /// <param name="clientAssertionAsyncDelegate">An async delegate computing the client assertion used to prove the identity of the application to Azure AD.
        /// This is a delegate that computes a Base-64 encoded JWT for each authentication call.</param>
        /// <returns>The ConfidentialClientApplicationBuilder to chain more .With methods</returns>
        /// <remarks> Callers can use this mechanism to cache their assertions </remarks>
        public ConfidentialClientApplicationBuilder WithClientAssertion(Func<AssertionRequestOptions, Task<string>> clientAssertionAsyncDelegate)
        {
            if (clientAssertionAsyncDelegate == null)
            {
                throw new ArgumentNullException(nameof(clientAssertionAsyncDelegate));
            }

            Config.ClientCredential = new SignedAssertionDelegateClientCredential(clientAssertionAsyncDelegate);
            return this;
        }

        /// <summary>
        /// Instructs MSAL to use an Azure regional token service. This feature is currently available to 
        /// first-party applications only. 
        /// </summary>
        /// <param name="azureRegion">Either the string with the region (preferred) or        
        /// use <see cref="ConfidentialClientApplication.AttemptRegionDiscovery"/> and MSAL will attempt to auto-detect the region.                
        /// </param>
        /// <remarks>
        /// The region value should be a short region name for the region where the service is deployed. 
        /// For example, "centralus" is short name for region Central US.
        /// Currently only tokens for the client credential flow can be obtained from the regional service.
        /// Requires configuration at the tenant level.
        /// Auto-detection works on a limited number of Azure artifacts (VMs, Azure functions). 
        /// If auto-detection fails, the non-regional endpoint will be used.
        /// If a specific region was provided and the token web request failed, verify that the region name is valid.
        /// See https://aka.ms/msal-net-region-discovery for more details.        
        /// </remarks>
        /// <returns>The builder to chain the .With methods</returns>
        public ConfidentialClientApplicationBuilder WithAzureRegion(string azureRegion = ConfidentialClientApplication.AttemptRegionDiscovery)
        {
            if (string.IsNullOrEmpty(azureRegion))
            {
                throw new ArgumentNullException(nameof(azureRegion));
            }

            Config.AzureRegion = azureRegion;

            return this;
        }

        /// <summary>
        /// When set to <c>true</c>, MSAL will lock cache access at the <see cref="ConfidentialClientApplication"/> level, i.e.
        /// the block of code between BeforeAccessAsync and AfterAccessAsync callbacks will be synchronized. 
        /// Apps can set this flag to <c>false</c> to enable an optimistic cache locking strategy, which may result in better performance
        /// at the cost of cache consistency. 
        /// Setting this flag to <c>false</c> is only recommended for apps which create a new <see cref="ConfidentialClientApplication"/> per request.
        /// </summary>
        /// <remarks>
        /// This flag is <c>true</c> by default. The default behavior is recommended.
        /// </remarks>
        public ConfidentialClientApplicationBuilder WithCacheSynchronization(bool enableCacheSynchronization)
        {
            Config.CacheSynchronizationEnabled = enableCacheSynchronization;
            return this;
        }

        /// <summary>
        /// Adds a known authority corresponding to a generic OpenIdConnect Identity Provider. 
        /// MSAL will append ".well-known/openid-configuration" to the authority and retrieve the OIDC 
        /// metadata from there, to figure out the endpoints.
        /// See https://openid.net/specs/openid-connect-core-1_0.html#Terminology
        /// </summary>
        /// <param name="authorityUri">OpenIdConnect authority</param>
        /// <returns>The builder to chain the .With methods</returns>
        /// <remarks>This is an experimental API and only available on Confidential Client flows.</remarks>        
        public ConfidentialClientApplicationBuilder WithGenericAuthority(string authorityUri)
        {
            ValidateUseOfExperimentalFeature("WithGenericAuthority");

            var authorityInfo = AuthorityInfo.FromGenericAuthority(authorityUri);
            Config.Authority = Authority.CreateAuthority(authorityInfo);

            return this;
        }

        /// <summary>
        /// Sets telemetry client for the application.
        /// </summary>
        /// <param name="telemetryClients">List of telemetry clients to add telemetry logs.</param>
        /// <returns>The builder to chain the .With methods</returns>
        public ConfidentialClientApplicationBuilder WithTelemetryClient(params ITelemetryClient[] telemetryClients)
        {
            if (telemetryClients == null)
            {
                throw new ArgumentNullException(nameof(telemetryClients));
            }

            if (telemetryClients.Length > 0)
            {
                foreach (var telemetryClient in telemetryClients)
                {
                    if (telemetryClient == null)
                    {
                        throw new ArgumentNullException(nameof(telemetryClient));
                    }

                    telemetryClient.Initialize();
                }

                Config.TelemetryClients = telemetryClients;
            }

            TelemetryClientLogMsalVersion();

            return this;
        }

        private void TelemetryClientLogMsalVersion()
        {
            if (Config.TelemetryClients.HasEnabledClients(TelemetryConstants.ConfigurationUpdateEventName))
            {
                MsalTelemetryEventDetails telemetryEventDetails = new MsalTelemetryEventDetails(TelemetryConstants.ConfigurationUpdateEventName);
                telemetryEventDetails.SetProperty(TelemetryConstants.MsalVersion, MsalIdHelper.GetMsalVersion());

                Config.TelemetryClients.TrackEvent(telemetryEventDetails);
            }
        }

        internal ConfidentialClientApplicationBuilder WithAppTokenCacheInternalForTest(ITokenCacheInternal tokenCacheInternal)
        {
            Config.AppTokenCacheInternalForTest = tokenCacheInternal;
            return this;
        }

        /// <inheritdoc/>
        internal override void Validate()
        {
            base.Validate();

            if (string.IsNullOrWhiteSpace(Config.RedirectUri))
            {
                Config.RedirectUri = Constants.DefaultConfidentialClientRedirectUri;
            }

            if (!Uri.TryCreate(Config.RedirectUri, UriKind.Absolute, out Uri uriResult))
            {
                throw new InvalidOperationException(MsalErrorMessage.InvalidRedirectUriReceived(Config.RedirectUri));
            }

            if (!string.IsNullOrEmpty(Config.AzureRegion) && (Config.CustomInstanceDiscoveryMetadata != null || Config.CustomInstanceDiscoveryMetadataUri != null))
            {
                throw new MsalClientException(MsalError.RegionDiscoveryWithCustomInstanceMetadata, MsalErrorMessage.RegionDiscoveryWithCustomInstanceMetadata);
            }
        }

        /// <summary>
        /// Builds an instance of <see cref="IConfidentialClientApplication"/> 
        /// from the parameters set in the <see cref="ConfidentialClientApplicationBuilder"/>.
        /// </summary>
        /// <exception cref="MsalClientException">Thrown when errors occur locally in the library itself (for example, because of incorrect configuration).</exception>
        /// <returns>An instance of <see cref="IConfidentialClientApplication"/></returns>
        public IConfidentialClientApplication Build()
        {
            return BuildConcrete();
        }

        internal ConfidentialClientApplication BuildConcrete()
        {
            return new ConfidentialClientApplication(BuildConfiguration());
        }
    }
}
