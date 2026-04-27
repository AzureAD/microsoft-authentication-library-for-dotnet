// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;

namespace Microsoft.Identity.Client.Platforms.Shared.Desktop.OsBrowser
{
    internal class MessageAndHttpCode(HttpStatusCode httpCode, string message)
    {
        public HttpStatusCode HttpCode { get; } = httpCode;
        public string Message { get; } = message ?? throw new ArgumentNullException(nameof(message));
    }
}
