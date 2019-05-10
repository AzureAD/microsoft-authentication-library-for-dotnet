// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;

namespace Microsoft.Identity.Client.Utils
{
    internal static class AssemblyUtils
    {
        public static string GetAssemblyFileVersionAttribute()
        {
            // TODO:  Pick one of these and let's finalize...
            // return typeof (MsalIdHelper).GetTypeInfo().Assembly.GetName().Version.ToString();
            return typeof(AssemblyUtils).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
        }

        public static string GetAssemblyInformationalVersion()
        {
            var attribute = typeof(AssemblyUtils).GetTypeInfo().Assembly
                                                 .GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            return attribute != null ? attribute.InformationalVersion : string.Empty;
        }
    }
}
