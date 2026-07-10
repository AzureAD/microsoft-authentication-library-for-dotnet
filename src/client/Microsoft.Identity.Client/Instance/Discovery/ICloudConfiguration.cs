// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Instance.Discovery
{
    /// <summary>
    /// Provides cloud-specific metadata resolved by authority host.
    /// Used to look up cloud-specific values (e.g., FIC token exchange audiences)
    /// that vary across Azure cloud environments.
    /// </summary>
    /// <remarks>
    /// MSAL ships a default implementation (<see cref="KnownCloudConfiguration"/>) that
    /// covers all publicly known Azure clouds. Callers can provide a custom implementation
    /// via <see cref="AbstractApplicationBuilder{T}.WithCloudConfiguration(ICloudConfiguration)"/>
    /// to add entries for private or internal-only clouds.
    /// </remarks>
    public interface ICloudConfiguration
    {
        /// <summary>
        /// Gets cloud-specific settings for the given authority host.
        /// </summary>
        /// <param name="authorityHost">
        /// The authority host name (e.g., "login.microsoftonline.com", "login.microsoftonline.us").
        /// Lookup is case-insensitive.
        /// </param>
        /// <returns>
        /// A <see cref="CloudSettings"/> instance with cloud-specific metadata,
        /// or <c>null</c> if the host is not recognized.
        /// </returns>
        CloudSettings GetSettingsByAuthority(string authorityHost);
    }
}
