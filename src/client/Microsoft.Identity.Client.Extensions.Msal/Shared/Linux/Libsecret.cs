// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Identity.Client.Extensions.Msal
{
    /// <summary>
    /// Data structures and methods required for saving and retrieving secret using keyring in linux
    /// https://developer.gnome.org/libsecret/0.18/
    /// </summary>
    internal static class Libsecret
    {
        /// <summary>
        /// type of the attribute of the schema for the secret store
        /// </summary>
        public enum SecretSchemaAttributeType
        {
            /// <summary>
            /// string attribute
            /// </summary>
            SECRET_SCHEMA_ATTRIBUTE_STRING = 0,

            /// <summary>
            /// integer attribute
            /// </summary>
            SECRET_SCHEMA_ATTRIBUTE_INTEGER = 1,

            /// <summary>
            /// boolean attribute
            /// </summary>
            SECRET_SCHEMA_ATTRIBUTE_BOOLEAN = 2,
        }

        /// <summary>
        /// flags for the schema creation
        /// </summary>
        public enum SecretSchemaFlags
        {
            /// <summary>
            /// no specific flag
            /// </summary>
            SECRET_SCHEMA_NONE = 0,

            /// <summary>
            /// during matching of the schema, set this flag to skip matching the name
            /// </summary>
            SECRET_SCHEMA_DONT_MATCH_NAME = 1 << 1,
        }

#pragma warning disable SA1300 // suppressing warning for lowercase function name

        /// <summary>
        /// creates a schema for saving secret
        /// </summary>
        /// <param name="name">Name of the schema</param>
        /// <param name="flags">flags to skip matching name for comparison</param>
        /// <param name="attribute1">first attribute of the schema</param>
        /// <param name="attribute1Type">type of the first attribute</param>
        /// <param name="attribute2">second attribute of the schema</param>
        /// <param name="attribute2Type">type of the second attribute</param>
        /// <param name="end">null parameter to indicate end of attributes</param>
        /// <returns>a schema for saving and retrieving secret</returns>
        [DllImport("libsecret-1.so.0", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr secret_schema_new(string name, int flags, string attribute1, int attribute1Type, string attribute2, int attribute2Type, IntPtr end);

        /// <summary>
        /// saves a secret in the secret store using the keyring
        /// </summary>
        /// <param name="schema">schema for saving secret</param>
        /// <param name="collection">collection where to save the secret</param>
        /// <param name="label">label of the secret</param>
        /// <param name="password">the secret to save</param>
        /// <param name="cancellable">optional GCancellable object or null</param>
        /// <param name="error">error encountered during saving</param>
        /// <param name="attribute1Type">type of the first attribute</param>
        /// <param name="attribute1Value">value of the first attribute</param>
        /// <param name="attribute2Type">type of the second attribute</param>
        /// <param name="attribute2Value">value of the second attribute</param>
        /// <param name="end">null parameter to indicate end of attributes</param>
        /// <returns>whether the save is successful or not</returns>
        [DllImport("libsecret-1.so.0", CallingConvention = CallingConvention.StdCall)]
        public static extern int secret_password_store_sync(IntPtr schema, string collection, string label, string password, IntPtr cancellable, out IntPtr error, string attribute1Type, string attribute1Value, string attribute2Type, string attribute2Value, IntPtr end);

        /// <summary>
        /// retrieve a secret from the secret store using the keyring
        /// </summary>
        /// <param name="schema">schema for retrieving secret</param>
        /// <param name="cancellable">optional GCancellable object or null</param>
        /// <param name="error">>error encountered during retrieval</param>
        /// <param name="attribute1Type">type of the first attribute</param>
        /// <param name="attribute1Value">value of the first attribute</param>
        /// <param name="attribute2Type">type of the second attribute</param>
        /// <param name="attribute2Value">value of the second attribute</param>
        /// <param name="end">null parameter to indicate end of attributes</param>
        /// <returns>the retrieved secret</returns>
        [DllImport("libsecret-1.so.0", CallingConvention = CallingConvention.StdCall)]
        public static extern string secret_password_lookup_sync(IntPtr schema, IntPtr cancellable, out IntPtr error, string attribute1Type, string attribute1Value, string attribute2Type, string attribute2Value, IntPtr end);

        /// <summary>
        /// clears a secret from the secret store using the keyring
        /// </summary>
        /// <param name="schema">schema for the secret</param>
        /// <param name="cancellable">optional GCancellable object or null</param>
        /// <param name="error">>error encountered during clearing</param>
        /// <param name="attribute1Type">type of the first attribute</param>
        /// <param name="attribute1Value">value of the first attribute</param>
        /// <param name="attribute2Type">type of the second attribute</param>
        /// <param name="attribute2Value">value of the second attribute</param>
        /// <param name="end">null parameter to indicate end of attributes</param>
        /// <returns>the retrieved secret</returns>
        [DllImport("libsecret-1.so.0", CallingConvention = CallingConvention.StdCall)]
        public static extern int secret_password_clear_sync(IntPtr schema, IntPtr cancellable, out IntPtr error, string attribute1Type, string attribute1Value, string attribute2Type, string attribute2Value, IntPtr end);

#pragma warning restore SA1300 // suppressing warning for lowercase function name
    }
}
