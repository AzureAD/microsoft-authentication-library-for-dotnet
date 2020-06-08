// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.UI;
using NSubstitute;
using Microsoft.Identity.Client.PlatformsCommon.Factories;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Test.Common.Mocks
{
    internal static class MsalMockHelpers
    {
        public static MockWebUI ConfigureMockWebUI(IPlatformProxy platformProxy, AuthorizationResult authorizationResult)
        {
            return ConfigureMockWebUI(platformProxy, authorizationResult, new Dictionary<string, string>());
        }

        public static MockWebUI ConfigureMockWebUI(
            IPlatformProxy platformProxy,
            AuthorizationResult authorizationResult,
            Dictionary<string, string> queryParamsToValidate)
        {
            return ConfigureMockWebUI(platformProxy, authorizationResult, queryParamsToValidate, null);
        }

        public static MockWebUI ConfigureMockWebUI(
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

            return webUi;
        }

        public static void ConfigureMockWebUI(IPlatformProxy platformProxy, IWebUI webUi = null)
        {
            IWebUIFactory mockFactory = Substitute.For<IWebUIFactory>();
            mockFactory.CreateAuthenticationDialog(Arg.Any<CoreUIParent>(), Arg.Any<RequestContext>()).Returns(webUi);
            platformProxy.SetWebUiFactory(mockFactory);
        }

        /// <summary>
        /// Configures a web ui that returns a succesfull result
        /// </summary>
        public static void ConfigureMockWebUI(IPublicClientApplication pca)
        {
            var app = pca as PublicClientApplication;
            MockWebUI webUi = new MockWebUI
            {
                MockResult = AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code")
            };

            ConfigureMockWebUI(app.ServiceBundle.PlatformProxy, webUi);
        }
    }
}
