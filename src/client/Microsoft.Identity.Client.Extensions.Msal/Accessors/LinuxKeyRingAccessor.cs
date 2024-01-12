// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using Microsoft.Identity.Extensions;

namespace Microsoft.Identity.Client.Extensions.Msal
{
    internal class LinuxKeyringAccessor : ICacheAccessor
    {
        private readonly TraceSourceLogger _logger;
        private IntPtr _libsecretSchema = IntPtr.Zero;

        private readonly string _cacheFilePath;
        private readonly string _keyringCollection;
        private readonly string _keyringSchemaName;
        private readonly string _keyringSecretLabel;
        private readonly string _attributeKey1;
        private readonly string _attributeValue1;
        private readonly string _attributeKey2;
        private readonly string _attributeValue2;

        public LinuxKeyringAccessor(
            string cacheFilePath,
            string keyringCollection,
            string keyringSchemaName,
            string keyringSecretLabel,
            string attributeKey1,
            string attributeValue1,
            string attributeKey2,
            string attributeValue2,
            TraceSourceLogger logger)
        {
            if (string.IsNullOrWhiteSpace(cacheFilePath))
            {
                throw new ArgumentNullException(nameof(cacheFilePath));
            }

            if (string.IsNullOrWhiteSpace(attributeKey1))
            {
                throw new ArgumentNullException(nameof(attributeKey1));
            }

            if (string.IsNullOrWhiteSpace(attributeValue1))
            {
                throw new ArgumentNullException(nameof(attributeValue1));
            }

            if (string.IsNullOrWhiteSpace(attributeKey2))
            {
                throw new ArgumentNullException(nameof(attributeKey2));
            }

            if (string.IsNullOrWhiteSpace(attributeValue2))
            {
                throw new ArgumentNullException(nameof(attributeValue2));
            }

            _cacheFilePath = cacheFilePath;
            _keyringCollection = keyringCollection;
            _keyringSchemaName = keyringSchemaName;
            _keyringSecretLabel = keyringSecretLabel;
            _attributeKey1 = attributeKey1;
            _attributeValue1 = attributeValue1;
            _attributeKey2 = attributeKey2;
            _attributeValue2 = attributeValue2;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ICacheAccessor CreateForPersistenceValidation()
        {
            return new LinuxKeyringAccessor(
                _cacheFilePath + ".test",
                _keyringCollection,
                _keyringSchemaName,
                "MSAL Persistence Test",
                _attributeKey1, "test",
                _attributeKey2, "test",
                _logger);
        }

        public void Clear()
        {
            _logger.LogInformation("Clearing cache");
            FileIOWithRetries.DeleteCacheFile(_cacheFilePath, _logger);

            _logger.LogInformation($"Before deleting secret from Linux keyring");

            IntPtr error = IntPtr.Zero;

            Libsecret.secret_password_clear_sync(
                schema: GetLibsecretSchema(),
                cancellable: IntPtr.Zero,
                error: out error,
                attribute1Type: _attributeKey1,
                attribute1Value: _attributeValue1,
                attribute2Type: _attributeKey2,
                attribute2Value: _attributeValue2,
                end: IntPtr.Zero);

            if (error != IntPtr.Zero)
            {
                try
                {
                    GError err = (GError)Marshal.PtrToStructure(error, typeof(GError));
                    throw new InteropException(
                        $"An error was encountered while clearing secret from keyring in the {nameof(Storage)} domain:'{err.Domain}' code:'{err.Code}' message:'{err.Message}'", 
                        err.Code);
                }
                catch (Exception e)
                {
                    throw new InteropException(
                        $"An exception was encountered while processing libsecret error information during clearing secret in the {nameof(Storage)} ex:'{e}'", 0, e);
                }
            }

            _logger.LogInformation("After deleting secret from linux keyring");
        }

        public byte[] Read()
        {
            _logger.LogInformation($"ReadDataCore, Before reading from linux keyring");

            byte[] fileData = null;

            IntPtr error = IntPtr.Zero;

            string secret = Libsecret.secret_password_lookup_sync(
                schema: GetLibsecretSchema(),
                cancellable: IntPtr.Zero,
                error: out error,
                attribute1Type: _attributeKey1,
                attribute1Value: _attributeValue1,
                attribute2Type: _attributeKey2,
                attribute2Value: _attributeValue2,
                end: IntPtr.Zero);

            if (error != IntPtr.Zero)
            {
                try
                {
                    GError err = (GError)Marshal.PtrToStructure(error, typeof(GError));
                    throw new InteropException(
                        $"An error was encountered while reading secret from keyring in the {nameof(Storage)} domain:'{err.Domain}' code:'{err.Code}' message:'{err.Message}'", err.Code);
                }
                catch (Exception e)
                {
                    throw new InteropException(
                        $"An exception was encountered while processing libsecret error information during reading in the {nameof(Storage)} ex:'{e}'", 0, e);
                }
            }
            else if (string.IsNullOrEmpty(secret))
            {
                _logger.LogWarning("No matching secret found in the keyring");
            }
            else
            {
                _logger.LogInformation("Base64 decoding the secret string");
                fileData = Convert.FromBase64String(secret);
                _logger.LogInformation($"ReadDataCore, read '{fileData?.Length}' bytes from the keyring");
            }

            return fileData;
        }

        public void Write(byte[] data)
        {
            _logger.LogInformation("Before saving to linux keyring");

            IntPtr error = IntPtr.Zero;

            Libsecret.secret_password_store_sync(
                schema: GetLibsecretSchema(),
                collection: _keyringCollection,
                label: _keyringSecretLabel,
                password: Convert.ToBase64String(data),
                cancellable: IntPtr.Zero,
                error: out error,
                attribute1Type: _attributeKey1,
                attribute1Value: _attributeValue1,
                attribute2Type: _attributeKey2,
                attribute2Value: _attributeValue2,
                end: IntPtr.Zero);

            if (error != IntPtr.Zero)
            {
                try
                {
                    GError err = (GError)Marshal.PtrToStructure(error, typeof(GError));
                    string message = $"An error was encountered while saving secret to keyring in the {nameof(Storage)} domain:'{err.Domain}' code:'{err.Code}' message:'{err.Message}'";
                    throw new InteropException(message, err.Code);
                }
                catch (Exception e)
                {
                    throw new InteropException(
                        $"An exception was encountered while processing libsecret error information during saving in the {nameof(Storage)}", 0, e);
                }
            }

            _logger.LogInformation("After saving to linux keyring");

            // Change the "last modified" attribute and trigger file changed events
            FileIOWithRetries.TouchFile(_cacheFilePath, _logger);
        }

        private IntPtr GetLibsecretSchema()
        {
            if (_libsecretSchema == IntPtr.Zero)
            {
                _logger.LogInformation("Before creating libsecret schema");

                _libsecretSchema = Libsecret.secret_schema_new(
                    name: _keyringSchemaName,
                    flags: (int)Libsecret.SecretSchemaFlags.SECRET_SCHEMA_DONT_MATCH_NAME,
                    attribute1: _attributeKey1,
                    attribute1Type: (int)Libsecret.SecretSchemaAttributeType.SECRET_SCHEMA_ATTRIBUTE_STRING,
                    attribute2: _attributeKey2,
                    attribute2Type: (int)Libsecret.SecretSchemaAttributeType.SECRET_SCHEMA_ATTRIBUTE_STRING,
                    end: IntPtr.Zero);

                if (_libsecretSchema == IntPtr.Zero)
                {
                    throw new InteropException("Failed to create libsecret schema from the {nameof(Storage)}", 0);                   
                }

                _logger.LogInformation("After creating libsecret schema");
            }

            return _libsecretSchema;
        }
    }
}
