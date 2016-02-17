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

namespace Microsoft.Identity.Client.Native
{
    //
    // Public facing interop enumerations
    //

    /// <summary>
    ///     Algorithm classes exposed by NCrypt
    /// </summary>
    [Flags]
    public enum NCryptAlgorithmOperations
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
            internal static extern ErrorCode NCryptDecrypt(SafeNCryptKeyHandle hKey,
                                                           [In, MarshalAs(UnmanagedType.LPArray)] byte[] pbInput,
                                                           int cbInput,
                                                           [In] ref BCryptNative.BCRYPT_OAEP_PADDING_INFO pvPadding,
                                                           [Out, MarshalAs(UnmanagedType.LPArray)] byte[] pbOutput,
                                                           int cbOutput,
                                                           [Out] out int pcbResult,
                                                           AsymmetricPaddingMode dwFlags);

            [DllImport("ncrypt.dll")]
            internal static extern ErrorCode NCryptDecrypt(SafeNCryptKeyHandle hKey,
                                                           [In, MarshalAs(UnmanagedType.LPArray)] byte[] pbInput,
                                                           int cbInput,
                                                           [In] ref BCryptNative.BCRYPT_PKCS1_PADDING_INFO pvPadding,
                                                           [Out, MarshalAs(UnmanagedType.LPArray)] byte[] pbOutput,
                                                           int cbOutput,
                                                           [Out] out int pcbResult,
                                                           AsymmetricPaddingMode dwFlags);

            [DllImport("ncrypt.dll")]
            internal static extern ErrorCode NCryptEncrypt(SafeNCryptKeyHandle hKey,
                                                           [In, MarshalAs(UnmanagedType.LPArray)] byte[] pbInput,
                                                           int cbInput,
                                                           [In] ref BCryptNative.BCRYPT_OAEP_PADDING_INFO pvPadding,
                                                           [Out, MarshalAs(UnmanagedType.LPArray)] byte[] pbOutput,
                                                           int cbOutput,
                                                           [Out] out int pcbResult,
                                                           AsymmetricPaddingMode dwFlags);

            [DllImport("ncrypt.dll")]
            internal static extern ErrorCode NCryptEncrypt(SafeNCryptKeyHandle hKey,
                                                           [In, MarshalAs(UnmanagedType.LPArray)] byte[] pbInput,
                                                           int cbInput,
                                                           [In] ref BCryptNative.BCRYPT_PKCS1_PADDING_INFO pvPadding,
                                                           [Out, MarshalAs(UnmanagedType.LPArray)] byte[] pbOutput,
                                                           int cbOutput,
                                                           [Out] out int pcbResult,
                                                           AsymmetricPaddingMode dwFlags);

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

            [DllImport("ncrypt.dll")]
            internal static extern ErrorCode NCryptVerifySignature(SafeNCryptKeyHandle hKey,
                                                                   [In] ref BCryptNative.BCRYPT_PKCS1_PADDING_INFO pPaddingInfo,
                                                                   [In, MarshalAs(UnmanagedType.LPArray)] byte[] pbHashValue,
                                                                   int cbHashValue,
                                                                   [In, MarshalAs(UnmanagedType.LPArray)] byte[] pbSignature,
                                                                   int cbSignature,
                                                                   AsymmetricPaddingMode dwFlags);

            [DllImport("ncrypt.dll")]
            internal static extern ErrorCode NCryptVerifySignature(SafeNCryptKeyHandle hKey,
                                                                   [In] ref BCryptNative.BCRYPT_PSS_PADDING_INFO pPaddingInfo,
                                                                   [In, MarshalAs(UnmanagedType.LPArray)] byte[] pbHashValue,
                                                                   int cbHashValue,
                                                                   [In, MarshalAs(UnmanagedType.LPArray)] byte[] pbSignature,
                                                                   int cbSignature,
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
        ///     Adapter to wrap specific NCryptVerifySignature P/Invokes with a specific padding info
        /// </summary>
        [SecurityCritical]
        private delegate ErrorCode NCryptSignatureVerifier<T>(SafeNCryptKeyHandle hKey,
                                                              ref T pvPaddingInfo,
                                                              byte[] pbHashValue,
                                                              int cbHashValue,
                                                              byte[] pbSignature,
                                                              int cbSignature,
                                                              AsymmetricPaddingMode dwFlags) where T : struct;

        //
        // Wrapper methods
        //

        /// <summary>
        ///     Generic decryption method, wrapped by decryption calls for specific padding modes
        /// </summary>
        [SecurityCritical]
        private static byte[] DecryptData<T>(SafeNCryptKeyHandle key,
                                             byte[] data,
                                             ref T paddingInfo,
                                             AsymmetricPaddingMode paddingMode,
                                             NCryptDecryptor<T> decryptor) where T : struct
        {
            Debug.Assert(key != null, "key != null");
            Debug.Assert(!key.IsClosed && !key.IsInvalid, "!key.IsClosed && !key.IsInvalid");
            Debug.Assert(data != null, "data != null");
            Debug.Assert(decryptor != null, "decryptor != null");

            // Figure out how big of a buffer is needed to store the decrypted data
            int decryptedSize = 0;
            ErrorCode error = decryptor(key,
                                        data,
                                        data.Length,
                                        ref paddingInfo,
                                        null,
                                        0,
                                        out decryptedSize,
                                        paddingMode);
            if (error != ErrorCode.Success && error != ErrorCode.BufferTooSmall)
            {
                throw new CryptographicException((int)error);
            }

            // Do the decryption
            byte[] decrypted = new byte[decryptedSize];
            error = decryptor(key,
                              data,
                              data.Length,
                              ref paddingInfo,
                              decrypted,
                              decrypted.Length,
                              out decryptedSize,
                              paddingMode);
            if (error != ErrorCode.Success)
            {
                throw new CryptographicException((int)error);
            }

            return decrypted;
        }

        /// <summary>
        ///     Decrypt data using OAEP padding
        /// </summary>
        [SecurityCritical]
        [SecuritySafeCritical]
        internal static byte[] DecryptDataOaep(SafeNCryptKeyHandle key,
                                               byte[] data,
                                               string hashAlgorithm)
        {
            Debug.Assert(!String.IsNullOrEmpty(hashAlgorithm), "!String.IsNullOrEmpty(hashAlgorithm)");

            BCryptNative.BCRYPT_OAEP_PADDING_INFO oaepInfo = new BCryptNative.BCRYPT_OAEP_PADDING_INFO();
            oaepInfo.pszAlgId = hashAlgorithm;

            return DecryptData(key,
                               data,
                               ref oaepInfo,
                               AsymmetricPaddingMode.Oaep,
                               UnsafeNativeMethods.NCryptDecrypt);
        }

        /// <summary>
        ///     Decrypt data using PKCS1 padding
        /// </summary>
        [SecurityCritical]
        [SecuritySafeCritical]
        internal static byte[] DecryptDataPkcs1(SafeNCryptKeyHandle key, byte[] data)
        {
            BCryptNative.BCRYPT_PKCS1_PADDING_INFO pkcs1Info = new BCryptNative.BCRYPT_PKCS1_PADDING_INFO();

            return DecryptData(key,
                               data,
                               ref pkcs1Info,
                               AsymmetricPaddingMode.Pkcs1,
                               UnsafeNativeMethods.NCryptDecrypt);
        }

        /// <summary>
        ///     Generic encryption method, wrapped by decryption calls for specific padding modes
        /// </summary>
        [SecurityCritical]
        [SecuritySafeCritical]
        private static byte[] EncryptData<T>(SafeNCryptKeyHandle key,
                                             byte[] data,
                                             ref T paddingInfo,
                                             AsymmetricPaddingMode paddingMode,
                                             NCryptEncryptor<T> encryptor) where T : struct
        {
            Debug.Assert(key != null, "key != null");
            Debug.Assert(!key.IsClosed && !key.IsInvalid, "!key.IsClosed && !key.IsInvalid");
            Debug.Assert(data != null, "data != null");
            Debug.Assert(encryptor != null, "encryptor != null");

            // Figure out how big of a buffer is to encrypt the data
            int encryptedSize = 0;
            ErrorCode error = encryptor(key,
                                        data,
                                        data.Length,
                                        ref paddingInfo,
                                        null,
                                        0,
                                        out encryptedSize,
                                        paddingMode);
            if (error != ErrorCode.Success && error != ErrorCode.BufferTooSmall)
            {
                throw new CryptographicException((int)error);
            }

            // Do the encryption
            byte[] encrypted = new byte[encryptedSize];
            error = encryptor(key,
                              data,
                              data.Length,
                              ref paddingInfo,
                              encrypted,
                              encrypted.Length,
                              out encryptedSize,
                              paddingMode);
            if (error != ErrorCode.Success)
            {
                throw new CryptographicException((int)error);
            }

            return encrypted;
        }

        /// <summary>
        ///     Encrypt data using OAEP padding
        /// </summary>
        [SecurityCritical]
        [SecuritySafeCritical]
        internal static byte[] EncryptDataOaep(SafeNCryptKeyHandle key,
                                               byte[] data,
                                               string hashAlgorithm)
        {
            Debug.Assert(!String.IsNullOrEmpty(hashAlgorithm), "!String.IsNullOrEmpty(hashAlgorithm)");

            BCryptNative.BCRYPT_OAEP_PADDING_INFO oaepInfo = new BCryptNative.BCRYPT_OAEP_PADDING_INFO();
            oaepInfo.pszAlgId = hashAlgorithm;

            return EncryptData(key,
                               data,
                               ref oaepInfo,
                               AsymmetricPaddingMode.Oaep,
                               UnsafeNativeMethods.NCryptEncrypt);
        }

        /// <summary>
        ///     Encrypt data using PKCS1 padding
        /// </summary>
        [SecurityCritical]
        [SecuritySafeCritical]
        internal static byte[] EncryptDataPkcs1(SafeNCryptKeyHandle key, byte[] data)
        {
            BCryptNative.BCRYPT_PKCS1_PADDING_INFO pkcs1Info = new BCryptNative.BCRYPT_PKCS1_PADDING_INFO();

            return EncryptData(key,
                               data,
                               ref pkcs1Info,
                               AsymmetricPaddingMode.Pkcs1,
                               UnsafeNativeMethods.NCryptEncrypt);
        }

        /// <summary>
        ///     Get an array of information about all of the algorithms supported by a provider
        /// </summary>
        [SecurityCritical]
        [SecuritySafeCritical]
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Safe use of Dispose")]
        internal static NCryptAlgorithmName[] EnumerateAlgorithms(SafeNCryptProviderHandle provider,
                                                                  NCryptAlgorithmOperations operations)
        {
            Debug.Assert(provider != null && !provider.IsClosed && !provider.IsInvalid, "Invalid provider");

            uint algorithmCount = 0;
            SafeNCryptBuffer algorithmBuffer = null;

            try
            {
                // Ask CNG for the list of algorithms
                ErrorCode enumStatus = UnsafeNativeMethods.NCryptEnumAlgorithms(provider,
                                                                                operations,
                                                                                out algorithmCount,
                                                                                out algorithmBuffer,
                                                                                0);
                if (enumStatus != ErrorCode.Success)
                {
                    throw new CryptographicException((int)enumStatus);
                }

                // Copy the algorithm names into a managed array
                NCryptAlgorithmName[] algorithms = new NCryptAlgorithmName[algorithmCount];
                for (uint i = 0; i < algorithms.Length; ++i)
                {
                    algorithms[i] = algorithmBuffer.ReadArray<NCryptAlgorithmName>(i);
                }

                return algorithms;
            }
            finally
            {
                if (algorithmBuffer != null)
                {
                    algorithmBuffer.Dispose();
                }
            }
        }

        /// <summary>
        ///     Get an array of information about the keys stored in a KSP
        /// </summary>
        [SecurityCritical]
        [SecuritySafeCritical]
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Safe use of LinkDemands")]
        internal static NCryptKeyName[] EnumerateKeys(SafeNCryptProviderHandle provider,
                                                      CngKeyOpenOptions openOptions)
        {
            Debug.Assert(provider != null && !provider.IsClosed && !provider.IsInvalid, "Invalid provider");

            IntPtr enumState = IntPtr.Zero;
            SafeNCryptBuffer algorithmBuffer = null;

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                List<NCryptKeyName> keys = new List<NCryptKeyName>();

                ErrorCode enumStatus = ErrorCode.Success;

                // Loop over the NCryptEnumKeys until it tells us that there are no more keys to enumerate
                do
                {
                    enumStatus = UnsafeNativeMethods.NCryptEnumKeys(provider,
                                                                    null,
                                                                    out algorithmBuffer,
                                                                    ref enumState,
                                                                    openOptions);

                    if (enumStatus == ErrorCode.Success)
                    {
                        keys.Add(algorithmBuffer.ReadArray<NCryptKeyName>(0));
                    }
                    else if (enumStatus != ErrorCode.NoMoreItems)
                    {
                        throw new CryptographicException((int)enumStatus);
                    }
                }
                while (enumStatus == ErrorCode.Success);

                return keys.ToArray();
            }
            finally
            {
                if (enumState != IntPtr.Zero)
                {
                    UnsafeNativeMethods.NCryptFreeBuffer(enumState);
                }

                if (algorithmBuffer != null)
                {
                    algorithmBuffer.Dispose();
                }
            }
        }

        /// <summary>
        ///     Get an array of information about all of the installed storage providers on the machine
        /// </summary>
        [SecurityCritical]
        [SecuritySafeCritical]
        internal static NCryptProviderName[] EnumerateStorageProviders()
        {
            uint providerCount = 0;
            SafeNCryptBuffer providerBuffer = null;

            try
            {
                // Ask CNG for the raw list of providers
                ErrorCode enumStatus = UnsafeNativeMethods.NCryptEnumStorageProviders(out providerCount,
                                                                                      out providerBuffer,
                                                                                      0);

                if (enumStatus != ErrorCode.Success)
                {
                    throw new CryptographicException((int)enumStatus);
                }

                // Copy the provider names into a managed array
                NCryptProviderName[] providers = new NCryptProviderName[providerCount];
                for (uint i = 0; i < providers.Length; ++i)
                {
                    providers[i] = providerBuffer.ReadArray<NCryptProviderName>(i);
                }

                return providers;
            }
            finally
            {
                if (providerBuffer != null)
                {
                    providerBuffer.Dispose();
                }
            }
        }

        /// <summary>
        ///     Open a raw handle to a KSP
        /// </summary>
        [SecurityCritical]
        internal static SafeNCryptProviderHandle OpenKeyStorageProvider(string providerName)
        {
            Debug.Assert(!String.IsNullOrEmpty(providerName), "!String.IsNullOrEmpty(providerName)");

            SafeNCryptProviderHandle providerHandle = null;
            ErrorCode openStatus = UnsafeNativeMethods.NCryptOpenStorageProvider(out providerHandle,
                                                                                 providerName,
                                                                                 0);
            if (openStatus != ErrorCode.Success)
            {
                throw new CryptographicException((int)openStatus);
            }

            return providerHandle;
        }

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

        /// <summary>
        ///     Generic signature verification method, wrapped by verification calls for specific padding modes
        /// </summary>
        [SecurityCritical]
        [SecuritySafeCritical]
        private static bool VerifySignature<T>(SafeNCryptKeyHandle key,
                                               byte[] hash,
                                               byte[] signature,
                                               ref T paddingInfo,
                                               AsymmetricPaddingMode paddingMode,
                                               NCryptSignatureVerifier<T> verifier) where T : struct
        {
            Debug.Assert(key != null, "key != null");
            Debug.Assert(!key.IsClosed && !key.IsInvalid, "!key.IsClosed && !key.IsInvalid");
            Debug.Assert(hash != null, "hash != null");
            Debug.Assert(signature != null, "signature != null");
            Debug.Assert(verifier != null, "verifier != null");

            ErrorCode error = verifier(key,
                                       ref paddingInfo,
                                       hash,
                                       hash.Length,
                                       signature,
                                       signature.Length,
                                       paddingMode);
            if (error != ErrorCode.Success && error != ErrorCode.BadSignature)
            {
                throw new CryptographicException((int)error);
            }

            return error == ErrorCode.Success;
        }

        /// <summary>
        ///     Verify the signature of a hash using PKCS #1 padding
        /// </summary>
        [SecurityCritical]
        [SecuritySafeCritical]
        internal static bool VerifySignaturePkcs1(SafeNCryptKeyHandle key,
                                                  byte[] hash,
                                                  string hashAlgorithm,
                                                  byte[] signature)
        {
            Debug.Assert(key != null, "key != null");
            Debug.Assert(!key.IsClosed && !key.IsInvalid, "!key.IsClosed && !key.IsInvalid");
            Debug.Assert(hash != null, "hash != null");
            Debug.Assert(!String.IsNullOrEmpty(hashAlgorithm), "!String.IsNullOrEmpty(hashAlgorithm)");
            Debug.Assert(signature != null, "signature != null");

            BCryptNative.BCRYPT_PKCS1_PADDING_INFO pkcs1Info = new BCryptNative.BCRYPT_PKCS1_PADDING_INFO();
            pkcs1Info.pszAlgId = hashAlgorithm;

            return VerifySignature(key,
                                   hash,
                                   signature,
                                   ref pkcs1Info,
                                   AsymmetricPaddingMode.Pkcs1,
                                   UnsafeNativeMethods.NCryptVerifySignature);
        }

        /// <summary>
        ///     Verify the signature of a hash using PSS padding
        /// </summary>
        [SecurityCritical]
        [SecuritySafeCritical]
        internal static bool VerifySignaturePss(SafeNCryptKeyHandle key,
                                                byte[] hash,
                                                string hashAlgorithm,
                                                int saltBytes,
                                                byte[] signature)
        {
            Debug.Assert(key != null, "key != null");
            Debug.Assert(!key.IsClosed && !key.IsInvalid, "!key.IsClosed && !key.IsInvalid");
            Debug.Assert(hash != null, "hash != null");
            Debug.Assert(!String.IsNullOrEmpty(hashAlgorithm), "!String.IsNullOrEmpty(hashAlgorithm)");
            Debug.Assert(signature != null, "signature != null");

            BCryptNative.BCRYPT_PSS_PADDING_INFO pssInfo = new BCryptNative.BCRYPT_PSS_PADDING_INFO();
            pssInfo.pszAlgId = hashAlgorithm;
            pssInfo.cbSalt = saltBytes;

            return VerifySignature(key,
                                   hash,
                                   signature,
                                   ref pssInfo,
                                   AsymmetricPaddingMode.Pss,
                                   UnsafeNativeMethods.NCryptVerifySignature);
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
