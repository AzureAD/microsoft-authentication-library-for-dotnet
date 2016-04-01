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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory.Native
{
    /// <summary>
    ///     Native interop layer for Win32 APIs
    /// </summary>
    internal static class Win32Native
    {
        //
        // Enumerations
        //

        [Flags]
        internal enum FormatMessageFlags
        {
            None = 0x00000000,
            AllocateBuffer = 0x00000100,           // FORMAT_MESSAGE_ALLOCATE_BUFFER
            FromModule = 0x00000800,           // FORMAT_MESSAGE_FROM_HMODULE
            FromSystem = 0x00001000,           // FORMAT_MESSAGE_FROM_SYSTEM
        }

        //
        // Structures
        //

        [StructLayout(LayoutKind.Sequential)]
        internal struct SYSTEMTIME
        {
            internal ushort wYear;
            internal ushort wMonth;
            internal ushort wDayOfWeek;
            internal ushort wDay;
            internal ushort wHour;
            internal ushort wMinute;
            internal ushort wSecond;
            internal ushort wMilliseconds;

            internal SYSTEMTIME(DateTime time)
            {
                wYear = (ushort)time.Year;
                wMonth = (ushort)time.Month;
                wDayOfWeek = (ushort)time.DayOfWeek;
                wDay = (ushort)time.Day;
                wHour = (ushort)time.Hour;
                wMinute = (ushort)time.Minute;
                wSecond = (ushort)time.Second;
                wMilliseconds = (ushort)time.Millisecond;
            }
        }

        [SuppressUnmanagedCodeSecurity]
        private static class UnsafeNativeMethods
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
            internal static extern SafeLibraryHandle LoadLibrary(string lpFileName);

            [DllImport("kernel32.dll", SetLastError = true)]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            internal static extern IntPtr LocalFree(IntPtr hMem);

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern int FormatMessage(FormatMessageFlags dwFlags,
                                                     SafeLibraryHandle lpSource,
                                                     int dwMessageId,
                                                     int dwLanguageId,
                                                     [In, Out] ref IntPtr lpBuffer,
                                                     int nSize,
                                                     IntPtr pArguments);
        }

        //
        // Wrapper APIs
        //

        /// <summary>
        ///     Lookup an error message in the message table of a specific library as well as the system
        ///     message table.
        /// </summary>
        [SecurityCritical]
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Safe to expose")]
        internal static string FormatMessageFromLibrary(int message, string library)
        {
            Debug.Assert(!String.IsNullOrEmpty(library), "!String.IsNullOrEmpty(library)");

            using (SafeLibraryHandle module = UnsafeNativeMethods.LoadLibrary(library))
            {
                IntPtr messageBuffer = IntPtr.Zero;

                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    int result = UnsafeNativeMethods.FormatMessage(FormatMessageFlags.AllocateBuffer | FormatMessageFlags.FromModule | FormatMessageFlags.FromSystem,
                                                                   module,
                                                                   message,
                                                                   0,
                                                                   ref messageBuffer,
                                                                   0,
                                                                   IntPtr.Zero);
                    if (result == 0)
                    {
                        Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                    }

                    return Marshal.PtrToStringUni(messageBuffer);
                }
                finally
                {
                    if (messageBuffer != IntPtr.Zero)
                    {
                        UnsafeNativeMethods.LocalFree(messageBuffer);
                    }
                }
            }
        }

        /// <summary>
        ///     Get an error message for an NTSTATUS error code
        /// </summary>
        internal static string GetNTStatusMessage(int ntstatus)
        {
            return FormatMessageFromLibrary(ntstatus, "ntdll.dll");
        }
    }

    /// <summary>
    ///     Safe handle base class for safe handles which are associated with an additional data buffer that
    ///     must be kept alive for the same amount of time as the handle itself.
    ///     
    ///     This is required rather than having a seperate safe handle own the key data buffer blob so
    ///     that we can ensure that the key handle is disposed of before the key data buffer is freed.
    /// </summary>
    internal abstract class SafeHandleWithBuffer : SafeHandleZeroOrMinusOneIsInvalid
    {
        private IntPtr m_dataBuffer;

        protected SafeHandleWithBuffer() : base(true)
        {
            return;
        }

        public override bool IsInvalid
        {
            get
            {
                return handle == IntPtr.Zero &&             // The handle is not valid
                       m_dataBuffer == IntPtr.Zero;         // And we don't own any native memory
            }
        }

        /// <summary>
        ///     Buffer that holds onto the key data object. This data must be allocated with CoAllocTaskMem, 
        ///     or the ReleaseBuffer method must be overriden to match the deallocation function with the
        ///     allocation function.  Once the buffer is assigned into the DataBuffer property, the safe
        ///     handle owns the buffer and users of this property should not attempt to free the memory.
        ///     
        ///     This property should be set only once, otherwise the first data buffer will leak.
        /// </summary>
        internal IntPtr DataBuffer
        {
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            get
            { return m_dataBuffer; }

            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            set
            {
                Debug.Assert(m_dataBuffer == IntPtr.Zero, "SafeHandleWithBuffer already owns a data buffer - this will result in a native memory leak.");
                Debug.Assert(value != IntPtr.Zero, "value != IntPtr.Zero");

                m_dataBuffer = value;
            }
        }

        /// <summary>
        ///     Release the buffer associated with the handle
        /// </summary>
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        protected virtual bool ReleaseBuffer()
        {
            Marshal.FreeCoTaskMem(m_dataBuffer);
            return true;
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        protected sealed override bool ReleaseHandle()
        {
            bool error = false;

            if (handle != IntPtr.Zero)
            {
                error = ReleaseNativeHandle();
            }

            if (m_dataBuffer != IntPtr.Zero)
            {
                error &= ReleaseBuffer();
            }

            return error;
        }

        /// <summary>
        ///     Release just the native handle associated with the safe handle
        /// </summary>
        /// <returns></returns>
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        protected abstract bool ReleaseNativeHandle();
    }

    /// <summary>
    ///     SafeHandle for a native HMODULE
    /// </summary>
    internal sealed class SafeLibraryHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeLibraryHandle() : base(true)
        {
        }

        [DllImport("kernel32.dll")]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        [SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass", Justification = "SafeHandle release method")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeLibrary(IntPtr hModule);

        protected override bool ReleaseHandle()
        {
            return FreeLibrary(handle);
        }
    }

    /// <summary>
    ///     SafeHandle for memory allocated with LocalAlloc
    /// </summary>
    internal sealed class SafeLocalAllocHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeLocalAllocHandle() : base(true)
        {
        }

        [DllImport("kernel32.dll")]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        [SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass", Justification = "SafeHandle release method")]
        private static extern IntPtr LocalFree(IntPtr hMem);

        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Protected as a SecurityCritical method")]
        internal T Read<T>(int offset) where T : struct
        {
            bool addedRef = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref addedRef);

                unsafe
                {
                    IntPtr pBase = new IntPtr((byte*)handle.ToPointer() + offset);
                    return (T)Marshal.PtrToStructure(pBase, typeof(T));
                }
            }
            finally
            {
                if (addedRef)
                {
                    DangerousRelease();
                }
            }

        }

        protected override bool ReleaseHandle()
        {
            return LocalFree(handle) == IntPtr.Zero;
        }
    }

}
