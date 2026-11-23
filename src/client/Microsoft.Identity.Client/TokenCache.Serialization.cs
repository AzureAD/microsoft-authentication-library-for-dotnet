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
        private readonly object _publicationGate = new object();
        private static readonly AsyncLocal<ReadHydrationScope> s_readHydration = new AsyncLocal<ReadHydrationScope>();
        private ReadHydrationScope _activeReadHydration;
        private const string ReadIsolationOptOutHint = " Set AppContext switch '" +
            DisableCacheReadIsolationSwitch + "' to true before constructing the application to restore the previous cache behavior.";

        TokenCacheSnapshot ITokenCacheSnapshotProvider.CreateSnapshot()
        {
            Validate();
            ValidateSerializationTarget();
            lock (_publicationGate)
                return new TokenCacheSnapshot(Accessor, _unknownNodes, IsAppTokenCache);
        }

        void ITokenCacheSnapshotProvider.RestoreSnapshot(TokenCacheSnapshot snapshot)
        {
            Validate();
            ValidateSerializationTarget();
            lock (_publicationGate)
            {
                if (snapshot is null)
                {
                    Accessor.Clear();
                    if (UseIsolatedCacheReads || Volatile.Read(ref _activeReadHydration) != null)
                        _unknownNodes = null;
                }
                else
                {
                    if (snapshot.IsApplicationCache != IsAppTokenCache)
                        throw new System.ArgumentException("The snapshot belongs to a different cache type.", nameof(snapshot));
                    _unknownNodes = snapshot.Restore(Accessor);
                }
                RecordCompleteHydration(replaceExisting: true, replaceUnknown: true);
            }
        }

        byte[] ITokenCacheSerializer.SerializeAdalV3()
        {
            ValidateSerializationTarget();
            lock (_publicationGate)
                return LegacyCachePersistence.LoadCache();
        }

        void ITokenCacheSerializer.DeserializeAdalV3(byte[] adalV3State)
        {
            ValidateLegacyDeserialization();
            lock (_publicationGate)
                LegacyCachePersistence.WriteCache(adalV3State);
        }

        byte[] ITokenCacheSerializer.SerializeMsalV2()
        {
            ValidateSerializationTarget();
            lock (_publicationGate)
                return new TokenCacheDictionarySerializer(Accessor).Serialize(_unknownNodes);
        }

        void ITokenCacheSerializer.DeserializeMsalV2(byte[] msalV2State)
        {
            ValidateLegacyDeserialization();
            lock (_publicationGate)
                _unknownNodes = new TokenCacheDictionarySerializer(Accessor).Deserialize(msalV2State, false);
        }

        byte[] ITokenCacheSerializer.SerializeMsalV3()
        {
            ValidateSerializationTarget();
            lock (_publicationGate)
                return new TokenCacheJsonSerializer(Accessor).Serialize(_unknownNodes);
        }

        void ITokenCacheSerializer.DeserializeMsalV3(byte[] msalV3State, bool shouldClearExistingCache)
        {
            ValidateSerializationTarget();
            lock (_publicationGate)
            {
                if (msalV3State == null || msalV3State.Length == 0)
                {
                    if (shouldClearExistingCache)
                    {
                        Accessor.Clear();
                        if (UseIsolatedCacheReads || Volatile.Read(ref _activeReadHydration) != null)
                            _unknownNodes = null;
                    }
                    RecordCompleteHydration(shouldClearExistingCache, shouldClearExistingCache);
                    return;
                }
                _unknownNodes = new TokenCacheJsonSerializer(Accessor).Deserialize(msalV3State, shouldClearExistingCache);
                RecordCompleteHydration(shouldClearExistingCache, replaceUnknown: true);
            }
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
            // Failed callbacks can leave partial entries and unknown JSON in this operation's target.
            Accessor.Clear();
            _unknownNodes = null;
            var scope = new ReadHydrationScope(this, source, s_readHydration.Value);
            Volatile.Write(ref _activeReadHydration, scope);
            s_readHydration.Value = scope;
            return scope;
        }

        internal void PublishReadView(TokenCache root)
        {
            var scope = Volatile.Read(ref _activeReadHydration);
            ValidateReadHydration();
            CopyDecodedStateTo(root, scope.ReplaceExisting, scope.ReplaceUnknown);
        }

        internal TokenCache CreatePinnedReadView()
        {
            var view = CreateReadView();
            lock (_publicationGate)
                CopyDecodedStateTo(view, replaceExisting: true, replaceUnknown: true);
            return view;
        }

        private void CopyDecodedStateTo(TokenCache target, bool replaceExisting, bool replaceUnknown)
        {
            // Clone once before touching the destination. Immutable credential strings are shared,
            // but mutable wrappers, nested account/claim state and unknown JSON remain independent.
            var accessTokens = Accessor.GetAllAccessTokens();
            for (int i = 0; i < accessTokens.Count; i++)
                accessTokens[i] = accessTokens[i].CloneForSnapshot();
            var refreshTokens = Accessor.GetAllRefreshTokens();
            for (int i = 0; i < refreshTokens.Count; i++)
                refreshTokens[i] = refreshTokens[i].CloneForSnapshot();
            var idTokens = Accessor.GetAllIdTokens();
            for (int i = 0; i < idTokens.Count; i++)
                idTokens[i] = idTokens[i].CloneForSnapshot();
            var accounts = Accessor.GetAllAccounts();
            for (int i = 0; i < accounts.Count; i++)
                accounts[i] = accounts[i].CloneForSnapshot();
            var metadata = Accessor.GetAllAppMetadata();
            for (int i = 0; i < metadata.Count; i++)
                metadata[i] = metadata[i].CloneForSnapshot();
            var unknownNodes = replaceUnknown ? TokenCacheSnapshot.CloneUnknownNodes(_unknownNodes) : null;

            // Only decoded publication is coordinated here, never provider I/O. The caller's
            // original-root lease excludes writers/removals for the entire read lifecycle.
            lock (target._publicationGate)
            {
                if (replaceExisting)
                    target.Accessor.Clear();
                foreach (var item in accessTokens)
                    target.Accessor.SaveAccessToken(item);
                foreach (var item in refreshTokens)
                    target.Accessor.SaveRefreshToken(item);
                foreach (var item in idTokens)
                    target.Accessor.SaveIdToken(item);
                foreach (var item in accounts)
                    target.Accessor.SaveAccount(item);
                foreach (var item in metadata)
                    target.Accessor.SaveAppMetadata(item);
                if (replaceUnknown)
                    target._unknownNodes = unknownNodes;
            }
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

        private void ValidateLegacyDeserialization()
        {
            ValidateSerializationTarget();
            if (Volatile.Read(ref _activeReadHydration) != null)
            {
                throw new NotSupportedException("Isolated cache reads require MSALv3 or structured snapshot hydration." + ReadIsolationOptOutHint);
            }
        }

        private void RecordCompleteHydration(bool replaceExisting, bool replaceUnknown)
        {
            var scope = Volatile.Read(ref _activeReadHydration);
            if (scope != null)
            {
                scope.ReplaceExisting |= replaceExisting;
                scope.ReplaceUnknown |= replaceUnknown;
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
            internal bool ReplaceExisting;
            internal bool ReplaceUnknown;

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
