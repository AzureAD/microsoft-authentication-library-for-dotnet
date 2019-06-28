// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client
{
#if !ANDROID_BUILDTIME && !iOS_BUILDTIME && !WINDOWS_APP_BUILDTIME && !MAC_BUILDTIME // Hide confidential client on mobile platforms

    /// <summary>
    /// </summary>
    public class ConfidentialClientApplicationBuilder : AbstractApplicationBuilder<ConfidentialClientApplicationBuilder>
    {
        /// <inheritdoc />
        internal ConfidentialClientApplicationBuilder(ApplicationConfiguration configuration)
            : base(configuration)
        {
        }

        /// <summary>
        /// Constructor of a ConfidentialClientApplicationBuilder from application configuration options.
        /// See https://aka.ms/msal-net-application-configuration
        /// </summary>
        /// <param name="options">Public client applications configuration options</param>
        /// <returns>A <see cref="ConfidentialClientApplicationBuilder"/> from which to set more
        /// parameters, and to create a public client application instance</returns>
        public static ConfidentialClientApplicationBuilder CreateWithApplicationOptions(
            ConfidentialClientApplicationOptions options)
        {
            var config = new ApplicationConfiguration();
            return new ConfidentialClientApplicationBuilder(config)
                   .WithOptions(options).WithClientSecret(options.ClientSecret);
        }

        /// <summary>
        /// Creates a ConfidentialClientApplicationBuilder from a clientID.
        /// See https://aka.ms/msal-net-application-configuration
        /// </summary>
        /// <param name="clientId">Client ID (also known as App ID) of the application as registered in the
        /// application registration portal (https://aka.ms/msal-net-register-app)/.</param>
        /// <returns>A <see cref="ConfidentialClientApplicationBuilder"/> from which to set more
        /// parameters, and to create a public client application instance</returns>
        public static ConfidentialClientApplicationBuilder Create(string clientId)
        {
            var config = new ApplicationConfiguration();
            return new ConfidentialClientApplicationBuilder(config).WithClientId(clientId);
        }

        /// <summary>
        /// Sets the certificate associated with the application
        /// </summary>
        /// <param name="certificate">The X509 certificate used as credentials to prove the identity of the application to Azure AD.</param>
        /// <remarks>You should use certificates with a private key size of at least 2048 bytes. Future versions of this library might reject certificates with smaller keys. </remarks>
        public ConfidentialClientApplicationBuilder WithCertificate(X509Certificate2 certificate)
        {
            if (certificate == null)
            {
                throw new ArgumentNullException(nameof(certificate));
            }

            Config.ClientCredentialCertificate = certificate;
            Config.ConfidentialClientCredentialCount++;
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
        /// <remarks>You should use certificates with a private key size of at least 2048 bytes. Future versions of this library might reject certificates with smaller keys. </remarks>
        public ConfidentialClientApplicationBuilder WithClientClaims(X509Certificate2 certificate, IDictionary<string, string> claimsToSign, bool mergeWithDefaultClaims = true)
        {
            if (certificate == null)
            {
                throw new ArgumentNullException(nameof(certificate));
            }

            if (claimsToSign == null || !claimsToSign.Any())
            {
                throw new ArgumentNullException(nameof(claimsToSign));
            }

            Config.ClientCredentialCertificate = certificate;
            Config.ClaimsToSign = claimsToSign;
            Config.MergeWithDefaultClaims = mergeWithDefaultClaims;
            Config.ConfidentialClientCredentialCount++;
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

            Config.ClientSecret = clientSecret;
            Config.ConfidentialClientCredentialCount++;
            return this;
        }

        /// <summary>
        /// Sets the application client assertion. See https://aka.ms/msal-net-client-assertion
        /// </summary>
        /// <param name="signedClientAssertion">The client assertion used to prove the identity of the application to Azure AD. This is a Base-64 encoded JWT.</param>
        /// <returns></returns>
        public ConfidentialClientApplicationBuilder WithClientAssertion(string signedClientAssertion)
        {
            if (string.IsNullOrWhiteSpace(signedClientAssertion))
            {
                throw new ArgumentNullException(nameof(signedClientAssertion));
            }

            Config.SignedClientAssertion = signedClientAssertion;
            Config.ConfidentialClientCredentialCount++;
            return this;
        }

        /// <inheritdoc />
        internal override void Validate()
        {
            base.Validate();

            Config.ClientCredential = new ClientCredentialWrapper(Config);

            if (string.IsNullOrWhiteSpace(Config.RedirectUri))
            {
                Config.RedirectUri = Constants.DefaultConfidentialClientRedirectUri;
            }

            if (!Uri.TryCreate(Config.RedirectUri, UriKind.Absolute, out Uri uriResult))
            {
                throw new InvalidOperationException(MsalErrorMessage.InvalidRedirectUriReceived(Config.RedirectUri));
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
#endif
}
