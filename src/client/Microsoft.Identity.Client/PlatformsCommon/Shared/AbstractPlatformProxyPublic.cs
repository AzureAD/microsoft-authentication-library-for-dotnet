// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.PlatformsCommon.Shared
{
    internal abstract class AbstractPlatformProxyPublic : AbstractPlatformProxy, IPlatformProxyPublic
    {
        protected AbstractPlatformProxyPublic(ILoggerAdapter logger) : base(logger)
        {
        }

        /// <inheritdoc />
        public IWebUIFactory GetWebUiFactory(ApplicationConfiguration appConfig)
        {
            return appConfig.WebUiFactoryCreator != null ?
              appConfig.WebUiFactoryCreator() :
              CreateWebUiFactory();
        }

        /// <inheritdoc />
        public abstract Task<string> GetUserPrincipalNameAsync();

        /// <inheritdoc />
        public abstract string GetDefaultRedirectUri(string clientId, bool useRecommendedRedirectUri = false);

        protected abstract IWebUIFactory CreateWebUiFactory();

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
    }
}
