// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.Cache.Keys
{
    /// <summary>
    /// App metadata is an optional entity in cache and can be used by apps to store additional metadata applicable to a particular client.
    /// </summary>
    internal class MsalAppMetadataCacheKey : IiOSKey
    {
        private readonly string _clientId;
        private readonly string _environment;

        public MsalAppMetadataCacheKey(string clientId, string environment)
        {
            _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        /// <summary>
        /// Ex: appmetadata-login.microsoftonline.com-b6c69a37-df96-4db0-9088-2ab96e1d8215
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ($"{StorageJsonKeys.AppMetadata}{MsalCacheKeys.CacheKeyDelimiter}" +
                $"{_environment}{MsalCacheKeys.CacheKeyDelimiter}{_clientId}").ToLowerInvariant();
        }

        #region iOS

        public string iOSService => $"{StorageJsonValues.AppMetadata}{MsalCacheKeys.CacheKeyDelimiter}{_clientId}".ToLowerInvariant();

        public string iOSGeneric => "1";

        public string iOSAccount => $"{_environment}".ToLowerInvariant();

        public int iOSType => (int)MsalCacheKeys.iOSCredentialAttrType.AppMetadata;

        #endregion

        #region Equals and GetHashCode
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var other = obj as MsalAppMetadataCacheKey;

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
