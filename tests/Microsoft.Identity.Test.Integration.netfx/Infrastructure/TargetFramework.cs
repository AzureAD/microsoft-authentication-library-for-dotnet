// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.Infrastructure
{
    /// <summary>
    /// Where we run integration tests. It's not worth running all tests on all frameworks
    /// </summary>
    [Flags]
    public enum RunOn
    {
        NetFx = 1,
        NetCore = 2
    }

    public static class RunOnHelper
    {
        public static void AssertFramework(this RunOn runOn)
        {
            if (IsNetClassic() && (runOn & RunOn.NetFx) != RunOn.NetFx)
            {
                Assert.Inconclusive("Test not configured to run on .Net Fx" );
            }

            if (IsNetCore() && (runOn & RunOn.NetCore) != RunOn.NetCore)
            {
                Assert.Inconclusive("Test not configured to run on .Net Core");
            }
        }

        private static bool IsNetClassic()
        {
#if NET_FX
            return true;
#elif NET_CORE
            return false;
#else
            throw new NotImplementedException();
#endif

        }

        private static bool IsNetCore()
        {
#if NET_FX
            return false;
#elif NET_CORE
            return true;
#else
            throw new NotImplementedException();
#endif

        }
    }
}
