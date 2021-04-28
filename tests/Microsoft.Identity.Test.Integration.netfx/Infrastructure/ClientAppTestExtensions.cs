// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common;

namespace Microsoft.Identity.Test.Integration.net45.Infrastructure
{
    public static class ClientAppTestExtensions
    {
        internal static PublicClientApplicationBuilder WithTestLogging(this PublicClientApplicationBuilder builder)
        {
            var httpClientFactory = new HttpSnifferClientFactory();

            return builder
                .WithLogging((lvl, msg, pii) => Trace.WriteLine($"[MSAL][{lvl}] {msg}"), LogLevel.Verbose, enablePiiLogging: true)
                .WithHttpClientFactory(httpClientFactory);

        }

        internal static ConfidentialClientApplicationBuilder WithTestLogging(this ConfidentialClientApplicationBuilder builder)
        {
            var httpClientFactory = new HttpSnifferClientFactory();

            return builder
                .WithLogging((lvl, msg, pii) => Trace.WriteLine($"[MSAL][{lvl}] {msg}"), LogLevel.Verbose, enablePiiLogging: true)
                .WithHttpClientFactory(httpClientFactory);            
        }

        internal static PublicClientApplicationBuilder WithTestLogging(this PublicClientApplicationBuilder builder, out HttpSnifferClientFactory factory)
        {
            factory = new HttpSnifferClientFactory();

            return builder
                .WithLogging((lvl, msg, pii) => Trace.WriteLine($"[MSAL][{lvl}] {msg}"), LogLevel.Verbose, enablePiiLogging: true)
                .WithHttpClientFactory(factory);

        }
    }
}
