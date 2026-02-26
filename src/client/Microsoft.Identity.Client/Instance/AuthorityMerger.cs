// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Instance
{
    /// <summary>
    /// Merges the config-level authority, the request-level authority override, and the
    /// account-level tenant/environment information into a single resolved <see cref="AuthorityInfo"/>.
    /// </summary>
    /// <remarks>
    /// Merge priority (highest first):
    /// <list type="number">
    ///   <item>Request-level authority override (fully specified)</item>
    ///   <item>Tenant-only override (request tenant applied to config authority host)</item>
    ///   <item>Environment override (multi-cloud: account environment applied to config tenant)</item>
    ///   <item>Config-level authority (baseline)</item>
    /// </list>
    ///
    /// Special cases handled:
    /// <list type="bullet">
    ///   <item>MSA passthrough: when Organizations tenant is configured but the account is a
    ///         consumer (MSA) account, the authority is switched to the consumers tenant.</item>
    ///   <item>Host consistency: if both a config and request authority are present and multi-cloud
    ///         support is disabled, the hosts must be equal or aliased.</item>
    /// </list>
    /// </remarks>
    internal static class AuthorityMerger
    {
        /// <summary>
        /// Merges authority information from all sources into a single <see cref="AuthorityInfo"/>.
        /// </summary>
        /// <param name="configAuthority">
        /// The authority configured at the application level. Must not be <see langword="null"/>.
        /// </param>
        /// <param name="requestOverride">
        /// The authority override supplied at the request level, or <see langword="null"/> if absent.
        /// </param>
        /// <param name="account">
        /// The account associated with the current request, used for tenant and environment merging.
        /// May be <see langword="null"/>.
        /// </param>
        /// <param name="isMsaPassthrough">
        /// Whether MSA passthrough is enabled (broker option). When <see langword="true"/> and the
        /// account is a consumer account targeting the Organizations tenant, the consumers tenant
        /// is injected automatically.
        /// </param>
        /// <param name="multiCloudSupportEnabled">
        /// Whether multi-cloud support is enabled. When <see langword="true"/>, the account's
        /// environment may be used to override the authority host.
        /// </param>
        /// <param name="requestContext">
        /// The current request context, used for instance discovery lookups when validating host consistency.
        /// </param>
        /// <returns>
        /// A merged <see cref="AuthorityInfo"/> that represents the effective authority for this request.
        /// </returns>
        public static async Task<AuthorityInfo> MergeAsync(
            Authority configAuthority,
            AuthorityInfo requestOverride,
            IAccount account,
            bool isMsaPassthrough,
            bool multiCloudSupportEnabled,
            RequestContext requestContext)
        {
            if (configAuthority == null)
            {
                throw new ArgumentNullException(nameof(configAuthority));
            }

            var configAuthorityInfo = configAuthority.AuthorityInfo;

            ValidateTypeMismatch(configAuthorityInfo, requestOverride);
            await ValidateSameHostAsync(configAuthorityInfo, requestOverride, multiCloudSupportEnabled, requestContext)
                .ConfigureAwait(false);

            // For non-AAD authority types: request override wins if present, otherwise use config
            if (configAuthorityInfo.AuthorityType != AuthorityType.Aad)
            {
                return requestOverride ?? configAuthorityInfo;
            }

            // ---- AAD-specific merge logic ----

            bool updateEnvironment = multiCloudSupportEnabled &&
                                     account != null &&
                                     !PublicClientApplication.IsOperatingSystemAccount(account);

            // No request override: use config authority, potentially updating tenant/environment
            if (requestOverride == null)
            {
                if (updateEnvironment)
                {
                    var withEnv = AuthorityInfo.AuthorityInfoHelper.CreateAuthorityWithEnvironment(
                        configAuthorityInfo, account.Environment);
                    return AuthorityInfo.AuthorityInfoHelper.CreateAuthorityWithTenant(
                        withEnv, account?.HomeAccountId?.TenantId, forceSpecifiedTenant: false).AuthorityInfo;
                }

                return AuthorityInfo.AuthorityInfoHelper.CreateAuthorityWithTenant(
                    configAuthority, account?.HomeAccountId?.TenantId, forceSpecifiedTenant: false).AuthorityInfo;
            }

            // Request override is provided
            // If the authority is not defined at the application level and the request level is not AAD, use it
            if (configAuthorityInfo.IsDefaultAuthority &&
                requestOverride.AuthorityType != AuthorityType.Aad)
            {
                return requestOverride;
            }

            var requestAadAuthority = updateEnvironment
                ? new AadAuthority(
                    AuthorityInfo.AuthorityInfoHelper.CreateAuthorityWithEnvironment(requestOverride, account?.Environment).AuthorityInfo)
                : new AadAuthority(requestOverride);

            // MSA passthrough: inject consumers tenant when organizations is configured
            if (requestAadAuthority.IsOrganizationsTenantWithMsaPassthroughEnabled(
                    isMsaPassthrough,
                    account?.HomeAccountId?.TenantId))
            {
                return requestAadAuthority.AuthorityInfo;
            }

            // If request authority is not a tenantless authority, use it directly
            if (!requestAadAuthority.IsCommonOrganizationsOrConsumersTenant())
            {
                return requestAadAuthority.AuthorityInfo;
            }

            // Request authority is tenantless ("common" / "organizations" / "consumers"):
            // fall back to config authority with tenant applied
            if (updateEnvironment)
            {
                var withEnv = AuthorityInfo.AuthorityInfoHelper.CreateAuthorityWithEnvironment(
                    configAuthorityInfo, account.Environment);
                return AuthorityInfo.AuthorityInfoHelper.CreateAuthorityWithTenant(
                    withEnv, account?.HomeAccountId?.TenantId, forceSpecifiedTenant: false).AuthorityInfo;
            }

            return AuthorityInfo.AuthorityInfoHelper.CreateAuthorityWithTenant(
                configAuthority, account?.HomeAccountId?.TenantId, forceSpecifiedTenant: false).AuthorityInfo;
        }

        private static void ValidateTypeMismatch(AuthorityInfo configAuthorityInfo, AuthorityInfo requestAuthorityInfo)
        {
            if (!configAuthorityInfo.IsDefaultAuthority &&
                requestAuthorityInfo != null &&
                configAuthorityInfo.AuthorityType != requestAuthorityInfo.AuthorityType)
            {
                throw new MsalClientException(
                    MsalError.AuthorityTypeMismatch,
                    MsalErrorMessage.AuthorityTypeMismatch(
                        configAuthorityInfo.AuthorityType,
                        requestAuthorityInfo.AuthorityType));
            }
        }

        private static async Task ValidateSameHostAsync(
            AuthorityInfo configAuthorityInfo,
            AuthorityInfo requestAuthorityInfo,
            bool multiCloudSupportEnabled,
            RequestContext requestContext)
        {
            if (multiCloudSupportEnabled ||
                requestAuthorityInfo == null ||
                string.Equals(requestAuthorityInfo.Host, configAuthorityInfo.Host, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (requestAuthorityInfo.AuthorityType == AuthorityType.B2C)
            {
                throw new MsalClientException(MsalError.B2CAuthorityHostMismatch, MsalErrorMessage.B2CAuthorityHostMisMatch);
            }

            // CIAM and Generic: let the STS handle any host differences
            if (requestAuthorityInfo.AuthorityType == AuthorityType.Ciam ||
                requestAuthorityInfo.AuthorityType == AuthorityType.Generic)
            {
                return;
            }

            bool usesRegional = !string.IsNullOrEmpty(requestContext.ServiceBundle.Config.AzureRegion);
            if (usesRegional)
            {
                throw new MsalClientException(MsalError.RegionalAndAuthorityOverride, MsalErrorMessage.RegionalAndAuthorityOverride);
            }

            if (await IsAuthorityAliasedAsync(requestContext, configAuthorityInfo, requestAuthorityInfo).ConfigureAwait(false))
            {
                return;
            }

            if (configAuthorityInfo.IsDefaultAuthority)
            {
                throw new MsalClientException(
                    MsalError.AuthorityHostMismatch,
                    $"You did not define an authority at the application level, so it defaults to the https://login.microsoftonline.com/common. " +
                    $"\n\rHowever, the request is for a different cloud {requestAuthorityInfo.Host}. This is not supported - the app and the request must target the same cloud. " +
                    $"\n\r\n\r Add .WithAuthority(\"https://{requestAuthorityInfo.Host}/common\") in the app builder. " +
                    $"\n\rSee https://aka.ms/msal-net-authority-override for details");
            }

            throw new MsalClientException(
                MsalError.AuthorityHostMismatch,
                $"\n\r The application is configured for cloud {configAuthorityInfo.Host} and the request for a different cloud - {requestAuthorityInfo.Host}. This is not supported - the app and the request must target the same cloud. " +
                $"\n\rSee https://aka.ms/msal-net-authority-override for details");
        }

        private static async Task<bool> IsAuthorityAliasedAsync(
            RequestContext requestContext,
            AuthorityInfo configAuthorityInfo,
            AuthorityInfo requestAuthorityInfo)
        {
            var instanceDiscoveryManager = requestContext.ServiceBundle.InstanceDiscoveryManager;
            var result = await instanceDiscoveryManager
                .GetMetadataEntryAsync(configAuthorityInfo, requestContext)
                .ConfigureAwait(false);

            return result.Aliases.Any(alias => alias.Equals(requestAuthorityInfo.Host));
        }
    }
}
