// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;

namespace Microsoft.Identity.Client.Extensions.Msal
{
    internal class DpApiEncryptedFileAccessor : ICacheAccessor
    {
        private readonly string _cacheFilePath;
        private readonly TraceSourceLogger _logger;
        private readonly ICacheAccessor _unencryptedFileAccessor;

        public DpApiEncryptedFileAccessor(string cacheFilePath, TraceSourceLogger logger)
        {
            if (string.IsNullOrEmpty(cacheFilePath))
            {
                throw new ArgumentNullException(nameof(cacheFilePath));
            }

            _cacheFilePath = cacheFilePath;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _unencryptedFileAccessor = new FileAccessor(_cacheFilePath, false, _logger);
        }

        public void Clear()
        {
            _logger.LogInformation("Clearing cache");
            _unencryptedFileAccessor.Clear();
        }

        public ICacheAccessor CreateForPersistenceValidation()
        {
            return new DpApiEncryptedFileAccessor(_cacheFilePath + ".test", _logger);
        }

        public byte[] Read()
        {

            byte[] fileData = _unencryptedFileAccessor.Read();

            if (fileData != null && fileData.Length > 0)
            {
                _logger.LogInformation($"Unprotecting the data");
                fileData = ProtectedData.Unprotect(fileData, optionalEntropy: null, scope: DataProtectionScope.CurrentUser);
            }

            return fileData;
        }

        public void Write(byte[] data)
        {
            if (data.Length != 0)
            {
                _logger.LogInformation($"Protecting the data");
                data = ProtectedData.Protect(data, optionalEntropy: null, scope: DataProtectionScope.CurrentUser);
            }

            _unencryptedFileAccessor.Write(data);
        }
    }
}
