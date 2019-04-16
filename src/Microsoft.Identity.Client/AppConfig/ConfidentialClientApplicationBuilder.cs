// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
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
        /// <returns></returns>
        public ConfidentialClientApplicationBuilder WithCertificate(X509Certificate2 certificate)
        {
            Config.ClientCredentialCertificate = certificate;
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
            Config.ClientSecret = clientSecret;
            return this;
        }

        /// <inheritdoc />
        internal override void Validate()
        {
            base.Validate();

            int countOfCredentialTypesSpecified = 0;

            if (!string.IsNullOrWhiteSpace(Config.ClientSecret))
            {
                countOfCredentialTypesSpecified++;
            }

            if (Config.ClientCredentialCertificate != null)
            {
                countOfCredentialTypesSpecified++;
            }

            if (Config.ClientCredential != null)
            {
                countOfCredentialTypesSpecified++;
            }

            if (countOfCredentialTypesSpecified > 1)
            {
                throw new InvalidOperationException(MsalErrorMessage.ClientSecretAndCertificateAreMutuallyExclusive);
            }

            if (!string.IsNullOrWhiteSpace(Config.ClientSecret))
            {
                Config.ClientCredential = new ClientCredentialWrapper(Config.ClientSecret);
            }

            if (Config.ClientCredentialCertificate != null)
            {
                Config.ClientCredential = new ClientCredentialWrapper(new ClientAssertionCertificateWrapper(Config.ClientCredentialCertificate));
            }

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
