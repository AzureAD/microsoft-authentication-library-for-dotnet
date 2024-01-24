// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.PlatformsCommon.Factories;

namespace Microsoft.Identity.Client.Internal
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
        private static readonly Lazy<string> s_msalVersion = new Lazy<string>(
            () =>
            {
                string fullVersion = typeof(MsalIdHelper).Assembly.FullName;
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

        public static IDictionary<string, string> GetMsalIdParameters(ILoggerAdapter logger)
        {
            var platformProxy = PlatformProxyFactory.CreatePlatformProxy(logger);
            if (platformProxy == null)
            {
                throw new MsalClientException(
                    MsalError.PlatformNotSupported,
                    MsalErrorMessage.PlatformNotSupported);
            }

            var parameters = new Dictionary<string, string>
            {
                [MsalIdParameter.Product] = platformProxy.GetProductName(),
                [MsalIdParameter.Version] = GetMsalVersion()
            };

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
        }

        public static string GetMsalVersion()
        {
            return s_msalVersion.Value;
        }
    }
}
