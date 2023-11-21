// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.OAuth2.Throttling;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.WsTrust;

namespace Microsoft.Identity.Client.Internal
{
    internal interface IServiceBundle
    {
        ApplicationConfiguration Config { get; }

        /// <summary>
        /// When outside of a request, the normal logger (requestContext.Logger) is not available. 
        /// This logger is at the app level - it is just not tied to a correlation ID.
        /// </summary>
        ILoggerAdapter ApplicationLogger { get; }
        IHttpManager HttpManager { get; }
        IInstanceDiscoveryManager InstanceDiscoveryManager { get; }
        IPlatformProxy PlatformProxy { get; }
        IWsTrustWebRequestManager WsTrustWebRequestManager { get; }
        IDeviceAuthManager DeviceAuthManager { get; }
        IThrottlingProvider ThrottlingManager { get; }

        IHttpTelemetryManager HttpTelemetryManager { get; }

        IKeyMaterialManager KeyMaterialManager { get; }

        #region Testing
        void SetPlatformProxyForTest(IPlatformProxy platformProxy);
        #endregion Testing
    }
}
