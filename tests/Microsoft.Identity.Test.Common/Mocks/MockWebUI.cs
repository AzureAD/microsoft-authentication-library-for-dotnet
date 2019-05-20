// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Common.Mocks
{
    internal class MockWebUI : IWebUI
    {
        public MockWebUI()
        {
            AddStateInAuthorizationResult = true;
        }

        public Exception ExceptionToThrow { get; set; }

        public AuthorizationResult MockResult { get; set; }

        public IDictionary<string, string> QueryParamsToValidate { get; set; }

        public bool AddStateInAuthorizationResult { get; set; }

        public string ExpectedEnvironment { get; set; }

        public Uri ActualAuthorizationUri { get; private set; }

        public async Task<AuthorizationResult> AcquireAuthorizationAsync(
            Uri authorizationUri,
            Uri redirectUri,
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            ActualAuthorizationUri = authorizationUri;

            if (ExceptionToThrow != null)
            {
                throw ExceptionToThrow;
            }

            if (ExpectedEnvironment != null)
            {
                Assert.AreEqual(ExpectedEnvironment, authorizationUri.Host);
            }

            IDictionary<string, string> inputQp = CoreHelpers.ParseKeyValueList(authorizationUri.Query.Substring(1), '&', true, null);
            Assert.IsNotNull(inputQp[OAuth2Parameter.State]);
            if (AddStateInAuthorizationResult)
            {
                MockResult.State = inputQp[OAuth2Parameter.State];
            }

            //match QP passed in for validation.
            if (QueryParamsToValidate != null)
            {
                Assert.IsNotNull(authorizationUri.Query);
                foreach (var key in QueryParamsToValidate.Keys)
                {
                    Assert.IsTrue(inputQp.ContainsKey(key));
                    Assert.AreEqual(QueryParamsToValidate[key], inputQp[key]);
                }
            }

            return await Task.Factory.StartNew(() => MockResult).ConfigureAwait(false);
        }

        public Uri UpdateRedirectUri(Uri redirectUri)
        {
            RedirectUriHelper.Validate(redirectUri);
            return redirectUri;
        }
    }
}
