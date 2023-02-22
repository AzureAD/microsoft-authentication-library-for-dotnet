// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Executors;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Class to be used for managed identity applications (on Azure resources like App Services, Virtual Machines, Azure Arc, Service Fabric and Cloud Shell).
    /// </summary>
    /// <remarks>
    /// Managed identity can be enabled on Azure resources as a system assigned managed identity or a user assigned managed identity.
    /// </remarks>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide confidential client on mobile
#endif
    public sealed partial class ManagedIdentityApplication
        : ClientApplicationBase,
            IManagedIdentityApplication
    {
        internal ManagedIdentityApplication(
            ApplicationConfiguration configuration)
            : base(configuration)
        {
            GuardMobileFrameworks();

            AppTokenCacheInternal = configuration.AppTokenCacheInternalForTest ?? new TokenCache(ServiceBundle, true);

            this.ServiceBundle.ApplicationLogger.Verbose(()=>$"ManagedIdentityApplication {configuration.GetHashCode()} created");
        }

        /// <summary>
        /// Application token cache. This case holds access tokens for the application. It's maintained
        /// and updated silently if needed when calling <see cref="AcquireTokenForManagedIdentity(string)"/>
        /// </summary>
        /// <remarks>On .NET Framework and .NET Core you can also customize the token cache serialization.
        /// See https://aka.ms/msal-net-token-cache-serialization. This is taken care of by MSAL.NET on other platforms
        /// </remarks>
        public ITokenCache AppTokenCache => AppTokenCacheInternal;

        // Stores all app tokens
        internal ITokenCacheInternal AppTokenCacheInternal { get; }

        /// <summary>
        /// Acquires token for managed identity for the specified resource.
        /// See https://aka.ms/msal-net-managed-identity.
        /// </summary>
        /// <param name="resource">resource requested to access a protected API. 
        /// For managed identity the resource should be of the form "{ResourceIdUri}" or "{ResourceIdUri/.default}" for instance
        /// <c>https://management.azure.net</c>, <c>https://graph.microsoft.com/.default</c></param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request</returns>
        public AcquireTokenForManagedIdentityParameterBuilder AcquireTokenForManagedIdentity(string resource)
        {
            return AcquireTokenForManagedIdentityParameterBuilder.Create(
                ClientExecutorFactory.CreateManagedIdentityExecutor(this),
                resource);
        }

        internal override async Task<AuthenticationRequestParameters> CreateRequestParametersAsync(
            AcquireTokenCommonParameters commonParameters,
            RequestContext requestContext,
            ITokenCacheInternal cache)
        {
            AuthenticationRequestParameters requestParams = await base.CreateRequestParametersAsync(commonParameters, requestContext, cache).ConfigureAwait(false);
            return requestParams;
        }

        ///<inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override Task<IEnumerable<IAccount>> GetAccountsAsync()
        {
            throw new NotImplementedException();
        }

        ///<inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override Task<IEnumerable<IAccount>> GetAccountsAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        ///<inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override Task<IEnumerable<IAccount>> GetAccountsAsync(string userFlow)
        {
            throw new NotImplementedException();
        }

        ///<inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override Task<IEnumerable<IAccount>> GetAccountsAsync(string userFlow, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        ///<inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override Task<IAccount> GetAccountAsync(string accountId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        ///<inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override Task<IAccount> GetAccountAsync(string accountId)
        {
            throw new NotImplementedException();
        }

        ///<inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override Task RemoveAsync(IAccount account)
        {
            throw new NotImplementedException();
        }

        ///<inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override Task RemoveAsync(IAccount account, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        ///<inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override AcquireTokenSilentParameterBuilder AcquireTokenSilent(IEnumerable<string> scopes, IAccount account)
        {
            throw new NotImplementedException();
        }

        ///<inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override AcquireTokenSilentParameterBuilder AcquireTokenSilent(IEnumerable<string> scopes, string loginHint)
        {
            throw new NotImplementedException();
        }
    }
}
