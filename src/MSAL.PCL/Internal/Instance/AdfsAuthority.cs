//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted free of charge to any person obtaining a copy
// of this software and associated documentation files(the "Software") to deal
// in the Software without restriction including without limitation the rights
// to use copy modify merge publish distribute sublicense and / or sell
// copies of the Software and to permit persons to whom the Software is
// furnished to do so subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND EXPRESS OR
// IMPLIED INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM DAMAGES OR OTHER
// LIABILITY WHETHER IN AN ACTION OF CONTRACT TORT OR OTHERWISE ARISING FROM
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal.Http;
using Microsoft.Identity.Client.Internal.OAuth2;

namespace Microsoft.Identity.Client.Internal.Instance
{
    internal class AdfsAuthority : Authority
    {
        private const string DefaultRealm = "http://schemas.microsoft.com/rel/trusted-realm";

        public AdfsAuthority(string authority) : base(authority)
        {
            this.AuthorityType = AuthorityType.Aad;
        }

        protected override async Task<string> Validate(string host, string tenant, CallState callState)
        {
            if (ValidateAuthority)
            {
                DrsMetadataResponse drsResponse = await GetDrsMetadata(callState);
                if (drsResponse.WebFingerEndpoint == null)
                {
                    throw new MsalServiceException(drsResponse.Error, drsResponse.ErrorDescription);
                }

                string resource = string.Format(CultureInfo.InvariantCulture, "https://{0}", host);
                string webfingerUrl = string.Format(CultureInfo.InvariantCulture,
                    "https://{0}/adfs/.well-known/webfinger?rel={1}&resource={2}", host, DefaultRealm, resource);

                HttpResponse httpResponse =
                    await HttpRequest.SendGet(new Uri(webfingerUrl), null, callState).ConfigureAwait(false);

                if (httpResponse.StatusCode != HttpStatusCode.OK)
                {
                    throw new MsalServiceException("invalid_authority", "authority validation failed.");
                }

                AdfsWebFingerResponse wfr = OAuth2Client.CreateResponse<AdfsWebFingerResponse>(httpResponse, callState,
                    false);
                if (
                    wfr.Links.FirstOrDefault(
                        a =>
                            (a.Rel.Equals(DefaultRealm, StringComparison.OrdinalIgnoreCase) &&
                             a.Href.Equals(resource, StringComparison.OrdinalIgnoreCase))) == null)
                {
                    throw new MsalException("invalid_authority");
                }
            }

            return GetDefaultOpenIdConfigurationEndpoint();
        }

        private async Task<DrsMetadataResponse> GetDrsMetadata(CallState callState)
        {
            if (string.IsNullOrEmpty(Domain))
            {
                throw new MsalException("UPN is required for ADFS authority validation.");
            }

            OAuth2Client client = new OAuth2Client();
            client.AddQueryParameter("api-version", "1.0");
            string endpoint = string.Format(CultureInfo.InvariantCulture,
                "https://enterpriseregistration.windows.net/{0}/EnrollmentServer/Contract", Domain);

            return await ExecuteClient<DrsMetadataResponse>(endpoint, client, callState).ConfigureAwait(false);
        }

        private async Task<T> ExecuteClient<T>(string endpoint, OAuth2Client client, CallState callState)
        {
            try
            {
                return
                    await client.ExecuteRequest<T>(new Uri(endpoint), HttpMethod.Get, callState).ConfigureAwait(false);
            }
            catch (RetryableRequestException exc)
            {
                throw exc.InnerException;
            }
        }
    }
}