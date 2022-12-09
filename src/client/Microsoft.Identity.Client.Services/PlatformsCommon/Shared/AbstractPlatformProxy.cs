// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.CacheImpl;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Client.PlatformsCommon.Shared
{
    internal abstract class AbstractPlatformProxy : IPlatformProxy
    {
        private readonly Lazy<string> _callingApplicationName;
        private readonly Lazy<string> _callingApplicationVersion;
        private readonly Lazy<ICryptographyManager> _cryptographyManager;
        private readonly Lazy<string> _deviceId;
        private readonly Lazy<string> _deviceModel;
        private readonly Lazy<string> _operatingSystem;
        private readonly Lazy<IPlatformLogger> _platformLogger;
        private readonly Lazy<string> _processorArchitecture;
        private readonly Lazy<string> _productName;
        private readonly Lazy<string> _runtimeVersion;

        protected AbstractPlatformProxy(ILoggerAdapter logger)
        {
            Logger = logger;
            _deviceModel = new Lazy<string>(InternalGetDeviceModel);
            _operatingSystem = new Lazy<string>(InternalGetOperatingSystem);
            _processorArchitecture = new Lazy<string>(InternalGetProcessorArchitecture);
            _callingApplicationName = new Lazy<string>(InternalGetCallingApplicationName);
            _callingApplicationVersion = new Lazy<string>(InternalGetCallingApplicationVersion);
            _deviceId = new Lazy<string>(InternalGetDeviceId);
            _productName = new Lazy<string>(InternalGetProductName);
            _cryptographyManager = new Lazy<ICryptographyManager>(InternalGetCryptographyManager);
            _platformLogger = new Lazy<IPlatformLogger>(InternalGetPlatformLogger);
            _runtimeVersion = new Lazy<string>(InternalGetRuntimeVersion);
        }

        protected IFeatureFlags OverloadFeatureFlags { get; set; }

        protected ILoggerAdapter Logger { get; }

        /// <inheritdoc />
        public string GetDeviceModel()
        {
            return _deviceModel.Value;
        }

        /// <inheritdoc />
        public string GetOperatingSystem()
        {
            return _operatingSystem.Value;
        }

        /// <inheritdoc />
        public string GetProcessorArchitecture()
        {
            return _processorArchitecture.Value;
        }

        /// <inheritdoc />
        public string GetCallingApplicationName()
        {
            return _callingApplicationName.Value;
        }

        /// <inheritdoc />
        public string GetCallingApplicationVersion()
        {
            return _callingApplicationVersion.Value;
        }

        /// <inheritdoc />
        public string GetDeviceId()
        {
            return _deviceId.Value;
        }

        /// <inheritdoc />
        public string GetProductName()
        {
            return _productName.Value;
        }

        /// <inheritdoc />
        public string GetRuntimeVersion()
        {
            return _runtimeVersion.Value;
        }

        /// <inheritdoc />
        public abstract ILegacyCachePersistence CreateLegacyCachePersistence();

        public ITokenCacheAccessor UserTokenCacheAccessorForTest { get; set; }
        public ITokenCacheAccessor AppTokenCacheAccessorForTest { get; set; }

        public virtual ITokenCacheAccessor CreateTokenCacheAccessor(CacheOptions tokenCacheAccessorOptions, bool isApplicationTokenCache = false)
        {
            if (isApplicationTokenCache)
            {
                return AppTokenCacheAccessorForTest ??
                    new InMemoryPartitionedAppTokenCacheAccessor(Logger, tokenCacheAccessorOptions);
            }
            else
            {
                return UserTokenCacheAccessorForTest ??
                    new InMemoryPartitionedUserTokenCacheAccessor(Logger, tokenCacheAccessorOptions);
            }
        }

        /// <inheritdoc />
        public ICryptographyManager CryptographyManager => _cryptographyManager.Value;

        /// <inheritdoc />
        public IPlatformLogger PlatformLogger => _platformLogger.Value;

        internal abstract IFeatureFlags CreateFeatureFlags();

        internal abstract string InternalGetDeviceModel();
        internal abstract string InternalGetOperatingSystem();
        internal abstract string InternalGetProcessorArchitecture();
        internal abstract string InternalGetCallingApplicationName();
        internal abstract string InternalGetCallingApplicationVersion();
        internal abstract string InternalGetDeviceId();
        internal abstract string InternalGetProductName();
        internal abstract ICryptographyManager InternalGetCryptographyManager();
        internal abstract IPlatformLogger InternalGetPlatformLogger();

        // RuntimeInformation.FrameworkDescription is available on all platforms except .NET Framework 4.7 and lower.
        internal virtual string InternalGetRuntimeVersion()
        {
#if !DESKTOP
            return System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
#else
            return string.Empty; // For DESKTOP this should not be hit, since NetDesktopPlatformProxy will take over
#endif
        }

        public virtual ICacheSerializationProvider CreateTokenCacheBlobStorage()
        {
            return null;
        }

        public virtual IFeatureFlags GetFeatureFlags()
        {
            return OverloadFeatureFlags ?? CreateFeatureFlags();
        }

        public void SetFeatureFlags(IFeatureFlags featureFlags)
        {
            OverloadFeatureFlags = featureFlags;
        }

        public virtual IPoPCryptoProvider GetDefaultPoPCryptoProvider()
        {
            throw new NotImplementedException();
        }

        public virtual IDeviceAuthManager CreateDeviceAuthManager()
        {
            return new NullDeviceAuthManager();
        }

        public virtual IMsalHttpClientFactory CreateDefaultHttpClientFactory()
        {
            return new SimpleHttpClientFactory();
        }

        /// <summary>
        /// On Android, iOS and UWP, MSAL will save the legacy ADAL cache in a known location.
        /// On other platforms, the app developer must use the serialization callbacks
        /// </summary>
        public virtual bool LegacyCacheRequiresSerialization => true;
    }
}
