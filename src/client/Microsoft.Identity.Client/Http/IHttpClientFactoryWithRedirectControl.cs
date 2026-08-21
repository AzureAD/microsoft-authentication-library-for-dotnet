// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;

namespace Microsoft.Identity.Client.Http
{
    internal interface IHttpClientFactoryWithRedirectControl
    {
        HttpClient GetHttpClient(bool allowAutoRedirect);
    }
}
