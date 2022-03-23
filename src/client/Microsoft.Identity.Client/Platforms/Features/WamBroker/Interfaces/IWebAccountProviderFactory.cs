// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Threading.Tasks;
using Windows.Security.Credentials;

namespace Microsoft.Identity.Client.Platforms.Features.WamBroker
{
    internal interface IWebAccountProviderFactory
    {
        Task<WebAccountProvider> GetAccountProviderAsync(string authorityOrTenant);
        Task<WebAccountProvider> GetDefaultProviderAsync();
        bool IsConsumerProvider(WebAccountProvider webAccountProvider);
        Task<bool> IsDefaultAccountMsaAsync();
        bool IsOrganizationsProvider(WebAccountProvider webAccountProvider);
    }
}
