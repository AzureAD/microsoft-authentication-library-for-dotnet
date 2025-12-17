// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.Infrastructure
{
    /// <summary>
    /// Ignores federated tests.
    /// </summary>
    internal class IgnoreFederatedTestsAttribute : TestMethodAttribute
    {
        public IgnoreFederatedTestsAttribute([CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            : base(filePath, lineNumber)
        {
        }

        public override async Task<TestResult[]> ExecuteAsync(ITestMethod testMethod)
        {
#if IGNORE_FEDERATED
            return new[]
            {
                    new TestResult
                    {
                        Outcome = UnitTestOutcome.Inconclusive,
                        TestFailureException = new AssertInconclusiveException(
                            $"Skipped on OneBranch pipeline")
                    }
                };
#else
            return await base.ExecuteAsync(testMethod).ConfigureAwait(false);
#endif
        }
    }
}
