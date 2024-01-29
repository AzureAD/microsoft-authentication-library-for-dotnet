// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.CacheImpl;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.TelemetryCore.OpenTelemetry;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.PlatformsCommon.Interfaces
{
    /// <summary>
    /// Common operations for extracting platform / operating system specifics. 
    /// Scope: per app
    /// </summary>
    internal interface IPlatformProxy
    {
        /// <summary>
        /// Gets the device model. On some TFMs this is not returned for security reasons.
        /// </summary>
        /// <returns>device model or null</returns>
        string GetDeviceModel();

        string GetOperatingSystem();

        string GetProcessorArchitecture();

        /// <summary>
        /// Gets the UPN of the user currently logged into the OS
        /// </summary>
        /// <returns></returns>
        Task<string> GetUserPrincipalNameAsync();

        /// <summary>
        /// Returns the name of the calling assembly
        /// </summary>
        /// <returns></returns>
        string GetCallingApplicationName();

        /// <summary>
        /// Returns the version of the calling assembly
        /// </summary>
        /// <returns></returns>
        string GetCallingApplicationVersion();

        /// <summary>
        /// Returns a device identifier. Varies by platform.
        /// </summary>
        /// <returns></returns>
        string GetDeviceId();

        /// <summary>
        /// Gets the default redirect URI for the platform, which sometimes includes the clientId
        /// </summary>
        string GetDefaultRedirectUri(string clientId, bool useRecommendedRedirectUri = false);

        /// <summary>
        /// Returns the MSAL platform, e.g. MSAL.NetCore, MSAL.Desktop.
        /// </summary>
        /// <returns></returns>
        string GetProductName();

        /// <summary>
        /// Returns the framework runtime version on which the app is running, e.g. .NET Core 3.1.3, .NET Framework 4.8.
        /// </summary>
        /// <returns>Runtime version</returns>
        string GetRuntimeVersion();

        ILegacyCachePersistence CreateLegacyCachePersistence();

        bool LegacyCacheRequiresSerialization { get; }

        ITokenCacheAccessor CreateTokenCacheAccessor(CacheOptions accessorOptions, bool isApplicationTokenCache = false);

        ICacheSerializationProvider CreateTokenCacheBlobStorage();

        ICryptographyManager CryptographyManager { get; }

        IPlatformLogger PlatformLogger { get; }

        IOtelInstrumentation OtelInstrumentation { get; }

        IWebUIFactory GetWebUiFactory(ApplicationConfiguration appConfig);

        IPoPCryptoProvider GetDefaultPoPCryptoProvider();

        IFeatureFlags GetFeatureFlags();

        void /* for test */ SetFeatureFlags(IFeatureFlags featureFlags);

        /// <summary>
        /// Go to a URL using the OS default browser. 
        /// </summary>
        Task StartDefaultOsBrowserAsync(string url, bool isBrokerConfigured);

        IBroker CreateBroker(ApplicationConfiguration appConfig, CoreUIParent uiParent);

        IDeviceAuthManager CreateDeviceAuthManager();

        /// <summary>
        /// Most brokers take care of both silent auth and interactive auth, however some (iOS) 
        /// does not support silent auth and gives the RT back to MSAL.
        /// </summary>
        /// <returns></returns>
        bool CanBrokerSupportSilentAuth();

        /// <summary>
        /// WAM broker has a deeper integration into MSAL because MSAL needs to store 
        /// WAM account IDs in the token cache. 
        /// </summary>
        bool BrokerSupportsWamAccounts { get; }

        IMsalHttpClientFactory CreateDefaultHttpClientFactory();

        IKeyMaterialManager GetKeyMaterialManager();
    }
}
