// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.AuthScheme.Bearer
{
    internal class BearerAuthenticationOperation : IAuthenticationOperation2
    {
        internal const string BearerTokenType = "bearer";

        public int TelemetryTokenType => TelemetryTokenTypeConstants.Bearer;

        public string AuthorizationHeaderPrefix => "Bearer";

        public string AccessTokenType => BearerTokenType;

        public string KeyId => null;

        public void FormatResult(AuthenticationResult authenticationResult)
        {
            // no-op
        }

        public Task FormatResultAsync(AuthenticationResult authenticationResult, CancellationToken cancellationToken = default)
        {
            // no-op, return completed task
            return Task.CompletedTask;
        }

        public IReadOnlyDictionary<string, string> GetTokenRequestParams()
        {
            // ESTS issues Bearer tokens by default, no need for any extra params
            return CollectionHelpers.GetEmptyDictionary<string, string>();
        }

        public Task<bool> ValidateCachedTokenAsync(MsalCacheValidationData cachedTokenData)
        {
            // no-op
            return Task.FromResult(true);
        }
    }
}
