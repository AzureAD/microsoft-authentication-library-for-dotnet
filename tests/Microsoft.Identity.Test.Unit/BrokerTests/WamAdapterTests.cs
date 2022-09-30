// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if !NET6_0
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Platforms.Features.WamBroker;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.BrokerTests
{
    [TestClass]
    [TestCategory(TestCategories.Broker)]
    public class WamAdapterTests : TestBase
    {

        [TestMethod]
        public void WAM_ProviderError_HasRedirectUri()
        {
            var wamResponse = NSubstitute.Substitute.For<IWebTokenRequestResultWrapper>();
            wamResponse.ResponseStatus.Returns(Windows.Security.Authentication.Web.Core.WebTokenRequestStatus.ProviderError);

            var wamPlugin = Substitute.For<IWamPlugin>();
            var logger = Substitute.For<ILoggerAdapter>();
            wamPlugin.MapTokenRequestError(Windows.Security.Authentication.Web.Core.WebTokenRequestStatus.ProviderError, 0, true)
                .Returns(Tuple.Create("some_provider_error", "", false));

            var exception = AssertException.Throws<MsalServiceException> (
                () => WamAdapters.CreateMsalResponseFromWamResponse(
                wamResponse, 
                wamPlugin, 
                TestConstants.ClientId, 
                logger, 
                true));

            Assert.IsTrue(exception.Message.Contains($"ms-appx-web://microsoft.aad.brokerplugin/{TestConstants.ClientId}"));
        }

    }
}
#endif
