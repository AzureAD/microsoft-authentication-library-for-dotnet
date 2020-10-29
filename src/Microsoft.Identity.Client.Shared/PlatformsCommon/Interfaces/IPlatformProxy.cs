// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Internal.Broker;

namespace Microsoft.Identity.Client.PlatformsCommon.Interfaces
{
    /// <summary>
    /// Common operations for extracting platform / operating system specifics
    /// </summary>
    internal interface IPlatformProxy
    {
        /// <summary>
        /// Gets the device model. On some TFMs this is not returned for security reasonons.
        /// </summary>
        /// <returns>device model or null</returns>
        string GetDeviceModel();

        string GetEnvironmentVariable(string variable);

        string GetOperatingSystem();

        string GetProcessorArchitecture();

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
        /// Gets the default redirect uri for the platform, which sometimes includes the clientId
        /// </summary>
        string GetDefaultRedirectUri(string clientId, bool useRecommendedRedirectUri = false);

        string GetProductName();

        ILegacyCachePersistence CreateLegacyCachePersistence();

        ITokenCacheAccessor CreateTokenCacheAccessor();

        ITokenCacheBlobStorage CreateTokenCacheBlobStorage();

        ICryptographyManager CryptographyManager { get; }

        IPlatformLogger PlatformLogger { get; }

        IPoPCryptoProvider GetDefaultPoPCryptoProvider();

        // MATS related data
        string GetDevicePlatformTelemetryId();
        int GetMatsOsPlatformCode();
        string GetMatsOsPlatform();

        IFeatureFlags GetFeatureFlags();

        void /* for test */ SetFeatureFlags(IFeatureFlags featureFlags);
        
        IDeviceAuthManager CreateDeviceAuthManager();

        IMsalHttpClientFactory CreateDefaultHttpClientFactory();

        #region Broker
        

        IBroker CreateBroker();

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


        #endregion
    }

}
