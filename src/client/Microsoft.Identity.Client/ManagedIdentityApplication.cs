// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Executors;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.ManagedIdentity;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Class to be used for managed identity applications (on Azure resources like App Services, Virtual Machines, Azure Arc, Service Fabric and Cloud Shell).
    /// </summary>
    /// <remarks>
    /// Managed identity can be enabled on Azure resources as a system assigned managed identity or a user assigned managed identity.
    /// </remarks>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide managed identity flow on mobile
#endif
    public sealed class ManagedIdentityApplication
        : ApplicationBase,
            IManagedIdentityApplication
    {
        internal ManagedIdentityClient ManagedIdentityClient { get; }

        internal ManagedIdentityApplication(
            ApplicationConfiguration configuration)
            : base(configuration)
        {
            GuardMobileFrameworks();

            AppTokenCacheInternal = configuration.AppTokenCacheInternalForTest ?? new TokenCache(ServiceBundle, true);

            this.ServiceBundle.ApplicationLogger.Verbose(() => $"ManagedIdentityApplication {configuration.GetHashCode()} created");

            ManagedIdentityClient = new ManagedIdentityClient();
        }

        // Stores all app tokens
        internal ITokenCacheInternal AppTokenCacheInternal { get; }

        /// <inheritdoc/>
        public AcquireTokenForManagedIdentityParameterBuilder AcquireTokenForManagedIdentity(string resource)
        {
            if (string.IsNullOrEmpty(resource))
            {
                throw new ArgumentNullException(nameof(resource));
            }

            return AcquireTokenForManagedIdentityParameterBuilder.Create(
                ClientExecutorFactory.CreateManagedIdentityExecutor(this),
                resource);
        }

        /// <summary>
        /// Detects the managed identity source available on the host and the strongest mTLS
        /// binding the host can produce. Useful for credential chains (such as
        /// <c>DefaultAzureCredential</c>) to decide whether managed identity is available and
        /// what binding strength to expect.
        /// </summary>
        /// <remarks>
        /// On hosts capable of key binding, detecting the strongest available strength may provision
        /// (and persist) a binding key as a side effect, pre-warming the cache reused by a subsequent
        /// token request. The key provider is created once per process and its key is cached.
        /// </remarks>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the detection to complete.</param>
        /// <returns>A <see cref="ManagedIdentityCapabilities"/> describing the detected source and host capabilities.</returns>
        public Task<ManagedIdentityCapabilities> GetManagedIdentityCapabilitiesAsync(CancellationToken cancellationToken)
        {
            return GetManagedIdentityCapabilitiesCoreAsync(timeout: null, cancellationToken);
        }

        /// <summary>
        /// Detects the managed identity source available on the host and the strongest mTLS
        /// binding the host can produce, using the supplied discovery options.
        /// </summary>
        /// <param name="options">Options that control managed identity capability discovery.</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the detection to complete.</param>
        /// <returns>A <see cref="ManagedIdentityCapabilities"/> describing the detected source and host capabilities.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <see cref="ManagedIdentityCapabilitiesOptions.ImdsProbeTimeout"/> is not positive
        /// or exceeds the maximum timeout supported across MSAL target frameworks.
        /// </exception>
        /// <exception cref="MsalServiceException">
        /// Thrown with error code <see cref="MsalError.RequestTimeout"/> when capability discovery
        /// exceeds <see cref="ManagedIdentityCapabilitiesOptions.ImdsProbeTimeout"/>.
        /// </exception>
        public Task<ManagedIdentityCapabilities> GetManagedIdentityCapabilitiesAsync(
            ManagedIdentityCapabilitiesOptions options,
            CancellationToken cancellationToken)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            TimeSpan? timeout = options.ImdsProbeTimeout;
            if (timeout.HasValue &&
                (timeout.Value <= TimeSpan.Zero ||
                 timeout.Value > TimeSpan.FromMilliseconds(int.MaxValue)))
            {
                throw new ArgumentOutOfRangeException(nameof(options.ImdsProbeTimeout));
            }

            return GetManagedIdentityCapabilitiesCoreAsync(timeout, cancellationToken);
        }

        private async Task<ManagedIdentityCapabilities> GetManagedIdentityCapabilitiesCoreAsync(
            TimeSpan? timeout,
            CancellationToken callerCancellationToken)
        {
            CancellationTokenSource effectiveTokenSource = null;
            CancellationToken effectiveToken = callerCancellationToken;

            try
            {
                if (timeout.HasValue)
                {
                    effectiveTokenSource = CancellationTokenSource.CreateLinkedTokenSource(callerCancellationToken);
                    effectiveTokenSource.CancelAfter(timeout.Value);
                    effectiveToken = effectiveTokenSource.Token;
                }

                // Create a temporary RequestContext for the logger and the IMDS probe request.
                var requestContext = new RequestContext(this.ServiceBundle, Guid.NewGuid(), null, effectiveToken);

                ManagedIdentityDiscoveryResult discoveryResult = await ManagedIdentityClient
                    .GetManagedIdentityCapabilitiesAsync(requestContext, effectiveToken)
                    .ConfigureAwait(false);

                return new ManagedIdentityCapabilities(
                    discoveryResult.Source,
                    discoveryResult.MaxSupportedBindingStrength,
                    discoveryResult.GetCombinedErrorReason());
            }
            catch (OperationCanceledException exception) when (
                timeout.HasValue &&
                !callerCancellationToken.IsCancellationRequested &&
                effectiveTokenSource?.IsCancellationRequested == true)
            {
                throw new MsalServiceException(
                    MsalError.RequestTimeout,
                    MsalErrorMessage.RequestTimeOut,
                    exception);
            }
            finally
            {
                effectiveTokenSource?.Dispose();
            }
        }

        /// <summary>
        /// Detects and returns the managed identity source available on the environment.
        /// </summary>
        /// <returns>Managed identity source detected on the environment if any.</returns>
        [Obsolete("Use GetManagedIdentityCapabilitiesAsync() instead. \"ManagedIdentityApplication mi = miBuilder.Build() as ManagedIdentityApplication;\"")]
        public static ManagedIdentitySource GetManagedIdentitySource()
        {
            var source = ManagedIdentityClient.GetManagedIdentitySourceNoImds();

            return source == ManagedIdentitySource.None
#pragma warning disable CS0618
                // ManagedIdentitySource.DefaultToImds is marked obsolete, but is intentionally used here as a sentinel value to support legacy detection logic.
                // This value signals that none of the environment-based managed identity sources were detected.
                ? ManagedIdentitySource.DefaultToImds
#pragma warning restore CS0618
                : source;

        }
    }
}
