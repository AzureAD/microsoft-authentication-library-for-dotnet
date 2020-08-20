// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;

namespace Microsoft.Identity.Client.Platforms.Features.WamBroker
{
    internal interface IWamProxy
    {
        Task<WebTokenRequestResult> GetTokenSilentlyAsync(WebAccount webAccount, WebTokenRequest webTokenRequest);
        Task<IReadOnlyList<WebAccount>> FindAllWebAccountsAsync(WebAccountProvider provider, string clientID);
        Task<WebTokenRequestResult> RequestTokenForWindowAsync(
            IntPtr _parentHandle, 
            WebTokenRequest webTokenRequest, 
            WebAccount wamAccount);
        Task<WebTokenRequestResult> RequestTokenForWindowAsync(IntPtr _parentHandle, WebTokenRequest webTokenRequest);
        Task<WebAccount> FindAccountAsync(WebAccountProvider provider, string wamAccountId);
    }
}
