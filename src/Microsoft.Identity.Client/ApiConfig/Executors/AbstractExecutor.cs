// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.ApiConfig.Executors
{
    internal abstract class AbstractExecutor
    {
        private readonly IAppConfig _appConfig;

        protected AbstractExecutor(IServiceBundle serviceBundle, IAppConfig appConfig)
        {
            ServiceBundle = serviceBundle;
            _appConfig = appConfig;
        }

        protected IServiceBundle ServiceBundle { get; }

        protected RequestContext CreateRequestContextAndLogVersionInfo(Guid correlationId)
        {
            var requestContext = new RequestContext(ServiceBundle, correlationId);

            requestContext.Logger.Info(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "MSAL {0} with assembly version '{1}'. CorrelationId({2})",
                    ServiceBundle.PlatformProxy.GetProductName(),
                    MsalIdHelper.GetMsalVersion(),
                    requestContext.CorrelationId));

            return requestContext;
        }
    }
}
