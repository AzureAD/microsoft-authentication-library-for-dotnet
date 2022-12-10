// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.PlatformsCommon.Interfaces
{
    /// <summary>
    /// Common operations for extracting platform / operating system specifics. 
    /// Scope: per app
    /// </summary>
    internal interface IPlatformProxyPublic : IPlatformProxy
    {
        /// <summary>
        /// Gets the UPN of the user currently logged into the OS
        /// </summary>
        /// <returns></returns>
        Task<string> GetUserPrincipalNameAsync();

        /// <summary>
        /// Gets the default redirect URI for the platform, which sometimes includes the clientId
        /// </summary>
        string GetDefaultRedirectUri(string clientId, bool useRecommendedRedirectUri = false);

        IWebUIFactory GetWebUiFactory(ApplicationConfigurationPublic appConfig);

        /// <summary>
        /// Go to a URL using the OS default browser. 
        /// </summary>
        Task StartDefaultOsBrowserAsync(string url, bool isBrokerConfigured);

        IBroker CreateBroker(ApplicationConfigurationPublic appConfig, CoreUIParent uiParent);

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
    }
}
