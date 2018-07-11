using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Test.Microsoft.Identity.Core.UIAutomation.infrastructure
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
