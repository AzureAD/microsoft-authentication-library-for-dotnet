//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal static class AdalIdParameter
    {
        /// <summary>
        /// ADAL Flavor: .NET or WinRT
        /// </summary>
        public const string Product = "x-client-SKU";

        /// <summary>
        /// ADAL assembly version
        /// </summary>
        public const string Version = "x-client-Ver";

        /// <summary>
        /// CPU platform with x86, x64 or ARM as value
        /// </summary>
        public const string CpuPlatform = "x-client-CPU";

        /// <summary>
        /// Version of the operating system. This will not be sent on WinRT
        /// </summary>
        public const string OS = "x-client-OS";

        /// <summary>
        /// Device model. This will not be sent on .NET
        /// </summary>
        public const string DeviceModel = "x-client-DM";
    }

    /// <summary>
    /// This class adds additional query parameters or headers to the requests sent to STS. This can help us in
    /// collecting statistics and potentially on diagnostics.
    /// </summary>
    internal partial class AdalIdHelper
    {
        public static void AddAsQueryParameters(RequestParameters parameters)
        {
            NetworkPlugin.RequestCreationHelper.AddAdalIdParameters(parameters);
        }

        public static void AddAsHeaders(IHttpWebRequest request)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();
            NetworkPlugin.RequestCreationHelper.AddAdalIdParameters(headers);
            HttpHelper.AddHeadersToRequest(request, headers);
        }

        public static string GetAdalVersion()
        {
            string fullVersion = typeof(AdalIdHelper).GetTypeInfo().Assembly.FullName;
            Regex regex = new Regex(@"Version=[\d]+.[\d]+.[\d]+.[\d]+");
            Match match = regex.Match(fullVersion);
            if (match.Success)
            {
                string[] version = match.Groups[0].Value.Split(new[] { '=' }, StringSplitOptions.None);
                return version[1];
            }

            return null;
        }

        public static string GetAssemblyFileVersion()
        {
            return typeof(AdalIdHelper).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
        }

        public static string GetAssemblyInformationalVersion()
        {
            AssemblyInformationalVersionAttribute attribute = typeof(AdalIdHelper).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            return (attribute != null) ? attribute.InformationalVersion : string.Empty;
        }
    }
}