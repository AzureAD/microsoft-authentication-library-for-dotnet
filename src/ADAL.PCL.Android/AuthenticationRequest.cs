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

using System;
using System.Collections.Generic;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    class AuthenticationRequest
    {
        public int RequestId { get; set; }

        public string Authority { get; set; }

        public string RedirectUri { get; set; }

        public string Resource { get; set; }

        public string ClientId { get; set; }

        public string LoginHint { get; set; }

        public string UserId { get; set; }

        public string BrokerAccountName { get; set; }

        public Guid CorrelationId { get; set; }

        public string ExtraQueryParamsAuthentication { get; set; }
        
        public bool Silent { get; set; }

        public string Version { get; set; }

        public AuthenticationRequest(IDictionary<string, string> brokerPayload)
        {
            Authority = brokerPayload["authority"];
            Resource = brokerPayload["resource"];
            ClientId = brokerPayload["client_id"];
            if (brokerPayload.ContainsKey("redirect_uri"))
            {
                RedirectUri = brokerPayload["redirect_uri"];
            }

            LoginHint = brokerPayload["username"];
            BrokerAccountName = LoginHint;
            if (brokerPayload.ContainsKey("extra_qp"))
            {
                ExtraQueryParamsAuthentication = brokerPayload["extra_qp"];
            }
            CorrelationId = Guid.Parse(brokerPayload["correlation_id"]);
            Version = brokerPayload["client_version"];
        }
    }
}