// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#if NETSTANDARD

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.TelemetryCore.Internal;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.PlatformsCommon.Factories;

namespace Microsoft.Identity.Client.Platforms.netstandard13
{
    /// <summary>
    /// Platform / OS specific logic.  No library (ADAL / MSAL) specific code should go in here.
    /// </summary>
    internal class NetstandardPublicClientPlatformProxy : Netstandard13PlatformProxy, IPublicClientPlatformProxy
    {
        public NetstandardPublicClientPlatformProxy(ICoreLogger logger)
            : base(logger)
        {
        }

        /// <summary>
        /// Get the user logged in
        /// </summary>
        public Task<string> GetUserPrincipalNameAsync()
        {
            throw new PlatformNotSupportedException(
                "MSAL cannot determine the username (UPN) of the currently logged in user." +
                "For Integrated Windows Authentication and Username/Password flows, please use .WithUsername() before calling ExecuteAsync(). " +
                "For more details see https://aka.ms/msal-net-iwa");
        }


        protected IWebUIFactory CreateWebUiFactory() => new WebUIFactory();

       

      
        public override Task StartDefaultOsBrowserAsync(string url)
        {
            throw new NotImplementedException();
        }

        public IWebUIFactory GetWebUiFactory()
        {
            throw new NotImplementedException();
        }

        public void SetWebUiFactory(IWebUIFactory webUiFactory)
        {
            throw new NotImplementedException();
        }

        public bool UseEmbeddedWebViewDefault => false;

        public bool IsSystemWebViewAvailable => false;
    }

    internal class WebUIFactory : IWebUIFactory
    {
        public IWebUI CreateAuthenticationDialog(CoreUIParent parent, RequestContext requestContext)
        {
            throw new PlatformNotSupportedException("Possible Cause: If you are using an XForms app, or generally a netstandard assembly, " +
                "make sure you add a reference to Microsoft.Identity.Client.dll from each platform assembly " +
                "(e.g. UWP, Android, iOS), not just from the common netstandard assembly");
        }
    }
}
#endif
