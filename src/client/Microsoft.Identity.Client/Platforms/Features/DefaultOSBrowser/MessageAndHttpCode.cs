// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Platforms.Shared.Desktop.OsBrowser
{
    internal class MessageAndHttpCode
    {
        public MessageAndHttpCode(HttpStatusCode httpCode, string message)
        {
            HttpCode = httpCode;
            Message = Guard.AgainstNull(message);
        }

        public HttpStatusCode HttpCode { get;  }
        public string Message { get; }
    }
}
