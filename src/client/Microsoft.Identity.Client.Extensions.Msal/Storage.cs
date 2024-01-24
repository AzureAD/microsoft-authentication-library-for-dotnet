// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Identity.Extensions;

namespace Microsoft.Identity.Client.Extensions.Msal
{

    /// <summary>
    /// Wrapper over persistence layer. Does not use cross-process locking. To add cross-process locking, wrap calls 
    /// with <see cref="CrossPlatLock"/>
    /// </summary>
    /// <remarks>Consider using the higher level <see cref="MsalCacheHelper"/></remarks>
    public class Storage
    {
        private readonly TraceSourceLogger _logger;

        internal /* internal for test only */ ICacheAccessor CacheAccessor { get; }

        /// <summary>
        /// The storage creation properties used to create this storage
        /// </summary>
        internal StorageCreationProperties StorageCreationProperties { get; }

        internal const string PersistenceValidationDummyData = "msal_persistence_test";

        /// <summary>
        /// A default logger for use if the user doesn't want to provide their own.
        /// </summary>
        private static readonly Lazy<TraceSourceLogger> s_staticLogger = new Lazy<TraceSourceLogger>(() =>
        {
            return new TraceSourceLogger(EnvUtils.GetNewTraceSource(nameof(MsalCacheHelper) + "Singleton"));
        });

        /// <summary>
        /// Initializes a new instance of the <see cref="Storage"/> class.
        /// The actual cache reading and writing is OS specific:
        /// <list type="bullet">
        /// <item>
        ///     <term>Windows</term>
        ///     <description>DPAPI encrypted file on behalf of the user. </description>
        /// </item>
        /// <item>
        ///     <term>Mac</term>
        ///     <description>Cache is stored in KeyChain.  </description>
        /// </item>
        /// <item>
        ///     <term>Linux</term>
        ///     <description>Cache is stored in Gnome KeyRing - https://developer.gnome.org/libsecret/0.18/  </description>
        /// </item>
        /// </list>
        /// </summary>
        /// <param name="creationProperties">Properties for creating the cache storage on disk</param>
        /// <param name="logger">logger</param>
        /// <returns></returns>
        public static Storage Create(StorageCreationProperties creationProperties, TraceSource logger = null)
        {
            TraceSourceLogger actualLogger = logger == null ? s_staticLogger.Value : new TraceSourceLogger(logger);

            ICacheAccessor cacheAccessor;

            if (creationProperties.UseUnencryptedFallback)
            {
                cacheAccessor = new FileAccessor(creationProperties.CacheFilePath, setOwnerOnlyPermissions: true, logger: actualLogger);
            }
            else
            {
                if (SharedUtilities.IsWindowsPlatform())
                {
                    cacheAccessor = new DpApiEncryptedFileAccessor(creationProperties.CacheFilePath, logger: actualLogger);
                }
                else if (SharedUtilities.IsMacPlatform())
                {
                    cacheAccessor = new MacKeychainAccessor(
                        creationProperties.CacheFilePath,
                        creationProperties.MacKeyChainServiceName,
                        creationProperties.MacKeyChainAccountName,
                        actualLogger);
                }
                else if (SharedUtilities.IsLinuxPlatform())
                {
                    if (creationProperties.UseLinuxUnencryptedFallback)
                    {
                        cacheAccessor = new FileAccessor(creationProperties.CacheFilePath, setOwnerOnlyPermissions: true, actualLogger);
                    }
                    else
                    {
                        cacheAccessor = new LinuxKeyringAccessor(
                           creationProperties.CacheFilePath,
                           creationProperties.KeyringCollection,
                           creationProperties.KeyringSchemaName,
                           creationProperties.KeyringSecretLabel,
                           creationProperties.KeyringAttribute1.Key,
                           creationProperties.KeyringAttribute1.Value,
                           creationProperties.KeyringAttribute2.Key,
                           creationProperties.KeyringAttribute2.Value,
                           actualLogger);
                    }
                }
                else
                {
                    throw new PlatformNotSupportedException();
                }
            }

            return new Storage(creationProperties, cacheAccessor, actualLogger);
        }

        internal /* internal for test, otherwise private */ Storage(
            StorageCreationProperties creationProperties,
            ICacheAccessor cacheAccessor,
            TraceSourceLogger logger)
        {
            StorageCreationProperties = creationProperties;
            _logger = logger;
            CacheAccessor = cacheAccessor;
            _logger.LogInformation($"Initialized '{nameof(Storage)}'");
        }

        /// <summary>
        /// Read and unprotect cache data
        /// </summary>
        /// <returns>Unprotected cache data</returns>
        public byte[] ReadData()
        {
            try
            {
                _logger.LogInformation($"Reading Data");
                byte[] data = CacheAccessor.Read();
                _logger.LogInformation($"Got '{data?.Length ?? 0}' bytes from file storage");
                return data ?? Array.Empty<byte>();
            }
            catch (Exception e)
            {
                _logger.LogError($"An exception was encountered while reading data from the {nameof(Storage)} : {e}");
                throw;
            }
        }

        /// <summary>
        /// Protect and write cache data to file. It overrides existing data.
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
                _logger.LogInformation($"Got '{data?.Length}' bytes to write to storage");
                CacheAccessor.Write(data);
            }
            catch (Exception e)
            {
                _logger.LogError($"An exception was encountered while writing data to {nameof(Storage)} : {e}");
                throw;
            }
        }

        /// <summary>
        /// Delete cache file
        /// </summary>
        /// <param name="ignoreExceptions">Throw on exceptions</param>
        public void Clear(bool ignoreExceptions = false)
        {
            try
            {
                _logger.LogInformation("Clearing the cache file");
                CacheAccessor.Clear();
            }
            catch (Exception e)
            {
                _logger.LogError($"An exception was encountered while clearing data from {nameof(Storage)} : {e}");

                if (!ignoreExceptions)
                    throw;
            }
        }

        /// <summary>
        /// Tries to write -> read -> clear a secret from the underlying persistence mechanism
        /// </summary>
        public void VerifyPersistence()
        {
            // do not use the _cacheAccessor for writing dummy data, as it might overwrite an actual token cache
            var persitenceValidatationAccessor = CacheAccessor.CreateForPersistenceValidation();

            try
            {
                _logger.LogInformation($"[Verify Persistence] Writing Data ");
                persitenceValidatationAccessor.Write(Encoding.UTF8.GetBytes(PersistenceValidationDummyData));

                _logger.LogInformation($"[Verify Persistence] Reading Data ");
                var data = persitenceValidatationAccessor.Read();

                if (data == null || data.Length == 0)
                {
                    throw new MsalCachePersistenceException(
                        "Persistence check failed. Data was written but it could not be read. " +
                        "Possible cause: on Linux, LibSecret is installed but D-Bus isn't running because it cannot be started over SSH.");
                }

                string dataRead = Encoding.UTF8.GetString(data);
                if (!string.Equals(PersistenceValidationDummyData, dataRead, StringComparison.Ordinal))
                {
                    throw new MsalCachePersistenceException(
                        $"Persistence check failed. Data written {PersistenceValidationDummyData} is different from data read {dataRead}");
                }
            }
            catch (InteropException e)
            {
                throw new MsalCachePersistenceException(
                    $"Persistence check failed. Reason: {e.Message}. OS error code {e.ErrorCode}.", e);
            }
            catch (Exception ex) when (!(ex is MsalCachePersistenceException))
            {
                throw new MsalCachePersistenceException("Persistence check failed. Inspect inner exception for details", ex);
            }
            finally
            {
                try
                {
                    _logger.LogInformation($"[Verify Persistence] Clearing data");
                    persitenceValidatationAccessor.Clear();
                }
                catch (Exception e)
                {
                    _logger.LogError($"[Verify Persistence] Could not clear the test data: " + e);
                }
            }
        }
    }
}
