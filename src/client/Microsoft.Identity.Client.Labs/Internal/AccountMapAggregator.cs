// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace Microsoft.Identity.Client.Labs.Internal
{
    /// <summary>
    /// Aggregates username maps from multiple providers and applies the password-secret selection policy.
    /// </summary>
    internal sealed class AccountMapAggregator
    {
        private readonly Dictionary<(AuthType, CloudType, Scenario), string> _usernames;
        private readonly LabsOptions _opt;

        public AccountMapAggregator(IEnumerable<IAccountMapProvider> providers, IOptions<LabsOptions> opt)
        {
            _opt = opt?.Value ?? throw new ArgumentNullException(nameof(opt));
            _usernames = new();

            foreach (var p in providers)
            {
                var map = p.GetUsernameMap();
                if (map is null)
                    continue;

                foreach (var kv in map)
                {
                    // last registered wins
                    _usernames[kv.Key] = kv.Value;
                }
            }
        }

        public string GetUsernameSecret(AuthType a, CloudType c, Scenario s)
        {
            if (_usernames.TryGetValue((a, c, s), out var name))
                return name;

            if (!_opt.EnableConventionFallback)
                throw new KeyNotFoundException($"No username secret mapping for ({a},{c},{s}).");

            return $"cld_{a.ToString().ToLowerInvariant()}_{c.ToString().ToLowerInvariant()}_{s.ToString().ToLowerInvariant()}_uname";
        }

        public string GetPasswordSecret(AuthType a, CloudType c, Scenario s)
        {
            // tuple override
            var tupleKey = $"{a}.{c}.{s}".ToLowerInvariant();
            if (_opt.PasswordSecretByTuple.TryGetValue(tupleKey, out var tupleSecret) &&
                !string.IsNullOrWhiteSpace(tupleSecret))
            {
                return tupleSecret;
            }

            // cloud override
            if (_opt.PasswordSecretByCloud.TryGetValue(c, out var cloudSecret) &&
                !string.IsNullOrWhiteSpace(cloudSecret))
            {
                return cloudSecret;
            }

            // global
            if (!string.IsNullOrWhiteSpace(_opt.GlobalPasswordSecret))
            {
                return _opt.GlobalPasswordSecret;
            }

            // convention
            if (_opt.EnableConventionFallback)
            {
                return $"cld_{a.ToString().ToLowerInvariant()}_{c.ToString().ToLowerInvariant()}_{s.ToString().ToLowerInvariant()}_pwd";
            }

            throw new KeyNotFoundException($"No password secret configured for ({a},{c},{s}).");
        }
    }
}
