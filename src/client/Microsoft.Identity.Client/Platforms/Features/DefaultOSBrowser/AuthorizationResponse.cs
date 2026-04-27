// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.Platforms.Shared.Desktop.OsBrowser
{
    /// <summary>
    /// Result from intercepting an authorization response
    /// </summary>
    internal class AuthorizationResponse(Uri requestUri, byte[] postData)
    {
        public Uri RequestUri { get; set; } = requestUri;
        public byte[] PostData { get; set; } = postData;
        public bool IsFormPost => PostData != null && PostData.Length > 0;
    }
}
