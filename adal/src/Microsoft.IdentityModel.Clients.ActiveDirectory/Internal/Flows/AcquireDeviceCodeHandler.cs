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
using Microsoft.Identity.Core;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.ClientCreds;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Helpers;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Http;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Instance;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.OAuth2;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Flows
{
    internal class AcquireDeviceCodeHandler
    {
        private readonly Authenticator _authenticator;
        private readonly ClientKey _clientKey;
        private readonly string _resource;
        private readonly RequestContext _requestContext;
        private readonly string _extraQueryParameters;

        public AcquireDeviceCodeHandler(Authenticator authenticator, string resource, string clientId, string extraQueryParameters)
        {
            _authenticator = authenticator;
            _requestContext = AcquireTokenHandlerBase.CreateCallState(clientId, _authenticator.CorrelationId);
            _clientKey = new ClientKey(clientId);
            _resource = resource;
            _extraQueryParameters = extraQueryParameters;
        }

        private DictionaryRequestParameters GetRequestParameters()
        {
            var deviceCodeRequestParameters = new DictionaryRequestParameters(_resource, _clientKey);

            if (_requestContext != null && _requestContext.Logger.CorrelationId != Guid.Empty)
            {
                deviceCodeRequestParameters[OAuthParameter.CorrelationId] = _requestContext.Logger.CorrelationId.ToString();
            }

            IDictionary<string, string> adalIdParameters = AdalIdHelper.GetAdalIdParameters();
            foreach (KeyValuePair<string, string> kvp in adalIdParameters)
            {
                deviceCodeRequestParameters[kvp.Key] = kvp.Value;
            }

            if (!string.IsNullOrWhiteSpace(_extraQueryParameters))
            {
                // Checks for extraQueryParameters duplicating standard parameters
                Dictionary<string, string> kvps = EncodingHelper.ParseKeyValueList(_extraQueryParameters, '&', false, _requestContext);
                foreach (KeyValuePair<string, string> kvp in kvps)
                {
                    if (deviceCodeRequestParameters.ContainsKey(kvp.Key))
                    {
                        throw new AdalException(AdalError.DuplicateQueryParameter, string.Format(CultureInfo.CurrentCulture, AdalErrorMessage.DuplicateQueryParameterTemplate, kvp.Key));
                    }
                }

                deviceCodeRequestParameters.ExtraQueryParameter = _extraQueryParameters;
            }
            return deviceCodeRequestParameters;
        }

        internal async Task<DeviceCodeResult> RunHandlerAsync()
        {
            await _authenticator.UpdateFromTemplateAsync(_requestContext).ConfigureAwait(false);
            AdalHttpClient client = new AdalHttpClient(_authenticator.DeviceCodeUri, _requestContext)
            {
                Client =
                {
                    BodyParameters = GetRequestParameters()
                }
            };
            DeviceCodeResponse response = await client.GetResponseAsync<DeviceCodeResponse>().ConfigureAwait(false);

            if (!string.IsNullOrEmpty(response.Error))
            {
                throw new AdalException(response.Error, response.ErrorDescription);
            }

            return response.GetResult(_clientKey.ClientId, _resource);
        }
    }
}
