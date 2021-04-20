// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Instance
{
    internal class AuthorityManager
    {
        private readonly RequestContext _requestContext;

        private readonly Authority _initialAuthority;
        private Authority _currentAuthority;

        bool _instanceDiscoveryAndValidationExecuted = false;
      
        public /* for test */ AuthorityManager(RequestContext requestContext, Authority initialAuthority)
        {
            _requestContext = requestContext;

            _initialAuthority = initialAuthority;
            _currentAuthority = initialAuthority;
        }

        public Authority OriginalAuthority => _initialAuthority;

        public Authority Authority => _currentAuthority;

        public async Task RunInstanceDiscoveryAndValidationAsync()
        {
            if (!_instanceDiscoveryAndValidationExecuted)
            {
                // This will make a network call unless instance discovery is cached, but this ok
                // GetAccounts and AcquireTokenSilent do not need this
                InstanceDiscoveryMetadataEntry metadata = await
                                _requestContext.ServiceBundle.InstanceDiscoveryManager.GetMetadataEntryAsync(
                                    _initialAuthority.AuthorityInfo,
                                    _requestContext)
                                .ConfigureAwait(false);

                _currentAuthority = Authority.CreateAuthorityWithEnvironment(
                                    _initialAuthority.AuthorityInfo,
                                    metadata.PreferredNetwork);

                // We can only validate the initial environment, not regional environments
                await _requestContext.ServiceBundle.AuthorityEndpointResolutionManager.ValidateAuthorityAsync(
                    _initialAuthority, 
                    _requestContext).ConfigureAwait(false);

                _instanceDiscoveryAndValidationExecuted = true;
            }
        }

        public AuthorityEndpoints GetEndpoints(string loginHint)
        {
            if (!_instanceDiscoveryAndValidationExecuted)
            {
                throw new InvalidOperationException("RunInstanceDiscoveryAndValidationAsync must be called first");
            }

            return _requestContext.ServiceBundle.AuthorityEndpointResolutionManager.ResolveEndpoints(
                _currentAuthority,
                loginHint,
                _requestContext);
        }
    }
}
