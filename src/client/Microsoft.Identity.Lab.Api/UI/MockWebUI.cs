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

namespace Microsoft.Identity.Lab.Api.Mocks
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
                if (ExpectedEnvironment != authorizationUri.Host)
                {
                    throw new InvalidOperationException(
                        $"Expected environment '{ExpectedEnvironment}' but got '{authorizationUri.Host}'.");
                }
            }

            IDictionary<string, string> inputQp = CoreHelpers.ParseKeyValueList(authorizationUri.Query.Substring(1), '&', true, null);
            if (inputQp[OAuth2Parameter.State] == null)
            {
                throw new InvalidOperationException($"Expected '{OAuth2Parameter.State}' query parameter but it was null.");
            }

            if (AddStateInAuthorizationResult)
            {
                MockResult.State = inputQp[OAuth2Parameter.State];
            }

            //match QP passed in for validation.
            if (QueryParamsToValidate != null)
            {
                if (string.IsNullOrEmpty(authorizationUri.Query))
                {
                    throw new InvalidOperationException("Expected authorization URI to have a query string.");
                }

                foreach (var key in QueryParamsToValidate.Keys)
                {
                    if (!inputQp.ContainsKey(key))
                    {
                        throw new InvalidOperationException($"Expected query parameter '{key}' not found in authorization URI.");
                    }

                    if (QueryParamsToValidate[key] != inputQp[key])
                    {
                        throw new InvalidOperationException(
                            $"Query parameter '{key}' mismatch. Expected '{QueryParamsToValidate[key]}' but got '{inputQp[key]}'.");
                    }
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
