//----------------------------------------------------------------------
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
using System.Globalization;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    class AcquireDeviceCodeHandler
    {
        private Authenticator authenticator;
        private ClientKey clientKey;
        private string resource;
        private CallState callState;
        private string extraQueryParameters;

        public AcquireDeviceCodeHandler(Authenticator authenticator, string resource, string clientId, string extraQueryParameters)
        {
            this.authenticator = authenticator;
            this.callState = AcquireTokenHandlerBase.CreateCallState(this.authenticator.CorrelationId);
            this.clientKey = new ClientKey(clientId);
            this.resource = resource;
            this.extraQueryParameters = extraQueryParameters;
        }
        
        private string CreateDeviceCodeRequestUriString()
        {
            var deviceCodeRequestParameters = new DictionaryRequestParameters(this.resource, this.clientKey);

            if (this.callState != null && this.callState.CorrelationId != Guid.Empty)
            {
                deviceCodeRequestParameters[OAuthParameter.CorrelationId] = this.callState.CorrelationId.ToString();
            }
            
            if (PlatformPlugin.HttpClientFactory.AddAdditionalHeaders)
            {
                IDictionary<string, string> adalIdParameters = AdalIdHelper.GetAdalIdParameters();
                foreach (KeyValuePair<string, string> kvp in adalIdParameters)
                {
                    deviceCodeRequestParameters[kvp.Key] = kvp.Value;
                }
            }

            if (!string.IsNullOrWhiteSpace(extraQueryParameters))
            {
                // Checks for extraQueryParameters duplicating standard parameters
                Dictionary<string, string> kvps = EncodingHelper.ParseKeyValueList(extraQueryParameters, '&', false, this.callState);
                foreach (KeyValuePair<string, string> kvp in kvps)
                {
                    if (deviceCodeRequestParameters.ContainsKey(kvp.Key))
                    {
                        throw new AdalException(AdalError.DuplicateQueryParameter, string.Format(CultureInfo.CurrentCulture, AdalErrorMessage.DuplicateQueryParameterTemplate, kvp.Key));
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
            AdalHttpClient client = new AdalHttpClient(CreateDeviceCodeRequestUriString(), this.callState);
            DeviceCodeResponse response = await client.GetResponseAsync<DeviceCodeResponse>().ConfigureAwait(false);

            if (!string.IsNullOrEmpty(response.Error))
            {
                throw new AdalException(response.Error, response.ErrorDescription);
            }

            return response.GetResult(clientKey.ClientId, resource);
        }

        private void ValidateAuthorityType()
        {
            if (this.authenticator.AuthorityType == AuthorityType.ADFS)
            {
                throw new AdalException(AdalError.InvalidAuthorityType,
                    string.Format(CultureInfo.CurrentCulture, AdalErrorMessage.InvalidAuthorityTypeTemplate, this.authenticator.Authority));
            }
        }

    }
}
