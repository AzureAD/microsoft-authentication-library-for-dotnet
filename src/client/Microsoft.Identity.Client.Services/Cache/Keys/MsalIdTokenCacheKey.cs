// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.Cache.Keys
{
    /// <summary>
    /// An object representing the key of the token cache Id Token dictionary. The
    /// format of the key is not important for this library, as long as it is unique.
    /// </summary>
    internal class MsalIdTokenCacheKey : IiOSKey
    {
        private readonly string _environment;
        private readonly string _homeAccountId;
        private readonly string _clientId;
        private readonly string _tenantId;

        internal string HomeAccountId => _homeAccountId;

        public MsalIdTokenCacheKey(
            string environment,
            string tenantId,
            string userIdentifier,
            string clientId)

        {
            if (string.IsNullOrEmpty(environment))
            {
                throw new ArgumentNullException(nameof(environment));
            }

            if (string.IsNullOrEmpty(clientId))
            {
                throw new ArgumentNullException(nameof(clientId));
            }

            _environment = environment;
            _homeAccountId = userIdentifier;
            _clientId = clientId;
            _tenantId = tenantId;
        }

        public override string ToString()
        {
            return MsalCacheKeys.GetCredentialKey(
                _homeAccountId,
                _environment,
                StorageJsonValues.CredentialTypeIdToken,
                _clientId,
                _tenantId,
                scopes: null);
        }

        #region iOS

        public string iOSAccount => MsalCacheKeys.GetiOSAccountKey(_homeAccountId, _environment);

        public string iOSGeneric => MsalCacheKeys.GetiOSGenericKey(StorageJsonValues.CredentialTypeIdToken, _clientId, _tenantId);

        public string iOSService => MsalCacheKeys.GetiOSServiceKey(StorageJsonValues.CredentialTypeIdToken, _clientId, _tenantId, scopes: null);

        public int iOSType => (int)MsalCacheKeys.iOSCredentialAttrType.IdToken;

        #endregion

        #region Equals and GetHashCode
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var other = obj as MsalIdTokenCacheKey;

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

