// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Internal.ClientCredential;

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
        /// Configures an async callback to provide the client credential certificate dynamically.
        /// The callback is invoked before each token acquisition request to the identity provider (including retries).
        /// This enables scenarios such as certificate rotation and dynamic certificate selection based on application context.
        /// </summary>
        /// <param name="builder">The confidential client application builder.</param>
        /// <param name="certificateProvider">
        /// An async callback that provides the certificate based on the application configuration.
        /// Called before each network request to acquire a token.
        /// Must return a valid <see cref="X509Certificate2"/> with a private key.</param>
        /// <param name="certificateOptions">Configuration options for the certificate handling.</param>
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
        /// <para>The callback can perform async operations such as fetching certificates from Azure Key Vault or other secret management systems.</para>
        /// <para>See https://aka.ms/msal-net-client-credentials for more details on client credentials.</para>
        /// </remarks>
        public static ConfidentialClientApplicationBuilder WithCertificate(
            this ConfidentialClientApplicationBuilder builder,
            Func<AssertionRequestOptions, Task<X509Certificate2>> certificateProvider, 
            CertificateOptions certificateOptions)
        {
            builder.ValidateUseOfExperimentalFeature();

            if (certificateProvider == null)
            {
                throw new ArgumentNullException(nameof(certificateProvider));
            }
            
            // Create a DynamicCertificateClientCredential with the certificate provider
            // The certificate will be resolved dynamically via the provider in ResolveCertificateAsync
            builder.Config.ClientCredential = new DynamicCertificateClientCredential(
                certificateProvider: certificateProvider);

            builder.Config.SendX5C = certificateOptions?.SendX5C ?? false;

            return builder;
        }

        /// <summary>
        /// Configures an async callback that is invoked when MSAL receives an error response from the identity provider (Security Token Service).
        /// The callback determines whether MSAL should retry the token request or propagate the exception.
        /// This callback is invoked after each service failure and can be called multiple times until it returns <c>false</c> or the request succeeds.
        /// </summary>
        /// <param name="builder">The confidential client application builder.</param>
        /// <param name="onMsalServiceFailure">
        /// An async callback that determines whether to retry after a service failure.
        /// Receives the assertion request options and the <see cref="MsalServiceException"/> that occurred.
        /// Returns <c>true</c> to retry the request, or <c>false</c> to stop retrying and propagate the exception.
        /// The callback will be invoked repeatedly after each service failure until it returns <c>false</c> or the request succeeds.
        /// </param>
        /// <returns>The builder to chain additional configuration calls.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="onMsalServiceFailure"/> is null.</exception>
        /// <remarks>
        /// <para>This callback is ONLY triggered for <see cref="MsalServiceException"/> - errors returned by the identity provider (e.g., HTTP 500, 503, throttling).</para>
        /// <para>This callback is NOT triggered for client-side errors (<see cref="MsalClientException"/>) or network failures handled internally by MSAL.</para>
        /// <para>This callback is only invoked for network token acquisition attempts, not when tokens are retrieved from cache.</para>
        /// <para>When the callback returns <c>true</c>, MSAL will invoke the certificate provider (if configured via <see cref="WithCertificate"/>)
        /// before making another token request, enabling certificate rotation scenarios.</para>
        /// <para>MSAL's internal throttling and retry mechanisms will still apply, including respecting Retry-After headers from the identity provider.</para>
        /// <para>To prevent infinite loops, ensure your callback has appropriate termination conditions (e.g., max retry count, timeout).</para>
        /// <para>The callback can perform async operations such as logging to remote services, checking external health endpoints, or querying configuration stores.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// int retryCount = 0;
        /// var app = ConfidentialClientApplicationBuilder
        ///     .Create(clientId)
        ///     .WithCertificate(async options => await GetCertificateFromKeyVaultAsync(options.TokenEndpoint))
        ///     .OnMsalServiceFailure(async (options, serviceException) =>
        ///     {
        ///         retryCount++;
        ///         await LogExceptionAsync(serviceException);
        ///         
        ///         // Retry up to 3 times for transient service errors (5xx)
        ///         return serviceException.StatusCode >= 500 &amp;&amp; retryCount &lt; 3;
        ///     })
        ///     .Build();
        /// </code>
        /// </example>
        public static ConfidentialClientApplicationBuilder OnMsalServiceFailure(
            this ConfidentialClientApplicationBuilder builder,
            Func<AssertionRequestOptions, ExecutionResult, Task<bool>> onMsalServiceFailure)
        {
            builder.ValidateUseOfExperimentalFeature();

            if (onMsalServiceFailure == null)
                throw new ArgumentNullException(nameof(onMsalServiceFailure));

            builder.Config.OnMsalServiceFailure = onMsalServiceFailure;
            return builder;
        }

        /// <summary>
        /// Configures an async callback that is invoked when a token acquisition request completes.
        /// This callback is invoked once per <c>AcquireTokenForClient</c> call, after all retry attempts have been exhausted.
        /// While named <c>OnCompletion</c> for the common case, this callback fires for both successful and failed acquisitions.
        /// This enables scenarios such as telemetry, logging, and custom result handling.
        /// </summary>
        /// <param name="builder">The confidential client application builder.</param>
        /// <param name="onCompletion">
        /// An async callback that receives the assertion request options and the execution result.
        /// The result contains either the successful <see cref="AuthenticationResult"/> or the <see cref="MsalException"/> that occurred.
        /// This callback is invoked after all retries have been exhausted (if an <see cref="OnMsalServiceFailure"/> handler is configured).
        /// </param>
        /// <returns>The builder to chain additional configuration calls.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="onCompletion"/> is null.</exception>
        /// <remarks>
        /// <para>This callback is invoked for both successful and failed token acquisitions. Check <see cref="ExecutionResult.Successful"/> to determine the outcome.</para>
        /// <para>This callback is only invoked for network token acquisition attempts, not when tokens are retrieved from cache.</para>
        /// <para>If multiple calls to <c>OnCompletion</c> are made, only the last configured callback will be used.</para>
        /// <para>Exceptions thrown by this callback will be caught and logged internally to prevent disruption of the authentication flow.</para>
        /// <para>The callback is invoked on the same thread/context as the token acquisition request.</para>
        /// <para>The callback can perform async operations such as sending telemetry to Application Insights, persisting logs to databases, or triggering webhooks.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var app = ConfidentialClientApplicationBuilder
        ///     .Create(clientId)
        ///     .WithCertificate(certificate)
        ///     .OnCompletion(async (options, result) =>
        ///     {
        ///         if (result.Successful)
        ///         {
        ///             await telemetry.TrackEventAsync("TokenAcquired", new { ClientId = options.ClientID });
        ///         }
        ///         else
        ///         {
        ///             await telemetry.TrackExceptionAsync(result.Exception);
        ///         }
        ///     })
        ///     .Build();
        /// </code>
        /// </example>
        public static ConfidentialClientApplicationBuilder OnCompletion(
            this ConfidentialClientApplicationBuilder builder,
            Func<AssertionRequestOptions, ExecutionResult, Task> onCompletion)
        {
            builder.ValidateUseOfExperimentalFeature();

            if (onCompletion == null)
            {
                throw new ArgumentNullException(nameof(onCompletion));
            }

            builder.Config.OnCompletion = onCompletion;
            return builder;
        }
    }
}
