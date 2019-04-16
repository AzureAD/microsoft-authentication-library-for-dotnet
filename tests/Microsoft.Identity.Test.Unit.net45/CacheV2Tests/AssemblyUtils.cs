// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Reflection;

namespace Microsoft.Identity.Test.Unit.CacheV2Tests
{
    public static class AssemblyUtilsEx
    {
        public static string GetExecutingAssemblyDirectory()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }
    }
}
