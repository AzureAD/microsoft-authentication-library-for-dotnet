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
using System.Windows.Automation;

namespace Test.ADAL.WinRT
{
    internal static class MouseHelper
    {
        public static void MoveTo(AutomationElement element)
        {
            var rect = element.Current.BoundingRectangle;
            int x = (int)(rect.Left + (rect.Width / 2));
            int y = (int)(rect.Top + (rect.Height / 2));
            x = (int)(x * (65536.0f / NativeMethods.GetSystemMetrics((int)NativeEnums.MetricIndex.SM_CXSCREEN)));
            y = (int)(y * (65536.0f / NativeMethods.GetSystemMetrics((int)NativeEnums.MetricIndex.SM_CYSCREEN)));

            NativeStructs.Input input = new NativeStructs.Input
            {
                type = NativeEnums.SendInputEventType.Mouse,
                mouseInput = new NativeStructs.MouseInput 
                    {
                        dx = x,
                        dy = y,
                        mouseData = 0,
                        dwFlags = NativeEnums.MouseEventFlags.Absolute | NativeEnums.MouseEventFlags.Move,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero,
                    },
            };

            NativeMethods.SendInput(1, ref input, Marshal.SizeOf(input));            
        }

        public static void LeftClick()
        {
            NativeStructs.Input input = new NativeStructs.Input
            {
                type = NativeEnums.SendInputEventType.Mouse,
                mouseInput = new NativeStructs.MouseInput
                {
                    dx = 0,
                    dy = 0,
                    mouseData = 0,
                    dwFlags = NativeEnums.MouseEventFlags.Absolute | NativeEnums.MouseEventFlags.LeftDown,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero,
                },
            };

            NativeMethods.SendInput(1, ref input, Marshal.SizeOf(input));

            input.mouseInput.dwFlags = NativeEnums.MouseEventFlags.Absolute | NativeEnums.MouseEventFlags.LeftUp;

            NativeMethods.SendInput(1, ref input, Marshal.SizeOf(input));
        }

        public static void AdjectCoordinate(int x, int y)
        {
        }
    }

    internal static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint SendInput(uint nInputs, ref NativeStructs.Input pInputs, int cbSize);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern int GetSystemMetrics(int nIndex);
    }

    internal static class NativeStructs
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct Input
        {
            public NativeEnums.SendInputEventType type;
            public MouseInput mouseInput;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MouseInput
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public NativeEnums.MouseEventFlags dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
    }

    internal static class NativeEnums
    {
        internal enum SendInputEventType : int
        {
            Mouse = 0,
            Keyboard = 1,
            Hardware = 2,
        }

        internal enum MetricIndex : int
        {
            SM_CXSCREEN = 0,
            SM_CYSCREEN = 1
        }

        [Flags]
        internal enum MouseEventFlags : uint
        {
            Move = 0x0001,
            LeftDown = 0x0002,
            LeftUp = 0x0004,
            RightDown = 0x0008,
            RightUp = 0x0010,
            MiddleDown = 0x0020,
            MiddleUp = 0x0040,
            XDown = 0x0080,
            XUp = 0x0100,
            Wheel = 0x0800,
            Absolute = 0x8000,
        }
    }
}
