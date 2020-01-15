// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Platforms.Shared.Apple
{
    internal class DeviceAuthHelper
    {
        public static bool CanHandleDeviceAuthChallenge { get { return false; } }

        public static Task<string> CreateDeviceAuthChallengeResponseAsync(IDictionary<string, string> challengeData)
        {
            return Task.FromResult(string.Format(CultureInfo.InvariantCulture, @"PKeyAuth Context=""{0}"",Version=""{1}""", challengeData[BrokerConstants.ChallengeResponseContext], challengeData[BrokerConstants.ChallengeResponseVersion]));
        }
    }
}
