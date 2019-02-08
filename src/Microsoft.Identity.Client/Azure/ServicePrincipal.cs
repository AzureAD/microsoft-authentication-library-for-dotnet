using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Instance;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Azure
{
#if !ANDROID_BUILDTIME && !iOS_BUILDTIME && !WINDOWS_APP_BUILDTIME && !MAC_BUILDTIME // Hide confidential client on mobile platforms

    /// <summary>
    ///     ServicePrincipalProbe looks to the application setting and environment variables to build a ICredentialProvider.
    /// </summary>
    public class ServicePrincipalProbe : IProbe
    {
        private readonly IServicePrincipalConfiguration _config;

        /// <summary>
        /// Create a new instance of a ServicePrincipalProbe
        /// </summary>
        /// <param name="config">optional configuration; if not specified the default configuration will use environment variables</param>
        public ServicePrincipalProbe(IServicePrincipalConfiguration config = null)
        {
            _config = config ?? new DefaultServicePrincipalConfiguration();
        }

        // Async method lacks 'await' operators and will run synchronously
        /// <summary>
        /// Check if the probe is available for use in the current environment
        /// </summary>
        /// <returns>True if a credential provider can be built</returns>
        public Task<bool> AvailableAsync() => Task.FromResult(IsClientSecret() || IsClientCertificate());

        /// <summary>
        /// Create a credential provider from the information discovered by the probe
        /// </summary>
        /// <returns>A service principal credential provider</returns>
        public async Task<ITokenProvider> ProviderAsync()
        {
            var available = await AvailableAsync().ConfigureAwait(false);
            if (!available)
            {
                throw new InvalidOperationException("The required environment variables are not available.");
            }

            var authorityWithTenant = string.Format(CultureInfo.InvariantCulture, AadAuthority.AADCanonicalAuthorityTemplate, _config.Authority, _config.TenantId);

            if (!IsClientCertificate())
            {
                return new ServicePrincipalTokenProvider(authorityWithTenant, _config.TenantId, _config.ClientId, _config.ClientSecret);
            }

            X509Certificate2 cert;
            if (!string.IsNullOrWhiteSpace(_config.CertificateBase64))
            {
                // If the certificate is provided as base64 encoded string in env, decode and hydrate a x509 cert
                var decoded = Convert.FromBase64String(_config.CertificateBase64);
                cert = new X509Certificate2(decoded);
            }
            else
            {
                // Try to use the certificate store
                var store = new X509Store(StoreNameWithDefault, StoreLocationFromEnv);
                store.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection certs;
                if(!string.IsNullOrEmpty(_config.CertificateSubjectDistinguishedName))
                {
                    certs = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, _config.CertificateSubjectDistinguishedName, true);
                }
                else
                {
                    certs = store.Certificates.Find(X509FindType.FindByThumbprint, _config.CertificateThumbprint, true);
                }

                if (certs.Count < 1)
                {
                    throw new InvalidOperationException(
                        $"Unable to find certificate with thumbprint '{_config.CertificateThumbprint}' in certificate store named '{StoreNameWithDefault}' and store location {StoreLocationFromEnv}");
                }

                cert = certs[0];
            }

            return new ServicePrincipalTokenProvider(authorityWithTenant, _config.TenantId, _config.ClientId, cert);
        }

        internal StoreLocation StoreLocationFromEnv
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

        internal string StoreNameWithDefault
        {
            get
            {
                var name = _config.CertificateStoreName;
                return string.IsNullOrWhiteSpace(name) ? "My" : name;
            }
        }

        internal bool IsClientSecret()
        {
            var vars = new List<string> { _config.TenantId, _config.ClientId, _config.ClientSecret };
            return vars.All(item => !string.IsNullOrWhiteSpace(item));
        }

        internal bool IsClientCertificate()
        {
            var tenantAndClient = new List<string> { _config.TenantId, _config.ClientId };
            var env = Environment.GetEnvironmentVariables();
            if (tenantAndClient.All(item => !string.IsNullOrWhiteSpace(item)))
            {
                return !string.IsNullOrWhiteSpace(_config.CertificateBase64) ||
                       !string.IsNullOrWhiteSpace(_config.CertificateThumbprint);
            }

            return false;
        }
    }

    /// <summary>
    /// IManagedIdentityConfiguration provides the configurable properties for the ManagedIdentityProbe
    /// </summary>
    public interface IServicePrincipalConfiguration
    {
        /// <summary>
        /// CertificateBase64 is the base64 encoded representation of an x509 certificate
        /// </summary>
        string CertificateBase64 { get; }

        /// <summary>
        /// CertificateThumbprint is the thumbprint of the certificate in the Windows Certificate Store
        /// </summary>
        string CertificateThumbprint { get; }

        /// <summary>
        /// CertificateSubjectDistinguishedName is the subject distinguished name of the certificate in the Windows Certificate Store
        /// </summary>
        string CertificateSubjectDistinguishedName { get; }

        /// <summary>
        /// CertificateStoreName is the name of the certificate store on Windows where the certificate is stored
        /// </summary>
        string CertificateStoreName { get; }

        /// <summary>
        /// CertificateStoreLocation is the location of the certificate store on Windows where the certificate is stored
        /// </summary>
        string CertificateStoreLocation { get; }

        /// <summary>
        /// TenantId is the AAD TenantID
        /// </summary>
        string TenantId { get; }

        /// <summary>
        /// ClientId is the service principal (application) ID
        /// </summary>
        string ClientId { get; }

        /// <summary>
        /// ClientSecret is the service principal (application) string secret
        /// </summary>
        string ClientSecret { get; }

        /// <summary>
        /// Authority is the URI pointing to the AAD endpoint
        /// </summary>
        string Authority { get; }
    }

    internal class DefaultServicePrincipalConfiguration : IServicePrincipalConfiguration
    {
        public string ClientId => Env.ClientId;

        public string CertificateBase64 => Env.CertificateBase64;

        public string CertificateThumbprint => Env.CertificateThumbprint;

        public string CertificateStoreName => Env.CertificateStoreName;

        public string TenantId => Env.TenantId;

        public string ClientSecret => Env.ClientSecret;

        public string CertificateStoreLocation => Env.CertificateStoreLocation;

        public string CertificateSubjectDistinguishedName => Env.CertificateSubjectDistinguishedName;

        public string Authority => string.IsNullOrWhiteSpace(Env.AadAuthority) ? AadAuthority.DefaultTrustedHost : Env.AadAuthority;
    }

    /// <summary>
    /// ServicePrincipalTokenProvider fetches an AAD token provided Service Principal credentials.
    /// </summary>
    public class ServicePrincipalTokenProvider : ITokenProvider
    {
        private readonly ConfidentialClientApplication _client;

        internal ServicePrincipalTokenProvider(string authority, string tenantId, string clientId, ClientCredential cred, IHttpManager httpManager)
        {
            _client = ConfidentialClientApplicationBuilder.Create(clientId)
                .WithTenantId(tenantId)
                .WithAuthority(new Uri(authority), true)
                .WithClientCredential(cred)
                .WithHttpManager(httpManager)
                .BuildConcrete();
        }

        /// <summary>
        ///     ServicePrincipalCredentialProvider constructor to build the provider with a certificate
        /// </summary>
        /// <param name="authority">Hostname of the security token service (STS) from which MSAL.NET will acquire the tokens. Ex: login.microsoftonline.com
        /// </param>
        /// <param name="tenantId">A string representation for a GUID, which is the ID of the tenant where the account resides</param>
        /// <param name="clientId">A string representation for a GUID ClientId (application ID) of the application</param>
        /// <param name="cert">A ClientAssertionCertificate which is the certificate secret for the application</param>
        public ServicePrincipalTokenProvider(string authority, string tenantId, string clientId, X509Certificate2 cert)
        {
            _client = ConfidentialClientApplicationBuilder.Create(clientId)
                .WithTenantId(tenantId)
                .WithAuthority(new Uri(authority), true)
                .WithCertificate(cert)
                .BuildConcrete();
        }

        /// <summary>
        ///     ServicePrincipalCredentialProvider constructor to build the provider with a string secret
        /// </summary>
        /// <param name="authority">Hostname of the security token service (STS) from which MSAL.NET will acquire the tokens. Ex: login.microsoftonline.com
        /// </param>
        /// <param name="tenantId">A string representation for a GUID, which is the ID of the tenant where the account resides</param>
        /// <param name="clientId">A string representation for a GUID ClientId (application ID) of the application</param>
        /// <param name="secret">A string secret for the application</param>
        public ServicePrincipalTokenProvider(string authority, string tenantId, string clientId, string secret)
        {
            _client = ConfidentialClientApplicationBuilder.Create(clientId)
                .WithTenantId(tenantId)
                .WithAuthority(new Uri(authority), true)
                .WithClientSecret(secret)
                .BuildConcrete();
        }

        /// <summary>
        ///     GetTokenAsync returns a token for a given set of scopes
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <returns>A token with expiration</returns>
        public async Task<IToken> GetTokenAsync(IEnumerable<string> scopes = null)
        {
            var res = await _client.AcquireTokenForClientAsync(scopes).ConfigureAwait(false);
            return new AccessTokenWithExpiration { ExpiresOn = res.ExpiresOn, AccessToken = res.AccessToken };
        }
    }

#endif
}
