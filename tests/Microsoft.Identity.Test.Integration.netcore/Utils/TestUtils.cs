using System;
using Microsoft.Identity.Client.Extensions.Msal;

namespace Microsoft.Identity.Test.Integration.Utils
{
    public static class TestUtils
    {
        /// <summary>
        /// Get the handle of the foreground window for Windows
        /// </summary>
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        // <summary>
        /// Get the handle of the console window for Linux
        /// </summary>
        [DllImport("libX11")]
        private static extern IntPtr XOpenDisplay(string display);

        [DllImport("libX11")]
        private static extern IntPtr XRootWindow(IntPtr display, int screen);

        [DllImport("libX11")]
        private static extern IntPtr XDefaultRootWindow(IntPtr display);

        /// <summary>
        /// Get window handle on xplat
        /// </summary>
        public static IntPtr GetWindowHandle()
        {
            if (SharedUtilities.IsWindowsPlatform())
            {
                return GetForegroundWindow();
            }
            else if (SharedUtilities.IsLinuxPlatform())
            {
                try {
                    return XRootWindow(XOpenDisplay(null), 0);
                } catch (System.Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex.ToString());
                    Console.ResetColor();
                }
                return IntPtr.Zero;
            }
            else
            {
                throw new PlatformNotSupportedException("Cannot get window handle on this platform.");
            }
        }

    }
}