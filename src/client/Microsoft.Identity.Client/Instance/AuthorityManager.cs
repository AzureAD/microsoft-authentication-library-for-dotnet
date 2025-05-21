﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Instance.Validation;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.Utils;
using static Microsoft.Identity.Client.AuthorityInfo;

namespace Microsoft.Identity.Client.Instance
{
    /// <summary>
    /// This object is at REQUEST level.
    /// </summary>
    internal class AuthorityManager
    {
        private static readonly ConcurrentHashSet<string> s_validatedEnvironments =
            new ConcurrentHashSet<string>();

        private readonly RequestContext _requestContext;

        private readonly Authority _initialAuthority;
        private Authority _currentAuthority;

        bool _instanceDiscoveryAndValidationExecuted = false;

        public AuthorityManager(RequestContext requestContext, Authority initialAuthority)
        {
            _requestContext = requestContext;

            _initialAuthority = initialAuthority;
            _currentAuthority = initialAuthority;
        }

        public Authority OriginalAuthority => _initialAuthority;

        public Authority Authority => _currentAuthority;

        private InstanceDiscoveryMetadataEntry _metadata;
        public async Task<InstanceDiscoveryMetadataEntry> GetInstanceDiscoveryEntryAsync()
        {
            await RunInstanceDiscoveryAndValidationAsync().ConfigureAwait(false);
            return _metadata;
        }

        public async Task RunInstanceDiscoveryAndValidationAsync()
        {
            if (DesktopOsHelper.IsMac() && _requestContext.ServiceBundle.Config.IsBrokerEnabled)
            {
                // On macOS, for broker flows we should avoid authority validation. Internally, we should avoid any network requests to prevent context switch.
                // Interactive calls need to happen in the main thread, making network calls will switch to another thread and not easy to go back.
                _metadata = _requestContext.ServiceBundle.InstanceDiscoveryManager.GetMetadataEntryAvoidNetwork(
                                _initialAuthority.AuthorityInfo,
                                _requestContext);
            }
            else if (!_instanceDiscoveryAndValidationExecuted)
            {
                // This will make a network call unless instance discovery is cached, but this OK
                // GetAccounts and AcquireTokenSilent do not need this
                _metadata = await
                                _requestContext.ServiceBundle.InstanceDiscoveryManager.GetMetadataEntryAsync(
                                    _initialAuthority.AuthorityInfo,
                                    _requestContext)
                                .ConfigureAwait(false);

                _currentAuthority = Authority.CreateAuthorityWithEnvironment(
                                    _initialAuthority.AuthorityInfo,
                                    _metadata.PreferredNetwork);

                // We can only validate the initial environment, not regional environments
                if (_initialAuthority.AuthorityInfo.ValidateAuthority &&
                    _requestContext.ServiceBundle.Config.IsInstanceDiscoveryEnabled)
                {
                    await ValidateAuthorityAsync(_initialAuthority).ConfigureAwait(false);
                }

                _instanceDiscoveryAndValidationExecuted = true;
            }
        }

        public static /* for test */ void ClearValidationCache()
        {
            s_validatedEnvironments.Clear();
        }

        private async Task ValidateAuthorityAsync(Authority authority)
        {
            // race conditions could occur here, where multiple requests validate the authority at the same time
            // but this is acceptable and once the cache is filled, no more HTTP requests will be made
            if (
                !s_validatedEnvironments.Contains(authority.AuthorityInfo.Host))
            {
                // validate the original authority, as the resolved authority might be regionalized and we cannot validate regionalized authorities.
                var validator = AuthorityInfoHelper.CreateAuthorityValidator(authority.AuthorityInfo, _requestContext);
                await validator.ValidateAuthorityAsync(authority.AuthorityInfo).ConfigureAwait(false);

                s_validatedEnvironments.Add(authority.AuthorityInfo.Host);
            }
        }
    }
}
