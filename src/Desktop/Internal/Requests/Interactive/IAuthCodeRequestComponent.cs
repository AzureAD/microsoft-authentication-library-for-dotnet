// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Internal
{
    internal interface IAuthCodeRequestComponent
    {
#if MSAL_DESKTOP || MSAL_XAMARIN
        Task<Tuple<string, string>> FetchAuthCodeAndPkceVerifierAsync(CancellationToken cancellationToken);
#endif

#if MSAL_CONFIDENTIAL
        Uri GetAuthorizationUriWithoutPkce();
#endif
    }
}
