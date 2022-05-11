// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.Integration.net45.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Broker;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Client.OAuth2;
using System.Runtime.InteropServices;
using System;
using NSubstitute;

namespace Microsoft.Identity.Test.Integration.NetCore
{
    
    [TestClass]
    public class RuntimeBrokerTests
    {
        /// <summary>
        /// Initialized by MSTest (do not make private or readonly)
        /// </summary>
        public TestContext TestContext { get; set; }
        private readonly CoreUIParent _parent = new CoreUIParent();
        private ICoreLogger _logger;
        private RuntimeBroker _wamBroker;

        [TestInitialize]
        public void Init()
        {
            ApplicationConfiguration applicationConfiguration = new ApplicationConfiguration();
            _logger = Substitute.For<ICoreLogger>();

            _wamBroker = new RuntimeBroker(_parent, applicationConfiguration, _logger);

        }

        [TestMethod]
        public async Task WamSilentAuthAsync()
        {
            string[] scopes = new[]
                {
                    "https://management.core.windows.net//.default"
                };

            var pcaBuilder = PublicClientApplicationBuilder
               .Create("04f0c124-f2bc-4f59-8241-bf6df9866bbd")
               .WithAuthority("https://login.microsoftonline.com/organizations");

            pcaBuilder = pcaBuilder.WithBroker2();
            var pca = pcaBuilder.Build();

            // Act
            try
            {
                var result = await pca.AcquireTokenSilent(scopes, PublicClientApplication.OperatingSystemAccount).ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
            }
            catch (MsalUiRequiredException ex)
            {
                Assert.IsTrue(ex.Message.Contains("Need user interaction to continue"));
            }

        }

        [TestMethod]
        public async Task WamSilentAuthWithLabAppAsync()
        {
            string[] scopes = new[]
                {
                    "user.read"
                };

            var pcaBuilder = PublicClientApplicationBuilder
               .Create("4b0db8c2-9f26-4417-8bde-3f0e3656f8e0")
               .WithAuthority("https://login.microsoftonline.com/organizations");

            pcaBuilder = pcaBuilder.WithBroker2();
            var pca = pcaBuilder.Build();

            // Act
            try
            {
                var result = await pca.AcquireTokenSilent(scopes, PublicClientApplication.OperatingSystemAccount).ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
            }
            catch (MsalUiRequiredException ex)
            {
                Assert.IsTrue(ex.Message.Contains("Need user interaction to continue"));
            }

        }
    }
}
