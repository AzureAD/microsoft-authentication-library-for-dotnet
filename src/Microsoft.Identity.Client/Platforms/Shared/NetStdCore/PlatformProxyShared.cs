// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Platforms.Shared.NetStdCore
{
    internal class PlatformProxyShared
    {
        public static void StartDefaultOsBrowser(string url)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch
            {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    try
                    {
                        Process.Start("xdg-open", url);
                    }
                    catch (Exception ex)
                    {
                        throw new MsalClientException(
                            MsalError.LinuxXdgOpen,
                            "Unable to open a web page using xdg-open. See inner exception for details. Possible causes for this error are: xdg-open is not installed or " +
                            "it cannot find a way to open an url - make sure you can open a web page by invoking from a terminal: xdg-open https://www.bing.com ",
                            ex);
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw new PlatformNotSupportedException(RuntimeInformation.OSDescription);
                }
            }
        }
    }
}
