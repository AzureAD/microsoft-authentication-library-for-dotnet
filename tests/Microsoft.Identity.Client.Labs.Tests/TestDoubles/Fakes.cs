// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Labs;
using Microsoft.Identity.Client.Labs.Internal;

namespace Microsoft.Identity.Client.Labs.Tests.TestDoubles
{
    internal sealed class FakeAccountMapProvider : IAccountMapProvider
    {
        private readonly IReadOnlyDictionary<(AuthType, CloudType, Scenario), string> _map;

        public FakeAccountMapProvider(IReadOnlyDictionary<(AuthType, CloudType, Scenario), string> map)
            => _map = map;

        public IReadOnlyDictionary<(AuthType auth, CloudType cloud, Scenario scenario), string> GetUsernameMap() => _map;
    }

    internal sealed class FakeAppMapProvider : IAppMapProvider
    {
        private readonly IReadOnlyDictionary<(CloudType, Scenario, AppKind), AppSecretKeys> _map;

        public FakeAppMapProvider(IReadOnlyDictionary<(CloudType, Scenario, AppKind), AppSecretKeys> map)
            => _map = map;

        public IReadOnlyDictionary<(CloudType cloud, Scenario scenario, AppKind kind), AppSecretKeys> GetAppMap() => _map;
    }

    internal sealed class FakeSecretStore : ISecretStore
    {
        private readonly Dictionary<string, string> _secrets = new();

        public FakeSecretStore(IDictionary<string, string>? initial = null)
        {
            if (initial != null)
            {
                foreach (var kv in initial)
                    _secrets[kv.Key] = kv.Value;
            }
        }

        public Task<string> GetAsync(string secretName, CancellationToken ct = default)
            => Task.FromResult(_secrets.TryGetValue(secretName, out var v) ? v : string.Empty);

        public void Set(string name, string value) => _secrets[name] = value;
    }
}
