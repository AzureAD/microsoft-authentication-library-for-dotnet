// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.TelemetryTests
{
    [TestClass]
    public class TelemetryPublicClientTests : TestBase
    {
        private const string AppName = "The app Name";
        private const string AppVersion = "1.2.3.4";

        [TestMethod]
        public void TestAcquireTokenSilent()
        {
            var eventPayloads = new List<ITelemetryEventPayload>();

            using (var harness = CreateTestHarness())
            {
                var pca = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                    .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithHttpManager(harness.HttpManager)
                    .WithClientName(AppName)
                    .WithClientVersion(AppVersion)
                    .WithTelemetry(new TelemetryConfig
                    {
                        DispatchAction = eventPayload => { eventPayloads.Add(eventPayload); }
                    })
                    .Build();

                var authResult = pca.AcquireTokenSilent(MsalTestConstants.Scope, MsalTestConstants.DisplayableId)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }

            foreach (var eventPayload in eventPayloads)
            {
                Console.WriteLine(eventPayload.ToJsonString());
            }

            Assert.AreEqual(1, eventPayloads.Count);
        }
    }
}
