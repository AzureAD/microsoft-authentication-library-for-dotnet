// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Internal.Requests;

namespace Microsoft.Identity.Client.AuthScheme
{
    /// <summary>
    /// Internal factory to resolve the effective IAuthenticationOperation at request execution time.
    /// </summary>
    internal interface IAuthenticationOperationFactory
    {
        IAuthenticationOperation Create(AuthenticationRequestParameters context);
    }

    /// <summary>
    /// Wraps an already determined operation (no-op factory).
    /// </summary>
    internal sealed class StaticAuthenticationOperationFactory : IAuthenticationOperationFactory
    {
        private readonly IAuthenticationOperation _operation;
        public StaticAuthenticationOperationFactory(IAuthenticationOperation operation) =>
            _operation = operation;
        public IAuthenticationOperation Create(AuthenticationRequestParameters context) => _operation;
    }
}
