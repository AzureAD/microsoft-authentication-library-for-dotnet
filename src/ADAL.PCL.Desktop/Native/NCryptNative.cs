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
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory.Native
{
    //
    // Public facing interop enumerations
    //

    /// <summary>
    ///     Algorithm classes exposed by NCrypt
    /// </summary>
    [Flags]
    internal enum NCryptAlgorithmOperations
    {
        None = 0x00000000,
        Cipher = 0x00000001,                        // NCRYPT_CIPHER_OPERATION
        Hash = 0x00000002,                          // NCRYPT_HASH_OPERATION
        AsymmetricEncryption = 0x00000004,          // NCRYPT_ASYMMETRIC_ENCRYPTION_OPERATION
        SecretAgreement = 0x00000008,               // NCRYPT_SECRET_AGREEMENT_OPERATION
        Signature = 0x00000010,                     // NCRYPT_SIGNATURE_OPERATION
        RandomNumberGeneration = 0x00000020,        // NCRYPT_RNG_OPERATION
    }

    /// <summary>
    ///     Native wrappers for ncrypt CNG APIs.
    ///     
    ///     The general pattern for this interop layer is that the NCryptNative type exports a wrapper method
    ///     for consumers of the interop methods.  This wrapper method puts a managed face on the raw
    ///     P/Invokes, by translating from native structures to managed types and converting from error
    ///     codes to exceptions.
    /// </summary>
    internal static class NCryptNative
    {
        //
        // Enumerations
        //

        /// <summary>
        ///     Well known key property names
        /// </summary>
        internal static class KeyPropertyName
        {
            internal const string Length = "Length";                // NCRYPT_LENGTH_PROPERTY
        }

        /// <summary>
        ///     NCrypt algorithm classes
        /// </summary>
        internal enum NCryptAlgorithmClass
        {
            None = 0x00000000,
            AsymmetricEncryption = 0x00000003,                      // NCRYPT_ASYMMETRIC_ENCRYPTION_INTERFACE
            SecretAgreement = 0x00000004,                           // NCRYPT_SECRET_AGREEMENT_INTERFACE
            Signature = 0x00000005,                                 // NCRYPT_SIGNATURE_INTERFACE
        }

        /// <summary>
        ///     Enum for some SECURITY_STATUS return codes
        /// </summary>
        internal enum ErrorCode
        {
            Success = 0x00000000,                                   // ERROR_SUCCESS
            BadSignature = unchecked((int)0x80090006),              // NTE_BAD_SIGNATURE
            BufferTooSmall = unchecked((int)0x80090028),            // NTE_BUFFER_TOO_SMALL
            NoMoreItems = unchecked((int)0x8009002a),               // NTE_NO_MORE_ITEMS
        }

        //
        // Structures
        //

        [StructLayout(LayoutKind.Sequential)]
        internal struct NCryptAlgorithmName
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            internal string pszName;

            internal NCryptAlgorithmClass dwClass;

            internal NCryptAlgorithmOperations dwAlgOperations;

            internal int dwFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct NCryptKeyName
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            internal string pszName;

            [MarshalAs(UnmanagedType.LPWStr)]
            internal string pszAlgId;

            internal int dwLegacyKeySpec;

            internal int dwFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct NCryptProviderName
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            internal string pszName;

            [MarshalAs(UnmanagedType.LPWStr)]
            internal string pszComment;
        }

        //
        // P/Invokes
        // 

        [SuppressUnmanagedCodeSecurity]
        internal static class UnsafeNativeMethods
        {
            [DllImport("ncrypt.dll")]
            internal static extern ErrorCode NCryptEnumAlgorithms(SafeNCryptProviderHandle hProvider,
                                                                  NCryptAlgorithmOperations dwAlgOperations,
                                                                  [Out] out uint pdwAlgCount,
                                                                  [Out] out SafeNCryptBuffer ppAlgList,
                                                                  int dwFlags);

            [DllImport("ncrypt.dll")]
            internal static extern ErrorCode NCryptEnumKeys(SafeNCryptProviderHandle hProvider,
                                                            [In, MarshalAs(UnmanagedType.LPWStr)] string pszScope,
                                                            [Out] out SafeNCryptBuffer ppKeyName,
                                                            [In, Out] ref IntPtr ppEnumState,
                                                            CngKeyOpenOptions dwFlags);

            [DllImport("ncrypt.dll")]
            internal static extern ErrorCode NCryptEnumStorageProviders([Out] out uint pdwProviderCount,
                                                                        [Out] out SafeNCryptBuffer ppProviderList,
                                                                        int dwFlags);

            [DllImport("ncrypt.dll")]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            [SuppressUnmanagedCodeSecurity]
            internal static extern ErrorCode NCryptFreeBuffer(IntPtr pvInput);

            [DllImport("ncrypt.dll")]
            internal static extern ErrorCode NCryptOpenStorageProvider([Out] out SafeNCryptProviderHandle phProvider,
                                                                       [MarshalAs(UnmanagedType.LPWStr)] string pszProviderName,
                                                                       int dwFlags);

            [DllImport("ncrypt.dll")]
            internal static extern ErrorCode NCryptSignHash(SafeNCryptKeyHandle hKey,
                                                            [In] ref BCryptNative.BCRYPT_PKCS1_PADDING_INFO pPaddingInfo,
                                                            [In, MarshalAs(UnmanagedType.LPArray)] byte[] pbHashValue,
                                                            int cbHashValue,
                                                            [Out, MarshalAs(UnmanagedType.LPArray)] byte[] pbSignature,
                                                            int cbSignature,
                                                            [Out] out int pcbResult,
                                                            AsymmetricPaddingMode dwFlags);

            [DllImport("ncrypt.dll")]
            internal static extern ErrorCode NCryptSignHash(SafeNCryptKeyHandle hKey,
                                                            [In] ref BCryptNative.BCRYPT_PSS_PADDING_INFO pPaddingInfo,
                                                            [In, MarshalAs(UnmanagedType.LPArray)] byte[] pbHashValue,
                                                            int cbHashValue,
                                                            [Out, MarshalAs(UnmanagedType.LPArray)] byte[] pbSignature,
                                                            int cbSignature,
                                                            [Out] out int pcbResult,
                                                            AsymmetricPaddingMode dwFlags);
        }

        /// <summary>
        ///     Adapter to wrap specific NCryptDecrypt P/Invokes with specific padding info
        /// </summary>
        [SecurityCritical]
        private delegate ErrorCode NCryptDecryptor<T>(SafeNCryptKeyHandle hKey,
                                                      byte[] pbInput,
                                                      int cbInput,
                                                      ref T pvPadding,
                                                      byte[] pbOutput,
                                                      int cbOutput,
                                                      out int pcbResult,
                                                      AsymmetricPaddingMode dwFlags);

        /// <summary>
        ///     Adapter to wrap specific NCryptEncrypt P/Invokes with specific padding info
        /// </summary>
        [SecurityCritical]
        private delegate ErrorCode NCryptEncryptor<T>(SafeNCryptKeyHandle hKey,
                                                      byte[] pbInput,
                                                      int cbInput,
                                                      ref T pvPadding,
                                                      byte[] pbOutput,
                                                      int cbOutput,
                                                      out int pcbResult,
                                                      AsymmetricPaddingMode dwFlags);
        /// <summary>
        ///     Adapter to wrap specific NCryptSignHash P/Invokes with a specific padding info
        /// </summary>
        [SecurityCritical]
        private delegate ErrorCode NCryptHashSigner<T>(SafeNCryptKeyHandle hKey,
                                                       ref T pvPaddingInfo,
                                                       byte[] pbHashValue,
                                                       int cbHashValue,
                                                       byte[] pbSignature,
                                                       int cbSignature,
                                                       out int pcbResult,
                                                       AsymmetricPaddingMode dwFlags);


        /// <summary>
        ///     Generic signature method, wrapped by signature calls for specific padding modes
        /// </summary>
        [SecurityCritical]
        private static byte[] SignHash<T>(SafeNCryptKeyHandle key,
                                          byte[] hash,
                                          ref T paddingInfo,
                                          AsymmetricPaddingMode paddingMode,
                                          NCryptHashSigner<T> signer) where T : struct
        {
            Debug.Assert(key != null, "key != null");
            Debug.Assert(!key.IsInvalid && !key.IsClosed, "!key.IsInvalid && !key.IsClosed");
            Debug.Assert(hash != null, "hash != null");
            Debug.Assert(signer != null, "signer != null");

            // Figure out how big the signature is
            int signatureSize = 0;
            ErrorCode error = signer(key,
                                     ref paddingInfo,
                                     hash,
                                     hash.Length,
                                     null,
                                     0,
                                     out signatureSize,
                                     paddingMode);
            if (error != ErrorCode.Success && error != ErrorCode.BufferTooSmall)
            {
                throw new CryptographicException((int)error);
            }

            // Sign the hash
            byte[] signature = new byte[signatureSize];
            error = signer(key,
                           ref paddingInfo,
                           hash,
                           hash.Length,
                           signature,
                           signature.Length,
                           out signatureSize,
                           paddingMode);
            if (error != ErrorCode.Success)
            {
                throw new CryptographicException((int)error);
            }

            return signature;
        }

        /// <summary>
        ///     Sign a hash, using PKCS1 padding
        /// </summary>
        [SecurityCritical]
        internal static byte[] SignHashPkcs1(SafeNCryptKeyHandle key,
                                             byte[] hash,
                                             string hashAlgorithm)
        {
            Debug.Assert(key != null, "key != null");
            Debug.Assert(!key.IsClosed && !key.IsInvalid, "!key.IsClosed && !key.IsInvalid");
            Debug.Assert(hash != null, "hash != null");
            Debug.Assert(!String.IsNullOrEmpty(hashAlgorithm), "!String.IsNullOrEmpty(hashAlgorithm)");

            BCryptNative.BCRYPT_PKCS1_PADDING_INFO pkcs1Info = new BCryptNative.BCRYPT_PKCS1_PADDING_INFO();
            pkcs1Info.pszAlgId = hashAlgorithm;

            return SignHash(key,
                            hash,
                            ref pkcs1Info,
                            AsymmetricPaddingMode.Pkcs1,
                            UnsafeNativeMethods.NCryptSignHash);
        }

        /// <summary>
        ///     Sign a hash, using PSS padding
        /// </summary>
        [SecurityCritical]
        internal static byte[] SignHashPss(SafeNCryptKeyHandle key,
                                           byte[] hash,
                                           string hashAlgorithm,
                                           int saltBytes)
        {
            Debug.Assert(key != null, "key != null");
            Debug.Assert(!key.IsClosed && !key.IsInvalid, "!key.IsClosed && !key.IsInvalid");
            Debug.Assert(hash != null, "hash != null");
            Debug.Assert(!String.IsNullOrEmpty(hashAlgorithm), "!String.IsNullOrEmpty(hashAlgorithm)");
            Debug.Assert(saltBytes >= 0, "saltBytes >= 0");

            BCryptNative.BCRYPT_PSS_PADDING_INFO pssInfo = new BCryptNative.BCRYPT_PSS_PADDING_INFO();
            pssInfo.pszAlgId = hashAlgorithm;
            pssInfo.cbSalt = saltBytes;

            return SignHash(key,
                            hash,
                            ref pssInfo,
                            AsymmetricPaddingMode.Pss,
                            UnsafeNativeMethods.NCryptSignHash);
        }
    }


    /// <summary>
    ///     Handle for buffers that need to be released with NCryptFreeBuffer
    /// </summary>
    internal sealed class SafeNCryptBuffer : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal SafeNCryptBuffer() : base(true)
        {
            return;
        }

        /// <summary>
        ///     Helper method to read a structure out of the buffer, treating it as if it were an array of
        ///     T.  This method does not do any validation that the read data is within the buffer itself. 
        ///     
        ///     Esentially, this method treats the safe handle as if it were a native T[], and returns
        ///     handle[index].  It will add enough padding space such that each T will begin on a
        ///     pointer-sized location.
        /// </summary>
        /// <typeparam name="T">type of structure to read from the buffer</typeparam>
        /// <param name="index">0 based index into the array to read the structure from</param>
        /// <returns>the value of the structure at the index into the array</returns>
        [SecurityCritical]
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Safe use by a critical method")]
        internal T ReadArray<T>(uint index) where T : struct
        {
            checked
            {
                // Figure out how big each structure is within the buffer.
                uint structSize = (uint)Marshal.SizeOf(typeof(T));
                if (structSize % UIntPtr.Size != 0)
                {
                    structSize += (uint)(UIntPtr.Size - (structSize % UIntPtr.Size));
                }

                unsafe
                {
                    UIntPtr pBufferBase = new UIntPtr(handle.ToPointer());
                    UIntPtr pElement = new UIntPtr(pBufferBase.ToUInt64() + (structSize * index));
                    return (T)Marshal.PtrToStructure(new IntPtr(pElement.ToPointer()), typeof(T));
                }
            }
        }

        protected override bool ReleaseHandle()
        {
            return NCryptNative.UnsafeNativeMethods.NCryptFreeBuffer(handle) == NCryptNative.ErrorCode.Success;
        }
    }
}
