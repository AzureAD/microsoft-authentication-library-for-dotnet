// ------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Mats.Internal.Events;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Base class for builders of token requests, which attempt to acquire a token
    /// based on the provided parameters
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class AbstractAcquireTokenParameterBuilder<T>
        where T : AbstractAcquireTokenParameterBuilder<T>
    {
        internal AcquireTokenCommonParameters CommonParameters { get; } = new AcquireTokenCommonParameters();

        /// <summary>
        /// Executes the Token request asynchronously, with a possibility of cancelling the
        /// asynchronous method.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token. See <see cref="CancellationToken"/> </param>
        /// <returns>Authentication result containing a token for the requested scopes and parameters
        /// set in the builder</returns>
        public abstract Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken);

        internal abstract ApiEvent.ApiIds CalculateApiEventId();

        internal abstract ApiTelemetryId ApiTelemetryId { get; }

        /// <summary>
        /// Executes the Token request asynchronously.
        /// </summary>
        /// <returns>Authentication result containing a token for the requested scopes and parameters
        /// set in the builder</returns>
        public Task<AuthenticationResult> ExecuteAsync()
        {
            return ExecuteAsync(CancellationToken.None);
        }

        /// <summary>
        /// Specifies which scopes to request
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <returns>The builder to chain the .With methods</returns>
        protected T WithScopes(IEnumerable<string> scopes)
        {
            CommonParameters.Scopes = scopes;
            return (T)this;
        }

        /// <summary>
        /// Sets Extra Query Parameters for the query string in the HTTP authentication request
        /// </summary>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority
        /// as a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithExtraQueryParameters(Dictionary<string, string> extraQueryParameters)
        {
            CommonParameters.ExtraQueryParameters = extraQueryParameters ?? new Dictionary<string, string>();
            return (T)this;
        }

        /// <summary>
        /// Sets claims in the query. Use when the AAD admin has enabled conditional access. Acquiring the token normally will result in a
        /// <see cref="MsalServiceException"/> with the <see cref="MsalServiceException.Claims"/> property set. Retry the 
        /// token acquisition, and use this value in the <see cref="WithClaims(string)"/> method. See https://aka.ms/msal-exceptions for details
        /// </summary>
        /// <param name="claims">A string with one or multiple claims.</param>
        /// <returns>The builder to chain .With methods</returns>
        public T WithClaims(string claims)
        {
            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithClaims);
            CommonParameters.Claims = claims;
            return (T)this;
        }

        /// <summary>
        /// Sets Extra Query Parameters for the query string in the HTTP authentication request
        /// </summary>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority.
        /// The string needs to be properly URL-encdoded and ready to send as a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// </param>
        /// <returns></returns>
        public T WithExtraQueryParameters(string extraQueryParameters)
        {
            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithExtraQueryParameters);
            if (!string.IsNullOrWhiteSpace(extraQueryParameters))
            {
                return WithExtraQueryParameters(CoreHelpers.ParseKeyValueList(extraQueryParameters, '&', true, null));
            }
            return (T)this;
        }

        /// <summary>
        /// Specifies the IEF specific policy for B2C scenarios
        /// </summary>
        /// <param name="policy">/>
        /// </param>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithIEFPolicy(string policy)
        {
            if(string.IsNullOrEmpty(policy))
            {
                throw new ArgumentNullException(nameof(policy));
            }

            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithIEFPolicy);
            CommonParameters.IEFPolicy = policy;
            return (T)this;
        }

        /// <summary>
        /// Specific authority for which the token is requested. Passing a different value than configured
        /// at the application constructor narrows down the selection to a specific tenant.
        /// This does not change the configured value in the application. This is specific
        /// to applications managing several accounts (like a mail client with several mailboxes).
        /// See https://aka.ms/msal-net-application-configuration
        /// </summary>
        /// <param name="authorityUri">Uri for the authority. In the case when the authority URI is 
        /// a known Azure AD URI, this setting needs to be consistent with what is declared in 
        /// the application registration portal</param>
        /// <param name="validateAuthority">Whether the authority should be validated against the server metadata.</param>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithAuthority(string authorityUri, bool validateAuthority = true)
        {
            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithAuthority);
            if (validateAuthority)
            {
                CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithValidateAuthority);
            }

            if (string.IsNullOrWhiteSpace(authorityUri))
            {
                throw new ArgumentNullException(nameof(authorityUri));
            }
            CommonParameters.AuthorityOverride = AuthorityInfo.FromAuthorityUri(authorityUri, validateAuthority);
            return (T)this;
        }

        /// <summary>
        /// Adds a known Azure AD authority to the application to sign-in users from a single
        /// organization (single tenant application) specified by its tenant ID. See https://aka.ms/msal-net-application-configuration.
        /// </summary>
        /// <param name="cloudInstanceUri">Azure Cloud instance</param>
        /// <param name="tenantId">Guid of the tenant from which to sign-in users</param>
        /// <param name="validateAuthority">Whether the authority should be validated against the server metadata.</param>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithAuthority(
            string cloudInstanceUri,
            Guid tenantId,
            bool validateAuthority = true)
        {
            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithAuthority);
            if (validateAuthority)
            {
                CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithValidateAuthority);
            }

            if (string.IsNullOrWhiteSpace(cloudInstanceUri))
            {
                throw new ArgumentNullException(nameof(cloudInstanceUri));
            }
            CommonParameters.AuthorityOverride = AuthorityInfo.FromAadAuthority(new Uri(cloudInstanceUri), tenantId, validateAuthority);
            return (T)this;
        }

        /// <summary>
        /// Adds a known Azure AD authority to the application to sign-in users from a single
        /// organization (single tenant application) described by its domain name. See https://aka.ms/msal-net-application-configuration.
        /// </summary>
        /// <param name="cloudInstanceUri">Uri to the Azure Cloud instance (for instance
        /// <c>https://login.microsoftonline.com)</c></param>
        /// <param name="tenant">domain name associated with the tenant from which to sign-in users</param>
        /// <param name="validateAuthority">Whether the authority should be validated against the server metadata.</param>
        /// <remarks>
        /// <paramref name="tenant"/> can also contain the string representation of a GUID (tenantId),
        /// or even <c>common</c>, <c>organizations</c> or <c>consumers</c> but in this case
        /// it's recommended to use another override (<see cref="WithAuthority(AzureCloudInstance, Guid, bool)"/>
        /// and <see cref="WithAuthority(AzureCloudInstance, AadAuthorityAudience, bool)"/>
        /// </remarks>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithAuthority(
            string cloudInstanceUri,
            string tenant,
            bool validateAuthority = true)
        {
            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithAuthority);
            if (validateAuthority)
            {
                CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithValidateAuthority);
            }

            if (string.IsNullOrWhiteSpace(cloudInstanceUri))
            {
                throw new ArgumentNullException(nameof(cloudInstanceUri));
            }
            CommonParameters.AuthorityOverride = AuthorityInfo.FromAadAuthority(new Uri(cloudInstanceUri), tenant, validateAuthority);
            return (T)this;
        }

        /// <summary>
        /// Adds a known Azure AD authority to the application to sign-in users from a single
        /// organization (single tenant application) described by its cloud instance and its tenant ID.
        /// See https://aka.ms/msal-net-application-configuration.
        /// </summary>
        /// <param name="azureCloudInstance">Instance of Azure Cloud (for instance Azure
        /// worldwide cloud, Azure German Cloud, US government ...)</param>
        /// <param name="tenantId">Tenant Id of the tenant from which to sign-in users</param>
        /// <param name="validateAuthority">Whether the authority should be validated against the server metadata.</param>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithAuthority(
            AzureCloudInstance azureCloudInstance,
            Guid tenantId,
            bool validateAuthority = true)
        {
            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithAuthority);
            if (validateAuthority)
            {
                CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithValidateAuthority);
            }

            CommonParameters.AuthorityOverride = AuthorityInfo.FromAadAuthority(azureCloudInstance, tenantId, validateAuthority);
            return (T)this;
        }

        /// <summary>
        /// Adds a known Azure AD authority to the application to sign-in users from a single
        /// organization (single tenant application) described by its cloud instance and its domain
        /// name or tenant ID. See https://aka.ms/msal-net-application-configuration.
        /// </summary>
        /// <param name="azureCloudInstance">Instance of Azure Cloud (for instance Azure
        /// worldwide cloud, Azure German Cloud, US government ...)</param>
        /// <param name="tenant">Domain name associated with the Azure AD tenant from which
        /// <param name="validateAuthority">Whether the authority should be validated against the server metadata.</param>
        /// to sign-in users. This can also be a guid</param>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithAuthority(
            AzureCloudInstance azureCloudInstance,
            string tenant,
            bool validateAuthority = true)
        {
            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithAuthority);
            if (validateAuthority)
            {
                CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithValidateAuthority);
            }

            CommonParameters.AuthorityOverride = AuthorityInfo.FromAadAuthority(azureCloudInstance, tenant, validateAuthority);
            return (T)this;
        }

        /// <summary>
        /// Adds a known Azure AD authority to the application to sign-in users specifying
        /// the cloud instance and the sign-in audience. See https://aka.ms/msal-net-application-configuration.
        /// </summary>
        /// <param name="azureCloudInstance">Instance of Azure Cloud (for instance Azure
        /// worldwide cloud, Azure German Cloud, US government ...)</param>
        /// <param name="authorityAudience">Sign-in audience (one AAD organization,
        /// any work and school accounts, or any work and school accounts and Microsoft personal
        /// accounts</param>
        /// <param name="validateAuthority">Whether the authority should be validated against the server metadata.</param>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithAuthority(AzureCloudInstance azureCloudInstance, AadAuthorityAudience authorityAudience, bool validateAuthority = true)
        {
            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithAuthority);
            if (validateAuthority)
            {
                CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithValidateAuthority);
            }

            CommonParameters.AuthorityOverride = AuthorityInfo.FromAadAuthority(azureCloudInstance, authorityAudience, validateAuthority);
            return (T)this;
        }

        /// <summary>
        /// Adds a known Azure AD authority to the application to sign-in users specifying
        /// the sign-in audience (the cloud being the Azure public cloud). See https://aka.ms/msal-net-application-configuration.
        /// </summary>
        /// <param name="authorityAudience">Sign-in audience (one AAD organization,
        /// any work and school accounts, or any work and school accounts and Microsoft personal
        /// accounts</param>
        /// <param name="validateAuthority">Whether the authority should be validated against the server metadata.</param>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithAuthority(AadAuthorityAudience authorityAudience, bool validateAuthority = true)
        {
            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithAuthority);
            if (validateAuthority)
            {
                CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithValidateAuthority);
            }

            CommonParameters.AuthorityOverride = AuthorityInfo.FromAadAuthority(authorityAudience, validateAuthority);
            return (T)this;
        }

        /// <summary>
        /// Adds a known Authority corresponding to an ADFS server. See https://aka.ms/msal-net-adfs
        /// </summary>
        /// <param name="authorityUri">Authority URL for an ADFS server</param>
        /// <param name="validateAuthority">Whether the authority should be validated against the server metadata.</param>
        /// <remarks>MSAL.NET will only support ADFS 2019 or later.</remarks>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithAdfsAuthority(string authorityUri, bool validateAuthority = true)
        {
            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithAdfsAuthority);
            if (validateAuthority)
            {
                CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithValidateAuthority);
            }

            if (string.IsNullOrWhiteSpace(authorityUri))
            {
                throw new ArgumentNullException(nameof(authorityUri));
            }
            CommonParameters.AuthorityOverride = new AuthorityInfo(AuthorityType.Adfs, authorityUri, validateAuthority);
            return (T)this;
        }

        /// <summary>
        /// Adds a known authority corresponding to an Azure AD B2C policy.
        /// See https://aka.ms/msal-net-b2c-specificities
        /// </summary>
        /// <param name="authorityUri">Azure AD B2C authority, including the B2C policy (for instance
        /// <c>"https://fabrikamb2c.b2clogin.com/tfp/{Tenant}/{policy}</c></param>)
        /// <returns>The builder to chain the .With methods</returns>
        public T WithB2CAuthority(string authorityUri)
        {
            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithB2CAuthority);

            if (string.IsNullOrWhiteSpace(authorityUri))
            {
                throw new ArgumentNullException(nameof(authorityUri));
            }
            CommonParameters.AuthorityOverride = new AuthorityInfo(AuthorityType.B2C, authorityUri, false);
            return (T)this;
        }

        /// <summary>
        /// Validates the parameters of the AcquireToken operation.
        /// </summary>
        protected virtual void Validate()
        {
        }

        internal void ValidateAndCalculateApiId()
        {
            Validate();
            CommonParameters.ApiId = CalculateApiEventId();
            CommonParameters.ApiTelemId = ApiTelemetryId;
            CommonParameters.TelemetryCorrelationId = Guid.NewGuid();
        }
    }
}
