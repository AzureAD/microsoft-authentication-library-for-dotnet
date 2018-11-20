// ------------------------------------------------------------------------------
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
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Microsoft.Identity.Core
{
    internal static class MsalIdParameter
    {
        /// <summary>
        ///     MSAL Flavor: .NET or WinRT
        /// </summary>
        public const string Product = "x-client-SKU";

        /// <summary>
        ///     MSAL assembly version
        /// </summary>
        public const string Version = "x-client-Ver";

        /// <summary>
        ///     CPU platform with x86, x64 or ARM as value
        /// </summary>
        public const string CpuPlatform = "x-client-CPU";

        /// <summary>
        ///     Version of the operating system. This will not be sent on WinRT
        /// </summary>
        public const string OS = "x-client-OS";

        /// <summary>
        ///     Device model. This will not be sent on .NET
        /// </summary>
        public const string DeviceModel = "x-client-DM";
    }

    /// <summary>
    ///     This class adds additional query parameters or headers to the requests sent to STS. This can help us in
    ///     collecting statistics and potentially on diagnostics.
    /// </summary>
    internal static class MsalIdHelper
    {
        private static readonly Lazy<IDictionary<string, string>> MsalIdParameters = new Lazy<IDictionary<string, string>>(
            () =>
            {
                var platformProxy = PlatformProxyFactory.GetPlatformProxy();
                if (platformProxy == null)
                {
                    throw CoreExceptionFactory.Instance.GetClientException(
                        CoreErrorCodes.PlatformNotSupported,
                        CoreErrorMessages.PlatformNotSupported);
                }

                var parameters = new Dictionary<string, string>
                {
                    [MsalIdParameter.Product] = platformProxy.GetProductName(),
                    [MsalIdParameter.Version] = GetMsalVersion()
                };

                string processorInformation = platformProxy.GetProcessorArchitecture();
                if (processorInformation != null)
                {
                    parameters[MsalIdParameter.CpuPlatform] = processorInformation;
                }

                string osInformation = platformProxy.GetOperatingSystem();
                if (osInformation != null)
                {
                    parameters[MsalIdParameter.OS] = osInformation;
                }

                string deviceInformation = platformProxy.GetDeviceModel();
                if (deviceInformation != null)
                {
                    parameters[MsalIdParameter.DeviceModel] = deviceInformation;
                }

                return parameters;
            });

        private static readonly Lazy<string> MsalVersion = new Lazy<string>(
            () =>
            {
                string fullVersion = typeof(MsalIdHelper).GetTypeInfo().Assembly.FullName;
                var regex = new Regex(@"Version=[\d]+.[\d+]+.[\d]+.[\d]+");
                var match = regex.Match(fullVersion);
                if (!match.Success)
                {
                    return null;
                }

                string[] version = match.Groups[0].Value.Split(
                    new[]
                    {
                        '='
                    },
                    StringSplitOptions.None);
                return version[1];
            });

        public static IDictionary<string, string> GetMsalIdParameters()
        {
            return MsalIdParameters.Value;
        }

        public static string GetMsalVersion()
        {
            return MsalVersion.Value;
        }
    }
}