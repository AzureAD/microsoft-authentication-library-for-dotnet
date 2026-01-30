// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common;

namespace Microsoft.Identity.Test.Integration.Infrastructure
{
    public static class ClientAppTestExtensions
    {
        internal static PublicClientApplicationBuilder WithTestLogging(this PublicClientApplicationBuilder builder)
        {
            return builder
                .WithLogging((lvl, msg, pii) => { }, LogLevel.Verbose, enablePiiLogging: false, enableDefaultPlatformLogging: false);
        }

        internal static ConfidentialClientApplicationBuilder WithTestLogging(this ConfidentialClientApplicationBuilder builder)
        {
            return builder
                .WithLogging((lvl, msg, pii) => { }, LogLevel.Verbose, enablePiiLogging: false, enableDefaultPlatformLogging: false);
        }

        internal static PublicClientApplicationBuilder WithTestLogging(this PublicClientApplicationBuilder builder, out HttpSnifferClientFactory httpClientFactory)
        {
            return builder
                .WithLogging((lvl, msg, pii) => { }, LogLevel.Verbose, enablePiiLogging: false, enableDefaultPlatformLogging: false);

        }

        internal static ConfidentialClientApplicationBuilder WithTestLogging(this ConfidentialClientApplicationBuilder builder, out HttpSnifferClientFactory httpClientFactory)
        {
            return builder
                .WithLogging((lvl, msg, pii) => { }, LogLevel.Verbose, enablePiiLogging: false, enableDefaultPlatformLogging: false);
        }
    }
}
