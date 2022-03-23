// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.ClientCredential;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// </summary>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide confidential client on mobile
#endif
    public class ConfidentialClientApplicationBuilder : AbstractApplicationBuilder<ConfidentialClientApplicationBuilder>
    {
        /// <inheritdoc />
        internal ConfidentialClientApplicationBuilder(ApplicationConfiguration configuration)
            : base(configuration)
        {
            ConfidentialClientApplication.GuardMobileFrameworks();
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
            ConfidentialClientApplication.GuardMobileFrameworks();

            var config = new ApplicationConfiguration();
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
            ConfidentialClientApplication.GuardMobileFrameworks();

            var config = new ApplicationConfiguration();
            return new ConfidentialClientApplicationBuilder(config)
                .WithClientId(clientId)
                .WithCacheSynchronization(false);
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
        /// Instructs MSAL.NET to use an Azure regional token service.
        /// </summary>
        /// <param name="azureRegion">Either the string with the region (preferred) or        
        /// use <see cref="ConfidentialClientApplication.AttemptRegionDiscovery"/> and MSAL.NET will attempt to auto-detect the region.                
        /// </param>
        /// <remarks>
        /// Region names as per https://azure.microsoft.com/en-ca/global-infrastructure/geographies/.
        /// See https://aka.ms/region-map for more details on region names.
        /// The region value should be short region name for the region where the service is deployed. 
        /// For example "centralus" is short name for region Central US.
        /// Not all auth flows can use the regional token service. 
        /// Service To Service (client credential flow) tokens can be obtained from the regional service.
        /// Requires configuration at the tenant level.
        /// Auto-detection works on a limited number of Azure artifacts (VMs, Azure functions). 
        /// If auto-detection fails, the non-regional endpoint will be used.
        /// If an invalid region name is provided, the non-regional endpoint MIGHT be used or the token request MIGHT fail.
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
        /// Apps can set this flag to <c>false</c> to enable an optimistic cache locking strategy, which may result in better performance, especially 
        /// when ConfidentialClientApplication objects are reused.
        /// </summary>
        /// <remarks>
        /// False by default.
        /// Not recommended for apps that call RemoveAsync
        /// </remarks>
        public ConfidentialClientApplicationBuilder WithCacheSynchronization(bool enableCacheSynchronization)
        {
            Config.CacheSynchronizationEnabled = enableCacheSynchronization;
            return this;
        }

        internal ConfidentialClientApplicationBuilder WithAppTokenCacheInternalForTest(ITokenCacheInternal tokenCacheInternal)
        {
            Config.AppTokenCacheInternalForTest = tokenCacheInternal;
            return this;
        }

        /// <inheritdoc />
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
        /// Builds the ConfidentialClientApplication from the parameters set
        /// in the builder
        /// </summary>
        /// <returns></returns>
        public IConfidentialClientApplication Build()
        {
            return BuildConcrete();
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        internal ConfidentialClientApplication BuildConcrete()
        {
            return new ConfidentialClientApplication(BuildConfiguration());
        }
    }
}
