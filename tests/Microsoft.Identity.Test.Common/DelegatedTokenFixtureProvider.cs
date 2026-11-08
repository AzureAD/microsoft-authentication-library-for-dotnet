// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    /// <summary>
    /// Reads externally provisioned delegated refresh tokens for shared test setup.
    /// </summary>
    public static class DelegatedTokenFixtureProvider
    {
        /// <summary>
        /// Finds a token for the exact client and user without logging credentials.
        /// </summary>
        /// <param name="clientId">The test application's client ID.</param>
        /// <param name="username">The delegated user's username.</param>
        /// <param name="refreshToken">The matching refresh token, if provisioned.</param>
        /// <returns>Whether the required credential is provisioned.</returns>
        public static bool TryGetRefreshToken(string clientId, string username, out string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new ArgumentException("A client ID is required.", nameof(clientId));
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("A delegated username is required.", nameof(username));
            }

            refreshToken = null;
            string path = Environment.GetEnvironmentVariable("MSAL_V5_DELEGATED_TOKEN_FIXTURE");
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            var entries = JsonSerializer.Deserialize<List<DelegatedTokenFixture>>(File.ReadAllText(path));
            if (entries is null || entries.Any(item => item is null))
            {
                throw new InvalidDataException("The delegated token fixture must contain an array of non-null entries.");
            }

            var entry = entries.SingleOrDefault(item =>
                string.Equals(item.ClientId, clientId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.Username, username, StringComparison.OrdinalIgnoreCase));
            refreshToken = entry?.RefreshToken;
            return !string.IsNullOrEmpty(refreshToken);
        }

        private sealed class DelegatedTokenFixture
        {
            public DelegatedTokenFixture()
            {
            }

            public string ClientId { get; set; }
            public string Username { get; set; }
            public string RefreshToken { get; set; }
        }
    }
}
