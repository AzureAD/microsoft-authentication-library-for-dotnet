using System;
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
            string clientId = "e76b0e44-0dd2-42fc-a541-49c7bae22dd8";
            string tenantId = "257df45c-ccc5-4836-8da3-673e38870b1d";
            string secret = "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";

            X509Certificate2 certificate = new X509Certificate2(Convert.FromBase64String(secret));
            Assert.IsNotNull(certificate);

            Environment.SetEnvironmentVariable("REGION_NAME", "ncus");
            string[] scopes = new string[] { $"{clientId}/.default", };
            var cca = ConfidentialClientApplicationBuilder.Create(clientId)
                .WithCertificate(certificate)
                .WithAuthority($"https://login.windows-ppe.net/{tenantId}")
                .Build();

            var result = await cca.AcquireTokenForClient(scopes)
                .WithAzureRegion(true)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(result);
        }
    }
}
