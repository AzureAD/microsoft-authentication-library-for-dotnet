// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.PlatformsCommon.Interfaces
{
    /// <summary>
    /// Common operations for extracting platform / operating system specifics
    /// </summary>
    internal interface IPlatformProxy
    {
        bool IsSystemWebViewAvailable { get; }

        bool UseEmbeddedWebViewDefault { get; }

        /// <summary>
        /// Gets the device model. On some TFMs this is not returned for security reasonons.
        /// </summary>
        /// <returns>device model or null</returns>
        string GetDeviceModel();

        string GetEnvironmentVariable(string variable);

        string GetOperatingSystem();

        string GetProcessorArchitecture();

        /// <summary>
        /// Gets the upn of the user currently logged into the OS
        /// </summary>
        /// <returns></returns>
        Task<string> GetUserPrincipalNameAsync();

        /// <summary>
        /// Returns true if the current OS logged in user is AD or AAD joined.
        /// </summary>
        /// <returns></returns>
        bool IsDomainJoined();

        Task<bool> IsUserLocalAsync(RequestContext requestContext);

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
        /// Get the redirect Uri as string, or the a broker specified value
        /// </summary>
        string GetBrokerOrRedirectUri(Uri redirectUri);

        /// <summary>
        /// Gets the default redirect uri for the platform, which sometimes includes the clientId
        /// </summary>
        string GetDefaultRedirectUri(string clientId);

        string GetProductName();

        ILegacyCachePersistence CreateLegacyCachePersistence();

        ITokenCacheAccessor CreateTokenCacheAccessor();

        ITokenCacheBlobStorage CreateTokenCacheBlobStorage();

        ICryptographyManager CryptographyManager { get; }

        IPlatformLogger PlatformLogger { get; }

        IWebUIFactory GetWebUiFactory();

        // MATS related data
        string GetDevicePlatformTelemetryId();
        string GetDeviceNetworkState();
        int GetMatsOsPlatformCode();
        string GetMatsOsPlatform();
        void /* for test */ SetWebUiFactory(IWebUIFactory webUiFactory);

        IFeatureFlags GetFeatureFlags();

        void /* for test */ SetFeatureFlags(IFeatureFlags featureFlags);

        /// <summary>
        /// Go to a Url using the OS default browser. 
        /// </summary>
        Task StartDefaultOsBrowserAsync(string url);
    }
}
