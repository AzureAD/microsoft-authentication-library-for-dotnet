using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Client.PlatformsCommon.Shared
{
    internal class NullDeviceAuthManager : IDeviceAuthManager
    {
        public bool CanHandleDeviceAuthChallenge => false;

        public Task<string> CreateDeviceAuthChallengeResponseAsync(HttpResponse response, Uri endpointUri)
        {
            return Task.FromResult(DeviceAuthHelper.GetBypassChallengeResponse(response));
        }
    }
}
