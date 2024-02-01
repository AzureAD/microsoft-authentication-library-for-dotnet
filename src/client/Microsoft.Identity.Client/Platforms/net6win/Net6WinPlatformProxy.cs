// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Platforms.Features.WebView2WebUi;
using Microsoft.Identity.Client.Platforms.netcore;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Platforms.net6win
{
    /// <summary>
    ///     Platform / OS specific logic.
    /// </summary>
    internal class Net6WinPlatformProxy : NetCorePlatformProxy
    {
        /// <inheritdoc/>
        public Net6WinPlatformProxy(ILoggerAdapter logger)
            : base(logger)
        {
        }

#if NET6_WIN
        [System.Runtime.Versioning.SupportedOSPlatform("windows10.0.17763.0")]
#endif
        public override IBroker CreateBroker(ApplicationConfiguration appConfig, CoreUIParent uiParent)
        {
            if (DesktopOsHelper.IsWin10OrServerEquivalent())
            {
                Logger.Info("WAM supported OS. ");

                return appConfig.BrokerCreatorFunc != null ?
                    appConfig.BrokerCreatorFunc(uiParent, appConfig, Logger) :
                    new Features.RuntimeBroker.RuntimeBroker(uiParent, appConfig, Logger);
            }
            else
            {
                Logger.Info("WAM is not available. WAM is supported only on Windows 10+ or Windows Server 2019+");
                return new NullBroker(Logger);
            }
        }

        public override bool BrokerSupportsWamAccounts => true;

        protected override IWebUIFactory CreateWebUiFactory() => new WebView2WebUiFactory();

        public override string GetDefaultRedirectUri(string clientId, bool useRecommendedRedirectUri = false)
        {
            if (useRecommendedRedirectUri)
            {
                return Constants.NativeClientRedirectUri;
            }

            return Constants.DefaultRedirectUri;
        }

     
    }
}
