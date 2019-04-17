// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    public static class ThreadTestUtils
    {
        public static void ParallelExecute(Action action, int numExecutions = 100)
        {
            var actions = new List<Action>();
            for (int i = 0; i < numExecutions; i++)
            {
                actions.Add(action);
            }

            Parallel.Invoke(
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = 8
                },
                actions.ToArray());
        }

        public static void RunActionOnThreadAndJoin(Action action)
        {
            var thread = new Thread(() => action());
            thread.Start();
            thread.Join();
        }
    }
}
