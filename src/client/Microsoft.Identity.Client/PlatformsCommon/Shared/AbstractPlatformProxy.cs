// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.CacheImpl;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Broker;
#if SUPPORTS_OTEL
using Microsoft.Identity.Client.Platforms.Features.OpenTelemetry;
#endif
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.TelemetryCore.OpenTelemetry;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.PlatformsCommon.Shared
{
    internal abstract class AbstractPlatformProxy : IPlatformProxy
    {
        
        public const string MacOsDescriptionForSTS = "MacOS";
        public const string LinuxOSDescriptionForSTS = "Linux";

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
        private readonly Lazy<IOtelInstrumentation> _otelInstrumentation;

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
            _otelInstrumentation = new Lazy<IOtelInstrumentation>(InternalGetOtelInstrumentation);
        }

        private IOtelInstrumentation InternalGetOtelInstrumentation()
        {
#if SUPPORTS_OTEL
            try
            {
                return new OtelInstrumentation();
            } catch (FileNotFoundException ex) 
            {
                // Can happen in in-process Azure Functions: https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4456
                Logger.Warning("Failed instantiating OpenTelemetry instrumentation. Exception: " + ex.Message);
                return new NullOtelInstrumentation();
            }
#else
            return new NullOtelInstrumentation();
#endif
        }

        protected IFeatureFlags OverloadFeatureFlags { get; set; }

        protected ILoggerAdapter Logger { get; }

        /// <inheritdoc/>
        public IWebUIFactory GetWebUiFactory(ApplicationConfiguration appConfig)
        {
            return appConfig.WebUiFactoryCreator != null ?
              appConfig.WebUiFactoryCreator() :
              CreateWebUiFactory();
        }

        /// <inheritdoc/>
        public string GetDeviceModel()
        {
            return _deviceModel.Value;
        }

        /// <inheritdoc/>
        public string GetOperatingSystem()
        {
            return _operatingSystem.Value;
        }

        /// <inheritdoc/>
        public string GetProcessorArchitecture()
        {
            return _processorArchitecture.Value;
        }

        /// <inheritdoc/>
        public abstract Task<string> GetUserPrincipalNameAsync();

        /// <inheritdoc/>
        public string GetCallingApplicationName()
        {
            return _callingApplicationName.Value;
        }

        /// <inheritdoc/>
        public string GetCallingApplicationVersion()
        {
            return _callingApplicationVersion.Value;
        }

        /// <inheritdoc/>
        public string GetDeviceId()
        {
            return _deviceId.Value;
        }

        /// <inheritdoc/>
        public abstract string GetDefaultRedirectUri(string clientId, bool useRecommendedRedirectUri = false);

        /// <inheritdoc/>
        public string GetProductName()
        {
            return _productName.Value;
        }

        /// <inheritdoc/>
        public string GetRuntimeVersion()
        {
            return _runtimeVersion.Value;
        }

        public virtual IKeyMaterialManager GetKeyMaterialManager()
        {
            return NullKeyMaterialManager.Instance;
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public ICryptographyManager CryptographyManager => _cryptographyManager.Value;

        /// <inheritdoc/>
        public IPlatformLogger PlatformLogger => _platformLogger.Value;

        public IOtelInstrumentation OtelInstrumentation => _otelInstrumentation.Value;

        protected abstract IWebUIFactory CreateWebUiFactory();
        protected abstract IFeatureFlags CreateFeatureFlags();
        protected abstract string InternalGetDeviceModel();
        protected abstract string InternalGetOperatingSystem();
        protected abstract string InternalGetProcessorArchitecture();
        protected abstract string InternalGetCallingApplicationName();
        protected abstract string InternalGetCallingApplicationVersion();
        protected abstract string InternalGetDeviceId();
        protected abstract string InternalGetProductName();
        protected abstract ICryptographyManager InternalGetCryptographyManager();
        protected abstract IPlatformLogger InternalGetPlatformLogger();

        // RuntimeInformation.FrameworkDescription is available on all platforms except .NET Framework 4.7 and lower.
        protected virtual string InternalGetRuntimeVersion()
        {
#if !NETFRAMEWORK
            return System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
#else
            return string.Empty; // For NETFRAMEWORK this should not be hit, since NetDesktopPlatformProxy will take over
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

        public virtual Task StartDefaultOsBrowserAsync(string url, bool IBrokerConfigured)
        {
            throw new NotImplementedException();
        }

        public virtual IBroker CreateBroker(ApplicationConfiguration appConfig, CoreUIParent uiParent)
        {
            return appConfig.BrokerCreatorFunc != null ?
                appConfig.BrokerCreatorFunc(uiParent, appConfig, Logger) :
                new NullBroker(Logger);
        }

        public virtual bool CanBrokerSupportSilentAuth()
        {
            return true;
        }

        public virtual bool BrokerSupportsWamAccounts => false;

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
