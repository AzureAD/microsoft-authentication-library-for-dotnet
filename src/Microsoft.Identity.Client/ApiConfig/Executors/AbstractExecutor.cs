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
        private readonly ClientApplicationBase _clientApplicationBase;

        protected AbstractExecutor(IServiceBundle serviceBundle, ClientApplicationBase clientApplicationBase)
        {
            ServiceBundle = serviceBundle;
            _clientApplicationBase = clientApplicationBase;
        }

        protected IServiceBundle ServiceBundle { get; }

        protected RequestContext CreateRequestContextAndLogVersionInfo(Guid telemetryCorrelationId)
        {
            var requestContext = new RequestContext(ServiceBundle, telemetryCorrelationId);

            requestContext.Logger.Info(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "MSAL {0} with assembly version '{1}'. TelemetryCorrelationId({2})",
                    ServiceBundle.PlatformProxy.GetProductName(),
                    MsalIdHelper.GetMsalVersion(),
                    requestContext.TelemetryCorrelationId));

            return requestContext;
        }
    }
}
