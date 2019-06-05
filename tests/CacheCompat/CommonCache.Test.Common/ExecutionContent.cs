// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CommonCache.Test.Common
{
    public class ExecutionContent
    {
        public bool IsError { get; set; }
        public string ErrorMessage { get; set; }
        public string StackTrace { get; set; }

        [JsonProperty("Results")]
        public List<CacheExecutorAccountResult> Results { get; set; } = new List<CacheExecutorAccountResult>();

        public static ExecutionContent CreateFromException(Exception ex)
        {
            return new ExecutionContent
            {
                IsError = true,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }

        public static ExecutionContent CreateSuccess(List<CacheExecutorAccountResult> results)
        {
            return new ExecutionContent
            {
                Results = results
            };
        }
    }
}
