// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Extensions.Msal
{
/// <summary>
/// Error returned by libsecret library if saving or retrieving fails
/// https://developer.gnome.org/glib/stable/glib-Error-Reporting.html
/// </summary>
internal struct GError
    {
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable CS0649 // Never assigned to (is marshalled)
        /// <summary>
        /// error domain
        /// </summary>
        public uint Domain;

        /// <summary>
        /// error code
        /// </summary>
        public int Code;

        /// <summary>
        /// detailed error message
        /// </summary>
        public string Message;
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore CS0649 // Never assigned to
    }
}
