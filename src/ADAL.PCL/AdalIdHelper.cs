//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
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
    internal static class AdalIdHelper
    {
        public static IDictionary<string, string> GetAdalIdParameters()
        {
            var parameters = new Dictionary<string, string>();

            parameters[AdalIdParameter.Product] = PlatformPlugin.PlatformInformation.GetProductName();
            parameters[AdalIdParameter.Version] = GetAdalVersion();

            var processorInofrmation = PlatformPlugin.PlatformInformation.GetProcessorArchitecture();
            if (processorInofrmation != null)
            {
                parameters[AdalIdParameter.CpuPlatform] = processorInofrmation;
            }

            var osInformation = PlatformPlugin.PlatformInformation.GetOperatingSystem();
            if (osInformation != null)
            {
                parameters[AdalIdParameter.OS] = osInformation;
            }

            var deviceInformation = PlatformPlugin.PlatformInformation.GetDeviceModel();
            if (deviceInformation != null)
            {
                parameters[AdalIdParameter.DeviceModel] = deviceInformation;
            }

            return parameters;
        }

        public static string GetAdalVersion()
        {
            string fullVersion =  typeof (AdalIdHelper).GetTypeInfo().Assembly.FullName;
            Regex regex = new Regex(@"Version=[\d]+.[\d+]+.[\d]+.[\d]+");
            Match match = regex.Match(fullVersion);
            if (match.Success)
            {
                string[] version = match.Groups[0].Value.Split(new []{'='},StringSplitOptions.None);
                return version[1];
            }

            return null;
        }

        public static string GetAssemblyFileVersion()
        {
            return PlatformPlugin.PlatformInformation.GetAssemblyFileVersionAttribute();
        }

        public static string GetAssemblyInformationalVersion()
        {
            AssemblyInformationalVersionAttribute attribute = typeof(AdalIdHelper).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            return (attribute != null) ? attribute.InformationalVersion : string.Empty;
        }
    }
}
