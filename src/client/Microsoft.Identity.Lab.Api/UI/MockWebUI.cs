// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;

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
                ValidationHelpers.AssertAreEqual(ExpectedEnvironment, authorizationUri.Host);
            }

            IDictionary<string, string> inputQp = CoreHelpers.ParseKeyValueList(authorizationUri.Query.Substring(1), '&', true, null);
            ValidationHelpers.AssertIsNotNull(inputQp[OAuth2Parameter.State]);
            if (AddStateInAuthorizationResult)
            {
                MockResult.State = inputQp[OAuth2Parameter.State];
            }

            //match QP passed in for validation.
            if (QueryParamsToValidate != null)
            {
                ValidationHelpers.AssertIsNotNull(authorizationUri.Query);
                foreach (var key in QueryParamsToValidate.Keys)
                {
                    ValidationHelpers.AssertIsTrue(inputQp.ContainsKey(key));
                    ValidationHelpers.AssertAreEqual(QueryParamsToValidate[key], inputQp[key]);
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
