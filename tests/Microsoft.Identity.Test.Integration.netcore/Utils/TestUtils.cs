using System;
using Microsoft.Identity.Client.Extensions.Msal;

namespace Microsoft.Identity.Test.Integration.Utils
{
    public static class TestUtils
    {
        /// <summary>
        /// Get the handle of the foreground window for Windows
        /// </summary>
#if WINDOWS
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
#endif
        /// <summary>
        /// Get the handle of the console window for Linux
        /// </summary>
#if LINUX
        [DllImport("libX11")]
        private static extern IntPtr XOpenDisplay(string display);

        [DllImport("libX11")]
        private static extern IntPtr XRootWindow(IntPtr display, int screen);

        [DllImport("libX11")]
        private static extern IntPtr XDefaultRootWindow(IntPtr display);
#endif
        /// <summary>
        /// Get window handle on xplat
        /// </summary>
        public static IntPtr GetWindowHandle()
        {
            #if WINDOWS
                return GetForegroundWindow();
            #elif LINUX
                try
                {
                    return XRootWindow(XOpenDisplay(null), 0);
                }
                catch (System.Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex.ToString());
                    Console.ResetColor();
                    return IntPtr.Zero;
                }
            #else
                throw new PlatformNotSupportedException("Cannot get window handle on this platform.");
            #endif
        }

    }
}