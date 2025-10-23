// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.ManagedIdentity.V2.Cache.Abstractions
{
    internal readonly struct MiCacheKey : IEquatable<MiCacheKey>
    {
        public Guid TenantId { get; }
        public Guid ManagedIdentityId { get; }
        public string TokenType { get; }   // always stored lowercase

        public MiCacheKey(Guid tenantId, Guid managedIdentityId, string tokenType)
        {
            TenantId = tenantId;
            ManagedIdentityId = managedIdentityId;
            // normalize once
            TokenType = string.IsNullOrEmpty(tokenType)
                ? string.Empty
                : tokenType.Trim().ToLowerInvariant();
        }

        public static MiCacheKey FromStrings(string tenantId, string managedIdentityId, string tokenType) =>
            new MiCacheKey(Guid.Parse(tenantId), Guid.Parse(managedIdentityId),
                string.IsNullOrEmpty(tokenType) ? string.Empty : tokenType.Trim().ToLowerInvariant());

        public bool Equals(MiCacheKey other) =>
            TenantId.Equals(other.TenantId) &&
            ManagedIdentityId.Equals(other.ManagedIdentityId) &&
            StringComparer.OrdinalIgnoreCase.Equals(TokenType, other.TokenType);

        public override bool Equals(object obj) => obj is MiCacheKey k && Equals(k);

        public override int GetHashCode()
        {
            unchecked
            {
                int h = TenantId.GetHashCode();
                h = (h * 397) ^ ManagedIdentityId.GetHashCode();
                h = (h * 397) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(TokenType ?? string.Empty);
                return h;
            }
        }

        public override string ToString() => $"Tenant={TenantId};MI={ManagedIdentityId};Type={TokenType}";
    }
}
