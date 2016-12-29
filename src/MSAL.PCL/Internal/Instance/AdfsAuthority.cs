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
using System.Collections.Generic;
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
        private readonly HashSet<string> _validForDomainsList = new HashSet<string>();
        public AdfsAuthority(string authority) : base(authority)
        {
            this.AuthorityType = AuthorityType.Adfs;
        }

        protected override bool ExistsInValidatedAuthorityCache(string userPrincipalName)
        {
            if (string.IsNullOrEmpty(userPrincipalName))
            {
                throw new MsalException("UPN is required for ADFS authority validation.");
            }

            return ValidatedAuthorities.ContainsKey(this.CanonicalAuthority) &&
                   ((AdfsAuthority) ValidatedAuthorities[this.CanonicalAuthority])._validForDomainsList.Contains(
                       GetDomainFromUpn(userPrincipalName));
        }

        protected override async Task<string> GetOpenIdConfigurationEndpoint(string host, string tenant,
            string userPrincipalName, CallState callState)
        {
            if (ValidateAuthority)
            {
                DrsMetadataResponse drsResponse = await GetMetadataFromEnrollmentServer(userPrincipalName, callState);
                if (!string.IsNullOrEmpty(drsResponse.Error))
                {
                    throw new MsalServiceException(drsResponse.Error, drsResponse.ErrorDescription);
                }

                if (drsResponse.IdentityProviderService?.PassiveAuthEndpoint == null)
                {
                    throw new MsalServiceException("missing_passive_auth_endpoint", "missing_passive_auth_endpoint");
                }

                string resource = string.Format(CultureInfo.InvariantCulture, "https://{0}", host);
                string webfingerUrl = string.Format(CultureInfo.InvariantCulture,
                    "https://{0}/adfs/.well-known/webfinger?rel={1}&resource={2}",
                    drsResponse.IdentityProviderService.PassiveAuthEndpoint.Host,
                    DefaultRealm, resource);

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

        protected override string CreateEndpointForAuthorityType(string host, string tenant)
        {
            return string.Format(CultureInfo.InvariantCulture,
                "https://{0}/{1}/.well-known/openid-configuration", host, tenant);
        }

        protected override void AddToValidatedAuthorities(string userPrincipalName)
        {
            AdfsAuthority authorityInstance = this;
            if (ValidatedAuthorities.ContainsKey(this.CanonicalAuthority))
            {
                authorityInstance = (AdfsAuthority) ValidatedAuthorities[this.CanonicalAuthority];
            }

            authorityInstance._validForDomainsList.Add(GetDomainFromUpn(userPrincipalName));
            ValidatedAuthorities[this.CanonicalAuthority] = authorityInstance;
        }

        private async Task<DrsMetadataResponse> GetMetadataFromEnrollmentServer(string userPrincipalName,
            CallState callState)
        {
            try
            {
                //attempt to connect to on-premise enrollment server first.
                return await QueryEnrollmentServerEndpoint(string.Format(CultureInfo.InvariantCulture,
                    "https://enterpriseregistration.{0}/enrollmentserver/contract",
                    GetDomainFromUpn(userPrincipalName)), callState).ConfigureAwait(false);
            }
            catch (Exception exc)
            {
                PlatformPlugin.Logger.Information(callState,
                    "On-Premise ADFS enrollment server endpoint lookup failed. Error - " + exc.Message);
            }

            return await QueryEnrollmentServerEndpoint(string.Format(CultureInfo.InvariantCulture,
                "https://enterpriseregistration.windows.net/{0}/enrollmentserver/contract",
                GetDomainFromUpn(userPrincipalName)), callState).ConfigureAwait(false);
        }

        private async Task<DrsMetadataResponse> QueryEnrollmentServerEndpoint(string endpoint, CallState callState)
        {
            OAuth2Client client = new OAuth2Client();
            client.AddQueryParameter("api-version", "1.0");
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

        private string GetDomainFromUpn(string upn)
        {
            if (!upn.Contains("@"))
            {
                throw new ArgumentException("userPrincipalName does not contain @ character.");
            }

            return upn.Split('@')[1];
        }
    }
}