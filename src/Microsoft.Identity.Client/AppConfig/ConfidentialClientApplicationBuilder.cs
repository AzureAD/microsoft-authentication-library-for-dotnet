// ------------------------------------------------------------------------------
// 
// Copyright (c) Microsoft Corporation.
// All rights reserved.
// 
// This code is licensed under the MIT License.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// ------------------------------------------------------------------------------

using System;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client.AppConfig
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
        /// Sets the Application token cache. This cache is used in client credential flows
        /// (<see cref="IConfidentialClientApplication.AcquireTokenForClientAsync(System.Collections.Generic.IEnumerable{string})"/>
        /// and its override
        /// </summary>
        /// <param name="tokenCache">Application token cache</param>
        /// <returns></returns>
        [Obsolete("You can access the AppTokenCache using the AppTokenCache property on the created IConfidentialClientApplication")]
        public ConfidentialClientApplicationBuilder WithAppTokenCache(TokenCache tokenCache)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets the user token cache. In a confidential client application, you should ensure that there is
        /// one cache per user.
        /// </summary>
        /// <param name="tokenCache">User token cache</param>
        /// <returns></returns>
        [Obsolete("You can access the UserTokenCache using the UserTokenCache property on the created IConfidentialClientApplication")]
        public ConfidentialClientApplicationBuilder WithUserTokenCache(TokenCache tokenCache)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets the certificate associated with the application
        /// </summary>
        /// <param name="certificate">The X509 certificate used as credentials to prove the identity of the application to Azure AD.</param>
        /// <returns></returns>
        public ConfidentialClientApplicationBuilder WithX509Certificate2(X509Certificate2 certificate)
        {
            Config.Certificate = certificate;
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

        // This is for back-compat with oldstyle API.  Once we deprecate that, we can remove this.
        internal ConfidentialClientApplicationBuilder WithClientCredential(ClientCredential clientCredential)
        {
            Config.ClientCredential = clientCredential;
            return this;
        }

        /// <inheritdoc />
        internal override ApplicationConfiguration BuildConfiguration()
        {
            base.BuildConfiguration();

            int countOfCredentialTypesSpecified = 0;

            if (!string.IsNullOrWhiteSpace(Config.ClientSecret))
            {
                countOfCredentialTypesSpecified++;
            }

            if (Config.Certificate != null)
            {
                countOfCredentialTypesSpecified++;
            }

            if (Config.ClientCredential != null)
            {
                countOfCredentialTypesSpecified++;
            }

            if (countOfCredentialTypesSpecified > 1)
            {
                // TODO(migration): move text into string literals file.
                throw new InvalidOperationException(
                    "ClientSecret and Certificate are mutually exclusive properties.  Only specify one.");
            }

            if (!string.IsNullOrWhiteSpace(Config.ClientSecret))
            {
                Config.ClientCredential = new ClientCredential(Config.ClientSecret);
            }

            if (Config.Certificate != null)
            {
                Config.ClientCredential = new ClientCredential(new ClientAssertionCertificate(Config.Certificate));
            }

            return Config;
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