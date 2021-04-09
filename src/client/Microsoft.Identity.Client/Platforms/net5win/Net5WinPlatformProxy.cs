// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs;
using Microsoft.Identity.Client.Platforms.Features.WebView2WebUi;
using Microsoft.Identity.Client.Platforms.netcore;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Platforms.net5win
{
    /// <summary>
    ///     Platform / OS specific logic.
    /// </summary>
    internal class Net5WinPlatformProxy : NetCorePlatformProxy
    {
        /// <inheritdoc />
        public Net5WinPlatformProxy(ICoreLogger logger)
            : base(logger)
        {
        }

#if NET5_WIN
        [System.Runtime.Versioning.SupportedOSPlatform("windows10.0.17763.0")]
#endif
        public override IBroker CreateBroker(ApplicationConfiguration appConfig, CoreUIParent uiParent)
        {
            if (DesktopOsHelper.IsWin10OrServerEquivalent())
            {
                return appConfig.BrokerCreatorFunc != null ?
                    appConfig.BrokerCreatorFunc(uiParent, appConfig, Logger) :
                    new Features.WamBroker.WamBroker(uiParent, appConfig, Logger);
            }
            else
            {
                Logger.Info("Not a Win10 machine. WAM is not available");
                return new NullBroker();
            }
        }

        public override bool CanBrokerSupportSilentAuth()
        {
            return true;
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
