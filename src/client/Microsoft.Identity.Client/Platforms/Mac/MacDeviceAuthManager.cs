using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Platforms.Shared.Apple;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Client.Platforms.Mac
{
    internal class MacDeviceAuthManager : IDeviceAuthManager
    {
        public bool CanHandleDeviceAuthChallenge => throw new NotImplementedException();

        public Task<string> CreateDeviceAuthChallengeResponseAsync(IDictionary<string, string> challengeData)
        {
            return Task.FromResult(string.Format(CultureInfo.InvariantCulture, @"PKeyAuth Context=""{0}"",Version=""{1}""", challengeData[BrokerConstants.ChallengeResponseContext], challengeData[BrokerConstants.ChallengeResponseVersion]));
        }
    }
}
