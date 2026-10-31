// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Identity.Client.Cache;
using JToken = System.Text.Json.Nodes.JsonNode;

namespace Microsoft.Identity.Client
{
    public sealed partial class TokenCache : ITokenCacheInternal, ITokenCacheSnapshotProvider
    {
        // Unknown token cache data support for forwards compatibility.
        private IDictionary<string, JToken> _unknownNodes;
        private static readonly AsyncLocal<ReadHydrationScope> s_readHydration = new AsyncLocal<ReadHydrationScope>();
        private ReadHydrationScope _activeReadHydration;
        private const string ReadIsolationOptOutHint = " Set AppContext switch '" +
            DisableCacheReadIsolationSwitch + "' to true before constructing the application to restore the previous cache behavior.";

        TokenCacheSnapshot ITokenCacheSnapshotProvider.CreateSnapshot()
        {
            Validate();
            ValidateSerializationTarget();
            return new TokenCacheSnapshot(Accessor, _unknownNodes, IsAppTokenCache);
        }

        void ITokenCacheSnapshotProvider.RestoreSnapshot(TokenCacheSnapshot snapshot)
        {
            Validate();
            ValidateCompleteHydration(clearsExistingState: true);
            if (snapshot is null)
            {
                Accessor.Clear();
                RecordCompleteHydration();
                return;
            }
            if (snapshot.IsApplicationCache != IsAppTokenCache)
            {
                throw new System.ArgumentException("The snapshot belongs to a different cache type.", nameof(snapshot));
            }
            _unknownNodes = snapshot.Restore(Accessor);
            RecordCompleteHydration();
        }

        byte[] ITokenCacheSerializer.SerializeAdalV3()
        {
            ValidateSerializationTarget();
            return LegacyCachePersistence.LoadCache();
        }

        void ITokenCacheSerializer.DeserializeAdalV3(byte[] adalV3State)
        {
            ValidateCompleteHydration(clearsExistingState: false);
            LegacyCachePersistence.WriteCache(adalV3State);
        }

        byte[] ITokenCacheSerializer.SerializeMsalV2()
        {
            ValidateSerializationTarget();
            return new TokenCacheDictionarySerializer(Accessor).Serialize(_unknownNodes);
        }

        void ITokenCacheSerializer.DeserializeMsalV2(byte[] msalV2State)
        {
            ValidateCompleteHydration(clearsExistingState: false);
            _unknownNodes = new TokenCacheDictionarySerializer(Accessor).Deserialize(msalV2State, false);
        }

        byte[] ITokenCacheSerializer.SerializeMsalV3()
        {
            ValidateSerializationTarget();
            return new TokenCacheJsonSerializer(Accessor).Serialize(_unknownNodes);
        }

        void ITokenCacheSerializer.DeserializeMsalV3(byte[] msalV3State, bool shouldClearExistingCache)
        {
            ValidateCompleteHydration(shouldClearExistingCache);
            if (msalV3State == null || msalV3State.Length == 0)
            {
                if (shouldClearExistingCache)
                {
                    Accessor.Clear();
                }
                RecordCompleteHydration();
                return;
            }
            _unknownNodes = new TokenCacheJsonSerializer(Accessor).Deserialize(msalV3State, shouldClearExistingCache);
            RecordCompleteHydration();
        }

        internal TokenCache CreateReadView()
        {
            return new TokenCache(ServiceBundle, IsAppTokenCache, isReadView: true)
            {
                BeforeAccess = BeforeAccess,
                AfterAccess = AfterAccess,
                BeforeWrite = BeforeWrite,
                AsyncBeforeAccess = AsyncBeforeAccess,
                AsyncAfterAccess = AsyncAfterAccess,
                AsyncBeforeWrite = AsyncBeforeWrite
            };
        }

        internal IDisposable BeginReadHydration(ITokenCacheInternal source)
        {
            var scope = new ReadHydrationScope(this, source, s_readHydration.Value);
            Volatile.Write(ref _activeReadHydration, scope);
            s_readHydration.Value = scope;
            return scope;
        }

        private void ValidateSerializationTarget()
        {
            // Nested callbacks must not bypass an outer operation's source-cache guard.
            for (var scope = s_readHydration.Value; scope != null; scope = scope.Previous)
            {
                if (ReferenceEquals(scope.Source, this))
                {
                    throw new NotSupportedException("Isolated cache serializer accessed a captured/shared cache instead of args.TokenCache." + ReadIsolationOptOutHint);
                }
            }
        }

        private void ValidateCompleteHydration(bool clearsExistingState)
        {
            ValidateSerializationTarget();
            if (Volatile.Read(ref _activeReadHydration) != null && !clearsExistingState)
            {
                throw new NotSupportedException("Isolated cache reads require complete MSALv3 hydration with clear-existing enabled." + ReadIsolationOptOutHint);
            }
        }

        private void RecordCompleteHydration()
        {
            var scope = Volatile.Read(ref _activeReadHydration);
            if (scope != null)
            {
                Volatile.Write(ref scope.Hydrated, true);
            }
        }

        internal void ValidateReadHydration()
        {
            var scope = Volatile.Read(ref _activeReadHydration);
            if (scope != null && !Volatile.Read(ref scope.Hydrated))
            {
                throw new NotSupportedException("Isolated cache reads require complete MSALv3 or structured snapshot hydration in each BeforeAccess callback." + ReadIsolationOptOutHint);
            }
        }

        private sealed class ReadHydrationScope : IDisposable
        {
            internal TokenCache Target;
            internal ITokenCacheInternal Source;
            internal readonly ReadHydrationScope Previous;
            internal bool Hydrated;

            internal ReadHydrationScope(TokenCache target, ITokenCacheInternal source, ReadHydrationScope previous)
            {
                Target = target;
                Source = source;
                Previous = previous;
            }

            public void Dispose()
            {
                // Providers can capture this execution context for work that outlives hydration.
                var target = Target;
                if (target != null)
                    Interlocked.CompareExchange(ref target._activeReadHydration, null, this);
                Target = null;
                Source = null;
                s_readHydration.Value = Previous;
            }
        }

    }
}
