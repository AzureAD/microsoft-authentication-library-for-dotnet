// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Test.Common.Core.Helpers;
using System;
using System.IdentityModel.Tokens.Jwt;

namespace Microsoft.Identity.Test.Integration.NetCore.HeadlessTests
{
    [TestClass]
    internal class AttributeTests
    {
        [TestMethod]
        public async Task TestAttributes()
        {
            //Arrange
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            //Fmi app/scenario parameters
            var clientId = "4df2cbbb-8612-49c1-87c8-f334d6d065ad";
            var scope = "api://AzureFMITokenExchange/.default";

            //Act
            //Create application
            var confidentialApp = ConfidentialClientApplicationBuilder
                        .Create(clientId)
                        .WithAuthority("https://login.microsoftonline.com/", "TenantId")
                        .WithClientAssertion(() => GetFmiCredential())
                        .BuildConcrete();

            //Acquire AuthN
            var authResult = await confidentialApp.AcquireTokenForClient(new[] { scope })
                                                    .WithFmiPath("SomeFmiPath/FmiCredentialPath") //Sets fmi path in client credential request.
                                                    .ExecuteAsync()
                                                    .ConfigureAwait(false);

            //Assert

        }

        private string GetFmiCredential()
        {
            throw new NotImplementedException();
        }
    }
}
