//----------------------------------------------------------------------
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
//------------------------------------------------------------------------------

using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;

namespace Microsoft.Identity.Test.LabInfrastructure
{
    public class KeyVaultSecretsProvider : IDisposable
    {
        /// <summary>
        /// Token cache used by the test infrastructure when authenticating against KeyVault
        /// </summary>
        /// <remarks>We aren't using the default cache to make sure the tokens used by this
        /// test infrastructure can't end up in the cache being used by the tests (the UI-less
        /// Desktop test app runs in the same AppDomain as the infrastructure and uses the
        /// default cache).</remarks>
        private readonly static TokenCache keyVaultTokenCache = new TokenCache();

        private KeyVaultClient _keyVaultClient;

        private KeyVaultConfiguration _config;

        private readonly string _keyVaultConfidentialClientID = "ebe49c8f-61de-4357-9194-7a786f6402b4";
        private readonly string _keyVaultPublicClientID = "3c1e0e0d-b742-45ba-a35e-01c664e14b16";

        private readonly string _keyVaultThumbPrint = "440A5BE6C4BE2FF02A0ADBED1AAA43D6CF12E269";

        private readonly string _dataFileName = "data.txt";

        private AuthenticationResult _authResult;

        /// <summary>Initialize the secrets provider with the "keyVault" configuration section.</summary>
        /// <remarks>
        /// <para>
        /// Authentication using <see cref="KeyVaultAuthenticationType.ClientCertificate"/>
        ///     1. Register Azure AD application of "Web app / API" type.
        ///        To set up certificate based access to the application PowerShell should be used.
        ///     2. Add an access policy entry to target Key Vault instance for this application.
        ///
        ///     The "keyVault" configuration section should define:
        ///         "authType": "ClientCertificate"
        ///         "clientId": [client ID]
        ///         "certThumbprint": [certificate thumbprint]
        /// </para>
        /// <para>
        /// Authentication using <see cref="KeyVaultAuthenticationType.UserCredential"/>
        ///     1. Register Azure AD application of "Native" type.
        ///     2. Add to 'Required permissions' access to 'Azure Key Vault (AzureKeyVault)' API.
        ///     3. When you run your native client application, it will automatically prompt user to enter Azure AD credentials.
        ///     4. To successfully access keys/secrets in the Key Vault, the user must have specific permissions to perform those operations.
        ///        This could be achieved by directly adding an access policy entry to target Key Vault instance for this user
        ///        or an access policy entry for an Azure AD security group of which this user is a member of.
        ///
        ///     The "keyVault" configuration section should define:
        ///         "authType": "UserCredential"
        ///         "clientId": [client ID]
        /// </para>
        /// </remarks>
        public KeyVaultSecretsProvider()
        {
            _config = new KeyVaultConfiguration
            {
                AuthType = KeyVaultAuthenticationType.ClientCertificate
            };

            //The data.txt is a place holder for the keyvault secret. It will only be written to during build time when testing appcenter.
            //After the tests are finished in appcenter, the file will be deleted from the appcenter servers.
            //The file will then be deleted locally Via VSTS task.
            if (File.Exists(_dataFileName))
            {
                var data = File.ReadAllText(_dataFileName);

                if (!string.IsNullOrWhiteSpace(data))
                {
                    _config.AuthType = KeyVaultAuthenticationType.ClientSecret;
                    _config.KeyVaultSecret = data;
                }
            }

            _config.ClientId =_config.AuthType == KeyVaultAuthenticationType.UserCredential? _keyVaultPublicClientID : _keyVaultConfidentialClientID;
            _config.CertThumbprint = _keyVaultThumbPrint;
            _keyVaultClient = new KeyVaultClient(AuthenticationCallbackAsync);
        }

        ~KeyVaultSecretsProvider()
        {
            Dispose();
        }

        public SecretBundle GetSecret(string secretUrl)
        {
            return _keyVaultClient.GetSecretAsync(secretUrl).GetAwaiter().GetResult();
        }

        private async Task<string> AuthenticationCallbackAsync(string authority, string resource, string scope)
        {
            if (_authResult != null)
            {
                return _authResult.AccessToken;
            }

            var scopes = new[] { resource + "/.default" };

            AuthenticationResult authResult;
            IConfidentialClientApplication confidentialApp;
            IPublicClientApplication publicApp;
            X509Certificate2 cert = null;
            switch (_config.AuthType)
            {
                case KeyVaultAuthenticationType.ClientCertificate:
                    cert = CertificateHelper.FindCertificateByThumbprint(_config.CertThumbprint);
                    if (cert == null)
                    {
                        throw new InvalidOperationException(
                            "Test setup error - cannot find a certificate in the My store for KeyVault. This is available for Microsoft employees only.");
                    }

                    confidentialApp = ConfidentialClientApplicationBuilder.Create(_config.ClientId)
                                              .WithAuthority(new Uri(authority), true)
                                              .WithCertificate(cert)
                                              .Build();

                    authResult = await confidentialApp.AcquireTokenForClientAsync(scopes).ConfigureAwait(false);
                    break;
                case KeyVaultAuthenticationType.ClientSecret:
                    confidentialApp = ConfidentialClientApplicationBuilder.Create(_config.ClientId)
                                              .WithAuthority(new Uri(authority), true)
                                              .WithClientSecret(_config.KeyVaultSecret)
                                              .Build();
                    authResult = await confidentialApp.AcquireTokenForClientAsync(scopes).ConfigureAwait(false);
                    break;
                case KeyVaultAuthenticationType.UserCredential:
                    publicApp = PublicClientApplicationBuilder.Create(_config.ClientId)
                                                .WithAuthority(new Uri(authority), true)
                                                .Build();
                    try
                    {
                        authResult = await publicApp.AcquireTokenByIntegratedWindowsAuthAsync(scopes).ConfigureAwait(false);
                    }
                    catch (MsalUiRequiredException ex)
                    {
                        publicApp = PublicClientApplicationBuilder.Create(_config.ClientId)
                                                .WithAuthority(new Uri(authority), true)
                                                .WithClaims(ex.Claims)
                                                .Build();

                        authResult = await publicApp.AcquireTokenAsync(scopes).ConfigureAwait(false);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _authResult = authResult;
            return authResult?.AccessToken;
        }

        public void Dispose()
        {
            if (_keyVaultClient != null)
            {
                _keyVaultClient.Dispose();
                _keyVaultClient = null;
            }

            GC.SuppressFinalize(this);
        }
    }
}
