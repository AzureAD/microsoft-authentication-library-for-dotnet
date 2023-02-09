// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Threading;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.ApiConfig.Executors
{
    internal abstract class AbstractExecutor
    {
        protected AbstractExecutor(IServiceBundle serviceBundle)
        {
            ServiceBundle = serviceBundle;
        }

        public IServiceBundle ServiceBundle { get; }

        protected RequestContext CreateRequestContextAndLogVersionInfo(Guid correlationId, CancellationToken userCancellationToken = default)
        {
            var requestContext = new RequestContext(ServiceBundle, correlationId, userCancellationToken);

            requestContext.Logger.Info(
                () => string.Format(
                    CultureInfo.InvariantCulture,
                    "MSAL {0} with assembly version '{1}'. CorrelationId({2})",
                    ServiceBundle.PlatformProxy.GetProductName(),
                    MsalIdHelper.GetMsalVersion(),
                    requestContext.CorrelationId));

            return requestContext;
        }
    }
}
