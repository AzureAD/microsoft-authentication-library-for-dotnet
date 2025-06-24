﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Platforms.Features.DesktopOs
{
    // https://developer.apple.com/documentation/security/keychain_services/keychain_items
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
            Handle = LibSystem.dlopen(SecurityFrameworkLib, 0);

            kSecClass = LibSystem.GetGlobal(Handle, "kSecClass");
            kSecMatchLimit = LibSystem.GetGlobal(Handle, "kSecMatchLimit");
            kSecReturnAttributes = LibSystem.GetGlobal(Handle, "kSecReturnAttributes");
            kSecReturnRef = LibSystem.GetGlobal(Handle, "kSecReturnRef");
            kSecReturnPersistentRef = LibSystem.GetGlobal(Handle, "kSecReturnPersistentRef");
            kSecClassGenericPassword = LibSystem.GetGlobal(Handle, "kSecClassGenericPassword");
            kSecMatchLimitOne = LibSystem.GetGlobal(Handle, "kSecMatchLimitOne");
            kSecMatchItemList = LibSystem.GetGlobal(Handle, "kSecMatchItemList");
            kSecAttrLabel = LibSystem.GetGlobal(Handle, "kSecAttrLabel");
            kSecAttrAccount = LibSystem.GetGlobal(Handle, "kSecAttrAccount");
            kSecAttrService = LibSystem.GetGlobal(Handle, "kSecAttrService");
            kSecValueRef = LibSystem.GetGlobal(Handle, "kSecValueRef");
            kSecValueData = LibSystem.GetGlobal(Handle, "kSecValueData");
            kSecReturnData = LibSystem.GetGlobal(Handle, "kSecReturnData");
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

        [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int SecKeychainItemCopyAttributesAndData(
            IntPtr itemRef,
            IntPtr info,
            IntPtr itemClass,
            SecKeychainAttributeList** attrList,
            uint* dataLength,
            void** data);

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

    internal static class LibSystem
    {
        private const string LibSystemLib = "/System/Library/Frameworks/System.framework/System";

        [DllImport(LibSystemLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr dlopen(string name, int flags);

        [DllImport(LibSystemLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr dlsym(IntPtr handle, string symbol);

        public static IntPtr GetGlobal(IntPtr handle, string symbol)
        {
            IntPtr ptr = dlsym(handle, symbol);
            return Marshal.PtrToStructure<IntPtr>(ptr);
        }
    }

    internal static class LibObjc
    {
        private const string LibObjcLib = "/usr/lib/libobjc.dylib";

        public static bool IsNsApplicationRunning()
        {
            // This function equals to calling objc code: `[[NSApplication sharedApplication] isRunning]`
            // The result indicates if there is an official Apple message loop running, we can use it as
            // whether it is UI-based app (MAUI) or console app.
            try
            {
                IntPtr nsApplicationClass = objc_getClass("NSApplication");
                if (nsApplicationClass == IntPtr.Zero)
                {
                    return false;
                }

                IntPtr sharedApplicationSelector = sel_registerName("sharedApplication");
                if (sharedApplicationSelector == IntPtr.Zero)
                {
                    return false;
                }

                IntPtr sharedApplication = objc_msgSend(nsApplicationClass, sharedApplicationSelector);
                if (sharedApplication == IntPtr.Zero)
                {
                    return false;
                }

                IntPtr isRunningSelector = sel_registerName("isRunning");
                if (isRunningSelector == IntPtr.Zero)
                {
                    return false;
                }

                IntPtr isRunningResult = objc_msgSend(sharedApplication, isRunningSelector);
                bool isRunning = isRunningResult != IntPtr.Zero;
                return isRunning;
            }
            catch (Exception)
            {
                return false;
            }
        }

        [DllImport(LibObjcLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr objc_getClass(string name);

        [DllImport(LibObjcLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr sel_registerName(string name);

        [DllImport(LibObjcLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

        [DllImport(LibObjcLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool objc_msgSend_bool(IntPtr receiver, IntPtr selector);

    }

}
