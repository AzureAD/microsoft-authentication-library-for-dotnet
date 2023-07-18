// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CacheTests
{
    [TestClass]
    public class CacheFailureTests : TestBase
    {
        [TestMethod]
        public async Task MsalOnly_FailToDeserializeTestsAsync()
        {
            using (var harness = base.CreateTestHarness())
            {
                string jsonContent = "LoremIpsum";
                byte[] byteContent = Encoding.UTF8.GetBytes(jsonContent);

                var cca = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .Build();

                MsalClientException cacheReadException = null;

                cca.AppTokenCache.SetBeforeAccess(notificationArgs =>
                {
                    try
                    {
                        notificationArgs.TokenCache.DeserializeMsalV3(byteContent);                        
                    }
                    catch (MsalClientException e)
                    {
                        cacheReadException = e;
                        throw;
                    }
                });

                var ex = await AssertException.TaskThrowsAsync<MsalClientException>(
                    () => cca.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync())
                    .ConfigureAwait(false);

                Assert.IsNotNull(cacheReadException);
                Assert.AreEqual(ex, cacheReadException);

                Assert.IsTrue(ex.Message.Contains("Lorem"));
                Assert.IsFalse(ex.Message.Contains("LoremIpsum"));
            }
        }

    }
}

