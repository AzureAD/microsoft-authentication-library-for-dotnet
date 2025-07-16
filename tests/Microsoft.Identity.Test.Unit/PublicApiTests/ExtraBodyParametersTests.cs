// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class ExtraBodyParametersTests : TestBase
    {
        //TODO: validate cache key components are associated with extra body parameters
        //TODO: Validate multiple OnBeforeTokenRequest events are executed
        [TestMethod]
        public async Task ValidateExtraBodyParameters()
        {
            using (var httpManager = new MockHttpManager())
            {
                //Arrange
                var clientId = "4df2cbbb-8612-49c1-87c8-f334d6d065ad";
                var scope = "api://AzureFMITokenExchange/.default";

                //Act
                //Create application
                var confidentialApp = ConfidentialClientApplicationBuilder
                            .Create(clientId)
                            .WithAuthority("https://login.microsoftonline.com/", "TenantId")
                            .WithClientSecret("ClientSecret")
                            .WithHttpManager(httpManager)
                            .BuildConcrete();

                //Acquire AuthN
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    expectedPostData: new Dictionary<string, string>
                    {
                        { "attributetoken", "AttributeToken" },
                        { "attributetoken2", "AttributeToken" }
                    });

                var extraBodyParams = new Dictionary<string, Func<CancellationToken, Task<string>>>
                    {
                        { "attributetoken", (CancellationToken ct) => GetPdpAuthorization() },
                        { "attributetoken2", (CancellationToken ct) => GetPdpAuthorization() }
                    };

                var authResult = await confidentialApp.AcquireTokenForClient(new[] { scope })
                                                        .WithFmiPath("SomeFmiPath/FmiCredentialPath") //Sets fmi path in client credential request.
                                                        .WithExtraBodyParameters(extraBodyParams) //Sets attributes in client credential request.
                                                        
                                                        .ExecuteAsync()
                                                        .ConfigureAwait(false);

                //Assert
                Assert.IsNotNull(authResult);
            }
        }

        private Task<string> GetPdpAuthorization()
        {
            return Task.FromResult("AttributeToken");
        }
    }
}
