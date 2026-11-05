// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Platforms.Android.SystemWebview
{
    internal sealed class SystemWebViewRequest
    {
        private readonly Uri _redirectUri;
        private readonly string _state;

        internal SystemWebViewRequest(Uri authorizationUri, Uri redirectUri, RequestContext requestContext)
        {
            AuthorizationUri = authorizationUri ?? throw new ArgumentNullException(nameof(authorizationUri));
            _redirectUri = redirectUri ?? throw new ArgumentNullException(nameof(redirectUri));
            RequestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));

            var parameters = CoreHelpers.ParseKeyValueList(authorizationUri.Query.TrimStart('?'), '&', true, null);
            if (!parameters.TryGetValue(OAuth2Parameter.State, out _state) || string.IsNullOrEmpty(_state))
            {
                throw new ArgumentException("The authorization URI must contain state.", nameof(authorizationUri));
            }
        }

        internal Uri AuthorizationUri { get; }
        internal RequestContext RequestContext { get; }

        internal bool IsValidRedirect(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri redirect) ||
                !string.Equals(redirect.Scheme, _redirectUri.Scheme, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(redirect.Host, _redirectUri.Host, StringComparison.OrdinalIgnoreCase) ||
                redirect.Port != _redirectUri.Port ||
                !string.Equals(redirect.UserInfo, _redirectUri.UserInfo, StringComparison.Ordinal) ||
                !string.Equals(redirect.AbsolutePath, _redirectUri.AbsolutePath, StringComparison.Ordinal) ||
                !string.IsNullOrEmpty(redirect.Fragment))
            {
                return false;
            }

            // Validate errors as well as successful responses before they can complete the pending operation.
            var parameters = CoreHelpers.ParseKeyValueList(redirect.Query.TrimStart('?'), '&', true, null);
            if (!parameters.TryGetValue(OAuth2Parameter.State, out string state) ||
                !string.Equals(state, _state, StringComparison.Ordinal))
            {
                return false;
            }

            var expectedParameters = CoreHelpers.ParseKeyValueList(_redirectUri.Query.TrimStart('?'), '&', true, null);
            foreach (var parameter in expectedParameters)
            {
                if (!parameters.TryGetValue(parameter.Key, out string value) ||
                    !string.Equals(value, parameter.Value, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
