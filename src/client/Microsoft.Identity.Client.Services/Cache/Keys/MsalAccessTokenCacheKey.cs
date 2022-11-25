// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.AuthScheme;

namespace Microsoft.Identity.Client.Cache.Keys
{
    /// <summary>
    ///     An object representing the key of the token cache AT dictionary. The
    ///     format of the key is not important for this library, as long as it is unique.
    /// </summary>
    /// <remarks>The format of the key is platform dependent</remarks>
    internal class MsalAccessTokenCacheKey : IiOSKey
    {
        private readonly string _clientId;
        private readonly string _environment;
        private readonly string _homeAccountId;
        private readonly string _normalizedScopes; // space separated, lowercase and ordered alphabetically
        private readonly string _tenantId;
        private readonly string _tokenType;
        private readonly string _credentialDescriptor;

        // Note: when using multiple parts, ordering is important
        private readonly string[] _extraKeyParts;

        internal string TenantId => _tenantId;
        internal string ClientId => _clientId;
        internal string HomeAccountId => _homeAccountId;

        internal MsalAccessTokenCacheKey(
            string environment,
            string tenantId,
            string userIdentifier,
            string clientId,
            string scopes,
            string tokenType)
        {
            if (string.IsNullOrEmpty(environment))
            {
                throw new ArgumentNullException(nameof(environment));
            }

            if (string.IsNullOrEmpty(clientId))
            {
                throw new ArgumentNullException(nameof(clientId));
            }

            if (string.IsNullOrEmpty(tokenType))
            {
                throw new ArgumentNullException(nameof(tokenType));
            }

            _environment = environment;
            _homeAccountId = userIdentifier;
            _clientId = clientId;
            _normalizedScopes = scopes;
            _tokenType = tokenType;
            _tenantId = tenantId;
            _credentialDescriptor = StorageJsonValues.CredentialTypeAccessToken;

            if (AuthSchemeHelper.StoreTokenTypeInCacheKey(tokenType))
            {
                _extraKeyParts = new[] { _tokenType };
                _credentialDescriptor = StorageJsonValues.CredentialTypeAccessTokenWithAuthScheme;
            }
        }

        public override string ToString()
        {
            return MsalCacheKeys.GetCredentialKey(
                _homeAccountId,
                _environment,
                _credentialDescriptor,
                _clientId,
                _tenantId,
                _normalizedScopes,
                _extraKeyParts);
        }

        public string ToLogString(bool piiEnabled = false)
        {
            return MsalCacheKeys.GetCredentialKey(
                piiEnabled? _homeAccountId : _homeAccountId?.GetHashCode().ToString(),
                _environment,
                _credentialDescriptor,
                _clientId,
                _tenantId,
                _normalizedScopes,
                _extraKeyParts);
        }

        #region iOS

        public string iOSAccount => MsalCacheKeys.GetiOSAccountKey(_homeAccountId, _environment);

        public string iOSService => MsalCacheKeys.GetiOSServiceKey(_credentialDescriptor, _clientId, _tenantId, _normalizedScopes, _extraKeyParts);

        public string iOSGeneric => MsalCacheKeys.GetiOSGenericKey(_credentialDescriptor, _clientId, _tenantId);

        public int iOSType => (int)MsalCacheKeys.iOSCredentialAttrType.AccessToken;

        #endregion

        #region Equals and GetHashCode
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var other = obj as MsalAccessTokenCacheKey;

            return string.Equals(
                ToString(),
                other.ToString(),
                StringComparison.OrdinalIgnoreCase);
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
        #endregion
    }
}
