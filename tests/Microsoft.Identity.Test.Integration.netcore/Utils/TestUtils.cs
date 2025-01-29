using System;
using System.Runtime.InteropServices;
using Microsoft.Identity.Client;

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
                throw new PlatformNotSupportedException("This platform is not supported.");
            }
        }

        public static BrokerOptions GetPlatformBroker() {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new BrokerOptions(BrokerOptions.OperatingSystems.Windows);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return new BrokerOptions(BrokerOptions.OperatingSystems.Linux);
            }
            else
            {
                throw new PlatformNotSupportedException("This platform is not supported.");
            }
        }

    }
}