// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// <inheritdoc />
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
        /// Sets Extra Query Parameters for the query string in the HTTP authentication request.
        /// </summary>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority
        /// as a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <returns>The builder to chain the .With methods.</returns>
        public T WithExtraQueryParameters(Dictionary<string, string> extraQueryParameters)
        {
            CommonParameters.ExtraQueryParameters = extraQueryParameters ??
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
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
        /// Sets Extra Query Parameters for the query string in the HTTP authentication request.
        /// </summary>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority.
        /// The string needs to be properly URL-encoded and ready to send as a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// </param>
        /// <returns>The builder to chain .With methods.</returns>
        public T WithExtraQueryParameters(string extraQueryParameters)
        {
            if (!string.IsNullOrWhiteSpace(extraQueryParameters))
            {
                return WithExtraQueryParameters(CoreHelpers.ParseKeyValueList(extraQueryParameters, '&', true, null));
            }
            return this as T;
        }

        /// <summary>
        /// Important: Use WithTenantId or WithTenantIdFromAuthority instead, or WithB2CAuthority for B2C authorities.
        /// 
        /// Specific authority for which the token is requested. Passing a different value than configured
        /// at the application constructor narrows down the selection to a specific tenant.
        /// This does not change the configured value in the application. This is specific
        /// to applications managing several accounts (like a mail client with several mailboxes).
        /// See https://aka.ms/msal-net-application-configuration.
        /// </summary>
        /// <param name="authorityUri">Uri for the authority. In the case when the authority URI is 
        /// a known Azure AD URI, this setting needs to be consistent with what is declared in 
        /// the application registration portal.</param>
        /// <param name="validateAuthority">Whether the authority should be validated against the server metadata.</param>
        /// <returns>The builder to chain the .With methods.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)] // Soft deprecate
        public T WithAuthority(string authorityUri, bool validateAuthority = true)
        {
            if (string.IsNullOrWhiteSpace(authorityUri))
            {
                throw new ArgumentNullException(nameof(authorityUri));
            }
            CommonParameters.AuthorityOverride = AuthorityInfo.FromAuthorityUri(authorityUri, validateAuthority);
            return this as T;
        }

        /// <summary>
        /// Important: Use WithTenantId or WithTenantIdFromAuthority instead, or WithB2CAuthority for B2C authorities.
        /// 
        /// Adds a known Azure AD authority to the application to sign-in users from a single
        /// organization (single tenant application) specified by its tenant ID. See https://aka.ms/msal-net-application-configuration.
        /// </summary>
        /// <param name="cloudInstanceUri">Azure Cloud instance.</param>
        /// <param name="tenantId">GUID of the tenant from which to sign-in users.</param>
        /// <param name="validateAuthority">Whether the authority should be validated against the server metadata.</param>
        /// <returns>The builder to chain the .With methods.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)] // Soft deprecate
        public T WithAuthority(
            string cloudInstanceUri,
            Guid tenantId,
            bool validateAuthority = true)
        {
            if (string.IsNullOrWhiteSpace(cloudInstanceUri))
            {
                throw new ArgumentNullException(nameof(cloudInstanceUri));
            }
            CommonParameters.AuthorityOverride = AuthorityInfo.FromAadAuthority(cloudInstanceUri, tenantId, validateAuthority);
            return this as T;
        }

        /// <summary>
        /// Important: Use WithTenantId or WithTenantIdFromAuthority instead, or WithB2CAuthority for B2C authorities.
        /// 
        /// Adds a known Azure AD authority to the application to sign-in users from a single
        /// organization (single tenant application) described by its domain name. See https://aka.ms/msal-net-application-configuration.
        /// </summary>
        /// <param name="cloudInstanceUri">Uri to the Azure Cloud instance (for instance
        /// <c>https://login.microsoftonline.com)</c>.</param>
        /// <param name="tenant">Tenant Id associated with the tenant from which to sign-in users.</param>
        /// <param name="validateAuthority">Whether the authority should be validated against the server metadata.</param>
        /// <remarks>
        /// <paramref name="tenant"/> can also contain the string representation of a GUID (tenantId),
        /// or even <c>common</c>, <c>organizations</c> or <c>consumers</c> but in this case
        /// it's recommended to use another override (<see cref="WithAuthority(AzureCloudInstance, Guid, bool)"/>
        /// and <see cref="WithAuthority(AzureCloudInstance, AadAuthorityAudience, bool)"/>
        /// </remarks>
        /// <returns>The builder to chain the .With methods.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)] // Soft deprecate
        public T WithAuthority(
            string cloudInstanceUri,
            string tenant,
            bool validateAuthority = true)
        {
            if (string.IsNullOrWhiteSpace(cloudInstanceUri))
            {
                throw new ArgumentNullException(nameof(cloudInstanceUri));
            }
            CommonParameters.AuthorityOverride = AuthorityInfo.FromAadAuthority(cloudInstanceUri, tenant, validateAuthority);
            return this as T;
        }

        /// <summary>
        /// Important: Use WithTenantId or WithTenantIdFromAuthority instead, or WithB2CAuthority for B2C authorities.
        /// 
        /// Adds a known Azure AD authority to the application to sign-in users from a single
        /// organization (single tenant application) described by its cloud instance and its tenant ID.
        /// See https://aka.ms/msal-net-application-configuration.
        /// </summary>
        /// <param name="azureCloudInstance">Instance of Azure Cloud (for instance Azure
        /// worldwide cloud, Azure German Cloud, US government ...).</param>
        /// <param name="tenantId">Tenant Id of the tenant from which to sign-in users.</param>
        /// <param name="validateAuthority">Whether the authority should be validated against the server metadata.</param>
        /// <returns>The builder to chain the .With methods.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)] // Soft deprecate
        public T WithAuthority(
            AzureCloudInstance azureCloudInstance,
            Guid tenantId,
            bool validateAuthority = true)
        {
            CommonParameters.AuthorityOverride = AuthorityInfo.FromAadAuthority(azureCloudInstance, tenantId, validateAuthority);
            return this as T;
        }

        /// <summary>
        /// Important: Use WithTenantId or WithTenantIdFromAuthority instead, or WithB2CAuthority for B2C authorities.
        /// 
        /// Adds a known Azure AD authority to the application to sign-in users from a single
        /// organization (single tenant application) described by its cloud instance and its domain
        /// name or tenant ID. See https://aka.ms/msal-net-application-configuration.
        /// </summary>
        /// <param name="azureCloudInstance">Instance of Azure Cloud (for instance Azure
        /// worldwide cloud, Azure German Cloud, US government ...).</param>
        /// <param name="tenant">Tenant Id of the tenant from which to sign-in users. This can also be a GUID.</param>
        /// <param name="validateAuthority">Whether the authority should be validated against the server metadata.</param>
        /// <returns>The builder to chain the .With methods.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)] // Soft deprecate
        public T WithAuthority(
            AzureCloudInstance azureCloudInstance,
            string tenant,
            bool validateAuthority = true)
        {
            CommonParameters.AuthorityOverride = AuthorityInfo.FromAadAuthority(azureCloudInstance, tenant, validateAuthority);
            return this as T;
        }

        /// <summary>
        /// Adds a known Azure AD authority to the application to sign-in users specifying
        /// the cloud instance and the sign-in audience. See https://aka.ms/msal-net-application-configuration.
        /// </summary>
        /// <param name="azureCloudInstance">Instance of Azure Cloud (for instance Azure
        /// worldwide cloud, Azure German Cloud, US government ...).</param>
        /// <param name="authorityAudience">Sign-in audience (one AAD organization,
        /// any work and school accounts, or any work and school accounts and Microsoft personal
        /// accounts.</param>
        /// <param name="validateAuthority">Whether the authority should be validated against the server metadata.</param>
        /// <returns>The builder to chain the .With methods.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)] // Soft deprecate
        public T WithAuthority(AzureCloudInstance azureCloudInstance, AadAuthorityAudience authorityAudience, bool validateAuthority = true)
        {
            CommonParameters.AuthorityOverride = AuthorityInfo.FromAadAuthority(azureCloudInstance, authorityAudience, validateAuthority);
            return this as T;
        }

        /// <summary>
        /// Important: Use WithTenantId or WithTenantIdFromAuthority instead, or WithB2CAuthority for B2C authorities.
        /// 
        /// Adds a known Azure AD authority to the application to sign-in users specifying
        /// the sign-in audience (the cloud being the Azure public cloud). See https://aka.ms/msal-net-application-configuration.
        /// </summary>
        /// <param name="authorityAudience">Sign-in audience (one AAD organization,
        /// any work and school accounts, or any work and school accounts and Microsoft personal
        /// accounts.</param>
        /// <param name="validateAuthority">Whether the authority should be validated against the server metadata.</param>
        /// <returns>The builder to chain the .With methods.</returns>        
        [EditorBrowsable(EditorBrowsableState.Never)] // Soft deprecate
        public T WithAuthority(AadAuthorityAudience authorityAudience, bool validateAuthority = true)
        {
            CommonParameters.AuthorityOverride = AuthorityInfo.FromAadAuthority(authorityAudience, validateAuthority);
            return this as T;
        }

        /// <summary>
        /// Overrides the tenant ID specified in the authority at the application level. This operation preserves the authority host (environment).
        /// 
        /// If an authority was not specified at the application level, the default used is https://login.microsoftonline.com/common.
        /// </summary>
        /// <param name="tenantId">The tenant ID, which can be either in GUID format or a domain name. Also known as the Directory ID.</param>
        /// <returns>The builder to chain the .With methods.</returns>
        /// <exception cref="ArgumentNullException">Thrown if tenantId is null or an empty string</exception>
        /// <exception cref="MsalClientException">Thrown if the application was configured with an authority that is not AAD specific (e.g. ADFS or B2C).</exception>
        /// <remarks>
        /// The tenant should be more restrictive than the one configured at the application level, e.g. don't use "common".
        /// Does not affect authority validation, which is specified at the application level.</remarks>
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
        /// 
        /// If an authority was not provided to the application builder, this method will replace the tenant ID in the default authority - https://login.microsoftonline.com/common.
        /// </summary>
        /// <param name="authorityUri">URI from which to extract the tenant ID</param>

        /// <returns>The builder to chain the .With methods.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="authorityUri"/> is null or an empty string</exception>
        /// <exception cref="MsalClientException">Thrown if the application was configured with an authority that is not AAD specific (e.g. ADFS or B2C).</exception>
        /// <remarks>
        /// The tenant should be more restrictive than the one configured at the application level, e.g. don't use "common".
        /// Does not affect authority validation, which is specified at the application level.</remarks>
        public T WithTenantIdFromAuthority(Uri authorityUri)
        {
            if (authorityUri == null)
            {
                throw new ArgumentNullException(nameof(authorityUri));
            }

            var authorityInfo = AuthorityInfo.FromAuthorityUri(authorityUri.ToString(), false);
            var authority = Authority.CreateAuthority(authorityInfo);
            return WithTenantId(authority.TenantId);
        }

        /// <summary>
        /// Adds a known Authority corresponding to an ADFS server. See https://aka.ms/msal-net-adfs.
        /// </summary>
        /// <param name="authorityUri">Authority URL for an ADFS server.</param>
        /// <param name="validateAuthority">Whether the authority should be validated against the server metadata.</param>
        /// <remarks>MSAL.NET supports ADFS 2019 or later.</remarks>
        /// <returns>The builder to chain the .With methods.</returns>                
        public T WithAdfsAuthority(string authorityUri, bool validateAuthority = true)
        {
            if (string.IsNullOrWhiteSpace(authorityUri))
            {
                throw new ArgumentNullException(nameof(authorityUri));
            }
            CommonParameters.AuthorityOverride = new AuthorityInfo(AuthorityType.Adfs, authorityUri, validateAuthority);
            return this as T;
        }

        /// <summary>
        /// Adds a known authority corresponding to an Azure AD B2C policy.
        /// See https://aka.ms/msal-net-b2c-specificities
        /// </summary>
        /// <param name="authorityUri">Azure AD B2C authority, including the B2C policy (for instance
        /// <c>"https://fabrikamb2c.b2clogin.com/tfp/{Tenant}/{policy}</c></param>).
        /// <returns>The builder to chain the .With methods.</returns>
        public T WithB2CAuthority(string authorityUri)
        {
            if (string.IsNullOrWhiteSpace(authorityUri))
            {
                throw new ArgumentNullException(nameof(authorityUri));
            }
            CommonParameters.AuthorityOverride = new AuthorityInfo(AuthorityType.B2C, authorityUri, false);
            return this as T;
        }

        internal /* for testing */ T WithAuthenticationScheme(IAuthenticationScheme scheme)
        {
            CommonParameters.AuthenticationScheme = scheme ?? throw new ArgumentNullException(nameof(scheme));
            return this as T;
        }
    }
}
