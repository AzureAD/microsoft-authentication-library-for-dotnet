// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.TelemetryCore.Internal;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.UI;
using Microsoft.Win32;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Platforms.netcore;

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

        public override IBroker CreateBroker(IAppConfigInternal appConfig, CoreUIParent uiParent)
        {
            return appConfig.BrokerCreatorFunc != null ?
                appConfig.BrokerCreatorFunc(uiParent, Logger) :
                new Features.WamBroker.WamBroker(uiParent, Logger);
        }

        public override bool CanBrokerSupportSilentAuth()
        {
            return true;
        }

        public override bool BrokerSupportsWamAccounts => true;

        protected override IWebUIFactory CreateWebUiFactory() => new Net5WebUiFactory();

        public override bool UseEmbeddedWebViewDefault => true;

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
