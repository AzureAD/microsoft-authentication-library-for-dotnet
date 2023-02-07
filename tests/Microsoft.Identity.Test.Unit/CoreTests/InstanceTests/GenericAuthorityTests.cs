// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Utils;
using Microsoft.IdentityModel.Protocols;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests.InstanceTests
{
    [TestClass]
    public class GenericAuthorityTests : TestBase
    {
        private const string DemoDuendeSoftwareDotCom = "https://demo.duendesoftware.com";

        [TestMethod]
        public async Task ShouldSupportClientCredentialsWithDuendeDemoInstanceAsync()
        {
            var applicationConfiguration = new ApplicationConfiguration(true);
            ConfidentialClientApplicationBuilder builder = new(applicationConfiguration);
            var app = builder.WithGenericAuthority(DemoDuendeSoftwareDotCom)
                .WithClientId("m2m")
                .WithClientSecret("secret")
                .Build();
            var response = await app.AcquireTokenForClient(new[] { "api" }).ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual("api", response.Scopes.AsSingleString());
            Assert.AreEqual("Bearer", response.TokenType);
        }
        
        [TestMethod]
        public async Task ShouldSupportClientCredentialsWithUserProvidedDiscoveryEndpointWithDuendeDemoInstanceAsync()
        {
            var applicationConfiguration = new ApplicationConfiguration(true);
            ConfidentialClientApplicationBuilder builder = new(applicationConfiguration);
            var app = builder.WithGenericAuthority(DemoDuendeSoftwareDotCom, 
                    DemoDuendeSoftwareDotCom + "/.well-known/openid-configuration")
                .WithClientId("m2m")
                .WithClientSecret("secret")
                .Build();
            var response = await app.AcquireTokenForClient(new[] { "api" }).ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual("api", response.Scopes.AsSingleString());
            Assert.AreEqual("Bearer", response.TokenType);
        }

        [TestMethod]
        [DeploymentItem("Resources\\demo_duendesoftware_com_well-known_openid_configuration.json")]
        public async Task ShouldSupportClientCredentialsWithUserProvidedDocumentRetrieverWithDuendeDemoInstanceAsync()
        {
            var applicationConfiguration = new ApplicationConfiguration(true);
            ConfidentialClientApplicationBuilder builder = new(applicationConfiguration);
            var app = builder.WithGenericAuthority(DemoDuendeSoftwareDotCom,
                    "Resources\\demo_duendesoftware_com_well-known_openid_configuration.json")
                .WithDocumentRetriever(new FileDocumentRetriever())
                .WithClientId("m2m")
                .WithClientSecret("secret")
                .Build();
            var response = await app.AcquireTokenForClient(new[] { "api" }).ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual("api", response.Scopes.AsSingleString());
            Assert.AreEqual("Bearer", response.TokenType);
        }
    }
}
