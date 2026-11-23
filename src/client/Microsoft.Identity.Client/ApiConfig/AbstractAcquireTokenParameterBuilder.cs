// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.AuthScheme;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client
{
    /// <inheritdoc/>
    /// <typeparam name="T"></typeparam>
    public abstract class AbstractAcquireTokenParameterBuilder<T> : BaseAbstractAcquireTokenParameterBuilder<T>
        where T : BaseAbstractAcquireTokenParameterBuilder<T>
    {

        /// <summary>
        /// Default constructor for AbstractAcquireTokenParameterBuilder.
        /// </summary>
        protected AbstractAcquireTokenParameterBuilder() : base() { }

        internal AbstractAcquireTokenParameterBuilder(IServiceBundle serviceBundle) : base(serviceBundle) { }

        /// <summary>
        /// Specifies which scopes to request. This method is used when your application needs
        /// to specify the scopes needed to call a protected API. See
        /// <see>https://docs.microsoft.com/azure/active-directory/develop/v2-permissions-and-consent</see> to learn
        /// more about scopes, permissions and consent, and
        /// <see>https://docs.microsoft.com/azure/active-directory/develop/msal-v1-app-scopes</see> to learn how
        /// to create scopes for legacy applications which used to expose OAuth2 permissions.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <returns>The builder to chain the .With methods.</returns>
        protected T WithScopes(IEnumerable<string> scopes)
        {
            CommonParameters.Scopes = scopes;
            return this as T;
        }

        /// <summary>
        /// Sets claims in the query. Use when the AAD admin has enabled conditional access. Acquiring the token normally will result in a
        /// <see cref="MsalUiRequiredException"/> with the <see cref="MsalServiceException.Claims"/> property set. Retry the 
        /// token acquisition, and use this value in the <see cref="WithClaims(string)"/> method. See https://aka.ms/msal-exceptions for details
        /// as well as https://aka.ms/msal-net-claim-challenge.
        /// </summary>
        /// <param name="claims">A string with one or multiple claims.</param>
        /// <returns>The builder to chain .With methods.</returns>
        public T WithClaims(string claims)
        {
            CommonParameters.Claims = claims;
            return this as T;
        }

        /// <summary>
        /// Overrides the tenant ID specified in the authority at the application level. This operation preserves the authority host (environment).
        /// 
        /// If an authority was not specified at the application level, the default used is `https://login.microsoftonline.com/common`.
        /// </summary>
        /// <param name="tenantId">Tenant ID of the Microsoft Entra ID tenant
        /// or a domain associated with this Microsoft Entra ID tenant, in order to sign-in a user of a specific organization only.</param>
        /// <returns>The builder to chain the .With methods.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="tenantId"/> is null or an empty string.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="tenantId"/> is not well-formatted (for example, has spaces).</exception>
        /// <exception cref="MsalClientException">Thrown in more general exception scenarios (for ex. if the application was configured with an authority that does not allow tenants).</exception>
        /// <remarks>
        /// The tenant should be more restrictive than the one configured at the application level, e.g. don't use "common".
        /// Does not affect authority validation, which is specified at the application level.
        /// </remarks>
        public T WithTenantId(string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                throw new ArgumentNullException(nameof(tenantId));
            }

            Authority newAuthority = AuthorityInfo.AuthorityInfoHelper.CreateAuthorityWithTenant(
                ServiceBundle.Config.Authority, 
                tenantId, 
                true);

            CommonParameters.AuthorityOverride = newAuthority.AuthorityInfo;

            return this as T;
        }

        /// <summary>
        /// Extracts the tenant ID from the provided authority URI and overrides the tenant ID specified in the authority at the application level. This operation preserves the authority host (environment) provided to the application builder.
        /// If an authority was not provided to the application builder, this method will replace the tenant ID in the default authority - `https://login.microsoftonline.com/common`.
        /// </summary>
        /// <param name="authorityUri">URI from which to extract the tenant ID</param>
        /// <returns>The builder to chain the .With methods.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="authorityUri"/> is null or an empty string.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="authorityUri"/> is not well-formatted (for example, has spaces).</exception>
        /// <exception cref="MsalClientException">Thrown in general exception scenarios (for example if the application was configured with an authority that does not allow tenants).</exception>
        /// <remarks>
        /// The tenant should be more restrictive than the one configured at the application level, e.g. don't use "common".
        /// Does not affect authority validation, which is specified at the application level.
        /// </remarks>
        public T WithTenantIdFromAuthority(Uri authorityUri)
        {
            if (authorityUri == null)
            {
                throw new ArgumentNullException(nameof(authorityUri));
            }

            var authorityInfo = AuthorityInfo.FromAuthorityUri(authorityUri.ToString(), false);
            if (authorityInfo.CanBeTenanted)
            {
                var authority = Authority.CreateAuthority(authorityInfo);
                return WithTenantId(authority.TenantId);
            }

            return this as T;
        }

        /// <summary>
        /// Adds a known authority corresponding to an Azure AD B2C policy.
        /// See https://aka.ms/msal-net-b2c-specificities
        /// </summary>
        /// <param name="authorityUri">Azure AD B2C authority, including the B2C policy (for instance
        /// <c>"https://fabrikamb2c.b2clogin.com/tfp/{Tenant}/{policy}</c></param>).
        /// <returns>The builder to chain the .With methods.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public T WithB2CAuthority(string authorityUri)
        {
            if (string.IsNullOrWhiteSpace(authorityUri))
            {
                throw new ArgumentNullException(nameof(authorityUri));
            }
            CommonParameters.AuthorityOverride = new AuthorityInfo(AuthorityType.B2C, authorityUri, false);
            return this as T;
        }

        internal /* for testing */ T WithAuthenticationOperation(IAuthenticationOperation authOperation)
        {
            CommonParameters.AuthenticationOperation = authOperation ?? throw new ArgumentNullException(nameof(authOperation));
            return this as T;
        }
    }
}
