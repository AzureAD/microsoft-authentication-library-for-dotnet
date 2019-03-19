// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.MatsTests
{
    [TestClass]
    public class MatsPublicClientTests
    {
        private const string AppName = "The app Name";
        private const string AppVersion = "1.2.3.4";

        [TestMethod]
        public void TestAcquireTokenSilent()
        {
            var batches = new List<IMatsTelemetryBatch>();

            using (var harness = new MockHttpAndServiceBundle())
            {
                var pca = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                    .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithHttpManager(harness.HttpManager)
                    .WithMatsTelemetry(new MatsConfig
                    {
                        AppName = AppName,
                        AppVer = AppVersion,
                        DispatchAction = batch => { batches.Add(batch); }
                    })
                    .Build();

                var authResult = pca.AcquireTokenSilent(MsalTestConstants.Scope, MsalTestConstants.DisplayableId)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }

            Assert.AreEqual(1, batches.Count);
        }
    }
}
