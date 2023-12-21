// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Text;

namespace Microsoft.Identity.Client.Extensions.Msal
{
    /// <summary>
    /// 
    /// </summary>
    internal class MacKeychainAccessor : ICacheAccessor
    {
        private readonly string _cacheFilePath;
        private readonly string _service;
        private readonly string _account;
        private readonly TraceSourceLogger _logger;

        private readonly MacOSKeychain _keyChain;

        public MacKeychainAccessor(string cacheFilePath, string keyChainServiceName, string keyChainAccountName, TraceSourceLogger logger)
        {
            if (string.IsNullOrWhiteSpace(cacheFilePath))
            {
                throw new ArgumentNullException(nameof(cacheFilePath));
            }

            if (string.IsNullOrWhiteSpace(keyChainServiceName))
            {
                throw new ArgumentNullException(nameof(keyChainServiceName));
            }

            if (string.IsNullOrWhiteSpace(keyChainAccountName))
            {
                throw new ArgumentNullException(nameof(keyChainAccountName));
            }

            _cacheFilePath = cacheFilePath;
            _service = keyChainServiceName;
            _account = keyChainAccountName;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _keyChain = new MacOSKeychain();
        }

        public void Clear()
        {
            _logger.LogInformation("Clearing cache");
            FileIOWithRetries.DeleteCacheFile(_cacheFilePath, _logger);

            _logger.LogInformation($"Before delete mac keychain service: {_service} account {_account}");
            _keyChain.Remove(_service, _account);
            _logger.LogInformation($"After delete mac keychain service: {_service} account {_account}");
        }

        public byte[] Read()
        {
            _logger.LogInformation($"ReadDataCore, Before reading from mac keychain service: {_service} account {_account}");
            var entry = _keyChain.Get(_service, _account);
            _logger.LogInformation($"ReadDataCore, After reading mac keychain {entry?.Password?.Length ?? 0} chars service: {_service} account {_account}");

            return entry?.Password;
        }

        public void Write(byte[] data)
        {
            _logger.LogInformation($"Before write to mac keychain service: {_service} account {_account}");

            _keyChain.AddOrUpdate(_service, _account, data);
            _logger.LogInformation($"After write to mac keychain service: {_service} account {_account}");

            // Change the "last modified" attribute and trigger file changed events
            FileIOWithRetries.TouchFile(_cacheFilePath, _logger);
        }

        public ICacheAccessor CreateForPersistenceValidation()
        {
            return new MacKeychainAccessor(
                _cacheFilePath + ".test",
                _service + Guid.NewGuid().ToString(),
                _account,
                _logger);
        }

        public override string ToString()
        {
            return $"MacKeyChain accessor pointing to: service {_service}, account {_account}, file {_cacheFilePath}";
        }
    }
}
