// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Identity.Client.Platforms.net45.Native
{
    /// <summary>
    ///     Safe handle base class for safe handles which are associated with an additional data buffer that
    ///     must be kept alive for the same amount of time as the handle itself.
    ///     
    ///     This is required rather than having a separate safe handle own the key data buffer blob so
    ///     that we can ensure that the key handle is disposed of before the key data buffer is freed.
    /// </summary>
    internal abstract class SafeHandleWithBuffer : SafeHandleZeroOrMinusOneIsInvalid
    {
        private IntPtr m_dataBuffer;

        protected SafeHandleWithBuffer() : base(true)
        {
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
        ///     or the ReleaseBuffer method must be overridden to match the deallocation function with the
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
        public SafeLibraryHandle() : base(true)
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
        public SafeLocalAllocHandle() : base(true)
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
