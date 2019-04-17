// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;

namespace CommonCache.Test.Common
{
    public interface ILanguageExecutor
    {
        Task<ProcessRunResults> ExecuteAsync(
            string clientId,
            string authority,
            string scope,
            string username,
            string password,
            string cacheFilePath,
            CancellationToken cancellationToken);
    }
}
