// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Foundation;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Client.Platforms.Android;
using Microsoft.Identity.Client.Platforms.iOS;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Security;

namespace Microsoft.Identity.Test.Unit.CacheTests
{
    [TestClass]
    [DoNotParallelize]
    public class NativeCacheConcurrencyTests
    {
        private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

        [TestCleanup]
        public void Cleanup() => SecKeyChain.Reset();

        [TestMethod]
        [DataRow(false, "accessToken", false)]
        [DataRow(false, "refreshToken", false)]
        [DataRow(false, "idToken", false)]
        [DataRow(false, "accessToken", true)]
        [DataRow(false, "refreshToken", true)]
        [DataRow(false, "idToken", true)]
        [DataRow(true, "accessToken", false)]
        [DataRow(true, "refreshToken", false)]
        [DataRow(true, "idToken", false)]
        [DataRow(true, "accessToken", true)]
        [DataRow(true, "refreshToken", true)]
        [DataRow(true, "idToken", true)]
        public async Task LegacyCleanup_PreservesConcurrentLowercaseSave(bool ios, string type, bool withClientInfo)
        {
            // Arrange
            var store = CreateStore(ios, type);
            var first = store.CreateAccessor();
            var second = store.CreateAccessor();
            var oldUpper = CreateItem(type, "HighClient", "old", withClientInfo);
            var newUpper = CreateItem(type, "HighClient", "upper", withClientInfo);
            var lower = CreateItem(type, "highclient", "lower", withClientInfo);
            store.SeedLegacy(oldUpper);

            // Act
            await RunInterleavedAsync(store, store.Key(oldUpper, legacy: true),
                () => Save(first, newUpper), () => Save(second, lower)).ConfigureAwait(false);

            // Assert
            Assert.AreEqual("upper", Secret(store.Read(newUpper)));
            Assert.AreEqual("lower", Secret(store.Read(lower)));
        }

        [TestMethod]
        [DataRow(false, "accessToken", false)]
        [DataRow(false, "refreshToken", false)]
        [DataRow(false, "idToken", false)]
        [DataRow(false, "accessToken", true)]
        [DataRow(false, "refreshToken", true)]
        [DataRow(false, "idToken", true)]
        [DataRow(true, "accessToken", false)]
        [DataRow(true, "refreshToken", false)]
        [DataRow(true, "idToken", false)]
        [DataRow(true, "accessToken", true)]
        [DataRow(true, "refreshToken", true)]
        [DataRow(true, "idToken", true)]
        public async Task LegacyDeletion_PreservesConcurrentLowercaseSave(bool ios, string type, bool withClientInfo)
        {
            // Arrange
            var store = CreateStore(ios, type);
            var first = store.CreateAccessor();
            var second = store.CreateAccessor();
            var upper = CreateItem(type, "HighClient", "old", withClientInfo);
            var lower = CreateItem(type, "highclient", "lower", withClientInfo);
            store.SeedLegacy(upper);

            // Act
            await RunInterleavedAsync(store, store.Key(upper, legacy: true),
                () => Delete(first, upper), () => Save(second, lower)).ConfigureAwait(false);

            // Assert
            Assert.IsNull(store.Read(upper));
            Assert.AreEqual("lower", Secret(store.Read(lower)));
        }

        [TestMethod]
        [DataRow("accessToken", false)]
        [DataRow("refreshToken", false)]
        [DataRow("idToken", false)]
        [DataRow("accessToken", true)]
        [DataRow("refreshToken", true)]
        [DataRow("idToken", true)]
        public async Task AndroidPreservation_DoesNotOverwriteConcurrentExactCaseSave(string type, bool withClientInfo)
        {
            // Arrange
            var store = CreateStore(false, type);
            var first = store.CreateAccessor();
            var second = store.CreateAccessor();
            var oldUpper = CreateItem(type, "HighClient", "old", withClientInfo);
            var newUpper = CreateItem(type, "HighClient", "upper", withClientInfo);
            var lower = CreateItem(type, "highclient", "lower", withClientInfo);
            store.SeedLegacy(oldUpper);

            // Act
            await RunInterleavedAsync(store, store.Key(oldUpper),
                () => Save(first, lower), () => Save(second, newUpper)).ConfigureAwait(false);

            // Assert
            Assert.AreEqual("upper", Secret(store.Read(newUpper)));
            Assert.AreEqual("lower", Secret(store.Read(lower)));
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public async Task Clear_DoesNotRaceWithInFlightPreservation(bool ios, bool withClientInfo)
        {
            // Arrange
            var store = CreateStore(ios, "refreshToken");
            var first = store.CreateAccessor();
            var second = store.CreateAccessor();
            var upper = CreateItem("refreshToken", "HighClient", "old", withClientInfo);
            var lower = CreateItem("refreshToken", "highclient", "lower", withClientInfo);
            store.SeedLegacy(upper);

            // Act
            await RunInterleavedAsync(store, store.Key(upper, legacy: true),
                () => Save(first, lower), () => second.Clear()).ConfigureAwait(false);

            // Assert
            Assert.IsNull(store.Read(upper));
            Assert.IsNull(store.Read(lower));
        }

        private static async Task RunInterleavedAsync(CacheStore store, string pauseKey, Action first, Action second)
        {
            using var paused = new ManualResetEventSlim();
            using var resume = new ManualResetEventSlim();
            using var secondStarted = new ManualResetEventSlim();
            int armed = 1;
            store.AfterRead = key =>
            {
                if (key == pauseKey && Interlocked.Exchange(ref armed, 0) == 1)
                {
                    paused.Set();
                    if (!resume.Wait(s_timeout))
                    {
                        throw new TimeoutException("Timed out releasing the paused native-cache operation.");
                    }
                }
            };
            var firstWorker = StartWorker(first);
            (Thread Thread, Task Task)? secondWorker = null;
            try
            {
                Assert.IsTrue(paused.Wait(s_timeout), "The first accessor did not reach the contested read.");
                secondWorker = StartWorker(() =>
                {
                    secondStarted.Set();
                    second();
                });
                Assert.IsTrue(secondStarted.Wait(s_timeout), "The competing accessor did not start.");
                var worker = secondWorker.Value;
                // The competitor either completes (the old race) or blocks on the shared mutation gate.
                // No sleep or elapsed-time assertion decides which operation wins.
                Assert.IsTrue(SpinWait.SpinUntil(() => worker.Task.IsCompleted ||
                    (worker.Thread.ThreadState & ThreadState.WaitSleepJoin) != 0, s_timeout),
                    "The competing accessor neither completed nor blocked.");
            }
            finally
            {
                resume.Set();
                Assert.IsTrue(firstWorker.Thread.Join(s_timeout), "The first accessor did not finish.");
                if (secondWorker.HasValue)
                {
                    Assert.IsTrue(secondWorker.Value.Thread.Join(s_timeout), "The competing accessor did not finish.");
                }
                store.AfterRead = null;
            }
            await firstWorker.Task.ConfigureAwait(false);
            await secondWorker.Value.Task.ConfigureAwait(false);
        }

        private static (Thread Thread, Task Task) StartWorker(Action action)
        {
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                    completion.SetResult(true);
                }
                catch (Exception exception)
                {
                    completion.SetException(exception);
                }
            }) { IsBackground = true };
            thread.Start();
            return (thread, completion.Task);
        }

        private static MsalCredentialCacheItemBase CreateItem(string type, string clientId, string secret, bool withClientInfo)
        {
            string clientInfo = withClientInfo ? MockHelpers.CreateClientInfo() : null;
            var date = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
            return type switch
            {
                "accessToken" => new MsalAccessTokenCacheItem("localhost", clientId, "scope", "tenant", secret,
                    date, date.AddHours(1), date.AddHours(1), clientInfo, "Member"),
                "refreshToken" => new MsalRefreshTokenCacheItem("localhost", clientId, secret, clientInfo, null, "Member"),
                "idToken" => new MsalIdTokenCacheItem("localhost", clientId, secret, clientInfo, "Member", "tenant"),
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }

        private static (string Android, IiOSKey Ios) Keys(MsalCredentialCacheItemBase item) => item switch
        {
            MsalAccessTokenCacheItem token => (token.CacheKey, token.iOSCacheKey),
            MsalRefreshTokenCacheItem token => (token.CacheKey, token.iOSCacheKey),
            MsalIdTokenCacheItem token => (token.CacheKey, token.iOSCacheKey),
            _ => throw new ArgumentOutOfRangeException(nameof(item))
        };

        private static void Save(ITokenCacheAccessor accessor, MsalCredentialCacheItemBase item)
        {
            switch (item)
            {
                case MsalAccessTokenCacheItem token: accessor.SaveAccessToken(token); break;
                case MsalRefreshTokenCacheItem token: accessor.SaveRefreshToken(token); break;
                case MsalIdTokenCacheItem token: accessor.SaveIdToken(token); break;
                default: throw new ArgumentOutOfRangeException(nameof(item));
            }
        }

        private static void Delete(ITokenCacheAccessor accessor, MsalCredentialCacheItemBase item)
        {
            switch (item)
            {
                case MsalAccessTokenCacheItem token: accessor.DeleteAccessToken(token); break;
                case MsalRefreshTokenCacheItem token: accessor.DeleteRefreshToken(token); break;
                case MsalIdTokenCacheItem token: accessor.DeleteIdToken(token); break;
                default: throw new ArgumentOutOfRangeException(nameof(item));
            }
        }

        private static string Secret(string payload) =>
            payload is null ? null : JsonNode.Parse(payload)["secret"].GetValue<string>();

        private static CacheStore CreateStore(bool ios, string type) =>
            ios ? (CacheStore)new IosStore() : new AndroidStore(type);

        private abstract class CacheStore
        {
            internal Action<string> AfterRead { get; set; }
            internal abstract ITokenCacheAccessor CreateAccessor();
            internal abstract string Key(MsalCredentialCacheItemBase item, bool legacy = false);
            internal abstract void SeedLegacy(MsalCredentialCacheItemBase item);
            internal abstract string Read(MsalCredentialCacheItemBase item);
        }

        private sealed class AndroidStore : CacheStore
        {
            private readonly Dictionary<string, Dictionary<string, object>> _stores = new Dictionary<string, Dictionary<string, object>>();
            private readonly object _lock = new object();
            private readonly string _type;

            internal AndroidStore(string type) => _type = type;

            internal override ITokenCacheAccessor CreateAccessor() => new AndroidTokenCacheAccessor(name =>
            {
                if (!_stores.TryGetValue(name, out var values))
                {
                    values = new Dictionary<string, object>(StringComparer.Ordinal);
                    _stores.Add(name, values);
                }
                var preferences = Substitute.For<ISharedPreferences>();
                preferences.All.Returns(_ => { lock (_lock) { return new Dictionary<string, object>(values); } });
                preferences.GetString(Arg.Any<string>(), Arg.Any<string>()).Returns(call =>
                {
                    string key = call.ArgAt<string>(0);
                    string payload;
                    lock (_lock)
                    {
                        payload = values.TryGetValue(key, out var value) ? (string)value : call.ArgAt<string>(1);
                    }
                    AfterRead?.Invoke(key);
                    return payload;
                });
                preferences.Edit().Returns(_ =>
                {
                    var changes = new Dictionary<string, object>();
                    bool clear = false;
                    var editor = Substitute.For<ISharedPreferencesEditor>();
                    editor.PutString(Arg.Any<string>(), Arg.Any<string>()).Returns(call =>
                    {
                        changes[call.ArgAt<string>(0)] = call.ArgAt<string>(1);
                        return editor;
                    });
                    editor.Remove(Arg.Any<string>()).Returns(call => { changes[call.Arg<string>()] = null; return editor; });
                    editor.Clear().Returns(_ => { clear = true; return editor; });
                    editor.When(call => call.Apply()).Do(_ =>
                    {
                        lock (_lock)
                        {
                            if (clear) { values.Clear(); }
                            foreach (var change in changes)
                            {
                                if (change.Value is null) { values.Remove(change.Key); }
                                else { values[change.Key] = change.Value; }
                            }
                        }
                    });
                    return editor;
                });
                return preferences;
            }, null);

            internal override string Key(MsalCredentialCacheItemBase item, bool legacy = false)
            {
                item.IsAndroidCacheItem = true;
                string key = Keys(item).Android;
                if (legacy) { key = key.ToLowerInvariant(); }
                return item.IsAndroidSubject ? item.HomeAccountId.Length + ":" + item.HomeAccountId + ":" + key : key;
            }

            private Dictionary<string, object> Store(MsalCredentialCacheItemBase item) =>
                _stores["com.microsoft.identity.client." + _type + (item.IsAndroidSubject ? ".subject.v1" : "")];

            internal override void SeedLegacy(MsalCredentialCacheItemBase item)
            {
                string key = Key(item, legacy: true);
                Store(item)[key] = item.ToJObject().ToString();
            }

            internal override string Read(MsalCredentialCacheItemBase item)
            {
                string key = Key(item);
                return Store(item).TryGetValue(key, out var value) ? (string)value : null;
            }
        }

        private sealed class IosStore : CacheStore
        {
            internal IosStore()
            {
                SecKeyChain.Reset();
                SecKeyChain.AfterQuery = record => AfterRead?.Invoke(record.Service);
            }

            internal override ITokenCacheAccessor CreateAccessor() => new iOSTokenCacheAccessor();
            internal override string Key(MsalCredentialCacheItemBase item, bool legacy = false) =>
                legacy ? Keys(item).Ios.iOSService.ToLowerInvariant() : Keys(item).Ios.iOSService;

            private static SecRecord Query(MsalCredentialCacheItemBase item) => new SecRecord(SecKind.GenericPassword)
            {
                Account = Keys(item).Ios.iOSAccount,
                Service = Keys(item).Ios.iOSService,
                CreatorType = Keys(item).Ios.iOSType,
                AccessGroup = "TEST.com.microsoft.adalcache"
            };

            internal override void SeedLegacy(MsalCredentialCacheItemBase item)
            {
                var record = Query(item);
                record.Service = Key(item, legacy: true);
                record.ValueData = NSData.FromString(item.ToJObject().ToString(), NSStringEncoding.UTF8);
                Assert.AreEqual(SecStatusCode.Success, SecKeyChain.Add(record));
            }

            internal override string Read(MsalCredentialCacheItemBase item) =>
                SecKeyChain.QueryAsRecord(Query(item), out _)?.ValueData.ToString(NSStringEncoding.UTF8);
        }
    }
}
