//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------


namespace Microsoft.Identity.Client
{
    internal class BrokerConstants
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
