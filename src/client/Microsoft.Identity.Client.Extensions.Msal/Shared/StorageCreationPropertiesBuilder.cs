// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Identity.Client.Extensions.Msal
{
    /// <summary>
    /// An incremental builder for <see cref="StorageCreationProperties"/> objects.
    /// </summary>
    public class StorageCreationPropertiesBuilder
    {
        private readonly string _cacheFileName;
        private readonly string _cacheDirectory;
        private string _clientId;
        private string _authority;
        private string _macKeyChainServiceName;
        private string _macKeyChainAccountName;
        private string _keyringSchemaName;
        private string _keyringCollection;
        private string _keyringSecretLabel;
        private KeyValuePair<string, string> _keyringAttribute1;
        private KeyValuePair<string, string> _keyringAttribute2;
        private int _lockRetryDelay = CrossPlatLock.LockfileRetryDelayDefault;
        private int _lockRetryCount = CrossPlatLock.LockfileRetryCountDefault;
        private bool _useLinuxPlaintextFallback = false;
        private bool _usePlaintextFallback = false;

        /// <summary>
        /// Constructs a new instance of this builder associated with the given cache file.
        /// </summary>
        /// <param name="cacheFileName">The name of the cache file to use when creating or opening storage.</param>
        /// <param name="cacheDirectory">The name of the directory containing the cache file.</param>
        /// <param name="clientId">The client id for the calling application</param>
        [Obsolete("Use StorageCreationPropertiesBuilder(string, string) instead. " +
            "If you need to consume the CacheChanged event then also use WithCacheChangedEvent(string, string)", false)]
        public StorageCreationPropertiesBuilder(string cacheFileName, string cacheDirectory, string clientId)
        {
            _cacheFileName = cacheFileName;
            _cacheDirectory = cacheDirectory;
            _clientId = clientId;
            _authority = "https://login.microsoftonline.com/common"; 
        }

        /// <summary>
        /// Constructs a new instance of this builder associated with the given cache file.
        /// </summary>
        /// <param name="cacheFileName">The name of the cache file to use when creating or opening storage.</param>
        /// <param name="cacheDirectory">The name of the directory containing the cache file.</param>
        public StorageCreationPropertiesBuilder(string cacheFileName, string cacheDirectory)
        {
            _cacheFileName = cacheFileName;
            _cacheDirectory = cacheDirectory;
        }

        /// <summary>
        /// Returns an immutable instance of <see cref="StorageCreationProperties"/> matching the configuration of this builder.
        /// </summary>
        /// <returns>An immutable instance of <see cref="StorageCreationProperties"/> matching the configuration of this builder.</returns>
        public StorageCreationProperties Build()
        {
            return new StorageCreationProperties(
                _cacheFileName,
                _cacheDirectory,
                _macKeyChainServiceName,
                _macKeyChainAccountName,
                _useLinuxPlaintextFallback,
                _usePlaintextFallback,
                _keyringSchemaName,
                _keyringCollection,
                _keyringSecretLabel,
                _keyringAttribute1,
                _keyringAttribute2,
                _lockRetryDelay,
                _lockRetryCount,
                _clientId,
                _authority);
        }

        /// <summary>
        /// Augments this builder with mac keychain values and returns the augmented builder.
        /// </summary>
        /// <param name="serviceName">The mac keychain service name</param>
        /// <param name="accountName">The mac keychain account name</param>
        /// <returns>The augmented builder</returns>
        public StorageCreationPropertiesBuilder WithMacKeyChain(string serviceName, string accountName)
        {
            _macKeyChainServiceName = serviceName;
            _macKeyChainAccountName = accountName;

         
            return this;
        }

        /// <summary>
        /// Enables the use of the MsalCacheHelper.CacheChanged event, which notifies about
        /// accounts added and removed. These accounts are scoped to the client_id and authority
        /// specified here.
        /// </summary>
        /// <param name="clientId">The client id for which you wish to receive notifications</param>
        /// <param name="authority">The authority for which you wish to receive notifications</param>
        /// <returns>The augmented builder</returns>        
        public StorageCreationPropertiesBuilder WithCacheChangedEvent(
            string clientId,
            string authority = "https://login.microsoftonline.com/common")
        {
            _clientId = clientId;
            _authority = authority;
            return this;
        }

        /// <summary>
        /// Augments this builder with a custom retry amount and delay between retries in the cases where a lock is used.
        /// </summary>
        /// <param name="lockRetryDelay">Delay between retries in ms, must be 1 or more</param>
        /// <param name="lockRetryCount">Number of retries, must be 1 or more</param>
        /// <returns>The augmented builder</returns>
        public StorageCreationPropertiesBuilder CustomizeLockRetry(int lockRetryDelay, int lockRetryCount)
        {
            if (lockRetryDelay < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(lockRetryDelay));
            }

            if (lockRetryCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(lockRetryCount));
            }

            _lockRetryCount = lockRetryCount;
            _lockRetryDelay = lockRetryDelay;
            return this;
        }

        /// <summary>
        /// Augments this builder with Linux KeyRing values and returns the augmented builder.
        /// </summary>
        /// <param name="schemaName">Schema name, e.g. "com.contoso.app". It is a logical container of secrets, similar to a namespace.</param>
        /// <param name="collection">A collection aggregates multiple schema. KeyRing defines 2 collections - "default' is a persisted schema and "session" is an in-memory schema that is destroyed on logout.</param>
        /// <param name="secretLabel">A user readable label for the secret, e.g. "Credentials used by Contoso apps"</param>
        /// <param name="attribute1">Additional string attribute that will be used to decorate the secret.</param>
        /// <param name="attribute2">Additional string attribute that will be used to decorate the secret</param>
        /// <returns>The augmented builder</returns>
        /// <remarks>
        /// Attributes are used like scoping keys - their name and values must match the secrets in the KeyRing.
        /// A suggested pattern is to use a product name (or a group of products) and a version. If you need to increment the version,
        /// the secrets associated with the old version will be ignored.
        /// </remarks>
        public StorageCreationPropertiesBuilder WithLinuxKeyring(
            string schemaName,
            string collection,
            string secretLabel,
            KeyValuePair<string, string> attribute1,
            KeyValuePair<string, string> attribute2)
        {
            if (string.IsNullOrEmpty(schemaName))
            {
                throw new ArgumentNullException(nameof(schemaName));
            }        

            _keyringSchemaName = schemaName;
            _keyringCollection = collection;
            _keyringSecretLabel = secretLabel;
            _keyringAttribute1 = attribute1;
            _keyringAttribute2 = attribute2;
            return this;
        }

        /// <summary>
        /// Use to allow storage of secrets in the cacheFile which was configured in the constructor of this class.
        /// WARNING Secrets are stored in PLAINTEXT!
        /// Should be used as a fallback for cases where Linux LibSecret is not available, for example
        /// over SSH connections. Users are responsible for security.
        /// </summary>
        /// <remarks>You can check if the persistence is available by calling msalCacheHelper.VerifyPersistence()
        /// For more details see https://github.com/AzureAD/microsoft-authentication-extensions-for-dotnet/blob/main/docs/keyring_fallback_proposal.md
        /// </remarks>
        /// <returns></returns>
        public StorageCreationPropertiesBuilder WithLinuxUnprotectedFile()
        {
            _useLinuxPlaintextFallback = true;
            return this;
        }

        /// <summary>
        /// Use to allow storage of secrets in the cacheFile which was configured in the constructor of this class.
        /// WARNING Secrets are stored in PLAINTEXT!
        /// 
        /// The application is responsible for storing the plaintext file in a secure location, such as an encrypted drive or ACL directory.
        /// 
        /// Should be used as a fall-back for cases where encrypted persistence is not available, for example: 
        /// - Linux and Mac over SSH connections
        /// - Certain virtualized Windows scenarios where DPAPI is not available         
        /// </summary>
        /// <remarks>You can check if the persistence is available by calling msalCacheHelper.VerifyPersistence()
        /// For more details see https://github.com/AzureAD/microsoft-authentication-extensions-for-dotnet/blob/main/docs/keyring_fallback_proposal.md
        /// </remarks>
        public StorageCreationPropertiesBuilder WithUnprotectedFile()
        {
            _usePlaintextFallback = true;
            return this;
        }
    }
}
