// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;

namespace Microsoft.Identity.Client.Platforms.Features.WamBroker
{
    /// <summary>
    /// Some Windows APIs do not exist in all versions of Windows, such as WebAuthenticationCoreManager.FindAllAccountsAsync
    /// Windows provides a mechanism to detect their presence via ApiInformation.IsMethodPresent, however, just **loading** a 
    /// class that has WebAuthenticationCoreManager.FindAllAccountsAsync in the code produces a MissingMethod exception.
    /// 
    /// This class groups all these legacy APIs and keeps them separated so as to avoid the problem - callers should ensure ApiInformation.IsMethodPresent 
    /// is called before.
    /// </summary>
    internal static class LegacyOsWamProxy
    {
        public static async Task<IReadOnlyList<WebAccount>> FindAllAccountsAsync(WebAccountProvider provider, string clientID, ICoreLogger logger)
        {
            FindAllAccountsResult findResult = await WebAuthenticationCoreManager.FindAllAccountsAsync(provider, clientID);

            // This is expected to happen with the MSA provider, which does not allow account listing
            if (findResult.Status != FindAllWebAccountsStatus.Success)
            {
                var error = findResult.ProviderError;
                logger.Info($"[WAM Proxy] WebAuthenticationCoreManager.FindAllAccountsAsync failed " +
                    $" with error code {error.ErrorCode} error message {error.ErrorMessage} and status {findResult.Status}");

                return Enumerable.Empty<WebAccount>().ToList();
            }

            logger.Info($"[WAM Proxy] FindAllWebAccountsAsync returning {findResult.Accounts.Count()} WAM accounts");
            return findResult.Accounts;
        }

        internal static void SetCorrelationId(WebTokenRequest request, string correlationId)
        {
            request.CorrelationId = correlationId;
        }
    }
}
