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
