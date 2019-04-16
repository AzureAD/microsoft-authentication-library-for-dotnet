// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace CommonCache.Test.Common
{
    public sealed class ProcessRunResults
    {
        public ProcessRunResults(string standardOut, string standardError)
        {
            StandardOut = standardOut;
            StandardError = standardError;
        }

        public string StandardOut { get; }
        public string StandardError { get; }
    }
}
