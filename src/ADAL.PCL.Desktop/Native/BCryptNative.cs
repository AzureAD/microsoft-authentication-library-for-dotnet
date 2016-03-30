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
    // Public facing enumerations
    //

    /// <summary>
    ///     Padding modes 
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags", Justification = "Public use of the enum is not as flags")]
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Justification = "The native BCRYPT_PAD_NONE value is 1, not 0, and this is for interop.")]
    public enum AsymmetricPaddingMode
    {
        /// <summary>
        ///     No padding
        /// </summary>
        None = 1,                       // BCRYPT_PAD_NONE

        /// <summary>
        ///     PKCS #1 padding
        /// </summary>
        Pkcs1 = 2,                      // BCRYPT_PAD_PKCS1

        /// <summary>
        ///     Optimal Asymmetric Encryption Padding
        /// </summary>
        Oaep = 4,                       // BCRYPT_PAD_OAEP

        /// <summary>
        ///     Probabilistic Signature Scheme padding
        /// </summary>
        Pss = 8                         // BCRYPT_PAD_PSS
    }

    /// <summary>
    ///     Native wrappers for bcrypt CNG APIs.
    ///     
    ///     The general pattern for this interop layer is that the BCryptNative type exports a wrapper method
    ///     for consumers of the interop methods.  This wrapper method puts a managed face on the raw
    ///     P/Invokes, by translating from native structures to managed types and converting from error
    ///     codes to exceptions.
    /// </summary>
    internal static class BCryptNative
    {
        //
        // Enumerations
        //

        /// <summary>
        ///     Well known algorithm names
        /// </summary>
        internal static class AlgorithmName
        {
            internal const string Aes = "AES";                          // BCRYPT_AES_ALGORITHM
            internal const string Rng = "RNG";                          // BCRYPT_RNG_ALGORITHM
            internal const string Rsa = "RSA";                          // BCRYPT_RSA_ALGORITHM
            internal const string TripleDes = "3DES";                   // BCRYPT_3DES_ALOGORITHM
            internal const string Sha1 = "SHA1";                        // BCRYPT_SHA1_ALGORITHM
            internal const string Sha256 = "SHA256";                    // BCRYPT_SHA256_ALGORITHM
            internal const string Sha384 = "SHA384";                    // BCRYPT_SHA384_ALGORITHM
            internal const string Sha512 = "SHA512";                    // BCRYPT_SHA512_ALGORITHM
            internal const string Pbkdf2 = "PBKDF2";                    // BCRYPT_PBKDF2_ALGORITHM       
        }

        /// <summary>
        ///     Flags for BCryptOpenAlgorithmProvider
        /// </summary>
        [Flags]
        internal enum AlgorithmProviderOptions
        {
            None                = 0x00000000,
            HmacAlgorithm       = 0x00000008,                           // BCRYPT_ALG_HANDLE_HMAC_FLAG
        }

        /// <summary>
        ///     Flags for use with the BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO structure
        /// </summary>
        [Flags]
        internal enum AuthenticatedCipherModeInfoFlags
        {
            None                = 0x00000000,
            ChainCalls          = 0x00000001,                           // BCRYPT_AUTH_MODE_CHAIN_CALLS_FLAG
            InProgress          = 0x00000002,                           // BCRYPT_AUTH_MODE_IN_PROGRESS_FLAG
        }

        /// <summary>
        ///     Well known chaining modes
        /// </summary>
        internal static class ChainingMode
        {
            internal const string Cbc = "ChainingModeCBC";              // BCRYPT_CHAIN_MODE_CBC
            internal const string Ccm = "ChainingModeCCM";              // BCRYPT_CHAIN_MODE_CCM
            internal const string Cfb = "ChainingModeCFB";              // BCRYPT_CHAIN_MODE_CFB
            internal const string Ecb = "ChainingModeECB";              // BCRYPT_CHAIN_MODE_ECB
            internal const string Gcm = "ChainingModeGCM";              // BCRYPT_CHAIN_MODE_GCM
        }

        /// <summary>
        ///     Result codes from BCrypt APIs
        /// </summary>
        internal enum ErrorCode
        {
            Success = 0x00000000,                                       // STATUS_SUCCESS
            AuthenticationTagMismatch = unchecked((int)0xC000A002),     // STATUS_AUTH_TAG_MISMATCH
            BufferToSmall = unchecked((int)0xC0000023),                 // STATUS_BUFFER_TOO_SMALL
        }

        internal static class HashPropertyName
        {
            internal const string HashLength = "HashDigestLength";      // BCRYPT_HASH_LENGTH
        }

        /// <summary>
        ///     Magic numbers for different key blobs
        /// </summary>
        internal enum KeyBlobMagicNumber
        {
            RsaPublic = 0x31415352,                                     // BCRYPT_RSAPUBLIC_MAGIC
            RsaPrivate = 0x32415352,                                    // BCRYPT_RSAPRIVATE_MAGIC
            KeyDataBlob = 0x4d42444b,                                   // BCRYPT_KEY_DATA_BLOB_MAGIC
        }

        /// <summary>
        ///     Well known key blob tyes
        /// </summary>
        internal static class KeyBlobType
        {
            internal const string KeyDataBlob = "KeyDataBlob";                  // BCRYPT_KEY_DATA_BLOB
            internal const string RsaFullPrivateBlob = "RSAFULLPRIVATEBLOB";    // BCRYPT_RSAFULLPRIVATE_BLOB
            internal const string RsaPrivateBlob = "RSAPRIVATEBLOB";            // BCRYPT_RSAPRIVATE_BLOB
            internal const string RsaPublicBlob = "RSAPUBLICBLOB";              // BCRYPT_PUBLIC_KEY_BLOB
        }

        /// <summary>
        /// BCrypt parameter types (used in parameter lists)
        /// </summary>
        internal enum ParameterTypes
        {
            KdfHashAlgorithm = 0x0,
            KdfSalt = 0xF,
            KdfIterationCount = 0x10
        }

        /// <summary>
        ///     Well known BCrypt provider names
        /// </summary>
        internal static class ProviderName
        {
            internal const string MicrosoftPrimitiveProvider = "Microsoft Primitive Provider";      // MS_PRIMITIVE_PROVIDER
        }

        //
        // Structures
        //

        [StructLayout(LayoutKind.Sequential)]
        [SuppressMessage("Microsoft.Design", "CA1049:TypesThatOwnNativeResourcesShouldBeDisposable", Justification = "The resouces lifetime is owned by the containing type - as a value type, the pointers will be copied and are not owned by the value type itself.")]
        internal struct BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO
        {
            internal int cbSize;
            internal int dwInfoVersion;

            [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Justification = "The handle is not owned by the value type")]
            internal IntPtr pbNonce;            // byte *
            internal int cbNonce;

            [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Justification = "The handle is not owned by the value type")]
            internal IntPtr pbAuthData;         // byte *
            internal int cbAuthData;

            [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Justification = "The handle is not owned by the value type")]
            internal IntPtr pbTag;              // byte *
            internal int cbTag;

            [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Justification = "The handle is not owned by the value type")]
            internal IntPtr pbMacContext;       // byte *
            internal int cbMacContext;

            internal int cbAAD;
            internal long cbData;
            internal AuthenticatedCipherModeInfoFlags dwFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct BCRYPT_KEY_DATA_BLOB
        {
            internal KeyBlobMagicNumber dwMagic;
            internal int dwVersion;
            internal int cbKeyData;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct BCRYPT_KEY_LENGTHS_STRUCT
        {
            internal int dwMinLength;
            internal int dwMaxLength;
            internal int dwIncrement;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct BCRYPT_OAEP_PADDING_INFO
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            internal string pszAlgId;

            internal IntPtr pbLabel;

            internal int cbLabel;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct BCRYPT_PKCS1_PADDING_INFO
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            internal string pszAlgId;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct BCRYPT_PSS_PADDING_INFO
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            internal string pszAlgId;

            internal int cbSalt;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct BCRYPT_RSAKEY_BLOB
        {
            internal KeyBlobMagicNumber Magic;
            internal int BitLength;
            internal int cbPublicExp;
            internal int cbModulus;
            internal int cbPrime1;
            internal int cbPrime2;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct BCryptBuffer
        {
            internal int cbBuffer;
            internal int BufferType;
            internal IntPtr pvBuffer;       // PVOID
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct BCryptBufferDesc
        {
            internal int ulVersion;
            internal int cBuffers;
            internal IntPtr pBuffers;       // PBCryptBuffer
        }

     }       // end class BCryptNative

    /// <summary>
    ///     SafeHandle for a native BCRYPT_ALG_HANDLE
    /// </summary>
    internal sealed class SafeBCryptAlgorithmHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeBCryptAlgorithmHandle() : base(true)
        {
            return;
        }

        [DllImport("bcrypt.dll")]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        [SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass", Justification = "SafeHandle release P/Invoke")]
        private static extern BCryptNative.ErrorCode BCryptCloseAlgorithmProvider(IntPtr hAlgorithm, int flags);

        protected override bool ReleaseHandle()
        {
            return BCryptCloseAlgorithmProvider(handle, 0) == BCryptNative.ErrorCode.Success;
        }
    }

    /// <summary>
    ///     SafeHandle for a BCRYPT_HASH_HANDLE.
    /// </summary>
    internal sealed class SafeBCryptHashHandle : SafeHandleWithBuffer
    {
        [DllImport("bcrypt.dll")]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        [SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass", Justification = "SafeHandle release P/Invoke")]
        private static extern BCryptNative.ErrorCode BCryptDestroyHash(IntPtr hHash);

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        protected override bool ReleaseNativeHandle()
        {
            return BCryptDestroyHash(handle) == BCryptNative.ErrorCode.Success;
        }
    }

    /// <summary>
    ///     SafeHandle for a native BCRYPT_KEY_HANDLE.
    /// </summary>
    internal sealed class SafeBCryptKeyHandle : SafeHandleWithBuffer
    {
        [DllImport("bcrypt.dll")]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        [SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass", Justification = "SafeHandle release P/Invoke")]
        private static extern BCryptNative.ErrorCode BCryptDestroyKey(IntPtr hKey);

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        protected override bool ReleaseNativeHandle()
        {
            return BCryptDestroyKey(handle) == BCryptNative.ErrorCode.Success;
        }
    }

}
