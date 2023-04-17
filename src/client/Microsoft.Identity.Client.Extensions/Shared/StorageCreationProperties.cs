// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Microsoft.Identity.Client.Extensions.Msal
{
    /// <summary>
    /// An immutable class containing information required to instantiate storage objects for MSAL caches in various platforms.
    /// </summary>
    public class StorageCreationProperties
    {
        /// <summary>
        /// This constructor is intentionally internal. To get one of these objects use <see cref="StorageCreationPropertiesBuilder.Build"/>.
        /// </summary>
        internal StorageCreationProperties(
            string cacheFileName,
            string cacheDirectory,
            string macKeyChainServiceName,
            string macKeyChainAccountName,
            bool useLinuxPlaintextFallback,
            bool usePlaintextFallback,
            string keyringSchemaName,
            string keyringCollection,
            string keyringSecretLabel,
            KeyValuePair<string, string> keyringAttribute1,
            KeyValuePair<string, string> keyringAttribute2,
            int lockRetryDelay,
            int lockRetryCount,
            string clientId,
            string authority)
        {
            CacheFileName = cacheFileName;
            CacheDirectory = cacheDirectory;
            CacheFilePath = Path.Combine(CacheDirectory, CacheFileName);

            UseLinuxUnencryptedFallback = useLinuxPlaintextFallback;
            UseUnencryptedFallback = usePlaintextFallback;

            MacKeyChainServiceName = macKeyChainServiceName;
            MacKeyChainAccountName = macKeyChainAccountName;

            KeyringSchemaName = keyringSchemaName;
            KeyringCollection = keyringCollection;
            KeyringSecretLabel = keyringSecretLabel;
            KeyringAttribute1 = keyringAttribute1;
            KeyringAttribute2 = keyringAttribute2;

            ClientId = clientId;
            Authority = authority;
            LockRetryDelay = lockRetryDelay;
            LockRetryCount = lockRetryCount;

            Validate();
        }

        private void Validate()
        {
            if (UseLinuxUnencryptedFallback && UseUnencryptedFallback)
            {
                throw new ArgumentException("UseLinuxUnencryptedFallback and UseUnencryptedFallback are mutually exclusive. UseLinuxUnencryptedFallback is the safer option. ");

            }
            if ((UseLinuxUnencryptedFallback || UseUnencryptedFallback) &&
                (
                    !string.IsNullOrEmpty(KeyringSecretLabel) ||
                    !string.IsNullOrEmpty(KeyringSchemaName) ||
                    !string.IsNullOrEmpty(KeyringCollection)))
            {
                throw new ArgumentException("Using plaintext storage is mutually exclusive with other Linux storage options. ");
            }

            if ((UseUnencryptedFallback ) && 
                (   !string.IsNullOrEmpty(MacKeyChainServiceName) ||
                    !string.IsNullOrEmpty(MacKeyChainAccountName)))
            {
                throw new ArgumentException("Using plaintext storage is mutually exclusive with other Mac storage options. ");

            }
        }

        /// <summary>
        /// Gets the full path to the cache file, combining the directory and filename.
        /// </summary>
        public string CacheFilePath { get; }

        /// <summary>
        /// The name of the cache file.
        /// </summary>
        public readonly string CacheFileName;

        /// <summary>
        /// The name of the directory containing the cache file.
        /// </summary>
        public readonly string CacheDirectory;

        /// <summary>
        /// The mac keychain service name.
        /// </summary>
        public readonly string MacKeyChainServiceName;

        /// <summary>
        /// The mac keychain account name.
        /// </summary>
        public readonly string MacKeyChainAccountName;

        /// <summary>
        /// The linux keyring schema name.
        /// </summary>
        public readonly string KeyringSchemaName;

        /// <summary>
        /// The linux keyring collection.
        /// </summary>
        public readonly string KeyringCollection;

        /// <summary>
        /// The linux keyring secret label.
        /// </summary>
        public readonly string KeyringSecretLabel;

        /// <summary>
        /// Additional linux keyring attribute.
        /// </summary>
        public readonly KeyValuePair<string, string> KeyringAttribute1;

        /// <summary>
        /// Additional linux keyring attribute.
        /// </summary>
        public readonly KeyValuePair<string, string> KeyringAttribute2;

        /// <summary>
        /// The delay between retries if a lock is contended and a retry is requested. (in ms)
        /// </summary>
        public readonly int LockRetryDelay;

        /// <summary>
        /// Flag which indicates that a plaintext file will be used on Linux for secret storage
        /// </summary>
        public readonly bool UseLinuxUnencryptedFallback;

        /// <summary>
        /// Flag which indicates that a plaintext file will be used on all OSes for secret storage
        /// </summary>
        public readonly bool UseUnencryptedFallback;

        /// <summary>
        /// The number of time to retry the lock if it is contended and retrying is possible
        /// </summary>
        public readonly int LockRetryCount;

        /// <summary>
        /// The client id.
        /// </summary>
        /// <remarks> Only required for the MsalCacheHelper.CacheChanged event</remarks>
        public string ClientId { get; }

        /// <summary>
        /// The authority
        /// </summary>
        /// <remarks> Only required for the MsalCacheHelper.CacheChanged event</remarks>
        public string Authority { get; }

        internal bool IsCacheEventConfigured
        {
            get
            {
                return !string.IsNullOrEmpty(ClientId) &&
                   !string.IsNullOrEmpty(Authority);
            }
        }
    }
}
