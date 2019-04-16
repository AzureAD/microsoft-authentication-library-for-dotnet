// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Client.CacheV2.Impl;
using Microsoft.Identity.Client.CacheV2.Impl.Utils;
using Microsoft.Identity.Client.CacheV2.Schema;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.CacheV2
{
    /// <inheritdoc />
    internal class CacheManager : ICacheManager
    {
        private readonly AuthenticationRequestParameters _authParameters;
        private readonly IStorageManager _storageManager;

        public CacheManager(IStorageManager storageManager, AuthenticationRequestParameters authParameters)
        {
            _storageManager = storageManager;
            _authParameters = authParameters;
        }

        /// <inheritdoc />
        public bool TryReadCache(out MsalTokenResponse msalTokenResponse, out IAccount account)
        {
            msalTokenResponse = null;
            account = null;

            string homeAccountId = _authParameters.Account.HomeAccountId.Identifier;
            var authority = new Uri(_authParameters.AuthorityInfo.CanonicalAuthority);
            string environment = authority.GetEnvironment();
            string realm = authority.GetRealm();
            string clientId = _authParameters.ClientId;
            string target = ScopeUtils.JoinScopes(_authParameters.Scope);

            if (string.IsNullOrWhiteSpace(homeAccountId) || string.IsNullOrWhiteSpace(environment) ||
                string.IsNullOrWhiteSpace(realm) || string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(target))
            {
                msalTokenResponse = null;
                account = null;
                return false;
            }

            var credentialsResponse = _storageManager.ReadCredentials(
                string.Empty,
                homeAccountId,
                environment,
                realm,
                clientId,
                string.Empty,
                target,
                new HashSet<CredentialType>
                {
                    CredentialType.OAuth2AccessToken,
                    CredentialType.OAuth2RefreshToken,
                    CredentialType.OidcIdToken
                });

            if (credentialsResponse.Status.StatusType != OperationStatusType.Success)
            {
                // error reading credentials from the cache
                return false;
            }

            if (!credentialsResponse.Credentials.Any())
            {
                // no credentials found in the cache
                return false;
            }

            if (credentialsResponse.Credentials.ToList().Count > 3)
            {
                // expected to read up to 3 credentials from cache, somehow read more...
            }

            Credential accessToken = null;
            Credential refreshToken = null;
            Credential idToken = null;

            foreach (var credential in credentialsResponse.Credentials)
            {
                switch (credential.CredentialType)
                {
                case CredentialType.OAuth2AccessToken:
                    if (accessToken != null)
                    {
                        // warning, more than one access token read from cache
                    }

                    accessToken = credential;
                    break;
                case CredentialType.OAuth2RefreshToken:
                    if (refreshToken != null)
                    {
                        // warning, more than one refresh token read from cache
                    }

                    refreshToken = credential;
                    break;
                case CredentialType.OidcIdToken:
                    if (idToken != null)
                    {
                        // warning, more than one idtoken read from cache
                    }

                    idToken = credential;
                    break;
                default:
                    // warning unknown credential type
                    break;
                }
            }

            if (idToken == null)
            {
                // warning, no id token
            }

            if (accessToken == null)
            {
                // warning no access token
            }
            else if (!IsAccessTokenValid(accessToken))
            {
                DeleteCachedAccessToken(
                    homeAccountId,
                    environment,
                    realm,
                    clientId,
                    target);
                accessToken = null;
            }

            if (accessToken != null)
            {
                refreshToken = null; // there's no need to return a refresh token, just the access token
            }
            else if (refreshToken == null)
            {
                // warning, no valid access token and no refresh token found in cache
                return false;
            }

            IdToken idTokenJwt = null;
            if (idToken != null)
            {
                idTokenJwt = new IdToken(idToken.Secret);
            }

            if (accessToken != null)
            {
                var accountResponse = _storageManager.ReadAccount(string.Empty, homeAccountId, environment, realm);
                if (accountResponse.Status.StatusType != OperationStatusType.Success)
                {
                    // warning, error reading account from cache
                }
                else
                {
                    account = accountResponse.Account;
                }

                if (account == null)
                {
                    // warning, no account in cache, will still return token if found
                }
            }

            msalTokenResponse = new TokenResponse(idTokenJwt, accessToken, refreshToken).ToMsalTokenResponse();
            return true;
        }

        /// <inheritdoc />
        public IAccount CacheTokenResponse(MsalTokenResponse msalTokenResponse)
        {
            var tokenResponse = new TokenResponse(msalTokenResponse);

            string homeAccountId = GetHomeAccountId(tokenResponse);
            var authority = new Uri(_authParameters.AuthorityInfo.CanonicalAuthority);
            string environment = authority.GetEnvironment();
            string realm = authority.GetRealm();
            string clientId = _authParameters.ClientId;
            string target = ScopeUtils.JoinScopes(tokenResponse.GrantedScopes);

            if (string.IsNullOrWhiteSpace(homeAccountId) || string.IsNullOrWhiteSpace(environment) ||
                string.IsNullOrWhiteSpace(realm) || string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(target))
            {
                // skipping writing to the cache, PK is empty
                return null;
            }

            var credentialsToWrite = new List<Credential>();
            long cachedAt = DateTime.UtcNow.Ticks; // todo: this is probably wrong

            if (tokenResponse.HasRefreshToken)
            {
                credentialsToWrite.Add(
                    Credential.CreateRefreshToken(
                        homeAccountId,
                        environment,
                        clientId,
                        cachedAt,
                        tokenResponse.RefreshToken,
                        string.Empty));
            }

            if (tokenResponse.HasAccessToken)
            {
                long expiresOn = tokenResponse.ExpiresOn.Ticks; // todo: this is probably wrong
                long extendedExpiresOn = tokenResponse.ExtendedExpiresOn.Ticks; // todo: this is probably wrong

                var accessToken = Credential.CreateAccessToken(
                    homeAccountId,
                    environment,
                    realm,
                    clientId,
                    target,
                    cachedAt,
                    expiresOn,
                    extendedExpiresOn,
                    tokenResponse.AccessToken,
                    string.Empty);
                if (IsAccessTokenValid(accessToken))
                {
                    credentialsToWrite.Add(accessToken);
                }
            }

            var idTokenJwt = tokenResponse.IdToken;
            if (!idTokenJwt.IsEmpty)
            {
                credentialsToWrite.Add(
                    Credential.CreateIdToken(
                        homeAccountId,
                        environment,
                        realm,
                        clientId,
                        cachedAt,
                        idTokenJwt.Raw,
                        string.Empty));
            }

            var status = _storageManager.WriteCredentials(string.Empty, credentialsToWrite);
            if (status.StatusType != OperationStatusType.Success)
            {
                // warning error writing to cache
            }

            // if id token jwt is empty, return null

            string localAccountId = GetLocalAccountId(idTokenJwt);
            var authorityType = GetAuthorityType();

            var account = Microsoft.Identity.Client.CacheV2.Schema.Account.Create(
                homeAccountId,
                environment,
                realm,
                localAccountId,
                authorityType,
                idTokenJwt.PreferredUsername,
                idTokenJwt.GivenName,
                idTokenJwt.FamilyName,
                idTokenJwt.MiddleName,
                idTokenJwt.Name,
                idTokenJwt.AlternativeId,
                tokenResponse.RawClientInfo,
                string.Empty);

            status = _storageManager.WriteAccount(string.Empty, account);

            if (status.StatusType != OperationStatusType.Success)
            {
                // warning error writing account to cache
            }

            return account;
        }

        /// <inheritdoc />
        public void DeleteCachedRefreshToken()
        {
            string homeAccountId = _authParameters.Account.HomeAccountId.ToString();
            var authority = new Uri(_authParameters.AuthorityInfo.CanonicalAuthority);
            string environment = authority.GetEnvironment();
            string clientId = _authParameters.ClientId;

            if (string.IsNullOrWhiteSpace(homeAccountId) || string.IsNullOrWhiteSpace(environment) ||
                string.IsNullOrWhiteSpace(clientId))
            {
                // warning failed to delete refresh token from cache, pk is empty
                return;
            }

            var status = _storageManager.DeleteCredentials(
                string.Empty,
                homeAccountId,
                environment,
                string.Empty,
                clientId,
                string.Empty,
                string.Empty,
                new HashSet<CredentialType>
                {
                    CredentialType.OAuth2RefreshToken
                });
            if (status.StatusType != OperationStatusType.Success)
            {
                // warning, error deleting invalid refresh token from cache
            }
        }

        private void DeleteCachedAccessToken(
            string homeAccountId,
            string environment,
            string realm,
            string clientId,
            string target)
        {
            var status = _storageManager.DeleteCredentials(
                string.Empty,
                homeAccountId,
                environment,
                realm,
                clientId,
                string.Empty,
                target,
                new HashSet<CredentialType>
                {
                    CredentialType.OAuth2AccessToken
                });
            if (status.StatusType != OperationStatusType.Success)
            {
                // warning, failure deleting access token
            }
        }

        internal static string GetLocalAccountId(IdToken idTokenJwt)
        {
            string localAccountId = idTokenJwt.Oid;
            if (string.IsNullOrWhiteSpace(localAccountId))
            {
                localAccountId = idTokenJwt.Subject;
            }

            return localAccountId;
        }

        internal CacheV2AuthorityType GetAuthorityType()
        {
            var authority = new Uri(_authParameters.AuthorityInfo.CanonicalAuthority);

            string[] pathSegments = authority.GetPath().Split('/');
            if (pathSegments.Count() < 2)
            {
                return CacheV2AuthorityType.MsSts;
            }

            return string.Compare(pathSegments[1], "adfs", StringComparison.OrdinalIgnoreCase) == 0
                       ? CacheV2AuthorityType.Adfs
                       : CacheV2AuthorityType.MsSts;
        }

        internal static string GetHomeAccountId(TokenResponse tokenResponse)
        {
            if (!string.IsNullOrWhiteSpace(tokenResponse.Uid) && !string.IsNullOrWhiteSpace(tokenResponse.Utid))
            {
                return $"{tokenResponse.Uid}.{tokenResponse.Utid}";
            }

            var idToken = tokenResponse.IdToken;
            string homeAccountId = idToken.Upn;
            if (!string.IsNullOrWhiteSpace(homeAccountId))
            {
                return homeAccountId;
            }

            homeAccountId = idToken.Email;
            if (!string.IsNullOrWhiteSpace(homeAccountId))
            {
                return homeAccountId;
            }

            return idToken.Subject;
        }

        internal static bool IsAccessTokenValid(Credential accessToken)
        {
            long now = TimeUtils.GetSecondsFromEpochNow();

            if (accessToken.ExpiresOn <= now + 300)
            {
                // access token is expired
                return false;
            }

            // living in the future
            if (accessToken.CachedAt > now)
            {
                return false;
            }

            return true;
        }
    }
}
