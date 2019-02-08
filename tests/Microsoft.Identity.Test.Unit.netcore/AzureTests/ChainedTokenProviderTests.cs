// ------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Azure;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.netcore.AzureTests
{
    [TestClass]
    public class ChainedTokenProviderTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
        }

        private void AddHostToInstanceCache(IServiceBundle serviceBundle, string host)
        {
            serviceBundle.AadInstanceDiscovery.TryAddValue(
                host,
                new InstanceDiscoveryMetadataEntry
                {
                    PreferredNetwork = host,
                    PreferredCache = host,
                    Aliases = new string[]
                    {
                        host
                    }
                });
        }

        [TestMethod]
        [TestCategory("ChainedTokenProviderTests")]
        public async Task SelectTheFirstAvailableProbeTestAsync()
        {
            var probes = new List<IProbe>
            {
                new MockProbe{ Available = false, Provider = new MockProvider() },
                new MockProbe{ Available = false, Provider = new MockProvider() },
                new MockProbe{ Available = true, Provider = new MockProvider{ Token = new MockToken{ AccessToken = "foo", ExpiresOn = DateTime.UtcNow.AddSeconds(60)} } },
                new MockProbe{ Available = true, Provider = new MockProvider{ Token = new MockToken{ AccessToken = "bar", ExpiresOn = DateTime.UtcNow.AddSeconds(60)} } },
            };
            var chain = new ChainedTokenProvider(probes);

            var token = await chain.GetTokenAsync(new List<string> { "something" }).ConfigureAwait(false);
            Assert.AreEqual("foo", token.AccessToken);
        }

        [TestMethod]
        [TestCategory("ChainedTokenProviderTests")]
        public async Task NoAvailableProbesTestAsync()
        {
            var probes = new List<IProbe>
            {
                new MockProbe{ Available = false, Provider = new MockProvider() },
            };
            var chain = new ChainedTokenProvider(probes);

            await Assert.ThrowsExceptionAsync<NoProbesAvailableException>(async () => await chain.GetTokenAsync(new List<string> { "something" }).ConfigureAwait(false)).ConfigureAwait(false);
        }
    }
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public class MockProbe : IProbe
    {
        public ITokenProvider Provider { get; set; }

        public bool Available { get; set; }


        public async Task<bool> AvailableAsync() => Available;


        public async Task<ITokenProvider> ProviderAsync() => Provider;
    }

    public class MockProvider : ITokenProvider
    {
        public IToken Token { get; set; }

        public async Task<IToken> GetTokenAsync(IEnumerable<string> scopes = null) => Token;
    }

    public class MockToken : IToken
    {
        public DateTimeOffset? ExpiresOn { get; set; }

        public string AccessToken { get; set; }
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}
