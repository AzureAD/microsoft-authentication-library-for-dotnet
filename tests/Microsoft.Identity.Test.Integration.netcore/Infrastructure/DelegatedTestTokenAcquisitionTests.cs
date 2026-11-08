// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.Infrastructure
{
    [TestClass]
    [DoNotParallelize]
    public class DelegatedTestTokenAcquisitionTests
    {
        private string _previousFixture;
        private string _fixturePath;

        [TestInitialize]
        public void Initialize()
        {
            ApplicationBase.ResetStateForTest();
            _previousFixture = Environment.GetEnvironmentVariable("MSAL_V5_DELEGATED_TOKEN_FIXTURE");
            _fixturePath = Path.GetTempFileName();
            Environment.SetEnvironmentVariable("MSAL_V5_DELEGATED_TOKEN_FIXTURE", _fixturePath);
        }

        [TestCleanup]
        public void Cleanup()
        {
            Environment.SetEnvironmentVariable("MSAL_V5_DELEGATED_TOKEN_FIXTURE", _previousFixture);
            File.Delete(_fixturePath);
        }

        [TestMethod]
        public async Task DelegatedSetup_RedeemsRefreshTokensForTwoDistinctUsersAsync()
        {
            // Arrange
            string[] scopes = { "User.Read" };
            string[] users = { "first@contoso.com", "second@contoso.com" };
            File.WriteAllText(_fixturePath, JsonSerializer.Serialize(users.Select((user, index) => new
            {
                ClientId = TestConstants.ClientId,
                Username = user,
                RefreshToken = $"fixture-rt-{index}"
            })));

            using (var httpManager = new MockHttpManager())
            {
                var app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithAuthority(TestConstants.AuthorityTestTenant)
                    .WithHttpManager(httpManager).Build();
                httpManager.AddInstanceDiscoveryMockHandler();
                var accounts = new List<IAccount>();
                for (int index = 0; index < users.Length; index++)
                {
                    var handler = httpManager.AddSuccessTokenResponseMockHandlerForPost(
                        TestConstants.AuthorityTestTenant,
                        bodyParameters: new Dictionary<string, string>
                        {
                            { "grant_type", "refresh_token" },
                            { "refresh_token", $"fixture-rt-{index}" }
                        },
                        responseMessage: MockHelpers.CreateSuccessResponseMessage(
                            MockHelpers.CreateSuccessTokenResponseString($"user-{index}", users[index], scopes)));

                    // Act
                    var result = await app.AcquireDelegatedTokenAsync(scopes, users[index]).ConfigureAwait(false);

                    // Assert
                    Assert.AreEqual(users[index], result.Account.Username);
                    Assert.IsFalse(handler.ActualRequestPostData.ContainsKey("password"));
                    accounts.Add(result.Account);

                    var cachedResult = await app.AcquireTokenSilent(scopes, result.Account)
                        .ExecuteAsync().ConfigureAwait(false);
                    Assert.AreEqual(TokenSource.Cache, cachedResult.AuthenticationResultMetadata.TokenSource);
                    Assert.AreEqual(result.AccessToken, cachedResult.AccessToken);
                }

                Assert.AreNotEqual(accounts[0].HomeAccountId.Identifier, accounts[1].HomeAccountId.Identifier);
                Assert.HasCount(2, (await app.GetAccountsAsync().ConfigureAwait(false)).ToList());
            }
        }

        [TestMethod]
        [DataRow("different-client", "user@contoso.com")]
        [DataRow(TestConstants.ClientId, "different@contoso.com")]
        public async Task DelegatedSetup_DoesNotUseUnmatchedFixtureAsync(string clientId, string username)
        {
            // Arrange
            File.WriteAllText(_fixturePath, JsonSerializer.Serialize(new[]
            {
                new { ClientId = clientId, Username = username, RefreshToken = "fixture-rt" }
            }));
            var app = PublicClientApplicationBuilder.Create(TestConstants.ClientId).Build();

            // Act / Assert
            await Assert.ThrowsAsync<AssertInconclusiveException>(() =>
                app.AcquireDelegatedTokenAsync(new[] { "User.Read" }, "user@contoso.com")).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DelegatedSetup_RejectsAnotherUsersTokenAsync()
        {
            // Arrange
            string[] scopes = { "User.Read" };
            File.WriteAllText(_fixturePath, JsonSerializer.Serialize(new[]
            {
                new { ClientId = TestConstants.ClientId, Username = "requested@contoso.com", RefreshToken = "fixture-rt" }
            }));
            using (var httpManager = new MockHttpManager())
            {
                var app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithAuthority(TestConstants.AuthorityTestTenant)
                    .WithHttpManager(httpManager).Build();
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddSuccessTokenResponseMockHandlerForPost(
                    TestConstants.AuthorityTestTenant,
                    responseMessage: MockHelpers.CreateSuccessResponseMessage(
                        MockHelpers.CreateSuccessTokenResponseString("wrong-user", "other@contoso.com", scopes)));

                // Act / Assert
                await Assert.ThrowsAsync<AssertFailedException>(() =>
                    app.AcquireDelegatedTokenAsync(scopes, "requested@contoso.com")).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task DelegatedSetup_MissingFixtureIsVisibleAsInconclusiveAsync()
        {
            // Arrange
            Environment.SetEnvironmentVariable("MSAL_V5_DELEGATED_TOKEN_FIXTURE", null);
            var app = PublicClientApplicationBuilder.Create(TestConstants.ClientId).Build();

            // Act / Assert
            await Assert.ThrowsAsync<AssertInconclusiveException>(() =>
                app.AcquireDelegatedTokenAsync(new[] { "User.Read" }, "user@contoso.com")).ConfigureAwait(false);
        }

        [TestMethod]
        [DataRow("null")]
        [DataRow("[null]")]
        public async Task DelegatedSetup_InvalidFixtureFailsInsteadOfSkippingAsync(string json)
        {
            // Arrange
            File.WriteAllText(_fixturePath, json);
            var app = PublicClientApplicationBuilder.Create(TestConstants.ClientId).Build();

            // Act / Assert
            await Assert.ThrowsAsync<InvalidDataException>(() =>
                app.AcquireDelegatedTokenAsync(new[] { "User.Read" }, "user@contoso.com")).ConfigureAwait(false);
        }
    }
}
