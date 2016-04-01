//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

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

            if (brokerPayload.ContainsKey("username"))
            {
                LoginHint = brokerPayload["username"];
                BrokerAccountName = LoginHint;
            }

            if (brokerPayload.ContainsKey("extra_qp"))
            {
                ExtraQueryParamsAuthentication = brokerPayload["extra_qp"];
            }
            CorrelationId = Guid.Parse(brokerPayload["correlation_id"]);
            Version = brokerPayload["client_version"];
        }
    }
}
