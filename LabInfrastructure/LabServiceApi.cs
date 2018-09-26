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

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace LabInfrastructure
{
    /// <summary>
    /// Wrapper for new lab service API
    /// </summary>
    public class LabServiceApi : ILabService
    {
        private IEnumerable<IUser> GetUsersFromLab(UserQueryParameters query)
        {
            LabUser user = new LabUser();
            WebClient webClient = new WebClient();

            //Disabled for now until there are tests that use it.
            webClient.QueryString.Add("mamca", "false");
            webClient.QueryString.Add("mdmca", "false");

            //Building user query
            if (query.FederationProvider != null)
                webClient.QueryString.Add("federationProvider", query.FederationProvider.ToString());

            webClient.QueryString.Add("mam", query.IsMamUser != null && (bool)(query.IsMamUser) ? "true" : "false");

            webClient.QueryString.Add("mfa", query.IsMfaUser != null && (bool)(query.IsMfaUser) ? "true" : "false");

            if (query.Licenses != null && query.Licenses.Count > 0)
                webClient.QueryString.Add("license", query.Licenses.ToArray().ToString());

            webClient.QueryString.Add("isFederated", query.IsFederatedUser != null && (bool)(query.IsFederatedUser) ? "true" : "false");

            if (query.IsUserType != null)
                webClient.QueryString.Add("usertype", query.IsUserType.ToString());

            webClient.QueryString.Add("external", query.IsExternalUser != null && (bool)(query.IsExternalUser) ? "true" : "false");

            //Fetch user
            string result = webClient.DownloadString("http://api.msidlab.com/api/user");

            user = JsonConvert.DeserializeObject<LabResponse>(result).Users;

            if (user == null)
                user = JsonConvert.DeserializeObject<LabUser>(result);

            if (!String.IsNullOrEmpty(user.HomeTenantId) && !String.IsNullOrEmpty(user.HomeUPN))
                user.InitializeHomeUser();

            yield return user;
        }

        /// <summary>
        /// Returns a test user account for use in testing.
        /// </summary>
        /// <param name="query">Any and all parameters that the returned user should satisfy.</param>
        /// <returns>Users that match the given query parameters.</returns>
        public IEnumerable<IUser> GetUsers(UserQueryParameters query)
        {
            foreach (var user in GetUsersFromLab(query))
            {
                if (!Uri.IsWellFormedUriString(user.CredentialUrl, UriKind.Absolute))
                {
                    Console.WriteLine($"User '{user.Upn}' has invalid Credential URL: '{user.CredentialUrl}'");
                    continue;
                }

                if (user.IsExternal && user.HomeUser == null)
                {
                    Console.WriteLine($"User '{user.Upn}' has no matching home user.");
                    continue;
                }

                yield return user;
            }
        }
    }
}
