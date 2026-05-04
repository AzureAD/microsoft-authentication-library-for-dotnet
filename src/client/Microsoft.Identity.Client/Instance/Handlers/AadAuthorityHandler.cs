// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Instance.Validation;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Instance.Handlers
{
    /// <summary>
    /// Handler for AAD authorities. Acts as the URI catch-all (CanHandle always returns true)
    /// and must be registered after all more-specific handlers.
    /// </summary>
    internal sealed class AadAuthorityHandler : IAuthorityHandler
    {
        public AuthorityType AuthorityType => AuthorityType.Aad;

        /// <summary>
        /// Catch-all: any URI not claimed by a more-specific handler is treated as AAD.
        /// This handler must be registered last among URI-detectable handlers.
        /// </summary>
        public bool CanHandle(Uri authorityUri, string host, string firstPathSegment) => true;

        public Authority Create(AuthorityInfo authorityInfo)
            => new AadAuthority(authorityInfo);

        public IAuthorityValidator CreateValidator(RequestContext requestContext)
            => new AadAuthorityValidator(requestContext);

        public Task<Authority> ResolveForRequestAsync(
            Authority configAuthority,
            AuthorityInfo requestAuthorityInfo,
            IAccount account,
            RequestContext requestContext)
        {
            var configAuthorityInfo = configAuthority.AuthorityInfo;

            bool updateEnvironment = requestContext.ServiceBundle.Config.MultiCloudSupportEnabled
                && account != null
                && !PublicClientApplication.IsOperatingSystemAccount(account);

            if (requestAuthorityInfo == null)
            {
                Authority result = updateEnvironment
                    ? AuthorityInfo.AuthorityInfoHelper.CreateAuthorityWithTenant(
                        AuthorityInfo.AuthorityInfoHelper.CreateAuthorityWithEnvironment(configAuthorityInfo, account.Environment),
                        account?.HomeAccountId?.TenantId,
                        forceSpecifiedTenant: false)
                    : AuthorityInfo.AuthorityInfoHelper.CreateAuthorityWithTenant(
                        configAuthority,
                        account?.HomeAccountId?.TenantId,
                        forceSpecifiedTenant: false);

                return Task.FromResult(result);
            }

            // In case the authority is defined only at the request level
            if (configAuthorityInfo.IsDefaultAuthority &&
                requestAuthorityInfo.AuthorityType != AuthorityType.Aad)
            {
                return Task.FromResult(requestAuthorityInfo.CreateAuthority());
            }

            var requestAadAuthority = updateEnvironment
                ? new AadAuthority(AuthorityInfo.AuthorityInfoHelper.CreateAuthorityWithEnvironment(requestAuthorityInfo, account?.Environment).AuthorityInfo)
                : new AadAuthority(requestAuthorityInfo);

            if (!requestAadAuthority.IsCommonOrganizationsOrConsumersTenant() ||
                requestAadAuthority.IsOrganizationsTenantWithMsaPassthroughEnabled(
                    requestContext.ServiceBundle.Config.IsBrokerEnabled
                    && requestContext.ServiceBundle.Config.BrokerOptions != null
                    && requestContext.ServiceBundle.Config.BrokerOptions.MsaPassthrough,
                    account?.HomeAccountId?.TenantId))
            {
                return Task.FromResult((Authority)requestAadAuthority);
            }

            Authority final = updateEnvironment
                ? AuthorityInfo.AuthorityInfoHelper.CreateAuthorityWithTenant(
                    AuthorityInfo.AuthorityInfoHelper.CreateAuthorityWithEnvironment(configAuthorityInfo, account.Environment),
                    account?.HomeAccountId?.TenantId,
                    forceSpecifiedTenant: false)
                : AuthorityInfo.AuthorityInfoHelper.CreateAuthorityWithTenant(
                    configAuthority,
                    account?.HomeAccountId?.TenantId,
                    forceSpecifiedTenant: false);

            return Task.FromResult(final);
        }
    }
}
