// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Executors;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.AuthScheme;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Abstract base class for managed identity application token request builders.
    /// </summary>
    /// <typeparam name="T"></typeparam>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide managed identity flow on mobile
#endif
    public abstract class AbstractManagedIdentityAcquireTokenParameterBuilder<T> : BaseAbstractAcquireTokenParameterBuilder<T>
        where T : BaseAbstractAcquireTokenParameterBuilder<T>
    {
        /// <summary>
        /// Default constructor for AbstractManagedIdentityParameterBuilder.
        /// </summary>
        protected AbstractManagedIdentityAcquireTokenParameterBuilder() : base() { }

        internal AbstractManagedIdentityAcquireTokenParameterBuilder(IManagedIdentityApplicationExecutor managedIdentityApplicationExecutor) : 
            base(managedIdentityApplicationExecutor.ServiceBundle) 
        {
            ClientApplicationBase.GuardMobileFrameworks();
            ManagedIdentityApplicationExecutor = managedIdentityApplicationExecutor;
        }

        internal IManagedIdentityApplicationExecutor ManagedIdentityApplicationExecutor { get; }

        internal abstract Task<AuthenticationResult> ExecuteInternalAsync(CancellationToken cancellationToken);

        /// <inheritdoc/>
        public override Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            ApplicationBase.GuardMobileFrameworks();
            ValidateAndCalculateApiId();
            return ExecuteInternalAsync(cancellationToken);
        }
    }
}
