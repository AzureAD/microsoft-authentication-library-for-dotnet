// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

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

        internal MsalAccessTokenCacheKey(
            string environment,
            string tenantId,
            string userIdentifier,
            string clientId,
            string scopes)
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
            _normalizedScopes = scopes;
            _tenantId = tenantId;
        }

        public override string ToString()
        {
            return MsalCacheKeys.GetCredentialKey(
                _homeAccountId,
                _environment,
                StorageJsonValues.CredentialTypeAccessToken,
                _clientId,
                _tenantId,
                _normalizedScopes);
        }

     
        #region iOS

        public string iOSAccount => MsalCacheKeys.GetiOSAccountKey(_homeAccountId, _environment);

        public string iOSService => MsalCacheKeys.GetiOSServiceKey(StorageJsonValues.CredentialTypeAccessToken, _clientId, _tenantId, _normalizedScopes);

        public string iOSGeneric => MsalCacheKeys.GetiOSGenericKey(StorageJsonValues.CredentialTypeAccessToken, _clientId, _tenantId);

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
                this.ToString(),
                other.ToString(),
                StringComparison.OrdinalIgnoreCase);
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }
        #endregion
    }
}
