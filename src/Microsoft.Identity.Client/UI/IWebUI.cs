// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.UI
{
    internal interface IWebUI
    {
        Task<AuthorizationResult> AcquireAuthorizationAsync(
            Uri authorizationUri,
            Uri redirectUri,
            RequestContext requestContext,
            CancellationToken cancellationToken);

        /// <summary>
        /// Extra validations on the redirect uri, for example system web views cannot work with the urn:oob... uri because
        /// there is no way of knowing which app to get back to.
        /// WebUIs can update the uri, for example use http://localhost:1234 instead of http://localhost
        /// Throws if uri is invalid
        /// </summary>
        Uri UpdateRedirectUri(Uri redirectUri);
    }
}
