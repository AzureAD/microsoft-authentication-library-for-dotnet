// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Utils
{
    internal static class TaskUtil
    {
        public static Task CompletedTask
        {
            get
            {
                return Task.CompletedTask; 
            }
        }
    }
}
