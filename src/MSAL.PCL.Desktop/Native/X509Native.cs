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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Identity.Client
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
