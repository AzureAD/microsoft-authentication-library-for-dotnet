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
            string keyringSchemaName,
            string keyringCollection,
            string keyringSecretLabel,
            KeyValuePair<string, string> keyringAttribute1,
            KeyValuePair<string, string> keyringAttribute2,
            string clientId,
            int lockRetryDelay,
            int lockRetryCount)
        {
            CacheFileName = cacheFileName;
            CacheDirectory = cacheDirectory;
            MacKeyChainServiceName = macKeyChainServiceName;
            MacKeyChainAccountName = macKeyChainAccountName;
            KeyringSchemaName = keyringSchemaName;
            KeyringCollection = keyringCollection;
            KeyringSecretLabel = keyringSecretLabel;
            KeyringAttribute1 = keyringAttribute1;
            KeyringAttribute2 = keyringAttribute2;
            ClientId = clientId;
            LockRetryDelay = lockRetryDelay;
            LockRetryCount = lockRetryCount;
        }

        /// <summary>
        /// Gets the full path to the cache file, combining the directory and filename.
        /// </summary>
        public string CacheFilePath => Path.Combine(CacheDirectory, CacheFileName);

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
        /// The number of time to retry the lock if it is contended and retrying is possible
        /// </summary>
        public readonly int LockRetryCount;

        /// <summary>
        /// The client id
        /// </summary>
        public string ClientId { get; }
    }
}
