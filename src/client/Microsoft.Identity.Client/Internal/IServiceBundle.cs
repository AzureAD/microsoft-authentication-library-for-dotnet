// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.OAuth2.Throttling;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.WsTrust;

namespace Microsoft.Identity.Client.Internal
{
    internal interface IServiceBundle
    {
        IAppConfigInternal Config { get; }
        ICoreLogger DefaultLogger { get; }
        IHttpManager HttpManager { get; }
        IInstanceDiscoveryManager InstanceDiscoveryManager { get; }
        IPlatformProxy PlatformProxy { get; }
        IWsTrustWebRequestManager WsTrustWebRequestManager { get; }
        IAuthorityEndpointResolutionManager AuthorityEndpointResolutionManager { get; }
        IDeviceAuthManager DeviceAuthManager { get; }
        IThrottlingProvider ThrottlingManager { get; }

        #region Telemetry
        IHttpTelemetryManager HttpTelemetryManager { get; }
        ITelemetryClient Mats { get; } // experimental / deprecated? 
        IMatsTelemetryManager MatsTelemetryManager { get; } // experimental / deprecated?         
        #endregion

        #region Testing
        void SetPlatformProxyForTest(IPlatformProxy platformProxy);
        #endregion Testing
    }
}
