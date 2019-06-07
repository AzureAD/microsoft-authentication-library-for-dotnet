// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;

namespace CommonCache.Test.Common
{
    public interface ILanguageExecutor
    {
        Task<ProcessRunResults> ExecuteAsync(
            string arguments,
            CancellationToken cancellationToken);
    }
}
