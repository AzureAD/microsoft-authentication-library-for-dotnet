// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.TestOnly
{
    /// <summary>
    /// Extension methods that add <see cref="MockHttpManager"/> support to MSAL application builders.
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Configures the <see cref="ManagedIdentityApplicationBuilder"/> to use the supplied
        /// <see cref="MockHttpManager"/> for all outgoing HTTP requests.
        /// </summary>
        /// <param name="builder">The builder to configure.</param>
        /// <param name="httpManager">The mock HTTP manager to inject.</param>
        /// <returns>The same <paramref name="builder"/> for fluent chaining.</returns>
        /// <example>
        /// <code>
        /// using var httpManager = new MockHttpManager();
        /// httpManager.AddManagedIdentityMtlsTokenMocks();
        ///
        /// var app = ManagedIdentityApplicationBuilder
        ///     .Create(ManagedIdentityId.SystemAssigned)
        ///     .WithHttpManager(httpManager)
        ///     .Build();
        /// </code>
        /// </example>
        public static ManagedIdentityApplicationBuilder WithHttpManager(
            this ManagedIdentityApplicationBuilder builder,
            MockHttpManager httpManager)
        {
            return builder.WithHttpManager((Client.Http.IHttpManager)httpManager);
        }

        /// <summary>
        /// Configures a <see cref="ConfidentialClientApplicationBuilder"/> to use the supplied
        /// <see cref="MockHttpManager"/> for all outgoing HTTP requests.
        /// </summary>
        /// <param name="builder">The builder to configure.</param>
        /// <param name="httpManager">The mock HTTP manager to inject.</param>
        /// <returns>The same <paramref name="builder"/> for fluent chaining.</returns>
        public static ConfidentialClientApplicationBuilder WithHttpManager(
            this ConfidentialClientApplicationBuilder builder,
            MockHttpManager httpManager)
        {
            return builder.WithHttpManager((Client.Http.IHttpManager)httpManager);
        }

        /// <summary>
        /// Configures a <see cref="PublicClientApplicationBuilder"/> to use the supplied
        /// <see cref="MockHttpManager"/> for all outgoing HTTP requests.
        /// </summary>
        /// <param name="builder">The builder to configure.</param>
        /// <param name="httpManager">The mock HTTP manager to inject.</param>
        /// <returns>The same <paramref name="builder"/> for fluent chaining.</returns>
        public static PublicClientApplicationBuilder WithHttpManager(
            this PublicClientApplicationBuilder builder,
            MockHttpManager httpManager)
        {
            return builder.WithHttpManager((Client.Http.IHttpManager)httpManager);
        }
    }
}
