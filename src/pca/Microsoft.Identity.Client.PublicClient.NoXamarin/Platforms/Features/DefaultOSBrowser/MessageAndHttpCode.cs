// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#if DESKTOP || NET_CORE || NETSTANDARD
using System;
using System.Net;

namespace Microsoft.Identity.Client.Platforms.netstandardcore.Desktop.OsBrowser
{
    internal class MessageAndHttpCode
    {
        public MessageAndHttpCode(HttpStatusCode httpCode, string message)
        {
            HttpCode = httpCode;
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        public HttpStatusCode HttpCode { get;  }
        public string Message { get; }
    }
}
#endif
