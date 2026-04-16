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
using Microsoft.Identity.Client.ApiConfig.Parameters;

namespace Microsoft.Identity.Test.Common.Mocks
{
    internal static class MsalMockHelpers
    {
      
        public static MockWebUI ConfigureMockWebUI(
            this IServiceBundle serviceBundle,
            AuthorizationResult authorizationResult,
            Dictionary<string, string> queryParamsToValidate = null,
            string environment = null)
        {
            MockWebUI webUi = new MockWebUI
            {
                QueryParamsToValidate = queryParamsToValidate ?? new Dictionary<string, string>(),
                MockResult = authorizationResult,
                ExpectedEnvironment = environment
            };

            ConfigureWebUiFactory(serviceBundle, webUi);

            return webUi;
        }

        /// <summary>
        /// Configures a web ui that returns a successful result
        /// </summary>
        public static void ConfigureMockWebUI(
            this IServiceBundle serviceBundle, 
            IWebUI webUi = null)
        {
            if (webUi == null)
            {
                webUi = new MockWebUI
                {
                    MockResult = AuthorizationResult.FromUri(serviceBundle.Config.RedirectUri + "?code=some-code")
                };
            }

            ConfigureWebUiFactory(serviceBundle, webUi);
        }

        private static void ConfigureWebUiFactory(IServiceBundle serviceBundle, IWebUI webUi)
        {
            IWebUIFactory mockFactory = Substitute.For<IWebUIFactory>();
            mockFactory.CreateAuthenticationDialog(
                Arg.Any<CoreUIParent>(),
                Arg.Any<WebViewPreference>(),
                Arg.Any<RequestContext>()).Returns(webUi);

            serviceBundle.Config.WebUiFactoryCreator = () => mockFactory;
        }
       
    }
}
