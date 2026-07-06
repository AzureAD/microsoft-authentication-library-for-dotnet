// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Extensibility
{
    /// <summary>
    /// Extensibility methods for <see cref="ManagedIdentityApplicationBuilder"/>.
    /// </summary>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide managed identity flow on mobile
#endif
    public static class ManagedIdentityApplicationBuilderExtensions
    {
        /// <summary>
        /// Configures an async callback invoked when a fire-and-forget background (proactive) token refresh completes.
        /// Proactive refresh runs on a background thread after the caller has already received a valid cached token,
        /// so its outcome (latency, failures) is otherwise unobservable. This callback surfaces it, e.g. for telemetry.
        /// </summary>
        /// <param name="builder">The managed identity application builder.</param>
        /// <param name="onBackgroundTokenRefreshCompleted">
        /// An async callback that receives the <see cref="ExecutionResult"/> of the background refresh. On success,
        /// <see cref="ExecutionResult.Result"/> holds the refreshed token; on failure, <see cref="ExecutionResult.Exception"/>
        /// holds the exception, whose <see cref="MsalException.AuthenticationResultMetadata"/> carries the failed
        /// attempt's HTTP duration.
        /// </param>
        /// <returns>The builder to chain additional configuration calls.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="onBackgroundTokenRefreshCompleted"/> is null.</exception>
        /// <remarks>
        /// <para>Invoked on a background thread. Check <see cref="ExecutionResult.Successful"/> to determine the outcome.</para>
        /// <para>Exceptions thrown by this callback are caught and logged internally so they cannot disrupt the refresh.</para>
        /// </remarks>
        public static ManagedIdentityApplicationBuilder OnBackgroundTokenRefreshCompleted(
            this ManagedIdentityApplicationBuilder builder,
            Func<ExecutionResult, Task> onBackgroundTokenRefreshCompleted)
        {
            builder.ValidateUseOfExperimentalFeature();

            if (onBackgroundTokenRefreshCompleted == null)
            {
                throw new ArgumentNullException(nameof(onBackgroundTokenRefreshCompleted));
            }

            builder.Config.OnBackgroundTokenRefreshCompleted = onBackgroundTokenRefreshCompleted;
            return builder;
        }
    }
}
