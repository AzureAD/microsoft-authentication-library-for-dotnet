// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.Infrastructure
{
    public class RunOnAttribute : TestMethodAttribute
    {
        private readonly TargetFrameworks _tfms;

        public RunOnAttribute(TargetFrameworks tfms)
        {
            _tfms = tfms;
        }

        public override TestResult[] Execute(ITestMethod testMethod)
        {
            if ((RunOnHelper.IsNetClassic() && ((_tfms & TargetFrameworks.NetFx) != TargetFrameworks.NetFx)) ||
                (RunOnHelper.IsNetCore() && ((_tfms & TargetFrameworks.NetCore) != TargetFrameworks.NetCore)) ||
                (RunOnHelper.IsNetStandard() && ((_tfms & TargetFrameworks.NetStandard) != TargetFrameworks.NetStandard)))
            {
                return new[]
                {
                    new TestResult
                    {
                        Outcome = UnitTestOutcome.Inconclusive,
                        TestFailureException = new AssertInconclusiveException(
                            $"Skipped on target framework {_tfms}")
                    }
                };
            }

            return base.Execute(testMethod);
        }
    }

    /// <summary>
    /// Where we run integration tests. It's not worth running all tests on all frameworks
    /// </summary>
    [Flags]
    public enum TargetFrameworks
    {
        NetFx = 1,
        NetCore = 2,
        NetStandard = 4
    }

    public static class RunOnHelper
    {
        public static void AssertFramework(this TargetFrameworks runOn)
        {
            if (IsNetClassic() && (runOn & TargetFrameworks.NetFx) != TargetFrameworks.NetFx)
            {
                Assert.Inconclusive("Test not configured to run on .Net Fx" );
            }

            if (IsNetCore() && (runOn & TargetFrameworks.NetCore) != TargetFrameworks.NetCore)
            {
                Assert.Inconclusive("Test not configured to run on .Net Core");
            }

            if (IsNetStandard() && (runOn & TargetFrameworks.NetStandard) != TargetFrameworks.NetStandard)
            {
                Assert.Inconclusive("Test not configured to run on NetStandard");
            }
        }

        public static bool IsNetClassic()
        {
#if NET_FX
            return true;
#elif NET_CORE || NETSTANDARD
            return false;
#else
            throw new NotImplementedException();
#endif

        }

        public static bool IsNetCore()
        {
#if NET_FX || NETSTANDARD
            return false;
#elif NET_CORE
            return true;
#else
            throw new NotImplementedException();
#endif

        }

        public static bool IsNetStandard()
        {
#if NET_FX || NET_CORE
            return false;
#elif NETSTANDARD
            return true;
#else
            throw new NotImplementedException();
#endif
        }
    }
}
