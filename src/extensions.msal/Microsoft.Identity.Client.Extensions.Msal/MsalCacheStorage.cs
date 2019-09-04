// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;

namespace Microsoft.Identity.Client.Extensions.Msal
{
    /// <summary>
    /// Persist cache to file
    /// </summary>
    public sealed class MsalCacheStorage
    {
        private const int FileLockRetryCount = 20;
        private const int FileLockRetryWaitInMs = 200;
        internal readonly StorageCreationProperties _creationProperties;
        private readonly TraceSource _logger;

        private IntPtr _libsecretSchema = IntPtr.Zero;

        /// <summary>
        /// A default logger for use if the user doesn't want to provide their own.
        /// </summary>
        private static readonly Lazy<TraceSource> s_staticLogger = new Lazy<TraceSource>(() =>
        {
            return (TraceSource)EnvUtils.GetNewTraceSource(nameof(MsalCacheHelper) + "Singleton");
        });


        /// <summary>
        /// Initializes a new instance of the <see cref="MsalCacheStorage"/> class.
        /// </summary>
        /// <param name="creationProperties">Properties for creating the cache storage on disk</param>
        /// <param name="logger">logger</param>
        public MsalCacheStorage(StorageCreationProperties creationProperties, TraceSource logger = null)
        {
            _creationProperties = creationProperties;
            _logger = logger ?? s_staticLogger.Value;
            _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, $"Initialized '{nameof(MsalCacheStorage)}'");
        }

        /// <summary>
        /// Gets cache file path
        /// </summary>
        public string CacheFilePath => _creationProperties.CacheFilePath;

        /// <summary>
        /// Gets a value indicating whether the persisted file has changed since we last read it.
        /// </summary>
        public bool HasChanged
        {
            get
            {
                // Attempts to make this more refined have all resulted in some form of cache inconsistency. Just returning
                // true here so we always load from disk.
                return true;
            }
        }

        /// <summary>
        /// Read and unprotect cache data
        /// </summary>
        /// <returns>Unprotected cache data</returns>
        public byte[] ReadData()
        {
            bool cacheFileExists = File.Exists(CacheFilePath);
            _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, $"ReadData Cache file exists '{cacheFileExists}'");

            byte[] data = null;
            try
            {
                _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, $"Reading Data");
                byte[] fileData = ReadDataCore();

                _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, $"Got '{fileData?.Length}' bytes from file storage");

                if (fileData != null && fileData.Length > 0)
                {
                    _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, $"Unprotecting the data");
                    data = SharedUtilities.IsWindowsPlatform() ?
                        ProtectedData.Unprotect(fileData, optionalEntropy: null, scope: DataProtectionScope.CurrentUser) :
                        fileData;
                }
                else if (fileData == null || fileData.Length == 0)
                {
                    data = new byte[0];
                    _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, $"Empty data does not need to be unprotected");
                }
                else
                {
                    _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, $"Data does not need to be unprotected");
                    return fileData;
                }
            }
            catch (Exception e)
            {
                _logger.TraceEvent(TraceEventType.Error, /*id*/ 0, $"An exception was encountered while reading data from the {nameof(MsalCacheStorage)} : {e}");
                ClearCore();
            }

            return data ?? new byte[0];
        }

        /// <summary>
        /// Protect and write cache data to file
        /// </summary>
        /// <param name="data">Cache data</param>
        public void WriteData(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            try
            {
                _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, $"Got '{data?.Length}' bytes to write to storage");
                if (SharedUtilities.IsWindowsPlatform() && data.Length != 0)
                {
                    _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, $"Protecting the data");
                    data = ProtectedData.Protect(data, optionalEntropy: null, scope: DataProtectionScope.CurrentUser);
                }

                WriteDataCore(data);
            }
            catch (Exception e)
            {
                _logger.TraceEvent(TraceEventType.Error, /*id*/ 0, $"An exception was encountered while writing data from the {nameof(MsalCacheStorage)} : {e}");
            }
        }

        /// <summary>
        /// Delete cache file
        /// </summary>
        public void Clear()
        {
            _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, "Clearing the cache file");
            ClearCore();
        }

        private byte[] ReadDataCore()
        {
            _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, "ReadDataCore");

            byte[] fileData = null;

            bool cacheFileExists = File.Exists(CacheFilePath);
            _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, $"ReadDataCore Cache file exists '{cacheFileExists}'");

            if (SharedUtilities.IsWindowsPlatform())
            {
                if (cacheFileExists)
                {
                    TryProcessFile(() =>
                    {
                        fileData = File.ReadAllBytes(CacheFilePath);
                        _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, $"ReadDataCore, read '{fileData.Length}' bytes from the file");
                    });
                }
            }
            else if (SharedUtilities.IsMacPlatform())
            {
                _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, $"ReadDataCore, Before reading from mac keychain");
                fileData = MacKeyChain.RetrieveKey(_creationProperties.MacKeyChainServiceName, _creationProperties.MacKeyChainAccountName, _logger);

                _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, $"ReadDataCore, read '{fileData?.Length}' bytes from the keychain");
            }
            else if (SharedUtilities.IsLinuxPlatform())
            {
                _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, $"ReadDataCore, Before reading from linux keyring");

                IntPtr error = IntPtr.Zero;

                string secret = Libsecret.secret_password_lookup_sync(
                    schema: GetLibsecretSchema(),
                    cancellable: IntPtr.Zero,
                    error: out error,
                    attribute1Type: _creationProperties.KeyringAttribute1.Key,
                    attribute1Value: _creationProperties.KeyringAttribute1.Value,
                    attribute2Type: _creationProperties.KeyringAttribute2.Key,
                    attribute2Value: _creationProperties.KeyringAttribute2.Value,
                    end: IntPtr.Zero);

                if (error != IntPtr.Zero)
                {
                    try
                    {
                        GError err = (GError)Marshal.PtrToStructure(error, typeof(GError));
                        _logger.TraceEvent(TraceEventType.Error, /*id*/ 0, $"An error was encountered while reading secret from keyring in the {nameof(MsalCacheStorage)} domain:'{err.Domain}' code:'{err.Code}' message:'{err.Message}'");
                    }
                    catch (Exception e)
                    {
                        _logger.TraceEvent(TraceEventType.Error, /*id*/ 0, $"An exception was encountered while processing libsecret error information during reading in the {nameof(MsalCacheStorage)} ex:'{e}'");
                    }
                }
                else if (string.IsNullOrEmpty(secret))
                {
                    _logger.TraceEvent(TraceEventType.Error, /*id*/ 0, "No matching secret found in the keyring");
                }
                else
                {
                    _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, "Base64 decoding the secret string");
                    fileData = Convert.FromBase64String(secret);
                    _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, $"ReadDataCore, read '{fileData?.Length}' bytes from the keyring");
                }
            }
            else
            {
                _logger.TraceEvent(TraceEventType.Error, /*id*/ 0, "Platform not supported");
                throw new PlatformNotSupportedException();
            }

            return fileData;
        }

        private void WriteDataCore(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, $"Write Data core, going to write '{data.Length}' to the storage");

            if (SharedUtilities.IsMacPlatform() || SharedUtilities.IsLinuxPlatform())
            {
                if (SharedUtilities.IsMacPlatform())
                {
                    _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, "Before write to mac keychain");
                    MacKeyChain.WriteKey(
                                         _creationProperties.MacKeyChainServiceName,
                                         _creationProperties.MacKeyChainAccountName,
                                         data);

                    _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, "After write to mac keychain");
                }
                else if (SharedUtilities.IsLinuxPlatform())
                {
                    _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, "Before saving to linux keyring");

                    IntPtr error = IntPtr.Zero;

                    Libsecret.secret_password_store_sync(
                        schema: GetLibsecretSchema(),
                        collection: _creationProperties.KeyringCollection,
                        label: _creationProperties.KeyringSecretLabel,
                        password: Convert.ToBase64String(data),
                        cancellable: IntPtr.Zero,
                        error: out error,
                        attribute1Type: _creationProperties.KeyringAttribute1.Key,
                        attribute1Value: _creationProperties.KeyringAttribute1.Value,
                        attribute2Type: _creationProperties.KeyringAttribute2.Key,
                        attribute2Value: _creationProperties.KeyringAttribute2.Value,
                        end: IntPtr.Zero);

                    if (error != IntPtr.Zero)
                    {
                        try
                        {
                            GError err = (GError)Marshal.PtrToStructure(error, typeof(GError));
                            _logger.TraceEvent(TraceEventType.Error, /*id*/ 0, $"An error was encountered while saving secret to keyring in the {nameof(MsalCacheStorage)} domain:'{err.Domain}' code:'{err.Code}' message:'{err.Message}'");
                        }
                        catch (Exception e)
                        {
                            _logger.TraceEvent(TraceEventType.Error, /*id*/ 0, $"An exception was encountered while processing libsecret error information during saving in the {nameof(MsalCacheStorage)} ex:'{e}'");
                        }
                    }

                    _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, "After saving to linux keyring");
                }

                // Change data to 1 byte so we can write it to the cache file to update the last write time using the same write code used for windows.
                data = new byte[] { 1 };
            }

            string directoryForCacheFile = Path.GetDirectoryName(CacheFilePath);
            if (!Directory.Exists(directoryForCacheFile))
            {
                string directory = Path.GetDirectoryName(CacheFilePath);
                _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, $"Creating directory '{directory}'");
                Directory.CreateDirectory(directory);
            }

            _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, $"Cache file directory exists. '{Directory.Exists(directoryForCacheFile)}' now writing cache file");

            TryProcessFile(() =>
            {
                File.WriteAllBytes(CacheFilePath, data);
            });
        }

        private void ClearCore()
        {
            _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, "Clearing cache");
            bool cacheFileExists = File.Exists(CacheFilePath);
            _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, $"ReadDataCore Cache file exists '{cacheFileExists}'");

            TryProcessFile(() =>
            {
                _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, "Before deleting the cache file");
                try
                {
                    File.Delete(CacheFilePath);
                }
                catch (Exception e)
                {
                    _logger.TraceEvent(TraceEventType.Error, /*id*/ 0, $"Problem deleting the cache file '{e}'");
                }

                _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, $"After deleting the cache file.");
            });

            if (SharedUtilities.IsMacPlatform())
            {
                _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, "Before delete mac keychain");
                MacKeyChain.DeleteKey(
                                      _creationProperties.MacKeyChainServiceName,
                                      _creationProperties.MacKeyChainAccountName);
                _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, "After delete mac keychain");
            }
            else if (SharedUtilities.IsLinuxPlatform())
            {
                _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, $"Before deletring secret from linux keyring");

                IntPtr error = IntPtr.Zero;

                Libsecret.secret_password_clear_sync(
                    schema: GetLibsecretSchema(),
                    cancellable: IntPtr.Zero,
                    error: out error,
                    attribute1Type: _creationProperties.KeyringAttribute1.Key,
                    attribute1Value: _creationProperties.KeyringAttribute1.Value,
                    attribute2Type: _creationProperties.KeyringAttribute2.Key,
                    attribute2Value: _creationProperties.KeyringAttribute2.Value,
                    end: IntPtr.Zero);

                if (error != IntPtr.Zero)
                {
                    try
                    {
                        GError err = (GError)Marshal.PtrToStructure(error, typeof(GError));
                        _logger.TraceEvent(TraceEventType.Error, /*id*/ 0, $"An error was encountered while clearing secret from keyring in the {nameof(MsalCacheStorage)} domain:'{err.Domain}' code:'{err.Code}' message:'{err.Message}'");
                    }
                    catch (Exception e)
                    {
                        _logger.TraceEvent(TraceEventType.Error, /*id*/ 0, $"An exception was encountered while processing libsecret error information during clearing secret in the {nameof(MsalCacheStorage)} ex:'{e}'");
                    }
                }

                _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, "After deleting secret from linux keyring");
            }
            else if (!SharedUtilities.IsWindowsPlatform())
            {
                _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, "Not supported platform");
                throw new PlatformNotSupportedException();
            }
        }

        private void TryProcessFile(Action action)
        {
            for (int tryCount = 0; tryCount <= FileLockRetryCount; tryCount++)
            {
                try
                {
                    action.Invoke();
                    return;
                }
                catch (Exception e)
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(FileLockRetryWaitInMs));

                    if (tryCount == FileLockRetryCount)
                    {
                        _logger.TraceEvent(TraceEventType.Error, /*id*/ 0, $"An exception was encountered while processing the cache file from the {nameof(MsalCacheStorage)} ex:'{e}'");
                    }
                }
            }
        }

        private IntPtr GetLibsecretSchema()
        {
            if (_libsecretSchema == IntPtr.Zero)
            {
                _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, "Before creating libsecret schema");

                _libsecretSchema = Libsecret.secret_schema_new(
                    name: _creationProperties.KeyringSchemaName,
                    flags: (int)Libsecret.SecretSchemaFlags.SECRET_SCHEMA_DONT_MATCH_NAME,
                    attribute1: _creationProperties.KeyringAttribute1.Key,
                    attribute1Type: (int)Libsecret.SecretSchemaAttributeType.SECRET_SCHEMA_ATTRIBUTE_STRING,
                    attribute2: _creationProperties.KeyringAttribute2.Key,
                    attribute2Type: (int)Libsecret.SecretSchemaAttributeType.SECRET_SCHEMA_ATTRIBUTE_STRING,
                    end: IntPtr.Zero);

                if (_libsecretSchema == IntPtr.Zero)
                {
                    _logger.TraceEvent(TraceEventType.Error, /*id*/ 0, $"Failed to create libsecret schema from the {nameof(MsalCacheStorage)}");
                }

                _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, "After creating libsecret schema");
            }

            return _libsecretSchema;
        }
    }
}
