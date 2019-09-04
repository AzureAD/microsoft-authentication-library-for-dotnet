// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Extensions.Msal.Providers.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Client.Extensions.Msal.Providers
{
    [TestClass]
    public class ChainedTokenProviderTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
        }

        [TestMethod]
        [TestCategory("ChainedTokenProviderTests")]
        public async Task SelectTheFirstAvailableProviderTestAsync()
        {
            var providers = new List<ITokenProvider>
            {
                new MockProvider{ Available = false },
                new MockProvider{ Available = false },
                new MockProvider{
                    Available = true,
                    Token = new MockToken{ AccessToken = "foo", ExpiresOn = DateTime.UtcNow.AddSeconds(60)}
                },
                new MockProvider
                {
                    Available = true,
                    Token = new MockToken{ AccessToken = "bar", ExpiresOn = DateTime.UtcNow.AddSeconds(60)}
                }
            };
            var chain = new TokenProviderChain(providers);

            var token = await chain.GetTokenAsync(new List<string> { "something" }).ConfigureAwait(false);
            Assert.AreEqual("foo", token.AccessToken);
        }

        [TestMethod]
        [TestCategory("ChainedTokenProviderTests")]
        public async Task NoAvailableProbesTestAsync()
        {
            var providers = new List<ITokenProvider>
            {
                new MockProvider{ Available = false, Token = new MockToken{} },
            };
            var chain = new TokenProviderChain(providers);

            await Assert.ThrowsExceptionAsync<NoProvidersAvailableException>(async () => await chain.GetTokenAsync(new List<string> { "something" })
                .ConfigureAwait(false)).ConfigureAwait(false);
        }
    }

    public class MockProvider : ITokenProvider
    {
        public IToken Token { get; set; }

        public bool Available { get; set; }


        public Task<bool> IsAvailableAsync(CancellationToken c = default) => Task.FromResult(Available);

        public Task<IToken> GetTokenAsync(IEnumerable<string> scopes, CancellationToken c = default)
        {
           return Task.FromResult(Token);
        }

        public Task<IToken> GetTokenWithResourceUriAsync(string resourceUri, CancellationToken c = default)
        {
            return Task.FromResult(Token);
        }
    }

    public class MockToken : IToken
    {
        public DateTimeOffset? ExpiresOn { get; set; }

        public string AccessToken { get; set; }
    }
}
