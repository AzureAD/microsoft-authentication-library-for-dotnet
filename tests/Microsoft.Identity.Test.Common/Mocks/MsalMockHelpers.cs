// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.UI;
using NSubstitute;
using Microsoft.Identity.Client.PlatformsCommon.Factories;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Test.Common.Mocks
{
    internal static class MsalMockHelpers
    {
        public static void ConfigureMockWebUI(IPlatformProxy platformProxy, AuthorizationResult authorizationResult)
        {
            ConfigureMockWebUI(platformProxy, authorizationResult, new Dictionary<string, string>());
        }

        public static void ConfigureMockWebUI(
            IPlatformProxy platformProxy,
            AuthorizationResult authorizationResult,
            Dictionary<string, string> queryParamsToValidate)
        {
            ConfigureMockWebUI(platformProxy, authorizationResult, queryParamsToValidate, null);
        }

        public static void ConfigureMockWebUI(
            IPlatformProxy platformProxy,
            AuthorizationResult authorizationResult,
            Dictionary<string, string> queryParamsToValidate,
            string environment)
        {
            MockWebUI webUi = new MockWebUI
            {
                QueryParamsToValidate = queryParamsToValidate,
                MockResult = authorizationResult,
                ExpectedEnvironment = environment
            };

            ConfigureMockWebUI(platformProxy, webUi);
        }

        public static void ConfigureMockWebUI(IPlatformProxy platformProxy, MockWebUI webUi)
        {
            IWebUIFactory mockFactory = Substitute.For<IWebUIFactory>();
            mockFactory.CreateAuthenticationDialog(Arg.Any<CoreUIParent>(), Arg.Any<RequestContext>()).Returns(webUi);
            platformProxy.SetWebUiFactory(mockFactory);
        }
    }
}
