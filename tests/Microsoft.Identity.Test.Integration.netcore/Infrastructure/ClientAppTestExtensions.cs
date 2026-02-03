// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common;

namespace Microsoft.Identity.Test.Integration.Infrastructure
{
    public static class ClientAppTestExtensions
    {
        private const string EnableLoggingEnvVar = "MSAL_TEST_LOGGING";
        private const string EnablePiiEnvVar = "MSAL_TEST_PII_LOGGING";

        private static bool IsEnabled(string envVar)
        {
            return Environment.GetEnvironmentVariable(envVar) != null;
        }

        private static void LogCallback(LogLevel level, string message, bool containsPii)
        {
            if (!IsEnabled(EnableLoggingEnvVar))
            {
                return; //no output unless explicitly enabled
            }

            if (containsPii && !IsEnabled(EnablePiiEnvVar))
            {
                return; //never leak PII by default
            }

            Trace.WriteLine($"[MSAL][{level}] {message}");
        }

        internal static PublicClientApplicationBuilder WithTestLogging(
            this PublicClientApplicationBuilder builder)
        {
            var httpClientFactory = new HttpSnifferClientFactory();

            return builder
                .WithLogging(
                    LogCallback,
                    IsEnabled(EnableLoggingEnvVar) ? LogLevel.Verbose : LogLevel.Error,
                    enablePiiLogging: IsEnabled(EnablePiiEnvVar),
                    enableDefaultPlatformLogging: false)
                .WithHttpClientFactory(httpClientFactory);
        }

        internal static ConfidentialClientApplicationBuilder WithTestLogging(
            this ConfidentialClientApplicationBuilder builder)
        {
            var httpClientFactory = new HttpSnifferClientFactory();

            return builder
                .WithLogging(
                    LogCallback,
                    IsEnabled(EnableLoggingEnvVar) ? LogLevel.Verbose : LogLevel.Error,
                    enablePiiLogging: IsEnabled(EnablePiiEnvVar),
                    enableDefaultPlatformLogging: false)
                .WithHttpClientFactory(httpClientFactory);
        }

        internal static PublicClientApplicationBuilder WithTestLogging(
            this PublicClientApplicationBuilder builder,
            out HttpSnifferClientFactory httpClientFactory)
        {
            httpClientFactory = new HttpSnifferClientFactory(); //required

            return builder
                .WithLogging(
                    LogCallback,
                    IsEnabled(EnableLoggingEnvVar) ? LogLevel.Verbose : LogLevel.Error,
                    enablePiiLogging: IsEnabled(EnablePiiEnvVar),
                    enableDefaultPlatformLogging: false)
                .WithHttpClientFactory(httpClientFactory);
        }

        internal static ConfidentialClientApplicationBuilder WithTestLogging(
            this ConfidentialClientApplicationBuilder builder,
            out HttpSnifferClientFactory httpClientFactory)
        {
            httpClientFactory = new HttpSnifferClientFactory(); //required

            return builder
                .WithLogging(
                    LogCallback,
                    IsEnabled(EnableLoggingEnvVar) ? LogLevel.Verbose : LogLevel.Error,
                    enablePiiLogging: IsEnabled(EnablePiiEnvVar),
                    enableDefaultPlatformLogging: false)
                .WithHttpClientFactory(httpClientFactory);
        }
    }
}
