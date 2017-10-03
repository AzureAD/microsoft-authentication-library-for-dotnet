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
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal static class X509Native
    {
        //
        // Enumerations
        // 

        /// <summary>
        ///     Flags for the CryptAcquireCertificatePrivateKey API
        /// </summary>
        internal enum AcquireCertificateKeyOptions
        {
            None = 0x00000000,
            AcquireOnlyNCryptKeys = 0x00040000,   // CRYPT_ACQUIRE_ONLY_NCRYPT_KEY_FLAG
        }

        //
        // P/Invokes
        //
        [SuppressUnmanagedCodeSecurity]
        internal static class UnsafeNativeMethods
        {
            [DllImport("crypt32.dll")]
            internal static extern SafeCertContextHandle CertDuplicateCertificateContext(IntPtr certContext);       // CERT_CONTEXT *

            [DllImport("crypt32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CryptAcquireCertificatePrivateKey(SafeCertContextHandle pCert,
                                                                          AcquireCertificateKeyOptions dwFlags,
                                                                          IntPtr pvReserved,        // void *
                                                                          [Out] out SafeNCryptKeyHandle phCryptProvOrNCryptKey,
                                                                          [Out] out int dwKeySpec,
                                                                          [Out, MarshalAs(UnmanagedType.Bool)] out bool pfCallerFreeProvOrNCryptKey);
        }


        /// <summary>
        ///     Duplicate the certificate context into a safe handle
        /// </summary>
        [SecurityCritical]
        internal static SafeCertContextHandle DuplicateCertContext(IntPtr context)
        {
            Debug.Assert(context != IntPtr.Zero);

            return UnsafeNativeMethods.CertDuplicateCertificateContext(context);
        }

        /// <summary>
        ///     Get the private key of a certificate
        /// </summary>
        [SecurityCritical]
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Safe use of LinkDemands")]
        internal static SafeNCryptKeyHandle AcquireCngPrivateKey(SafeCertContextHandle certificateContext)
        {
            Debug.Assert(certificateContext != null, "certificateContext != null");
            Debug.Assert(!certificateContext.IsClosed && !certificateContext.IsInvalid, "!certificateContext.IsClosed && !certificateContext.IsInvalid");

            bool freeKey = true;
            SafeNCryptKeyHandle privateKey = null;

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                int keySpec = 0;
                if (!UnsafeNativeMethods.CryptAcquireCertificatePrivateKey(certificateContext,
                                                                           AcquireCertificateKeyOptions.AcquireOnlyNCryptKeys,
                                                                           IntPtr.Zero,
                                                                           out privateKey,
                                                                           out keySpec,
                                                                           out freeKey))
                {
                    throw new CryptographicException(Marshal.GetLastWin32Error());
                }

                return privateKey;
            }
            finally
            {
                // If we're not supposed to release they key handle, then we need to bump the reference count
                // on the safe handle to correspond to the reference that Windows is holding on to.  This will
                // prevent the CLR from freeing the object handle.
                // 
                // This is certainly not the ideal way to solve this problem - it would be better for
                // SafeNCryptKeyHandle to maintain an internal bool field that we could toggle here and
                // have that suppress the release when the CLR calls the ReleaseHandle override.  However, that
                // field does not currently exist, so we'll use this hack instead.
                if (privateKey != null && !freeKey)
                {
                    bool addedRef = false;
                    privateKey.DangerousAddRef(ref addedRef);
                }
            }
        }
    }
}
