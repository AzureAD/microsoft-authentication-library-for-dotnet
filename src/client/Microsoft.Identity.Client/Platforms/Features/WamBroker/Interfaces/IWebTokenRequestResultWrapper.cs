// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Windows.Security.Authentication.Web.Core;

namespace Microsoft.Identity.Client.Platforms.Features.WamBroker
{
    internal interface IWebTokenRequestResultWrapper
    {
        IReadOnlyList<WebTokenResponse> ResponseData { get; }
        WebProviderError ResponseError { get; }
        WebTokenRequestStatus ResponseStatus { get; }
    }
}
