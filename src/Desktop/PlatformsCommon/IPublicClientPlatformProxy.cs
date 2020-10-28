// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.UI;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.PlatformsCommon.Factories
{
    internal interface IPublicClientPlatformProxy : IPlatformProxy
    {
        /// <summary>
        /// Gets the upn of the user currently logged into the OS
        /// </summary>
        /// <returns></returns>
        Task<string> GetUserPrincipalNameAsync();

        bool IsSystemWebViewAvailable { get; }

        bool UseEmbeddedWebViewDefault { get; }

        IWebUIFactory GetWebUiFactory();

        void /* for test */ SetWebUiFactory(IWebUIFactory webUiFactory);


        /// <summary>
        /// Go to a Url using the OS default browser. 
        /// </summary>
        Task StartDefaultOsBrowserAsync(string url);

    }
}
