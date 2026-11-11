// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;

namespace Microsoft.Identity.Client
{
    public partial class SystemWebViewOptions
    {
        internal static void OpenEdgeBrowserOnWindows(
            string url,
            /* for test */ Action<ProcessStartInfo> startProcess = null)
        {
            var startInfo = new ProcessStartInfo($"microsoft-edge:{url}")
            {
                UseShellExecute = true
            };
            (startProcess ?? StartBrowserProcess)(startInfo);
        }

        internal static void OpenDefaultBrowserOnWindows(
            string url,
            /* for test */ Action<ProcessStartInfo> startProcess = null)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                (startProcess ?? StartBrowserProcess)(startInfo);
            }
            catch
            {
                // Use Edge's registered URI handler if the default browser cannot be started.
                OpenEdgeBrowserOnWindows(url, startProcess);
            }
        }

        private static void StartBrowserProcess(ProcessStartInfo startInfo)
        {
            Process.Start(startInfo);
        }
    }
}
