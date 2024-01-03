// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using static Microsoft.Identity.Extensions.Mac.LibSystem;

namespace Microsoft.Identity.Extensions.Mac
{
#pragma warning disable IDE1006 // Naming Styles

    internal static class SecurityFramework
    {
        private const string SecurityFrameworkLib = "/System/Library/Frameworks/Security.framework/Security";

        public static readonly IntPtr Handle;
        public static readonly IntPtr kSecClass;
        public static readonly IntPtr kSecMatchLimit;
        public static readonly IntPtr kSecReturnAttributes;
        public static readonly IntPtr kSecReturnRef;
        public static readonly IntPtr kSecReturnPersistentRef;
        public static readonly IntPtr kSecClassGenericPassword;
        public static readonly IntPtr kSecMatchLimitOne;
        public static readonly IntPtr kSecMatchItemList;
        public static readonly IntPtr kSecAttrLabel;
        public static readonly IntPtr kSecAttrAccount;
        public static readonly IntPtr kSecAttrService;
        public static readonly IntPtr kSecValueRef;
        public static readonly IntPtr kSecValueData;
        public static readonly IntPtr kSecReturnData;

        static SecurityFramework()
        {
            Handle = dlopen(SecurityFrameworkLib, 0);

            kSecClass = GetGlobal(Handle, "kSecClass");
            kSecMatchLimit = GetGlobal(Handle, "kSecMatchLimit");
            kSecReturnAttributes = GetGlobal(Handle, "kSecReturnAttributes");
            kSecReturnRef = GetGlobal(Handle, "kSecReturnRef");
            kSecReturnPersistentRef = GetGlobal(Handle, "kSecReturnPersistentRef");
            kSecClassGenericPassword = GetGlobal(Handle, "kSecClassGenericPassword");
            kSecMatchLimitOne = GetGlobal(Handle, "kSecMatchLimitOne");
            kSecMatchItemList = GetGlobal(Handle, "kSecMatchItemList");
            kSecAttrLabel = GetGlobal(Handle, "kSecAttrLabel");
            kSecAttrAccount = GetGlobal(Handle, "kSecAttrAccount");
            kSecAttrService = GetGlobal(Handle, "kSecAttrService");
            kSecValueRef = GetGlobal(Handle, "kSecValueRef");
            kSecValueData = GetGlobal(Handle, "kSecValueData");
            kSecReturnData = GetGlobal(Handle, "kSecReturnData");
        }

        [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SessionGetInfo(int session, out int sessionId, out SessionAttributeBits attributes);

        [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SecAccessCreate(IntPtr descriptor, IntPtr trustedList, out IntPtr accessRef);

        [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SecKeychainItemCreateFromContent(IntPtr itemClass, IntPtr attrList, uint length,
            IntPtr data, IntPtr keychainRef, IntPtr initialAccess, out IntPtr itemRef);

        [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SecKeychainAddGenericPassword(
            IntPtr keychain,
            uint serviceNameLength,
            string serviceName,
            uint accountNameLength,
            string accountName,
            uint passwordLength,
            byte[] passwordData,
            out IntPtr itemRef);

        [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SecKeychainFindGenericPassword(
            IntPtr keychainOrArray,
            uint serviceNameLength,
            string serviceName,
            uint accountNameLength,
            string accountName,
            out uint passwordLength,
            out IntPtr passwordData,
            out IntPtr itemRef);

        //[DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        //public static extern unsafe int SecKeychainItemCopyAttributesAndData(
        //    IntPtr itemRef,
        //    IntPtr info,
        //    IntPtr itemClass,
        //    SecKeychainAttributeList** attrList,
        //    uint* dataLength,
        //    void** data);

        [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SecKeychainItemModifyAttributesAndData(
            IntPtr itemRef,
            IntPtr attrList, // SecKeychainAttributeList*
            uint length,
            byte[] data);

        [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SecKeychainItemDelete(
            IntPtr itemRef);

        [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SecKeychainItemFreeContent(
            IntPtr attrList, // SecKeychainAttributeList*
            IntPtr data);

        [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SecKeychainItemFreeAttributesAndData(
            IntPtr attrList, // SecKeychainAttributeList*
            IntPtr data);

        [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SecItemCopyMatching(IntPtr query, out IntPtr result);

        [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SecKeychainItemCopyFromPersistentReference(IntPtr persistentItemRef, out IntPtr itemRef);

        [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SecKeychainItemCopyContent(IntPtr itemRef, IntPtr itemClass, IntPtr attrList,
            out uint length, out IntPtr outData);

        public const int CallerSecuritySession = -1;

        // https://developer.apple.com/documentation/security/1542001-security_framework_result_codes
        public const int OK = 0;
        public const int ErrorSecNoSuchKeychain = -25294;
        public const int ErrorSecInvalidKeychain = -25295;
        public const int ErrorSecAuthFailed = -25293;
        public const int ErrorSecDuplicateItem = -25299;
        public const int ErrorSecItemNotFound = -25300;
        public const int ErrorSecInteractionNotAllowed = -25308;
        public const int ErrorSecInteractionRequired = -25315;
        public const int ErrorSecNoSuchAttr = -25303;

        public const int ErrSecUserCanceled = -128;

        public static void ThrowIfError(int error, string defaultErrorMessage = "Unknown error.")
        {
            switch (error)
            {
            case OK:
                return;
            case ErrorSecNoSuchKeychain:
                throw new InteropException("The keychain does not exist.", error);
            case ErrorSecInvalidKeychain:
                throw new InteropException("The keychain is not valid.", error);
            case ErrorSecAuthFailed:
                throw new InteropException("KeyChain authorization/authentication failed.", error);
            case ErrorSecDuplicateItem:
                throw new InteropException("KeyChain - the item already exists.", error);
            case ErrorSecItemNotFound:
                throw new InteropException("KeyChain - the item cannot be found.", error);
            case ErrorSecInteractionNotAllowed:
                throw new InteropException("KeyChain - interaction with the Security Server is not allowed.", error);
            case ErrorSecInteractionRequired:
                throw new InteropException("KeyChain - user interaction is required.", error);
            case ErrorSecNoSuchAttr:
                throw new InteropException("KeyChain - the attribute does not exist.", error);
            case ErrSecUserCanceled:
                throw new InteropException("KeyChain - user cancelled the operation.", error);
            default:
                throw new InteropException(defaultErrorMessage, error);
            }
        }
    }

    [Flags]
    internal enum SessionAttributeBits
    {
        SessionIsRoot = 0x0001,
        SessionHasGraphicAccess = 0x0010,
        SessionHasTty = 0x0020,
        SessionIsRemote = 0x1000,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SecKeychainAttributeInfo
    {
        public uint Count;
        public IntPtr Tag; // uint* (SecKeychainAttrType*)
        public IntPtr Format; // uint* (CssmDbAttributeFormat*)
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SecKeychainAttributeList
    {
        public uint Count;
        public IntPtr Attributes; // SecKeychainAttribute*
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SecKeychainAttribute
    {
        public SecKeychainAttrType Tag;
        public uint Length;
        public IntPtr Data;
    }

    internal enum CssmDbAttributeFormat : uint
    {
        String = 0,
        SInt32 = 1,
        UInt32 = 2,
        BigNum = 3,
        Real = 4,
        TimeDate = 5,
        Blob = 6,
        MultiUInt32 = 7,
        Complex = 8
    };

    internal enum SecKeychainAttrType : uint
    {
        // https://developer.apple.com/documentation/security/secitemattr/accountitemattr
        AccountItem = 1633903476,
    }
#pragma warning restore IDE1006 // Naming Styles

}

