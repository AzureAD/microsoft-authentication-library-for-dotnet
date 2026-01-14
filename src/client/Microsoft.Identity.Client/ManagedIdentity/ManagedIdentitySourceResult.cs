// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.ManagedIdentity
{
    /// <summary>
    /// Result of managed identity source detection, including the detected source and any failure information from IMDS probes.
    /// </summary>
    /// <remarks>
    /// This class is returned by <see cref="ManagedIdentityApplication.GetManagedIdentitySourceAsync(bool, System.Threading.CancellationToken)"/> to provide
    /// detailed information about managed identity source detection, including failure reasons when IMDS probes fail.
    /// This information is useful for credential chains like DefaultAzureCredential to determine whether to skip
    /// managed identity authentication entirely.
    /// </remarks>
    public class ManagedIdentitySourceResult
    {
        /// <summary>
        /// Gets the detected managed identity source.
        /// </summary>
        /// <value>
        /// The <see cref="ManagedIdentitySource"/> that was detected on the environment.
        /// Returns <see cref="ManagedIdentitySource.None"/> if no managed identity source was detected.
        /// </value>
        public ManagedIdentitySource Source { get; }

        /// <summary>
        /// Gets or sets the failure reason from the IMDSv1 probe, if it failed.
        /// </summary>
        /// <value>
        /// A string describing why the IMDSv1 probe failed, or <c>null</c> if the probe succeeded or was not attempted.
        /// </value>
        public string ImdsV1FailureReason { get; set; }

        /// <summary>
        /// Gets or sets the failure reason from the IMDSv2 probe, if it failed.
        /// </summary>
        /// <value>
        /// A string describing why the IMDSv2 probe failed, or <c>null</c> if the probe succeeded or was not attempted.
        /// </value>
        public string ImdsV2FailureReason { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedIdentitySourceResult"/> class.
        /// </summary>
        /// <param name="source">The detected managed identity source.</param>
        public ManagedIdentitySourceResult(ManagedIdentitySource source)
        {
            Source = source;
        }
    }
}
