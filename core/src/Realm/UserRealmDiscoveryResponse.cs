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
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.Identity.Core.Helpers;
using Microsoft.Identity.Core.Http;

namespace Microsoft.Identity.Core.Realm
{
    [DataContract]
    internal sealed class UserRealmDiscoveryResponse
    {
        [DataMember(Name = "ver")]
        public string Version { get; set; }

        [DataMember(Name = "account_type")]
        public string AccountType { get; set; }

        [DataMember(Name = "federation_protocol")]
        public string FederationProtocol { get; set; }

        [DataMember(Name = "federation_metadata_url")]
        public string FederationMetadataUrl { get; set; }

        [DataMember(Name = "federation_active_auth_url")]
        public string FederationActiveAuthUrl { get; set; }

        [DataMember(Name = "cloud_audience_urn")]
        public string CloudAudienceUrn { get; set; }

        internal static async Task<UserRealmDiscoveryResponse> CreateByDiscoveryAsync(string userRealmUri, string userName, RequestContext requestContext)
        {
            var msg = "Sending request to userrealm endpoint.";
            requestContext.Logger.Info(msg);
            requestContext.Logger.InfoPii(msg);
            var httpResponse = await HttpRequest.SendGetAsync(
                new UriBuilder(userRealmUri + userName + "?api-version=1.0").Uri, null, requestContext).ConfigureAwait(false);
            return httpResponse.StatusCode == System.Net.HttpStatusCode.OK ? JsonHelper.DeserializeFromJson<UserRealmDiscoveryResponse>(httpResponse.Body) : null;
        }
    }
}