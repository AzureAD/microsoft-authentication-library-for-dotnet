using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Integration.net45.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.net45.HeadlessTests
{
    [TestClass]
    public class RegionalAuthIntegrationTests
    {
        [TestMethod]
        public async Task RegionalAuthHappyPathAsync()
        {
            string clientId = "fd1f0c70-5cb6-40af-98fa-09603f35a219";
            string tenantId = "f686d426-8d16-42db-81b7-ab578e110ccd";
            string secret = "pqS_bZ2:4Pq@7w?SB76?s]MveRnjrhWp";
            var dict = new Dictionary<string, string>
            {
                ["allowestsrnonmsi"] = "true"
            };

            Environment.SetEnvironmentVariable("REGION_NAME", "ncus");
            string[] scopes = new string[] { $"{clientId}/.default", };
            var cca = ConfidentialClientApplicationBuilder.Create(clientId)
                .WithClientSecret(secret)
                .WithAuthority($"https://login.windows-ppe.net/{tenantId}")
                .Build();

            var result = await cca.AcquireTokenForClient(scopes)
                .WithAzureRegion(true)
                .WithExtraQueryParameters(dict)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(result);
        }
    }
}
