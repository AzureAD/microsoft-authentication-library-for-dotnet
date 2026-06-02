// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.ManagedIdentity.V2;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    /// <summary>
    /// Internal result of managed identity source discovery. Carries the public-facing source
    /// plus internal-only routing details (such as the detected IMDS protocol version) that are
    /// not exposed on the public <see cref="ManagedIdentityCapabilities"/> surface.
    /// </summary>
    internal sealed class ManagedIdentityDiscoveryResult
    {
        /// <summary>
        /// The detected managed identity source. The IMDS v1/v2 distinction is folded into
        /// <see cref="ManagedIdentitySource.Imds"/>; the version is carried separately in
        /// <see cref="DetectedImdsVersion"/> for internal routing.
        /// </summary>
        public ManagedIdentitySource Source { get; }

        /// <summary>
        /// The IMDS protocol version detected, when <see cref="Source"/> is
        /// <see cref="ManagedIdentitySource.Imds"/>; otherwise <c>null</c>. Internal routing only.
        /// </summary>
        public ImdsVersion? DetectedImdsVersion { get; }

        /// <summary>
        /// The highest binding strength the host can produce.
        /// </summary>
        public MtlsBindingStrength MaxSupportedBindingStrength { get; }

        /// <summary>
        /// Failure reason from the IMDSv1 probe, if it failed; otherwise <c>null</c>.
        /// </summary>
        public string ImdsV1FailureReason { get; }

        /// <summary>
        /// Failure reason from the IMDSv2 probe, if it failed; otherwise <c>null</c>.
        /// </summary>
        public string ImdsV2FailureReason { get; }

        public ManagedIdentityDiscoveryResult(
            ManagedIdentitySource source,
            ImdsVersion? detectedImdsVersion = null,
            MtlsBindingStrength maxSupportedBindingStrength = MtlsBindingStrength.Bearer,
            string imdsV1FailureReason = null,
            string imdsV2FailureReason = null)
        {
            Source = source;
            DetectedImdsVersion = detectedImdsVersion;
            MaxSupportedBindingStrength = maxSupportedBindingStrength;
            ImdsV1FailureReason = imdsV1FailureReason;
            ImdsV2FailureReason = imdsV2FailureReason;
        }

        /// <summary>
        /// Builds a single, combined error reason from the IMDS probe failures, or <c>null</c>
        /// when neither probe reported a failure.
        /// </summary>
        public string GetCombinedErrorReason()
        {
            bool hasV1 = !string.IsNullOrEmpty(ImdsV1FailureReason);
            bool hasV2 = !string.IsNullOrEmpty(ImdsV2FailureReason);

            if (!hasV1 && !hasV2)
            {
                return null;
            }

            var sb = new System.Text.StringBuilder();
            if (hasV2)
            {
                sb.Append($"IMDSv2: {ImdsV2FailureReason}.");
            }
            if (hasV1)
            {
                if (sb.Length > 0)
                {
                    sb.Append(' ');
                }
                sb.Append($"IMDSv1: {ImdsV1FailureReason}.");
            }

            return sb.ToString();
        }
    }
}
