// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Internal
{
    /// <summary>
    /// Meant to be used in confidential client applications, an instance of <c>ClientCredential</c> is passed
    /// to the constructors of (<see cref="ConfidentialClientApplication"/>)
    /// as credentials proving that the application (the client) is what it claims it is. These credentials can be
    /// either a client secret (an application password) or a certificate.
    /// This class has one constructor for each case.
    /// These credentials are added in the application registration portal (in the secret section).
    /// </summary>
    /// <remarks>
    /// Important: this object is held at APP level and shared between all request, so it MUST be kept immutable
    /// </remarks>
    internal sealed class ClientCredentialWrapper
    {
        public ClientCredentialWrapper(ApplicationConfiguration config)
        {
            ConfidentialClientApplication.GuardMobileFrameworks();            

            if (config.ConfidentialClientCredentialCount > 1)
            {
                throw new MsalClientException(MsalError.ClientCredentialAuthenticationTypesAreMutuallyExclusive, MsalErrorMessage.ClientCredentialAuthenticationTypesAreMutuallyExclusive);
            }

            AuthenticationType = ConfidentialClientAuthenticationType.None;

            if (!string.IsNullOrWhiteSpace(config.ClientSecret))
            {
                AuthenticationType = ConfidentialClientAuthenticationType.ClientSecret;
            }

            if (config.ClientCredentialCertificate != null)
            {
                if (config.ClaimsToSign != null && config.ClaimsToSign.Any())
                {
                    AuthenticationType = ConfidentialClientAuthenticationType.ClientCertificateWithClaims;
                    AppendDefaultClaims = config.MergeWithDefaultClaims;
                }
                else
                {
                    AuthenticationType = ConfidentialClientAuthenticationType.ClientCertificate;
                }
            }

            if (!string.IsNullOrWhiteSpace(config.SignedClientAssertion))
            {
                AuthenticationType = ConfidentialClientAuthenticationType.SignedClientAssertion;
            }

            if (config.SignedClientAssertionDelegate != null)
            {
                AuthenticationType = ConfidentialClientAuthenticationType.SignedClientAssertionDelegate;
            }

            switch (AuthenticationType)
            {
                case ConfidentialClientAuthenticationType.None:
                    break;
                case ConfidentialClientAuthenticationType.ClientCertificate:
                    Certificate = config.ClientCredentialCertificate;
                    break;
                case ConfidentialClientAuthenticationType.ClientCertificateWithClaims:
                    Certificate = config.ClientCredentialCertificate;
                    ClaimsToSign = config.ClaimsToSign;
                    break;
                case ConfidentialClientAuthenticationType.ClientSecret:
                    Secret = config.ClientSecret;
                    break;
                case ConfidentialClientAuthenticationType.SignedClientAssertion:
                    SignedAssertion = config.SignedClientAssertion;
                    break;
                case ConfidentialClientAuthenticationType.SignedClientAssertionDelegate:
                    SignedAssertionDelegate = config.SignedClientAssertionDelegate;
                    break;
                default:
                    throw new NotImplementedException();
            }

            if (Certificate != null)
            {
                Thumbprint = Base64UrlHelpers.Encode(Certificate.GetCertHash());
            }
        }

        #region TestBuilders
        //The following builders methods are intended for testing
        public static ClientCredentialWrapper CreateWithCertificate(X509Certificate2 certificate, IDictionary<string, string> claimsToSign = null)
        {
            ApplicationConfiguration applicationConfiguration = new ApplicationConfiguration();

            applicationConfiguration.ClientCredentialCertificate = certificate;
            applicationConfiguration.ConfidentialClientCredentialCount = 1;
            applicationConfiguration.ClaimsToSign = claimsToSign;

            return new ClientCredentialWrapper(applicationConfiguration);
        }

        public static ClientCredentialWrapper CreateWithSecret(string secret)
        {
            ApplicationConfiguration applicationConfiguration = new ApplicationConfiguration();
            applicationConfiguration.ClientSecret = secret;
            applicationConfiguration.ConfidentialClientCredentialCount = 1;

            return new ClientCredentialWrapper(applicationConfiguration);
        }

        #endregion TestBuilders       

        public static int MinKeySizeInBits { get; } = 2048;
        internal string Thumbprint { get; }
        internal X509Certificate2 Certificate { get; }
        // The cached assertion created from the JWT signing operation
        internal string Secret { get; }
        // The signed assertion passed in by the user
        internal string SignedAssertion { get; }
        internal Func<CancellationToken, Task<string>> SignedAssertionDelegate { get; }
        internal bool AppendDefaultClaims { get; }
        internal ConfidentialClientAuthenticationType AuthenticationType { get; }
        internal IDictionary<string, string> ClaimsToSign { get; }

        public async Task AddConfidentialClientParametersAsync(
            OAuth2Client oAuth2Client,
            ICoreLogger logger,
            ICryptographyManager cryptographyManager,
            string clientId,
            Authority authority,
            bool sendX5C,
            CancellationToken cancellationToken)
        {
            using (logger.LogMethodDuration())
            {
                switch (AuthenticationType)
                {
                    case ConfidentialClientAuthenticationType.None:
                        // allow this to account for extensiblity cases where the params are injected into the request
                        break;
                    case ConfidentialClientAuthenticationType.ClientCertificate:
                        string tokenEndpoint = authority.GetTokenEndpoint();

                        var jwtToken2 = new JsonWebToken(
                           cryptographyManager,
                           clientId,
                           tokenEndpoint);

                        string assertion2 = jwtToken2.Sign(this, sendX5C);

                        oAuth2Client.AddBodyParameter(OAuth2Parameter.ClientAssertionType, OAuth2AssertionType.JwtBearer);
                        oAuth2Client.AddBodyParameter(OAuth2Parameter.ClientAssertion, assertion2);

                        break;
                    case ConfidentialClientAuthenticationType.ClientCertificateWithClaims:
                        tokenEndpoint = authority.GetTokenEndpoint();

                        var jwtToken = new JsonWebToken(
                            cryptographyManager,
                            clientId,
                            tokenEndpoint,
                            ClaimsToSign,
                            AppendDefaultClaims);
                        string assertion = jwtToken.Sign(this, sendX5C);

                        oAuth2Client.AddBodyParameter(OAuth2Parameter.ClientAssertionType, OAuth2AssertionType.JwtBearer);
                        oAuth2Client.AddBodyParameter(OAuth2Parameter.ClientAssertion, assertion);

                        break;
                    case ConfidentialClientAuthenticationType.ClientSecret:
                        oAuth2Client.AddBodyParameter(OAuth2Parameter.ClientSecret, Secret);
                        break;
                    case ConfidentialClientAuthenticationType.SignedClientAssertion:
                        oAuth2Client.AddBodyParameter(OAuth2Parameter.ClientAssertionType, OAuth2AssertionType.JwtBearer);
                        oAuth2Client.AddBodyParameter(OAuth2Parameter.ClientAssertion, SignedAssertion);
                        break;
                    case ConfidentialClientAuthenticationType.SignedClientAssertionDelegate:
                        oAuth2Client.AddBodyParameter(OAuth2Parameter.ClientAssertionType, OAuth2AssertionType.JwtBearer);
                        string signedAssertion = await SignedAssertionDelegate(cancellationToken).ConfigureAwait(false);
                        oAuth2Client.AddBodyParameter(OAuth2Parameter.ClientAssertion, signedAssertion);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        internal enum ConfidentialClientAuthenticationType
        {
            None = 0,
            ClientCertificate,
            ClientCertificateWithClaims,
            ClientSecret,
            SignedClientAssertion,
            SignedClientAssertionDelegate
        }
    }
}
