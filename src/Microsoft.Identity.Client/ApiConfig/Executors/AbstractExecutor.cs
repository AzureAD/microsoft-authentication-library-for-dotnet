// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
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

        protected RequestContext CreateRequestContextAndLogVersionInfo(Guid telemetryCorrelationId)
        {
            var requestContext = new RequestContext(
                _appConfig.ClientId,
                MsalLogger.Create(telemetryCorrelationId, ServiceBundle.Config),
                telemetryCorrelationId);

            requestContext.Logger.Info(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "MSAL {0} with assembly version '{1}', file version '{2}' and informational version '{3}'. TelemetryCorrelationId({4})",
                    ServiceBundle.PlatformProxy.GetProductName(),
                    MsalIdHelper.GetMsalVersion(),
                    AssemblyUtils.GetAssemblyFileVersionAttribute(),
                    AssemblyUtils.GetAssemblyInformationalVersion(),
                    requestContext.TelemetryCorrelationId));

            return requestContext;
        }
    }
}
