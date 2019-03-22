// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.ApiConfig.Executors
{
    internal abstract class AbstractExecutor
    {
        private readonly ClientApplicationBase _clientApplicationBase;

        protected AbstractExecutor(IServiceBundle serviceBundle, ClientApplicationBase clientApplicationBase)
        {
            ServiceBundle = serviceBundle;
            _clientApplicationBase = clientApplicationBase;
        }

        protected IServiceBundle ServiceBundle { get; }

        protected void LogVersionInfo()
        {
            CreateRequestContext().Logger.Info(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "MSAL {0} with assembly version '{1}', file version '{2}' and informational version '{3}'",
                    ServiceBundle.PlatformProxy.GetProductName(),
                    MsalIdHelper.GetMsalVersion(),
                    AssemblyUtils.GetAssemblyFileVersionAttribute(),
                    AssemblyUtils.GetAssemblyInformationalVersion()));
        }

        protected RequestContext CreateRequestContext()
        {
            return new RequestContext(_clientApplicationBase.AppConfig.ClientId, MsalLogger.Create(Guid.NewGuid(), ServiceBundle.Config));
        }
    }
}
