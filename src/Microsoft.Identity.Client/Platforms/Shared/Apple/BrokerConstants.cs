// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


namespace Microsoft.Identity.Client.Platforms.Shared.Apple
{
    internal static class BrokerConstants
    {
        public const string ChallengeResponseHeader = "Authorization";

        public const string ChallengeResponseType = "PKeyAuth";

        public const string ChallengeResponseToken = "AuthToken";

        public const string ChallengeResponseContext = "Context";

        public const string ChallengeResponseVersion = "Version";

        public const string BrowserExtPrefix = "browser://";

        public const string BrowserExtInstallPrefix = "msauth://";

        public const string DeviceAuthChallengeRedirect = "urn:http-auth:PKeyAuth";
        public const string ChallengeHeaderKey = "x-ms-PKeyAuth";
        public const string ChallengeHeaderValue = "1.0";
    }
}
