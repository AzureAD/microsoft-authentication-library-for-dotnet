// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace CommonCache.Test.Common
{
    public class ExecutionContent
    {
        public bool IsError { get; set; }
        public string ErrorMessage { get; set; }
        public string StackTrace { get; set; }
        public CacheExecutorResults Results { get; set; }

        public static ExecutionContent CreateFromException(Exception ex)
        {
            return new ExecutionContent
            {
                IsError = true,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }

        public static ExecutionContent CreateSuccess(CacheExecutorResults results)
        {
            return new ExecutionContent
            {
                Results = results
            };
        }
    }
}
