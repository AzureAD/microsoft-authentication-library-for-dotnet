using System;
using System.Runtime.InteropServices;

namespace Microsoft.Identity.Test.Integration.Utils
{
    public static class TestUtils
    {
        /// <summary>
        /// Get the handle of the foreground window for Windows
        /// </summary>
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        /// <summary>
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
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetForegroundWindow();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return XRootWindow(XOpenDisplay(null), 0);
            }
            else
            {
                throw new PlatformNotSupportedException("This platform is not supported.");
            }
        }

    }
}