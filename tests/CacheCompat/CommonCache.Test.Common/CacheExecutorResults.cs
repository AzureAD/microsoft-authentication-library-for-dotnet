// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace CommonCache.Test.Common
{
    public class CacheExecutorResults
    {
        public CacheExecutorResults(string username, bool receivedTokenFromCache)
        {
            Username = username;
            ReceivedTokenFromCache = receivedTokenFromCache;
        }

        public string Username { get; }
        public bool ReceivedTokenFromCache { get; }
    }
}
