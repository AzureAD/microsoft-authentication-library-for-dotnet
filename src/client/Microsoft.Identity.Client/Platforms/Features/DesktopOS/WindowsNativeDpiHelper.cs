// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Identity.Client.Platforms.Features.DesktopOs
{
    internal static class WindowsDpiHelper
    {
        static WindowsDpiHelper()
        {
            const double DefaultDpi = 96.0;

            const int LOGPIXELSX = 88;
            const int LOGPIXELSY = 90;

            double deviceDpiX;
            double deviceDpiY;

            IntPtr dC = GetDC(IntPtr.Zero);
            if (dC != IntPtr.Zero)
            {
                deviceDpiX = GetDeviceCaps(dC, LOGPIXELSX);
                deviceDpiY = GetDeviceCaps(dC, LOGPIXELSY);
                ReleaseDC(IntPtr.Zero, dC);
            }
            else
            {
                deviceDpiX = DefaultDpi;
                deviceDpiY = DefaultDpi;
            }

            int zoomPercentX = (int)(100 * (deviceDpiX / DefaultDpi));
            int zoomPercentY = (int)(100 * (deviceDpiY / DefaultDpi));

            ZoomPercent = Math.Min(zoomPercentX, zoomPercentY);
        }

        /// <summary>
        /// </summary>
        public static int ZoomPercent { get; }

        [DllImport("User32.dll", CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
        internal static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("User32.dll", CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
        internal static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("Gdi32.dll", CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
        internal static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        [DllImport("User32.dll", CallingConvention = CallingConvention.Winapi, ExactSpelling = true)]
        internal static extern bool IsProcessDPIAware();
    }
}

