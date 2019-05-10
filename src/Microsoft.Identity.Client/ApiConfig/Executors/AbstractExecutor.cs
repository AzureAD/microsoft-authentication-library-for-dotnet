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
        private readonly ClientApplicationBase _clientApplicationBase;

        protected AbstractExecutor(IServiceBundle serviceBundle, ClientApplicationBase clientApplicationBase)
        {
            ServiceBundle = serviceBundle;
            _clientApplicationBase = clientApplicationBase;
        }

        protected IServiceBundle ServiceBundle { get; }

        protected RequestContext CreateRequestContextAndLogVersionInfo(Guid telemetryCorrelationId)
        {
            var requestContext = new RequestContext(
                _clientApplicationBase.AppConfig.ClientId,
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
