// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Extensibility
{
    /// <summary>
    /// Extensibility methods for <see cref="ConfidentialClientApplicationBuilder"/>
    /// </summary>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide confidential client on mobile
#endif
    public static class ConfidentialClientApplicationBuilderExtensions
    {
        /// <summary>
        /// Allows setting a callback which returns an access token, based on the passed-in parameters.
        /// MSAL will pass in its authentication parameters to the callback and it is expected that the callback
        /// will construct a <see cref="AppTokenProviderResult"/> and return it to MSAL.
        /// MSAL will cache the token response the same way it does for other authentication results.
        /// </summary>
        /// <remarks>This is part of an extensibility mechanism designed to be used only by Azure SDK in order to
        /// enhance managed identity support. Only client_credential flow is supported.</remarks>
        public static ConfidentialClientApplicationBuilder WithAppTokenProvider(
            this ConfidentialClientApplicationBuilder builder,
            Func<AppTokenProviderParameters, Task<AppTokenProviderResult>> appTokenProvider)
        {
            builder.Config.AppTokenProvider = appTokenProvider ?? throw new ArgumentNullException(nameof(appTokenProvider));
            return builder;
        }

        /// <summary>
        /// Configures a callback to provide the client credential certificate dynamically.
        /// The callback is invoked before each token acquisition request to the identity provider (including retries).
        /// This enables scenarios such as certificate rotation and dynamic certificate selection based on application context.
        /// </summary>
        /// <param name="builder">The confidential client application builder.</param>
        /// <param name="certificateProvider">
        /// A callback that provides the certificate based on the application configuration.
        /// Called before each network request to acquire a token.
        /// Must return a valid <see cref="X509Certificate2"/> with a private key.
        /// </param>
        /// <returns>The builder to chain additional configuration calls.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="certificateProvider"/> is null.</exception>
        /// <exception cref="MsalClientException">
        /// Thrown at build time if both <see cref="ConfidentialClientApplicationBuilder.WithCertificate(X509Certificate2)"/> 
        /// and this method are configured.
        /// </exception>
        /// <remarks>
        /// <para>This method cannot be used together with <see cref="ConfidentialClientApplicationBuilder.WithCertificate(X509Certificate2)"/>.</para>
        /// <para>The callback is not invoked when tokens are retrieved from cache, only for network calls.</para>
        /// <para>The certificate returned by the callback will be used to sign the client assertion (JWT) for that token request.</para>
        /// <para>See https://aka.ms/msal-net-client-credentials for more details on client credentials.</para>
        /// </remarks>
        public static ConfidentialClientApplicationBuilder WithCertificate(
            this ConfidentialClientApplicationBuilder builder,
            Func<IAppConfig, X509Certificate2> certificateProvider)
        {
            if (certificateProvider == null)
            {
                throw new ArgumentNullException(nameof(certificateProvider));
            }
                
            builder.Config.ClientCredentialCertificateProvider = certificateProvider;
            return builder;
        }

        /// <summary>
        /// Configures a retry policy for token acquisition failures.
        /// The policy is invoked after each failed token request to determine whether a retry should be attempted.
        /// MSAL will respect throttling hints from the identity provider and apply appropriate delays between retries.
        /// </summary>
        /// <param name="builder">The confidential client application builder.</param>
        /// <param name="retryPolicy">
        /// A callback that determines whether to retry after a failure.
        /// Receives the application configuration and the exception that occurred.
        /// Returns <c>true</c> to retry the request, or <c>false</c> to stop retrying and throw the exception.
        /// The callback will be invoked repeatedly after each failure until it returns <c>false</c>.
        /// </param>
        /// <returns>The builder to chain additional configuration calls.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="retryPolicy"/> is null.</exception>
        /// <remarks>
        /// <para>The retry policy is only invoked for network failures, not for cached token retrievals.</para>
        /// <para>When the policy returns <c>true</c>, MSAL will invoke the certificate provider callback again (if configured)
        /// before making another token request, enabling certificate rotation scenarios.</para>
        /// <para>MSAL's internal throttling and retry mechanisms will still apply, including respecting Retry-After headers.</para>
        /// <para>To prevent infinite loops, ensure your retry policy has appropriate termination conditions.</para>
        /// </remarks>
        public static ConfidentialClientApplicationBuilder WithRetry(
            this ConfidentialClientApplicationBuilder builder,
            Func<IAppConfig, MsalException, bool> retryPolicy)
        {
            if (retryPolicy == null)
                throw new ArgumentNullException(nameof(retryPolicy));

            builder.Config.RetryPolicy = retryPolicy;
            return builder;
        }

        /// <summary>
        /// Configures an observer callback that receives the final result of token acquisition.
        /// The observer is invoked once at the completion of <c>ExecuteAsync</c>, with either a success or failure result.
        /// This enables scenarios such as telemetry, logging, and custom error handling.
        /// </summary>
        /// <param name="builder">The confidential client application builder.</param>
        /// <param name="observer">
        /// A callback that receives the application configuration and the execution result.
        /// The result contains either the successful <see cref="AuthenticationResult"/> or the <see cref="MsalException"/> that occurred.
        /// This callback is invoked after all retries have been exhausted (if retry policy is configured).
        /// </param>
        /// <returns>The builder to chain additional configuration calls.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="observer"/> is null.</exception>
        /// <remarks>
        /// <para>The observer is only invoked for network token acquisition attempts, not for cached token retrievals.</para>
        /// <para>If multiple calls to <c>WithObserver</c> are made, only the last configured observer will be used.</para>
        /// <para>Exceptions thrown by the observer callback will be caught and logged internally to prevent disruption of the authentication flow.</para>
        /// <para>The observer is called on the same thread as the token acquisition request.</para>
        /// </remarks>
        public static ConfidentialClientApplicationBuilder WithObserver(
            this ConfidentialClientApplicationBuilder builder,
            Action<IAppConfig, ExecutionResult> observer)
        {
            if (observer == null)
                throw new ArgumentNullException(nameof(observer));

            builder.Config.ExecutionObserver = observer;
            return builder;
        }
    }
}
