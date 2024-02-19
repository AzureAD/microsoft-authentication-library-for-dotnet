// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Platforms.Features.RuntimeSsoPolicy;
using Microsoft.Identity.Client.PlatformsCommon.Shared;

namespace Microsoft.Identity.Client.SsoPolicy
{
    /// <summary>
    /// SsoPolicy extension methods for SsoPolicy enforcement.
    /// </summary>
    public static class SsoPolicyExtension
    {
        /// <summary>
        /// Extends the <see cref="PublicClientApplicationBuilder"/> to include support for Single Sign-On (SSO) Policy enforcement.
        /// </summary>
        /// <param name="builder">The <see cref="PublicClientApplicationBuilder"/> instance to which SSO Policy support will be added.</param>
        /// <returns>A <see cref="PublicClientApplicationBuilder"/> with added support for SSO Policy.</returns>
        public static PublicClientApplicationBuilder WithSsoPolicy(this PublicClientApplicationBuilder builder)
        {
            AddRuntimeSupportForSsoPolicy(builder);
            return builder;
        }
        private static void AddRuntimeSupportForSsoPolicy(PublicClientApplicationBuilder builder)
        {
            builder.Config.SsoPolicyCreatorFunc =
                 (appConfig, logger) =>
                 {
                     return new RuntimeSsoPolicy(appConfig, logger);
                 };
        }
    }
}
