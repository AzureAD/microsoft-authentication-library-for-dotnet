// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.Platforms.Shared.Desktop.OsBrowser
{
    /// <summary>
    /// Result from intercepting an authorization response
    /// </summary>
    internal class AuthorizationResponse
    {
        public AuthorizationResponse(Uri requestUri, byte[] postData)
        {
            RequestUri = requestUri;
            PostData = postData;
        }

        public Uri RequestUri { get; set; }
        public byte[] PostData { get; set; }
        public bool IsFormPost => PostData != null && PostData.Length > 0;
    }
}
