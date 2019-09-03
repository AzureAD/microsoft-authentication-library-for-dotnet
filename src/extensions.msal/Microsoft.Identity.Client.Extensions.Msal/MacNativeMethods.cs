// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Identity.Client.Extensions.Msal
{
/// <summary>
/// Helper native methods for MAC keychain
/// </summary>
internal static class MacNativeMethods
    {
        /// <summary>
        /// Location of the security framework
        /// </summary>
        public const string securityFrameworkLib = "/System/Library/Frameworks/Security.framework/Security";

        /// <summary>
        /// Location of the core foundation libs
        /// </summary>
        public const string coreFoundationFrameworkLib = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

        /// <summary>
        /// Definitions for all Keychain error codes can be found at:
        /// https://developer.apple.com/library/mac/documentation/Security/Reference/keychainservices/#//apple_ref/doc/uid/TP30000898-CH5g-CJBEABHG
        /// </summary>
        public const int errSecSuccess = 0;

        /// <summary>
        /// See link for errSucSuccess
        /// </summary>
        public const int errSecDuplicateItem = -25299;

        /// <summary>
        /// See link for errSucSuccess
        /// </summary>
        public const int errSecItemNotFound = -25300;

        /// <summary>
        /// Find entry in the keychian
        /// </summary>
        /// <param name="keychainOrArray">Store</param>
        /// <param name="serviceNameLength">Length of service name</param>
        /// <param name="serviceName">Service name</param>
        /// <param name="accountNameLength">Account name length</param>
        /// <param name="accountName">account name</param>
        /// <param name="passwordLength">password length</param>
        /// <param name="passwordData">Password data</param>
        /// <param name="itemRef">Item reference</param>
        /// <returns>Code indicating result</returns>
        [DllImport(securityFrameworkLib)]
        internal static extern int SecKeychainFindGenericPassword(
                                                                  IntPtr keychainOrArray,
                                                                  uint serviceNameLength,
                                                                  string serviceName,
                                                                  uint accountNameLength,
                                                                  string accountName,
                                                                  out uint passwordLength,
                                                                  out IntPtr passwordData,
                                                                  out IntPtr itemRef);

        /// <summary>
        /// Add a password.
        /// </summary>
        /// <param name="keychain">Pointer to the keychain</param>
        /// <param name="serviceNameLength">Service name length</param>
        /// <param name="serviceName">Service name</param>
        /// <param name="accountNameLength">account name length</param>
        /// <param name="accountName"> account name</param>
        /// <param name="passwordLength">password length</param>
        /// <param name="passwordData">password</param>
        /// <param name="itemRef">Item from the keychain</param>
        /// <returns>Code indicating result</returns>
        [DllImport(securityFrameworkLib)]
        internal static extern int SecKeychainAddGenericPassword(
                                                                 IntPtr keychain,
                                                                 uint serviceNameLength,
                                                                 string serviceName,
                                                                 uint accountNameLength,
                                                                 string accountName,
                                                                 uint passwordLength,
                                                                 byte[] passwordData,
                                                                 out IntPtr itemRef);

        /// <summary>
        /// Modify keychain data
        /// </summary>
        /// <param name="itemRef">Item to modify</param>
        /// <param name="attrList">Attribute list</param>
        /// <param name="length">Length of list</param>
        /// <param name="data">data</param>
        /// <returns>Code indicating result</returns>
        [DllImport(securityFrameworkLib)]
        internal static extern int SecKeychainItemModifyAttributesAndData(
                                                                          IntPtr itemRef,
                                                                          IntPtr attrList,
                                                                          uint length,
                                                                          byte[] data);

        /// <summary>
        /// Delete an item from the keychain
        /// </summary>
        /// <param name="itemRef">Item to delete</param>
        /// <returns>Code indicating result</returns>
        [DllImport(securityFrameworkLib)]
        internal static extern int SecKeychainItemDelete(IntPtr itemRef);

        /// <summary>
        /// Free item from memeory
        /// </summary>
        /// <param name="attrList">attribute list</param>
        /// <param name="data">Data</param>
        /// <returns>Code indicating the result</returns>
        [DllImport(securityFrameworkLib)]
        internal static extern int SecKeychainItemFreeContent(IntPtr attrList, IntPtr data);

        /// <summary>
        /// Release pointer
        /// </summary>
        /// <param name="cf">Pointer to release</param>
        [DllImport(coreFoundationFrameworkLib)]
        internal static extern void CFRelease(IntPtr cf);
    }
}
