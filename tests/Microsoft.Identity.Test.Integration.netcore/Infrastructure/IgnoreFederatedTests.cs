// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.Infrastructure
{
    internal class IgnoreFederatedTestsAttribute : TestMethodAttribute
    {
        public override TestResult[] Execute(ITestMethod testMethod)
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
            return base.Execute(testMethod);
#endif
        }
    }
}
