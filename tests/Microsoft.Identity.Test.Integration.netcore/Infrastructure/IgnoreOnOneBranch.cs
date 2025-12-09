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
    internal class IgnoreOnOneBranchAttribute : TestMethodAttribute
    {
        public IgnoreOnOneBranchAttribute(
            [CallerFilePath] string callerFilePath = "", 
            [CallerLineNumber] int callerLineNumber = -1) : base(callerFilePath, callerLineNumber)
        {
        }

        public override Task<TestResult[]> ExecuteAsync(ITestMethod testMethod)
        {
#if ONEBRANCH_BUILD
            var tr = new[]
            {
                    new TestResult
                    {
                        Outcome = UnitTestOutcome.Inconclusive,
                        TestFailureException = new AssertInconclusiveException(
                            $"Skipped on OneBranch pipeline")
                    }
                };
            return Task.FromResult(tr);
#else
            return base.ExecuteAsync(testMethod);
#endif
        }
    }
}
