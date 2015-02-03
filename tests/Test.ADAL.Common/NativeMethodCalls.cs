//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Test.ADAL.Common
{
    /// <summary>
    /// Window related native API calls to interact with windows for sending input, clicking button etc. It is a small subset of functions listed at
    /// http://msdn.microsoft.com/en-us/library/windows/desktop/ff468919(v=vs.85).aspx
    /// </summary>
    public static class NativeWindowCalls
    {
        const int MaxClassNameCapacity = 255;

        public const int WmSysCommand = 0x0112;
        public const int WmClose = 0x0010;
        public const UInt32 WmChar = 0x0102;
        public const int ScClose = 0xF060;
        public const int WmCommand = 0x0111;

        public const int KeyeventfExtendedkey = 0x1;
        public const int KeyeventfKeyup = 0x2;
        public const int KeyeventfTab = 0x09;

        public const int BtnClick = 245;
        public const int WmActivate = 6;
        public const int MaActivate = 1;

        public const Int64 PasswordStyle = 0x0020L;
        public const int GwChild = 5;
        public const int GwHwndnext = 2;

        public enum WindowShowStyle
        {
            Hide = 0,
            ShowNormal = 1,
            ShowMinimized = 2,
            ShowMaximized = 3,
            Maximize = 3,
            ShowNormalNoActivate = 4,
            Show = 5,
            Minimize = 6,
            ShowMinNoActivate = 7,
            ShowNoActivate = 8,
            Restore = 9,
            ShowDefault = 10,
            ForceMinimized = 11
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWINFO
        {
            public uint cbSize;
            public RECT rcWindow;
            public RECT rcClient;
            public uint dwStyle;
            public uint dwExStyle;
            public uint dwWindowStatus;
            public uint cxWindowBorders;
            public uint cyWindowBorders;
            public ushort atomWindowType;
            public ushort wCreatorVersion;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 1)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public POINT ptMinPosition;
            public POINT ptMaxPosition;
            public RECT rcNormalPosition;
        }

        /// <summary>
        /// Retrieves information about the specified window.
        /// </summary>
        /// <param name="hwnd">A handle to the window whose information is to be retrieved.</param>
        /// <param name="pwi">A pointer to a WINDOWINFO structure to receive the information. Note that you must set the cbSize member to sizeof(WINDOWINFO) before calling this function.</param>
        /// <returns>If the function succeeds, the return value is nonzero. If the function fails, the return value is zero. To get extended error information, call GetLastError.</returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowInfo(IntPtr hwnd, ref WINDOWINFO pwi);

        /// <summary>
        /// Retrieves a handle to a window whose class name and window name match the specified strings. 
        /// The function searches child windows, beginning with the one following the specified child window. 
        /// This function does not perform a case-sensitive search.
        /// </summary>
        /// <param name="parentHandle"></param>
        /// <param name="childAfter">Set to null to search all</param>
        /// <param name="className"></param>
        /// <param name="windowTitle">set to null to search all</param>
        /// <returns></returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetActiveWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GetWindow(IntPtr hWnd, int uCmd);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr handleToWindow, StringBuilder windowText, int maxTextLength);

        [DllImport("user32.dll", EntryPoint = "SetWindowText", CharSet = CharSet.Ansi)]
        public static extern bool SetWindowText(IntPtr hwnd, string lpString);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsWindowEnabled(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        /// <summary>
        /// MSDN: Sends the specified message to a window or windows. The SendMessage function calls the window procedure for the specified window and does not return until the window procedure has processed the message. 
        /// To send a message and return immediately, use the SendMessageCallback or SendNotifyMessage function. To post a message to a thread's message queue and return immediately, use the PostMessage or PostThreadMessage function.
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns>depends on call type</returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        // to send chars
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SendMessage(HandleRef hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, int wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int y, int cx, int cy, int wFlags);
        
        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("User32.dll")]
        public static extern IntPtr GetParent(IntPtr hwnd);

        // to go through child windows for lookup..best to find ok buttons
        public delegate bool EnumChildWindowsProc(IntPtr hWnd, IntPtr lParam);

        /// <summary>
        /// MSDN: Enumerates the child windows that belong to the specified parent window by passing the handle to each child window, in turn, to an application-defined callback function. EnumChildWindows continues until the last child window is enumerated or the callback function returns FALSE.
        /// </summary>
        /// <param name="hWndParent">A handle to the parent window whose child windows are to be enumerated. If this parameter is NULL, this function is equivalent to EnumWindows.</param>
        /// <param name="lpEnumFunc">A pointer to an application-defined callback function. For more information, see EnumChildProc.</param>
        /// <param name="lParam">An application-defined value to be passed to the callback function.</param>
        /// <returns></returns>
        [DllImport("user32", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumChildWindows(IntPtr hWndParent, EnumChildWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32", CharSet = CharSet.Auto, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumThreadWindows(int threadId, EnumChildWindowsProc lpEnumFunc, IntPtr lParam);
                
        [DllImport("user32", EntryPoint = "GetClassNameA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        public static extern int GetClassName(IntPtr handleToWindow, StringBuilder className, int maxClassNameLength);

        [DllImport("Kernel32", ExactSpelling = true)]
        public static extern Int32 GetCurrentThreadId();

        public static string GetFullClassName(IntPtr hwnd)
        {
            StringBuilder className = new StringBuilder(MaxClassNameCapacity);
            int result = GetClassName(hwnd, className, MaxClassNameCapacity);

            return result == 0 ? String.Empty : className.ToString();
        }

        public static bool ClassNameAreEqual(IntPtr hWnd, string expectedClassName)
        {
            if (hWnd == IntPtr.Zero)
            {
                return false;
            }

            if (string.IsNullOrEmpty(expectedClassName))
            {
                return false;
            }

            var className = GetFullClassName(hWnd);
            return className.Equals(expectedClassName, StringComparison.OrdinalIgnoreCase);
        }

        // Get window in the current thread for classname and title
        public static IntPtr GetCurrentThreatWindowHwnd(string className, string title)
        {
            var hWnd = IntPtr.Zero;
            int id = GetCurrentThreadId();
            EnumThreadWindows(GetCurrentThreadId(), (childHwnd, lParam) =>
            {
                string titleText = GetWindowText(childHwnd);

                if (ClassNameAreEqual(childHwnd, className) && titleText.ToLower() == title.ToLower())
                {
                    hWnd = childHwnd;
                    return false;
                }

                return true;
            }, IntPtr.Zero);

            return hWnd;
        }

        public static IntPtr GetCurrentProcessWindowHwnd(List<int> threadidList, string className, string title)
        {
            var hWnd = IntPtr.Zero;
            int id = GetCurrentThreadId();
            foreach (int threadid in threadidList)
            {
                EnumThreadWindows(threadid, (childHwnd, lParam) =>
                {
                    string titleText = GetWindowText(childHwnd);

                    if (ClassNameAreEqual(childHwnd, className) && titleText.ToLower() == title.ToLower())
                    {
                        hWnd = childHwnd;
                        return false;
                    }

                    return true;
                }, IntPtr.Zero);

                if (hWnd != IntPtr.Zero)
                    break;
            }
            return hWnd;
        }

        public static IntPtr GetChildWindowHwnd(IntPtr parentHwnd, string className)
        {
            var hWnd = IntPtr.Zero;
            EnumChildWindows(parentHwnd, (childHwnd, lParam) =>
            {
                if (ClassNameAreEqual(childHwnd, className))
                {
                    hWnd = childHwnd;
                    return false;
                }

                return true;
            }, IntPtr.Zero);

            return hWnd;
        }

        /// <summary>
        /// Get window handle with classname and title. This is hte best way to get Ok button for clicking.
        /// </summary>
        /// <param name="parentHwnd"></param>
        /// <param name="className"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        public static IntPtr GetChildWindowHwnd(IntPtr parentHwnd, string className, string title)
        {
            var hWnd = IntPtr.Zero;
            EnumChildWindows(parentHwnd, (childHwnd, lParam) =>
            {
                string titleText = GetWindowText(childHwnd);

                if (ClassNameAreEqual(childHwnd, className) && titleText.ToLower() == title.ToLower())
                {
                    hWnd = childHwnd;
                    return false;
                }

                return true;
            }, IntPtr.Zero);

            return hWnd;
        }

        public static IntPtr GetEmptyUsernameField(IntPtr parentHwnd)
        {
            var hWnd = IntPtr.Zero;
            string className = "Edit";

            EnumChildWindows(parentHwnd, (childHwnd, lParam) =>
            {
                Int64 styles = GetWindowStyle(childHwnd);
                string content = GetWindowText(childHwnd);
                if (string.IsNullOrWhiteSpace(content) && ClassNameAreEqual(childHwnd, className) && ((styles & PasswordStyle) == 0))
                {
                    hWnd = childHwnd;
                    return false;
                }

                return true;
            }, IntPtr.Zero);

            return hWnd;
        }

        public static IntPtr GetEmptyPasswordField(IntPtr parentHwnd)
        {
            var hWnd = IntPtr.Zero;
            string className = "Edit";

            EnumChildWindows(parentHwnd, (childHwnd, lParam) =>
            {
                Int64 styles = GetWindowStyle(childHwnd);
                string content = GetWindowText(childHwnd);
                bool eq = ClassNameAreEqual(childHwnd, className);
                if (string.IsNullOrWhiteSpace(content) && eq && ((styles & PasswordStyle) > 0))
                {
                    hWnd = childHwnd;
                    return false;
                }

                return true;
            }, IntPtr.Zero);

            return hWnd;
        }

        /// <summary>
        /// Useful for finding window with style
        /// </summary>
        /// <param name="hwnd"></param>
        /// <returns></returns>
        public static Int64 GetWindowStyle(IntPtr hwnd)
        {
            var info = new WINDOWINFO();
            info.cbSize = (uint)Marshal.SizeOf(info);
            GetWindowInfo(hwnd, ref info);
            return Convert.ToInt64(info.dwStyle);
        }

        /// <summary>
        /// Send key inputs one char by one char
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="input"></param>
        public static void SendString(IntPtr hwnd, string input)
        {
            //return SetWindowText(hwnd, input);
            foreach (var c in input)
            {
                SendChar(c, hwnd);
            }

            
        }

        /// <summary>
        /// Set text for the control
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool SetText(IntPtr hwnd, string input)
        {
            return SetWindowText(hwnd, input);
        }

        // title
        public static string GetWindowText(IntPtr hwnd)
        {
            var length = GetWindowTextLength(hwnd) + 1;
            var buffer = new StringBuilder(length);
            GetWindowText(hwnd, buffer, length);
            return buffer.ToString();
        }

        private static void SendChar(char c, IntPtr handle)
        {
            SendMessage(new HandleRef(null, handle), WmChar, new IntPtr(Convert.ToInt64(c)), IntPtr.Zero);
        }
    }
}
