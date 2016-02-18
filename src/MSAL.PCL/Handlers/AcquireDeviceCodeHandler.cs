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
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Handlers
{
    class AcquireDeviceCodeHandler
    {
        private Authenticator authenticator;
        private ClientKey clientKey;
        private HashSet<string> scope;
        private CallState callState;
        private string extraQueryParameters;

        public AcquireDeviceCodeHandler(Authenticator authenticator, string[] scope, string clientId, string extraQueryParameters)
        {
            this.authenticator = authenticator;
            this.callState = AcquireTokenHandlerBase.CreateCallState(this.authenticator.CorrelationId);
            this.clientKey = new ClientKey(clientId);
            this.scope = scope.CreateSetFromArray();
            this.extraQueryParameters = extraQueryParameters;
        }
        
        private string CreateDeviceCodeRequestUriString()
        {
            var deviceCodeRequestParameters = new DictionaryRequestParameters(this.scope, this.clientKey);

            if (this.callState != null && this.callState.CorrelationId != Guid.Empty)
            {
                deviceCodeRequestParameters[OAuthParameter.CorrelationId] = this.callState.CorrelationId.ToString();
            }
            
                IDictionary<string, string> msalIdParameters = MsalIdHelper.GetMsalIdParameters();
                foreach (KeyValuePair<string, string> kvp in msalIdParameters)
                {
                    deviceCodeRequestParameters[kvp.Key] = kvp.Value;
                }
        
            if (!string.IsNullOrWhiteSpace(extraQueryParameters))
            {
                // Checks for extraQueryParameters duplicating standard parameters
                Dictionary<string, string> kvps = EncodingHelper.ParseKeyValueList(extraQueryParameters, '&', false, this.callState);
                foreach (KeyValuePair<string, string> kvp in kvps)
                {
                    if (deviceCodeRequestParameters.ContainsKey(kvp.Key))
                    {
                        throw new MsalException(MsalError.DuplicateQueryParameter, string.Format(MsalErrorMessage.DuplicateQueryParameterTemplate, kvp.Key));
                    }
                }

                deviceCodeRequestParameters.ExtraQueryParameter = extraQueryParameters;
            }

            return new Uri(new Uri(this.authenticator.DeviceCodeUri), "?" + deviceCodeRequestParameters).AbsoluteUri;
        }

        internal async Task<DeviceCodeResult> RunHandlerAsync()
        {
            await this.authenticator.UpdateFromTemplateAsync(this.callState).ConfigureAwait(false);
            this.ValidateAuthorityType();
            MsalHttpClient client = new MsalHttpClient(CreateDeviceCodeRequestUriString(), this.callState);
            DeviceCodeResponse response = await client.GetResponseAsync<DeviceCodeResponse>(ClientMetricsEndpointType.DeviceCode).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(response.Error))
            {
                throw new MsalException(response.Error, response.ErrorDescription);
            }

            return response.GetResult(clientKey.ClientId, scope);
        }

        private void ValidateAuthorityType()
        {
            if (this.authenticator.AuthorityType == AuthorityType.ADFS)
            {
                throw new MsalException(MsalError.InvalidAuthorityType,
                    string.Format(MsalErrorMessage.InvalidAuthorityTypeTemplate, this.authenticator.Authority));
            }
        }

    }
}
