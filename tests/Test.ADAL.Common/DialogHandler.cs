//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;

namespace Test.ADAL.Common
{
    /// <summary>
    /// Handle dialog screen to enter input and then press ok btn if any
    /// </summary>
    public class DialogHandler
    {

        public DialogHandler()
        {
            SecurityWindow = IntPtr.Zero;
        }

        public DialogHandler(IntPtr wndw)
        {
            SecurityWindow = wndw;
        }

        private IntPtr securityWindow;

        public IntPtr SecurityWindow
        {
            get
            {
                if (securityWindow == IntPtr.Zero)
                {
                    securityWindow = NativeWindowCalls.GetCurrentThreatWindowHwnd("#32770", "Windows Security");
                }

                return securityWindow;
            }
            set
            {
                securityWindow = value;
            }
        }

        /// <summary>
        /// Enter input and then click ok button
        /// </summary>
        /// <param name="username">Username for dialog</param>
        /// <param name="password">Password for dialog</param>
        /// <returns></returns>
        public bool EnterInput(string username, string password)
        {
            //webbrowserhost is the parent for security dialog

            NativeWindowCalls.SetForegroundWindow(SecurityWindow);
            // #32770 for security dialog
            if (securityWindow != IntPtr.Zero)
            {
                // username field may not be as edit field ( If dialog is remembering the credentials, username is text field
                IntPtr usernameWndPtr = NativeWindowCalls.GetEmptyUsernameField(SecurityWindow);

                if (usernameWndPtr != IntPtr.Zero)
                {
                    string titleText = NativeWindowCalls.GetWindowText(usernameWndPtr);
                    NativeWindowCalls.SetFocus(usernameWndPtr);
                    NativeWindowCalls.SetText(usernameWndPtr, username);
                    titleText = NativeWindowCalls.GetWindowText(usernameWndPtr);
                    if (titleText != username)
                        return false; // it did not enter
                }

                // Password field
                IntPtr childWndPtr = NativeWindowCalls.GetEmptyPasswordField(SecurityWindow);

                if (childWndPtr != IntPtr.Zero)
                {
                    NativeWindowCalls.SetFocus(childWndPtr);
                    NativeWindowCalls.SetText(childWndPtr, password);
                    // Verify to make sure it is set on this control
                    string passwordText = NativeWindowCalls.GetWindowText(childWndPtr);
                    if (passwordText == password)
                    {
                        // Ok button's classname in this dialog is "Button"
                        // there are many other buttons that are not visible to user in this dialog
                        IntPtr okBtn = NativeWindowCalls.GetChildWindowHwnd(SecurityWindow, "Button", "Ok");
                        return this.ClickOkButton(okBtn);
                    }
                }
            }
            return false;
        }

        private bool ClickOkButton(IntPtr hwnd)
        {
            // for debugging
            var info = new NativeWindowCalls.WINDOWINFO();
            info.cbSize = (uint)Marshal.SizeOf(info);
            NativeWindowCalls.GetWindowInfo(hwnd, ref info);
            string titleCheck = NativeWindowCalls.GetWindowText(hwnd);

            // verify
            if (hwnd != IntPtr.Zero)
            {
                // click ok
                NativeWindowCalls.SendMessage(hwnd, NativeWindowCalls.WmActivate, NativeWindowCalls.MaActivate, 0);
                NativeWindowCalls.SendMessage(hwnd, NativeWindowCalls.BtnClick, 0, 0);
                return true;
            }

            return false;
        }
    }
}
