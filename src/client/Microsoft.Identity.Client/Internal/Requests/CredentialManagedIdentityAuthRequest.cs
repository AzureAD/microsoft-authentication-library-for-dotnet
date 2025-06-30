// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Credential;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal sealed class CredentialManagedIdentityAuthRequest : ManagedIdentityAuthRequest
    {
        private readonly IMdsMetadataClient _meta;
        private readonly CredentialIssuerClient _issuer;
        private readonly CredentialStore _store;
        private readonly AccessTokenClient _tokenClient;

        public CredentialManagedIdentityAuthRequest(
            IServiceBundle sb,
            AuthenticationRequestParameters arp,
            AcquireTokenForManagedIdentityParameters p)
            : base(sb, arp, p)
        {
            _meta = new ImdsMetadataClient(sb.HttpManager, sb.ApplicationLogger, sb.Config.RetryPolicyFactory);
            _issuer = new CredentialIssuerClient(sb.HttpManager, sb.ApplicationLogger, sb.Config.RetryPolicyFactory);
            _store = new CredentialStore();
            _tokenClient = new AccessTokenClient(sb.HttpManager, sb.ApplicationLogger);
        }

        protected override async Task<AuthenticationResult> SendTokenRequestForManagedIdentityAsync(
            ILoggerAdapter log, CancellationToken ct)
        {
            await ResolveAuthorityAsync().ConfigureAwait(false);

            await s_semaphoreSlim.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                // 1. access-token cache?
                MsalAccessTokenCacheItem cached =
                    await CacheManager.FindAccessTokenAsync().ConfigureAwait(false);

                if (cached != null &&
                    !_managedIdentityParameters.ForceRefresh &&
                    string.IsNullOrEmpty(AuthenticationRequestParameters.Claims))
                {
                    return CreateAuthenticationResultFromCache(cached);
                }

                // 2. ensure MI credential (cert + metadata)
                if (!_store.TryGetValid(out X509Certificate2 cert,
                                        out ManagedIdentityCredentialResponse cred))
                {
                    PlatformMetadataResponse meta = await _meta.GetAsync(ct).ConfigureAwait(false);

                    KeyMaterial key = KeyGuardHelper.TryCreateKeyGuardKey() ??
                                      KeyGuardHelper.CreateRsaKey();

                    string csr = CsrHelper.Build(meta.ClientId, meta.TenantId, meta.Cuid, key);

                    cred = await _issuer.IssueAsync(csr,
                        meta.AttestationEndpoint, 
                        AuthenticationRequestParameters.RequestContext,
                        ct).ConfigureAwait(false);

                    cert = CredentialStore.Import(cred.Credential);
                    _store.Save(cert, cred);
                }

                // 3. mTLS token exchange
                MsalTokenResponse token = await _tokenClient
                    .GetTokenAsync(cert, cred, AuthenticationRequestParameters, ct)
                    .ConfigureAwait(false);

                return await CacheTokenResponseAndCreateAuthenticationResultAsync(token)
                             .ConfigureAwait(false);
            }
            finally
            {
                s_semaphoreSlim.Release();
            }
        }
    }
}
