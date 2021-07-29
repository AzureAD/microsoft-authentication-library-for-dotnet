// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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
    internal sealed class ClientCredentialWrapper
    {
        public ClientCredentialWrapper(ApplicationConfiguration config)
        {
            ConfidentialClientApplication.GuardMobileFrameworks();

            ValidateInput(config);

            switch (AuthenticationType)
            {
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
            }
        }

        #region TestBuilders
        //The following builders methods are intended for testing
        public static ClientCredentialWrapper CreateWithCertificate(X509Certificate2 certificate, IDictionary<string, string> claimsToSign = null)
        {
            return new ClientCredentialWrapper(certificate, claimsToSign);
        }

        public static ClientCredentialWrapper CreateWithSecret(string secret)
        {
            var app = new ClientCredentialWrapper(secret, ConfidentialClientAuthenticationType.ClientSecret);
            app.AuthenticationType = ConfidentialClientAuthenticationType.ClientSecret;
            return app;
        }

        public static ClientCredentialWrapper CreateWithSignedClientAssertion(string signedClientAssertion)
        {
            var app = new ClientCredentialWrapper(signedClientAssertion, ConfidentialClientAuthenticationType.SignedClientAssertion);
            app.AuthenticationType = ConfidentialClientAuthenticationType.SignedClientAssertion;
            return app;
        }

        private ClientCredentialWrapper(X509Certificate2 certificate, IDictionary<string, string> claimsToSign = null)
        {
            ConfidentialClientApplication.GuardMobileFrameworks();

            Certificate = certificate;

            if (claimsToSign != null && claimsToSign.Any())
            {
                ClaimsToSign = claimsToSign;
                AuthenticationType = ConfidentialClientAuthenticationType.ClientCertificateWithClaims;
                return;
            }

            AuthenticationType = ConfidentialClientAuthenticationType.ClientCertificate;
        }

        private ClientCredentialWrapper(string secretOrAssertion, ConfidentialClientAuthenticationType authType)
        {
            ConfidentialClientApplication.GuardMobileFrameworks();

            if (authType == ConfidentialClientAuthenticationType.SignedClientAssertion)
            {
                SignedAssertion = secretOrAssertion;
            }
            else
            {
                Secret = secretOrAssertion;
            }
        }

        #endregion TestBuilders       

        private void ValidateInput(ApplicationConfiguration config)
        {
            if (config.ConfidentialClientCredentialCount > 1)
            {
                throw new MsalClientException(MsalError.ClientCredentialAuthenticationTypesAreMutuallyExclusive, MsalErrorMessage.ClientCredentialAuthenticationTypesAreMutuallyExclusive);
            }

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

            if (AuthenticationType == ConfidentialClientAuthenticationType.None)
            {
                throw new MsalClientException(
                    MsalError.ClientCredentialAuthenticationTypeMustBeDefined,
                    MsalErrorMessage.ClientCredentialAuthenticationTypeMustBeDefined);
            }
        }

        internal byte[] Sign(ICryptographyManager cryptographyManager, string message)
        {
            return cryptographyManager.SignWithCertificate(message, Certificate);
        }

        public static int MinKeySizeInBits { get; } = 2048;
        internal string Thumbprint { get { return Base64UrlHelpers.Encode(Certificate.GetCertHash()); } }
        internal X509Certificate2 Certificate { get; private set; }
        // The cached assertion created from the JWT signing operation
        internal string CachedAssertion { get; set; }
        internal long ValidTo { get; set; }
        internal bool ContainsX5C { get; set; }
        internal string Audience { get; set; }
        internal string Secret { get; private set; }
        // The signed assertion passed in by the user
        internal string SignedAssertion { get; private set; }
        internal Func<string> SignedAssertionDelegate { get; private set; }
        internal bool AppendDefaultClaims { get; private set; }
        internal ConfidentialClientAuthenticationType AuthenticationType { get; private set; }
        internal IDictionary<string, string> ClaimsToSign { get; private set; }

        public Dictionary<string, string> CreateClientCredentialBodyParameters(
            ICoreLogger logger,
            ICryptographyManager cryptographyManager,
            string clientId,
            Authority authority,
            bool sendX5C,
            bool withCaching = false) // TODO: for testing only, pls remove
        {
            using (logger.LogMethodDuration())
            {
                Dictionary<string, string> parameters = new Dictionary<string, string>();

                switch (AuthenticationType)
                {                  
                    case ConfidentialClientAuthenticationType.ClientCertificate:
                        parameters[OAuth2Parameter.ClientAssertionType] = OAuth2AssertionType.JwtBearer;
                        string tokenEndpoint = authority.GetTokenEndpoint();

                        if (withCaching)
                        {

                            if (CachedAssertion == null ||
                                !CanUseCachedAssertion(tokenEndpoint, sendX5C))
                            {
                                var jwtToken2 = new JsonWebToken(
                                   cryptographyManager,
                                   clientId,
                                   tokenEndpoint);

                                CachedAssertion = jwtToken2.Sign(this, sendX5C);
                                ValidTo = jwtToken2.ValidTo;
                                ContainsX5C = sendX5C;
                                Audience = tokenEndpoint;

                            }
                            parameters[OAuth2Parameter.ClientAssertion] = CachedAssertion;

                        }
                        else
                        {
                            var jwtToken2 = new JsonWebToken(
                                   cryptographyManager,
                                   clientId,
                                   tokenEndpoint);
                            parameters[OAuth2Parameter.ClientAssertion] = jwtToken2.Sign(this, sendX5C);
                        }



                        break;
                    case ConfidentialClientAuthenticationType.ClientCertificateWithClaims:
                        parameters[OAuth2Parameter.ClientAssertionType] = OAuth2AssertionType.JwtBearer;
                        tokenEndpoint = authority.GetTokenEndpoint();

                        var jwtToken = new JsonWebToken(
                            cryptographyManager,
                            clientId,
                            tokenEndpoint,
                            ClaimsToSign,
                            AppendDefaultClaims);
                        parameters[OAuth2Parameter.ClientAssertion] = jwtToken.Sign(this, sendX5C);
                        break;
                    case ConfidentialClientAuthenticationType.ClientSecret:
                        parameters[OAuth2Parameter.ClientSecret] = Secret;
                        break;
                    case ConfidentialClientAuthenticationType.SignedClientAssertion:
                        parameters[OAuth2Parameter.ClientAssertionType] = OAuth2AssertionType.JwtBearer;
                        parameters[OAuth2Parameter.ClientAssertion] = SignedAssertion;
                        break;
                    case ConfidentialClientAuthenticationType.SignedClientAssertionDelegate:
                        parameters[OAuth2Parameter.ClientAssertionType] = OAuth2AssertionType.JwtBearer;
                        parameters[OAuth2Parameter.ClientAssertion] = SignedAssertionDelegate();
                        break;
                    default:
                        throw new NotImplementedException();


                }
                return parameters;
            }
        }

        /// <summary>
        ///     Determines whether or not the cached client assertion can be used again for the next authentication request by
        ///     checking its values against incoming request parameters.
        /// </summary>
        /// <returns>Returns true if the previously cached client assertion is valid</returns>
        public bool CanUseCachedAssertion(string audience, bool sendX5C)
        {          
            if (string.IsNullOrWhiteSpace(CachedAssertion))
            {
                return false;
            }

            //Check if all current client assertion values match incoming parameters and expiration time
            //The clientCredential object contains the previously used values in the cached client assertion string
            bool expired = ValidTo <=
                           JsonWebToken.ConvertToTimeT(
                               DateTime.UtcNow + TimeSpan.FromMinutes(Constants.ExpirationMarginInMinutes));

            bool parametersMatch = string.Equals(Audience, audience, StringComparison.OrdinalIgnoreCase) &&
                                   ContainsX5C == sendX5C;

            return !expired && parametersMatch;
        }
    }

    internal enum ConfidentialClientAuthenticationType
    {
        None,
        ClientCertificate,
        ClientCertificateWithClaims,
        ClientSecret,
        SignedClientAssertion,
        SignedClientAssertionDelegate
    }
}
