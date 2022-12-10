// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client
{
    internal sealed class ApplicationConfigurationPublic : ApplicationConfiguration, IAppConfig
    {
        public ApplicationConfigurationPublic() : base(false)
        {
        }

        public WindowsBrokerOptions WindowsBrokerOptions { get; set; }

        public Func<CoreUIParent, ApplicationConfiguration, ILoggerAdapter, IBroker> BrokerCreatorFunc { get; set; }
        public Func<IWebUIFactory> WebUiFactoryCreator { get; set; }
    }
}
