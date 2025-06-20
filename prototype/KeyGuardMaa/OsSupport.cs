// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Win32;

internal static class OsSupport
{
    /// <summary>
    /// Returns <c>true</c> only on Windows Server 2022 (build 20348) or any later Server build.
    /// On Windows 10/11 client, Server 2019, Linux, etc. returns <c>false</c>.
    /// Quick checking the OS build number and the product name in the registry.
    /// MSAL has all the built in logic to do this. 
    /// </summary>
    internal static bool IsServer2022OrLater()
    {
        if (!OperatingSystem.IsWindows())
            return false;

        // Build number: 20348 = Server 2022 RTM
        Version v = Environment.OSVersion.Version;
        if (v.Build < 20348)
            return false;

        // Registry → HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProductName
        // e.g. "Windows Server 2022 Datacenter"
        try
        {
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            string productName = key?.GetValue("ProductName") as string ?? string.Empty;

            return productName.Contains("Server", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            // if the key is inaccessible (unlikely), assume "not server"
            return false;
        }
    }
}
